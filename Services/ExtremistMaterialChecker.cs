using Microsoft.EntityFrameworkCore;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.ExtremistMaterials;
using System.Text;
using System.Text.RegularExpressions;

namespace MonitoringServiceCore.Services
{
    public class ExtremistMaterialChecker
    {
        private readonly MonitoringDbContext _dbContext;
        private readonly ILogger<ExtremistMaterialChecker> _logger;

        public ExtremistMaterialChecker(MonitoringDbContext dbContext, ILogger<ExtremistMaterialChecker> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Проверяет HTML-контент на наличие экстремистских материалов из столбца Text
        /// </summary>
        public async Task<ExtremistCheckResult> CheckContentAsync(string htmlContent, string url)
        {
            var result = new ExtremistCheckResult
            {
                Url = url,
                CheckTime = DateTime.UtcNow,
                FoundMaterials = new List<FoundMaterial>()
            };

            try
            {
                var materials = await _dbContext.ExtremistMaterials.ToListAsync();
                if (!materials.Any())
                {
                    result.ErrorMessage = "Список экстремистских материалов не загружен";
                    _logger.LogWarning("Список экстремистских материалов пуст");
                    return result;
                }

                _logger.LogInformation("Начинаем проверку контента на наличие {Count} экстремистских материалов", materials.Count);

                var cleanText = StripHtml(htmlContent);
                var lowerCleanText = cleanText.ToLowerInvariant();
                var lowerHtml = htmlContent.ToLowerInvariant();

                foreach (var material in materials)
                {
                    // Проверяем текст из столбца Text (слова в кавычках через запятую)
                    if (!string.IsNullOrEmpty(material.Text))
                    {
                        var foundMaterial = CheckMaterialText(material, lowerCleanText, lowerHtml);
                        if (foundMaterial != null)
                        {
                            result.FoundMaterials.Add(foundMaterial);
                        }
                    }
                }

                result.HasExtremistMaterials = result.FoundMaterials.Any();

                _logger.LogInformation("Проверка завершена. Найдено материалов: {Count}", result.FoundMaterials.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке экстремистских материалов для URL {Url}", url);
                result.ErrorMessage = $"Ошибка при проверке: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Проверяет наличие текста из материала в проверяемом контенте
        /// </summary>
        private FoundMaterial? CheckMaterialText(ExtremistMaterial material, string cleanText, string htmlText)
        {
            if (string.IsNullOrEmpty(material.Text)) return null;

            // Разбиваем текст по запятым (могут быть несколько фраз через запятую)
            var textParts = material.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in textParts)
            {
                var trimmedPart = part.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(trimmedPart) || trimmedPart.Length < 3) continue;

                // Ищем точное совпадение фразы
                if (cleanText.Contains(trimmedPart) || htmlText.Contains(trimmedPart))
                {
                    var foundMaterial = new FoundMaterial
                    {
                        Number = material.Number,
                        Description = material.Description,
                        MatchedKeyword = trimmedPart,
                        DecisionDate = material.DecisionDate,
                        MatchType = "Совпадение по тексту"
                    };

                    // Находим контекст вокруг найденного слова
                    foundMaterial.Context = GetContextAroundKeyword(cleanText, trimmedPart);

                    return foundMaterial;
                }
            }

            return null;
        }

        /// <summary>
        /// Получает контекст вокруг найденного ключевого слова
        /// </summary>
        private string GetContextAroundKeyword(string text, string keyword, int contextLength = 150)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword)) return string.Empty;

            var index = text.ToLowerInvariant().IndexOf(keyword);
            if (index < 0) return string.Empty;

            var start = Math.Max(0, index - contextLength);
            var length = Math.Min(text.Length - start, contextLength * 2);
            var context = text.Substring(start, length);

            // Добавляем многоточие, если контекст обрезан
            if (start > 0) context = "..." + context;
            if (start + length < text.Length) context = context + "...";

            return context;
        }

        /// <summary>
        /// Удаляет HTML-теги из строки
        /// </summary>
        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            // Удаляем скрипты и стили
            var result = Regex.Replace(html, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            result = Regex.Replace(result, @"<style[^>]*>.*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Удаляем HTML теги
            result = Regex.Replace(result, @"<[^>]+>", " ");

            // Декодируем HTML сущности
            result = System.Net.WebUtility.HtmlDecode(result);

            // Заменяем множественные пробелы на один
            result = Regex.Replace(result, @"\s+", " ");

            return result.Trim();
        }

        /// <summary>
        /// Проверяет текст с дополнительным контекстом
        /// </summary>
        public async Task<ExtremistCheckResult> CheckContentWithContextAsync(string htmlContent, string url)
        {
            return await CheckContentAsync(htmlContent, url);
        }
    }
}

  