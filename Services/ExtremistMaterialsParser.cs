using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MonitoringServiceCore.Database.ExtremistMaterials;
using System.Text;
using System.Text.RegularExpressions;

namespace MonitoringServiceCore.Services
{
    public class ExtremistMaterialsParser
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ExtremistMaterialsParser> _logger;
        private readonly string _folderPath;
        private readonly string _filePath;
        private const string FileName = "exportfsm.docx";
        private const string SourceUrl = "https://minjust.gov.ru/uploaded/files/exportfsm.docx";

        // Простое регулярное выражение для поиска текста в двойных кавычках
        private static readonly Regex QuotedTextRegex = new Regex(
            @"""([^""]+)""",
            RegexOptions.Compiled | RegexOptions.Multiline
        );

        // Регулярное выражение для поиска номера записи
        private static readonly Regex NumberRegex = new Regex(
            @"^(\d+)\.",
            RegexOptions.Compiled | RegexOptions.Multiline
        );

        // Регулярное выражение для поиска даты
        private static readonly Regex DateRegex = new Regex(
            @"\b(\d{2}\.\d{2}\.\d{4})\b",
            RegexOptions.Compiled
        );

        public ExtremistMaterialsParser(IWebHostEnvironment environment, ILogger<ExtremistMaterialsParser> logger)
        {
            _environment = environment;
            _logger = logger;
            _folderPath = Path.Combine(_environment.WebRootPath, "EtremistMaterial");
            _filePath = Path.Combine(_folderPath, FileName);
        }

        public async Task<bool> DownloadFileAsync()
        {
            try
            {
                if (!Directory.Exists(_folderPath))
                {
                    Directory.CreateDirectory(_folderPath);
                    _logger.LogInformation("Создана папка: {Folder}", _folderPath);
                }

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                httpClient.Timeout = TimeSpan.FromMinutes(5);

                _logger.LogInformation("Начинаем загрузку файла с {Url}", SourceUrl);
                var response = await httpClient.GetAsync(SourceUrl);
                response.EnsureSuccessStatusCode();

                using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);

                _logger.LogInformation("Файл успешно загружен: {Path} (Размер: {Size} байт)", _filePath, new FileInfo(_filePath).Length);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке файла");
                return false;
            }
        }

        public bool FileExists()
        {
            return File.Exists(_filePath);
        }

        public long GetFileSize()
        {
            return File.Exists(_filePath) ? new FileInfo(_filePath).Length : 0;
        }

        public string GetFilePath()
        {
            return _filePath;
        }

        /// <summary>
        /// Парсинг файла - извлекает текст в кавычках
        /// </summary>
        public List<ExtremistMaterial> ParseMaterialsFromDocx()
        {
            var materials = new List<ExtremistMaterial>();

            if (!File.Exists(_filePath))
            {
                _logger.LogWarning("Файл не найден: {Path}", _filePath);
                return materials;
            }

            try
            {
                _logger.LogInformation("Начинаем парсинг файла: {Path}", _filePath);

                var fullText = ExtractTextFromDocx();

                if (string.IsNullOrEmpty(fullText))
                {
                    _logger.LogWarning("Не удалось извлечь текст из DOCX файла");
                    return materials;
                }

                _logger.LogInformation("Извлечено текста: {Length} символов", fullText.Length);

                // Разбиваем на строки
                var lines = fullText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                int currentNumber = 0;
                string currentLine = "";
                List<string> currentQuotedTexts = new List<string>();
                DateTime? currentDate = null;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine)) continue;

                    // Проверяем, начинается ли строка с номера
                    var numberMatch = NumberRegex.Match(trimmedLine);
                    if (numberMatch.Success)
                    {
                        // Сохраняем предыдущую запись
                        if (currentNumber > 0 && currentQuotedTexts.Any())
                        {
                            var material = CreateMaterial(currentNumber, currentLine, currentQuotedTexts, currentDate);
                            materials.Add(material);
                            _logger.LogDebug("Добавлен материал #{Number}, найдено текстов: {Count}", currentNumber, currentQuotedTexts.Count);
                        }

                        // Начинаем новую запись
                        currentNumber = int.Parse(numberMatch.Groups[1].Value);
                        currentLine = trimmedLine;
                        currentQuotedTexts.Clear();
                        currentDate = null;

                        // Ищем текст в кавычках в этой строке
                        ExtractQuotedTextFromLine(trimmedLine, currentQuotedTexts);

                        // Ищем дату
                        currentDate = ExtractDateFromLine(trimmedLine);
                    }
                    else
                    {
                        // Продолжение предыдущей записи
                        if (currentNumber > 0)
                        {
                            currentLine += " " + trimmedLine;

                            // Ищем текст в кавычках в продолжении
                            ExtractQuotedTextFromLine(trimmedLine, currentQuotedTexts);

                            // Ищем дату в продолжении
                            if (!currentDate.HasValue)
                            {
                                currentDate = ExtractDateFromLine(trimmedLine);
                            }
                        }
                    }
                }

                // Добавляем последнюю запись
                if (currentNumber > 0 && currentQuotedTexts.Any())
                {
                    var material = CreateMaterial(currentNumber, currentLine, currentQuotedTexts, currentDate);
                    materials.Add(material);
                }

                _logger.LogInformation("Из DOCX извлечено {Count} материалов", materials.Count);

                // Выводим первые 5 материалов для проверки
                foreach (var m in materials.Take(5))
                {
                    _logger.LogInformation("Материал #{Number}: Текст в кавычках: {Text}", m.Number, m.Text);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге DOCX файла");
            }

            return materials;
        }

        private ExtremistMaterial CreateMaterial(int number, string line, List<string> quotedTexts, DateTime? date)
        {
            var utcNow = DateTime.UtcNow; 

            return new ExtremistMaterial
            {
                Id = Guid.NewGuid(),
                Number = number,
                Text = string.Join(", ", quotedTexts.Distinct()),
                Description = line.Length > 500 ? line.Substring(0, 500) + "..." : line,
                DecisionDate = date.HasValue ? DateTime.SpecifyKind(date.Value, DateTimeKind.Utc) : (DateTime?)null,
                RawText = line.Length > 1000 ? line.Substring(0, 1000) + "..." : line,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };
        }

        private void ExtractQuotedTextFromLine(string line, List<string> quotedTexts)
        {
            var quotedMatches = QuotedTextRegex.Matches(line);
            foreach (Match match in quotedMatches)
            {
                string quoted = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(quoted) && quoted.Length > 3)
                {
                    quotedTexts.Add(quoted);
                }
            }
        }

        private DateTime? ExtractDateFromLine(string line)
        {
            var dateMatch = DateRegex.Match(line);
            if (dateMatch.Success)
            {
                if (DateTime.TryParseExact(dateMatch.Groups[1].Value, "dd.MM.yyyy",
                    null, System.Globalization.DateTimeStyles.None, out var date))
                {
                    return date;
                }
            }
            return null;
        }

        private string ExtractTextFromDocx()
        {
            var textBuilder = new StringBuilder();

            try
            {
                using (var wordDocument = WordprocessingDocument.Open(_filePath, false))
                {
                    var body = wordDocument.MainDocumentPart?.Document.Body;
                    if (body == null) return string.Empty;

                    // Рекурсивно извлекаем текст из всех элементов
                    ExtractTextFromElement(body, textBuilder);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при извлечении текста из DOCX");
            }

            return textBuilder.ToString();
        }

        /// <summary>
        /// Рекурсивно извлекает текст из элемента OpenXml
        /// </summary>
        private void ExtractTextFromElement(OpenXmlElement element, StringBuilder textBuilder)
        {
            foreach (var child in element.Elements())
            {
                // Если это параграф, обрабатываем его
                if (child is Paragraph paragraph)
                {
                    var paragraphText = new StringBuilder();

                    foreach (var run in paragraph.Elements<Run>())
                    {
                        foreach (var text in run.Elements<Text>())
                        {
                            if (!string.IsNullOrEmpty(text.Text))
                            {
                                paragraphText.Append(text.Text);
                            }
                        }
                    }

                    // Проверяем гиперссылки
                    foreach (var hyperlink in paragraph.Elements<Hyperlink>())
                    {
                        foreach (var run in hyperlink.Elements<Run>())
                        {
                            foreach (var text in run.Elements<Text>())
                            {
                                if (!string.IsNullOrEmpty(text.Text))
                                {
                                    paragraphText.Append(text.Text);
                                }
                            }
                        }
                    }

                    if (paragraphText.Length > 0)
                    {
                        textBuilder.AppendLine(paragraphText.ToString());
                    }
                }
                // Если это таблица, обрабатываем её ячейки
                else if (child is Table table)
                {
                    foreach (var row in table.Elements<TableRow>())
                    {
                        foreach (var cell in row.Elements<TableCell>())
                        {
                            // Рекурсивно извлекаем текст из ячейки
                            ExtractTextFromElement(cell, textBuilder);
                        }
                    }
                }
                // Если это другой контейнер, рекурсивно обрабатываем его
                else if (child is OpenXmlCompositeElement composite)
                {
                    ExtractTextFromElement(composite, textBuilder);
                }
                // Обработка обычного текста
                else if (child is Text text)
                {
                    if (!string.IsNullOrEmpty(text.Text))
                    {
                        textBuilder.Append(text.Text);
                    }
                }
                // Обработка Break (перенос строки)
                else if (child is Break)
                {
                    textBuilder.AppendLine();
                }
            }
        }
       
    }
}