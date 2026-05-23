using HtmlAgilityPack;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.ExtremistMaterialPackage;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace MonitoringServiceCore.Services
{
    public class ExtremistMaterialsParser
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ExtremistMaterialsParser> _logger;
        private readonly MonitoringDbContext _dbContext;

        // Базовый URL для страниц со списком
        private const string BaseUrl = "https://minjust.gov.ru/ru/extremist-materials/";

        public ExtremistMaterialsParser(
            IHttpClientFactory httpClientFactory,
            ILogger<ExtremistMaterialsParser> logger,
            MonitoringDbContext dbContext)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Запускает полный сбор всех материалов со всех страниц
        /// </summary>
        public async Task<ParseResult> ParseAllPagesAsync(int startPage = 1, int endPage = 55)
        {
            var result = new ParseResult
            {
                StartTime = DateTime.Now,
                TotalPagesProcessed = 0,
                TotalMaterialsFound = 0
            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            for (int page = startPage; page <= endPage; page++)
            {
                _logger.LogInformation("Начинается обработка страницы {Page}", page);

                var pageResult = await ParseSinglePageAsync(client, page);
                result.PagesResults.Add(pageResult);
                result.TotalMaterialsFound += pageResult.MaterialsCount;
                result.TotalPagesProcessed++;

                if (pageResult.Materials.Any())
                {
                    // Сохраняем материалы в БД
                    foreach (var material in pageResult.Materials)
                    {
                        if (!_dbContext.ExtremistMaterials.Any(m => m.Number == material.Number))
                        {
                            _dbContext.ExtremistMaterials.Add(material);
                        }
                    }
                    await _dbContext.SaveChangesAsync();
                }

                // Задержка между запросами, чтобы не нагружать сервер
                await Task.Delay(500);
            }

            result.EndTime = DateTime.Now;
            result.Duration = result.EndTime - result.StartTime;

            _logger.LogInformation(
                "Парсинг завершён. Обработано страниц: {Pages}, найдено материалов: {Materials}",
                result.TotalPagesProcessed, result.TotalMaterialsFound);

            return result;
        }

        /// <summary>
        /// Парсит одну страницу и возвращает список материалов
        /// </summary>
        private async Task<PageParseResult> ParseSinglePageAsync(HttpClient client, int pageNumber)
        {
            var result = new PageParseResult
            {
                PageNumber = pageNumber,
                Materials = new List<ExtremistMaterial>(),
                Success = false
            };

            try
            {
                string url = pageNumber == 1
                    ? BaseUrl
                    : $"{BaseUrl}?page={pageNumber}";

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Страница {Page} вернула код {StatusCode}",
                        pageNumber, response.StatusCode);
                    result.ErrorMessage = $"HTTP {response.StatusCode}";
                    return result;
                }

                var html = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Находим таблицу с материалами
                var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'table-bordered')]");
                if (table == null)
                {
                    _logger.LogWarning("Таблица не найдена на странице {Page}", pageNumber);
                    result.ErrorMessage = "Таблица не найдена";
                    return result;
                }

                // Получаем все строки таблицы, пропуская заголовок
                var rows = table.SelectNodes(".//tr")?.Skip(1).ToList();
                if (rows == null || !rows.Any())
                {
                    result.ErrorMessage = "В таблице нет данных";
                    return result;
                }

                foreach (var row in rows)
                {
                    var cells = row.SelectNodes(".//td");
                    if (cells == null || cells.Count < 3) continue;

                    var material = new ExtremistMaterial
                    {
                        Id = Guid.NewGuid(),
                        PageNumber = pageNumber,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Номер материала
                    if (int.TryParse(cells[0].InnerText.Trim(), out int number))
                    {
                        material.Number = number;
                    }

                    // Текст описания материала
                    material.Description = HtmlEntity.DeEntitize(cells[1].InnerText.Trim());
                    material.RawText = material.Description;

                    // Дата решения суда (обычно в конце описания или в отдельной ячейке)
                    material.DecisionDate = ExtractDecisionDate(cells[2]?.InnerText ?? material.Description);

                    result.Materials.Add(material);
                }

                result.MaterialsCount = result.Materials.Count;
                result.Success = true;
                _logger.LogInformation("Страница {Page}: найдено {Count} материалов",
                    pageNumber, result.MaterialsCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге страницы {Page}", pageNumber);
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Извлекает дату из текста (формат: дд.мм.гггг)
        /// </summary>
        private DateTime? ExtractDecisionDate(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            // Ищем дату в формате дд.мм.гггг
            var match = Regex.Match(text, @"\b(\d{2})\.(\d{2})\.(\d{4})\b");
            if (match.Success && DateTime.TryParseExact(match.Value, "dd.MM.yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }

            return null;
        }
    }

    // Результат парсинга одной страницы
    public class PageParseResult
    {
        public int PageNumber { get; set; }
        public List<ExtremistMaterial> Materials { get; set; } = new();
        public int MaterialsCount { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    // Общий результат парсинга
    public class ParseResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalPagesProcessed { get; set; }
        public int TotalMaterialsFound { get; set; }
        public List<PageParseResult> PagesResults { get; set; } = new();
    }
}
