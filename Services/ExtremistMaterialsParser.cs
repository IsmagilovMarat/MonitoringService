using MonitoringServiceCore.Database.ExtremistMaterialPackage;

namespace MonitoringServiceCore.Services
{
    public class ExtremistMaterialsParser
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ExtremistMaterialsParser> _logger;
        private readonly string _folderPath;
        private const string FileName = "exportfsm.docx";
        private const string SourceUrl = "https://minjust.gov.ru/uploaded/files/exportfsm.docx";

        public ExtremistMaterialsParser(IWebHostEnvironment environment, ILogger<ExtremistMaterialsParser> logger)
        {
            _environment = environment;
            _logger = logger;
            _folderPath = Path.Combine(_environment.WebRootPath, "ExtemistMaterial");
        }

        /// <summary>
        /// Скачивает файл с сайта Минюста и сохраняет в wwwroot
        /// </summary>
        public async Task<bool> DownloadFileAsync()
        {
            try
            {
                if (!Directory.Exists(_folderPath))
                    Directory.CreateDirectory(_folderPath);

                var filePath = Path.Combine(_folderPath, FileName);

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                var response = await httpClient.GetAsync(SourceUrl);
                response.EnsureSuccessStatusCode();

                await using var fs = new FileStream(filePath, FileMode.Create);
                await response.Content.CopyToAsync(fs);

                _logger.LogInformation("Файл успешно загружен: {Path}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке файла");
                return false;
            }
        }

        /// <summary>
        /// Проверяет, существует ли файл и не устарел ли он (старше N часов)
        /// </summary>
        public bool IsFileOutdated(int hours = 24)
        {
            var filePath = Path.Combine(_folderPath, FileName);
            if (!File.Exists(filePath)) return true;
            var lastWrite = File.GetLastWriteTime(filePath);
            return lastWrite < DateTime.Now.AddHours(-hours);
        }

        /// <summary>
        /// Извлекает все записи из DOCX-файла в список объектов ExtremistMaterial
        /// </summary>
        public List<ExtremistMaterial> ParseMaterialsFromDocx()
        {
            var materials = new List<ExtremistMaterial>();
            var filePath = Path.Combine(_folderPath, FileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Файл не найден: {Path}", filePath);
                return materials;
            }

            using var document = DocX.Load(filePath);
            var fullText = document.Text; // Весь текст документа

            // Разделяем текст на строки (каждая запись обычно на новой строке)
            var lines = fullText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // Ищем строки формата: "номер | описание | дата"
                var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)\s*\|\s*(.+?)\s*\|\s*(\d{2}\.\d{2}\.\d{4})");
                if (!match.Success) continue;

                int number = int.Parse(match.Groups[1].Value);
                string description = match.Groups[2].Value.Trim();
                DateTime? decisionDate = null;
                if (DateTime.TryParseExact(match.Groups[3].Value, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
                    decisionDate = date;

                // Извлекаем слова в кавычках из описания
                string quotedText = ExtractQuotedText(description);

                var material = new ExtremistMaterial
                {
                    Id = Guid.NewGuid(),
                    Number = number,
                    Text = quotedText,
                    Description = description,
                    DecisionDate = decisionDate,
                    RawText = line,
                    CreatedAt = DateTime.UtcNow
                };
                materials.Add(material);
            }

            _logger.LogInformation("Из DOCX извлечено {Count} материалов", materials.Count);
            return materials;
        }

        private string ExtractQuotedText(string text)
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(text, @"[""«]([^""»]+)[""»]");
            var quoted = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in matches)
                quoted.Add(m.Groups[1].Value.Trim());
            return string.Join(", ", quoted);
        }
    }
}