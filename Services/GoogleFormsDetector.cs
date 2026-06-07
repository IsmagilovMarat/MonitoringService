using MonitoringServiceCore.Database;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.GoogleForms;
using MonitoringServiceCore.Database.SiteAnalysisNamespace;
using System.Text.RegularExpressions;

namespace MonitoringServiceCore.Services
{
    public class GoogleFormsDetector
    {
        private readonly SiteDataDownloader _siteDataDownloader;
        private readonly MonitoringDbContext _dbContext;

        // ИСПРАВЛЕННЫЕ регулярные выражения - более точные и надежные
        private readonly List<Regex> _googleFormsPatterns = new List<Regex>
        {
            new Regex(@"https?://(?:docs\.google\.com/forms/d/e/|forms\.gle/)[a-zA-Z0-9_-]+", RegexOptions.IgnoreCase),
            new Regex(@"https?://(?:www\.)?google\.com/forms/about/", RegexOptions.IgnoreCase),
            new Regex(@"<iframe[^>]*src=[""'](https?://(?:docs\.google\.com/forms/d/e/|forms\.gle/)[^""']+)[""'][^>]*>", RegexOptions.IgnoreCase),
            new Regex(@"https?://(?:docs\.google\.com/forms)", RegexOptions.IgnoreCase),
            new Regex(@"google\.com/forms/d/e/[a-zA-Z0-9_-]+/viewform", RegexOptions.IgnoreCase),
            new Regex(@"data-forms-embed", RegexOptions.IgnoreCase),
            new Regex(@"google-form-embed", RegexOptions.IgnoreCase)
        };

        // Только уникальные и точные индикаторы
        private readonly List<string> _googleFormsIndicators = new List<string>
        {
            "/forms/d/e/",           // Уникальный паттерн Google Forms
            "viewform",              // Ключевое слово для просмотра формы
            "formResponse",          // Конечная точка отправки
            "data-forms-embed",      // Специфичный атрибут Google Forms
            "google-form-embed"      // Дополнительный атрибут
        };

        public GoogleFormsDetector(
            SiteDataDownloader siteDataDownloader,
            MonitoringDbContext dbContext)
        {
            _siteDataDownloader = siteDataDownloader ?? throw new ArgumentNullException(nameof(siteDataDownloader));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<GoogleFormsDetectionResult> DetectGoogleFormsAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL не может быть пустым", nameof(url));

            var result = new GoogleFormsDetectionResult
            {
                Url = url,
                DetectionTime = DateTime.UtcNow
            };

            try
            {
                string htmlContent = await _siteDataDownloader.DownloadHtmlAsync(url);
                result.HtmlLoaded = true;
                result.HtmlLength = htmlContent.Length;

                // Проверка наличия Google Forms
                result.HasGoogleForms = DetectGoogleFormsInHtml(htmlContent, out var detectionMethod);

                if (result.HasGoogleForms)
                {
                    result.FormUrls = ExtractValidGoogleFormUrls(htmlContent);
                    result.FormTypes = DetermineFormTypes(htmlContent);
                    result.IsPotentiallyMalicious = CheckForMaliciousForms(htmlContent);
                    result.FormDetails = ExtractFormDetails(htmlContent);

                    // Дополнительная проверка: если URL не найдены, возможно ложное срабатывание
                    if (result.FormUrls.Count == 0 && result.HasGoogleForms)
                    {
                        result.HasGoogleForms = false;
                    }
                }

                result.SecurityAnalysis = AnalyzePageSecurity(htmlContent);
                await SaveDetectionResultToDatabaseAsync(url, result);
            }
            catch (HttpRequestException ex)
            {
                result.ErrorMessage = $"Ошибка загрузки страницы: {ex.Message}";
                result.HasGoogleForms = false;
                result.HtmlLoaded = false;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Неожиданная ошибка: {ex.Message}";
                result.HasGoogleForms = false;
            }

            return result;
        }

        private bool DetectGoogleFormsInHtml(string html, out string detectionMethod)
        {
            detectionMethod = "none";

            if (string.IsNullOrEmpty(html))
                return false;

            // 1. Проверка по точным URL паттернам
            foreach (var pattern in _googleFormsPatterns)
            {
                if (pattern.IsMatch(html))
                {
                    detectionMethod = "url_pattern";
                    return true;
                }
            }

            // 2. Проверка на наличие формы в iframe с специфичными атрибутами
            if (html.Contains("forms.google.com") &&
                (html.Contains("<iframe") || html.Contains("viewform")))
            {
                detectionMethod = "iframe_detection";
                return true;
            }

            // 3. Проверка на специфичные Google Forms атрибуты
            if (html.Contains("data-forms-embed") ||
                html.Contains("google-form-embed") ||
                (html.Contains("google.com/forms") && html.Contains("entry.")))
            {
                detectionMethod = "attribute_detection";
                return true;
            }

            // 4. Проверка индикаторов только в определенном контексте
            string htmlLower = html.ToLower();
            foreach (var indicator in _googleFormsIndicators)
            {
                if (htmlLower.Contains(indicator))
                {
                    if (IsLikelyActualForm(html, indicator))
                    {
                        detectionMethod = "indicator_context";
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsLikelyActualForm(string html, string indicator)
        {
            int indicatorIndex = html.ToLower().IndexOf(indicator);
            if (indicatorIndex == -1) return false;

            int start = Math.Max(0, indicatorIndex - 500);
            int end = Math.Min(html.Length, indicatorIndex + 500);
            string context = html.Substring(start, end - start);

            return context.Contains("<form") ||
                   context.Contains("<iframe") ||
                   context.Contains("method=\"post\"") ||
                   context.Contains("action=\"") ||
                   context.Contains("entry.");
        }

        private List<string> ExtractValidGoogleFormUrls(string html)
        {
            var urls = new List<string>();
            var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pattern in _googleFormsPatterns)
            {
                var matches = pattern.Matches(html);
                foreach (Match match in matches)
                {
                    string url = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;

                    if (!seenUrls.Contains(url) && IsValidGoogleFormUrl(url))
                    {
                        // Нормализация URL
                        url = NormalizeGoogleFormUrl(url);

                        if (!seenUrls.Contains(url))
                        {
                            urls.Add(url);
                            seenUrls.Add(url);
                        }
                    }
                }
            }

            return urls.Distinct().ToList();
        }

        private string NormalizeGoogleFormUrl(string url)
        {
            // Добавляем viewform если его нет
            if (url.Contains("/forms/d/e/") &&
                !url.Contains("/viewform") &&
                !url.Contains("/formResponse"))
            {
                url = url.TrimEnd('/') + "/viewform";
            }

            // Удаляем якоря
            int anchorIndex = url.IndexOf('#');
            if (anchorIndex > 0)
                url = url.Substring(0, anchorIndex);

            return url;
        }

        private bool IsValidGoogleFormUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url)) return false;

                var uri = new Uri(url);
                return uri.Host.Contains("google.com") ||
                       uri.Host.Contains("forms.gle") ||
                       uri.Host.Contains("docs.google.com");
            }
            catch
            {
                return false;
            }
        }

        private List<string> DetermineFormTypes(string html)
        {
            var types = new List<string>();
            string lowerHtml = html.ToLower();

            if (lowerHtml.Contains("/viewform"))
                types.Add("Стандартная форма");

            if (lowerHtml.Contains("/formresponse"))
                types.Add("Ajax форма");

            if (lowerHtml.Contains("embedded=true") ||
                (lowerHtml.Contains("<iframe") && lowerHtml.Contains("google.com/forms")))
                types.Add("Встроенная форма (iframe)");

            if (lowerHtml.Contains("?usp=pp_url") || lowerHtml.Contains("prefill"))
                types.Add("Форма с предзаполнением");

            if (lowerHtml.Contains("/template/"))
                types.Add("Шаблон Google Forms");

            if (lowerHtml.Contains("entry.") && lowerHtml.Contains("google.com/forms"))
                types.Add("Активная форма с полями ввода");

            return types.Distinct().ToList();
        }

        private bool CheckForMaliciousForms(string html)
        {
            string lowerHtml = html.ToLower();

            bool hasPhishingIndicators = (lowerHtml.Contains("verify your account") ||
                                         lowerHtml.Contains("confirm your identity") ||
                                         lowerHtml.Contains("update your information") ||
                                         lowerHtml.Contains("unusual activity") ||
                                         lowerHtml.Contains("suspicious activity") ||
                                         lowerHtml.Contains("подтвердите") ||
                                         lowerHtml.Contains("верификация")) &&
                                         lowerHtml.Contains("google.com/forms");

            bool requestsSensitiveData = (lowerHtml.Contains("password") ||
                                         lowerHtml.Contains("пароль") ||
                                         lowerHtml.Contains("credit card") ||
                                         lowerHtml.Contains("банковская карта") ||
                                         lowerHtml.Contains("ssn") ||
                                         lowerHtml.Contains("passport")) &&
                                         lowerHtml.Contains("entry.");

            bool urgentLanguage = (lowerHtml.Contains("immediately") ||
                                  lowerHtml.Contains("urgent") ||
                                  lowerHtml.Contains("as soon as possible") ||
                                  lowerHtml.Contains("срочно") ||
                                  lowerHtml.Contains("немедленно")) &&
                                  (lowerHtml.Contains("verify") || lowerHtml.Contains("confirm"));

            return hasPhishingIndicators || requestsSensitiveData || urgentLanguage;
        }

        private List<FormDetail> ExtractFormDetails(string html)
        {
            var formDetails = new List<FormDetail>();
            var formMatches = Regex.Matches(html, @"<form[^>]*>([\s\S]*?)</form>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            for (int i = 0; i < formMatches.Count; i++)
            {
                var match = formMatches[i];
                string formContent = match.Value;

                string action = Regex.Match(formContent, @"action=[""']([^""']+)[""']", RegexOptions.IgnoreCase).Groups[1].Value;
                string method = Regex.Match(formContent, @"method=[""']([^""']+)[""']", RegexOptions.IgnoreCase).Groups[1].Value;
                string id = Regex.Match(formContent, @"id=[""']([^""']+)[""']", RegexOptions.IgnoreCase).Groups[1].Value;
                string name = Regex.Match(formContent, @"name=[""']([^""']+)[""']", RegexOptions.IgnoreCase).Groups[1].Value;

                bool isGoogleForm = action.Contains("google.com/forms") ||
                                   action.Contains("docs.google.com/forms") ||
                                   action.Contains("forms.gle") ||
                                   formContent.Contains("google forms", StringComparison.OrdinalIgnoreCase) ||
                                   formContent.Contains("/forms/d/e/");

                var detail = new FormDetail
                {
                    Index = i + 1,
                    Id = id,
                    Name = name,
                    Action = string.IsNullOrEmpty(action) ? "Не указан" : action,
                    Method = string.IsNullOrEmpty(method) ? "get" : method.ToLower(),
                    InputFieldsCount = Regex.Matches(formContent, @"<input", RegexOptions.IgnoreCase).Count,
                    HasSubmitButton = Regex.IsMatch(formContent, @"<button[^>]*type=[""']submit[""']|<input[^>]*type=[""']submit[""']", RegexOptions.IgnoreCase),
                    IsGoogleForm = isGoogleForm
                };

                formDetails.Add(detail);
            }

            return formDetails;
        }

        private SecurityAnalysis AnalyzePageSecurity(string html)
        {
            var analysis = new SecurityAnalysis
            {
                HasHttps = html.Contains("https://")
            };

            analysis.HasCSP = html.Contains("Content-Security-Policy") ||
                             html.Contains("http-equiv=\"Content-Security-Policy\"");

            var externalScripts = Regex.Matches(html, @"<script[^>]*src=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            analysis.ExternalScriptsCount = externalScripts.Count;

            analysis.HasMixedContent = Regex.IsMatch(html, @"src=[""']http://[^""']+[""']", RegexOptions.IgnoreCase);

            if (analysis.HasHttps && analysis.HasCSP && !analysis.HasMixedContent)
                analysis.SecurityLevel = "Высокий";
            else if (analysis.HasHttps && !analysis.HasMixedContent)
                analysis.SecurityLevel = "Средний";
            else
                analysis.SecurityLevel = "Низкий";

            return analysis;
        }

        private async Task SaveDetectionResultToDatabaseAsync(string url, GoogleFormsDetectionResult result)
        {
            try
            {
                var uri = new Uri(url);
                var domain = uri.Host;

                var siteAnalysis = new SiteAnalysis
                {
                    Id = Guid.NewGuid(),
                    Url = url,
                    DomainUrl = domain,
                    AnalyzedDate = DateTime.UtcNow,
                    HasGoogleForms = result.HasGoogleForms,
                    GoogleFormsFound = result.FormUrls ?? new List<string>(),
                    CountOfViolations = result.IsPotentiallyMalicious ? 1 : 0,
                    OverallScore = CalculateOverallScore(result)
                };

                _dbContext.SiteAnalyses.Add(siteAnalysis);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения в БД: {ex.Message}");
            }
        }

        private int CalculateOverallScore(GoogleFormsDetectionResult result)
        {
            int score = 100;

            if (result.HasGoogleForms)
                score -= 30;

            if (result.IsPotentiallyMalicious)
                score -= 40;

            if (result.SecurityAnalysis?.SecurityLevel == "Низкий")
                score -= 20;
            else if (result.SecurityAnalysis?.SecurityLevel == "Средний")
                score -= 10;

            return Math.Max(0, score);
        }

        public async Task<List<GoogleFormsDetectionResult>> DetectGoogleFormsBatchAsync(List<string> urls)
        {
            var results = new List<GoogleFormsDetectionResult>();

            foreach (var url in urls)
            {
                var result = await DetectGoogleFormsAsync(url);
                results.Add(result);
                await Task.Delay(100);
            }

            return results;
        }

        public async Task<DomainStatistics> GetDomainStatisticsAsync(string domain)
        {
            var stats = new DomainStatistics
            {
                Domain = domain,
                TotalPagesChecked = 0,
                PagesWithGoogleForms = 0,
                TotalFormsFound = 0
            };

            try
            {
                var siteAnalyses = _dbContext.SiteAnalyses
                    .Where(s => s.DomainUrl == domain)
                    .ToList();

                stats.TotalPagesChecked = siteAnalyses.Count;
                stats.PagesWithGoogleForms = siteAnalyses.Count(s => s.HasGoogleForms);
                stats.TotalFormsFound = siteAnalyses
                    .Where(s => s.GoogleFormsFound != null)
                    .Sum(s => s.GoogleFormsFound?.Count ?? 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения статистики: {ex.Message}");
            }

            return stats;
        }
    }

    // Класс SurroundingContentAnalysis УДАЛЕН как неиспользуемый

    public class SecurityAnalysis
    {
        public bool HasHttps { get; set; }
        public bool HasCSP { get; set; }
        public int ExternalScriptsCount { get; set; }
        public bool HasMixedContent { get; set; }
        public string SecurityLevel { get; set; } = "Не определен";
    }

    public class FormDetail
    {
        public int Index { get; set; }
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public int InputFieldsCount { get; set; }
        public bool HasSubmitButton { get; set; }
        public bool IsGoogleForm { get; set; }
    }

    public class DomainStatistics
    {
        public string Domain { get; set; } = string.Empty;
        public int TotalPagesChecked { get; set; }
        public int PagesWithGoogleForms { get; set; }
        public int TotalFormsFound { get; set; }
        public double Percentage => TotalPagesChecked > 0
            ? (double)PagesWithGoogleForms / TotalPagesChecked * 100
            : 0;
    }
}