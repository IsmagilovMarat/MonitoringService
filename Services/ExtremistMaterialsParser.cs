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
        private readonly string _folderPath;
        private readonly string _filePath;
        private const string FileName = "exportfsm.docx";
        private const string SourceUrl = "https://minjust.gov.ru/uploaded/files/exportfsm.docx";

        private static readonly Regex QuotedTextRegex = new Regex(
            @"""([^""]+)""",
            RegexOptions.Compiled | RegexOptions.Multiline
        );

        private static readonly Regex NumberRegex = new Regex(
            @"^(\d+)\.",
            RegexOptions.Compiled | RegexOptions.Multiline
        );

        private static readonly Regex DateRegex = new Regex(
            @"\b(\d{2}\.\d{2}\.\d{4})\b",
            RegexOptions.Compiled
        );

        public ExtremistMaterialsParser(IWebHostEnvironment environment, ILogger<ExtremistMaterialsParser> logger)
        {
            _environment = environment;
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
                }

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                httpClient.Timeout = TimeSpan.FromMinutes(5);

                var response = await httpClient.GetAsync(SourceUrl);
                response.EnsureSuccessStatusCode();

                using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);

                return true;
            }
            catch (Exception ex)
            {
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

        public List<ExtremistMaterial> ParseMaterialsFromDocx()
        {
            var materials = new List<ExtremistMaterial>();

            if (!File.Exists(_filePath))
            {
                return materials;
            }

            try
            {
                var fullText = ExtractTextFromDocx();

                if (string.IsNullOrEmpty(fullText))
                {
                    return materials;
                }

                var lines = fullText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                int currentNumber = 0;
                string currentLine = "";
                List<string> currentQuotedTexts = new List<string>();
                DateTime? currentDate = null;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine)) continue;

                    var numberMatch = NumberRegex.Match(trimmedLine);
                    if (numberMatch.Success)
                    {
                        if (currentNumber > 0 && currentQuotedTexts.Any())
                        {
                            var material = CreateMaterial(currentNumber, currentLine, currentQuotedTexts, currentDate);
                            materials.Add(material);
                        }

                        currentNumber = int.Parse(numberMatch.Groups[1].Value);
                        currentLine = trimmedLine;
                        currentQuotedTexts.Clear();
                        currentDate = null;

                        ExtractQuotedTextFromLine(trimmedLine, currentQuotedTexts);

                        currentDate = ExtractDateFromLine(trimmedLine);
                    }
                    else
                    {
                        if (currentNumber > 0)
                        {
                            currentLine += " " + trimmedLine;

                            ExtractQuotedTextFromLine(trimmedLine, currentQuotedTexts);

                            if (!currentDate.HasValue)
                            {
                                currentDate = ExtractDateFromLine(trimmedLine);
                            }
                        }
                    }
                }

                if (currentNumber > 0 && currentQuotedTexts.Any())
                {
                    var material = CreateMaterial(currentNumber, currentLine, currentQuotedTexts, currentDate);
                    materials.Add(material);
                }
            }
            catch (Exception ex)
            {
                throw ex;
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

                    ExtractTextFromElement(body, textBuilder);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return textBuilder.ToString();
        }
        private void ExtractTextFromElement(OpenXmlElement element, StringBuilder textBuilder)
        {
            foreach (var child in element.Elements())
            {
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
                else if (child is Table table)
                {
                    foreach (var row in table.Elements<TableRow>())
                    {
                        foreach (var cell in row.Elements<TableCell>())
                        {
                            ExtractTextFromElement(cell, textBuilder);
                        }
                    }
                }
                else if (child is OpenXmlCompositeElement composite)
                {
                    ExtractTextFromElement(composite, textBuilder);
                }
                else if (child is Text text)
                {
                    if (!string.IsNullOrEmpty(text.Text))
                    {
                        textBuilder.Append(text.Text);
                    }
                }
                else if (child is Break)
                {
                    textBuilder.AppendLine();
                }
            }
        }
       
    }
}