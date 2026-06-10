using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.GoogleForms;
using System.Text.RegularExpressions;

namespace MonitoringServiceCore.Services
{
    public class GoogleFormsDetector
    {
        private readonly SiteDataDownloader _siteDataDownloader;
        private readonly MonitoringDbContext _dbContext;
        private readonly ILogger<GoogleFormsDetector> _logger;

        private readonly List<Regex> _googleFormsPatterns = new List<Regex>
        {
            new Regex(@"(?:docs\.google\.com/forms|google\.com/forms|forms\.gle)", RegexOptions.IgnoreCase),
            new Regex(@"https?://(?:docs\.google\.com)/forms/d/e/[a-zA-Z0-9_-]+", RegexOptions.IgnoreCase),
            new Regex(@"https?://forms\.gle/[a-zA-Z0-9_-]+", RegexOptions.IgnoreCase),
            new Regex(@"/forms/d/e/[a-zA-Z0-9_-]+", RegexOptions.IgnoreCase),
        };

        private readonly List<string> _googleFormsIndicators = new List<string>
        {
            "docs.google.com/forms",
            "google.com/forms",
            "forms.gle",
            "/forms/d/e/",
            "viewform",
            "formResponse",
            "entry.",
            "data-forms-embed",
            "google-form-embed"
        };

        // Паттерны для фишинговых ссылок Google
        private readonly List<Regex> _googlePhishingPatterns = new List<Regex>
        {
            new Regex(@"accounts\.google\.com/v3/signin[^>]*docs\.google\.com/forms", RegexOptions.IgnoreCase),
            new Regex(@"accounts\.google\.com.*?forms.*?create", RegexOptions.IgnoreCase),
            new Regex(@"click here.*?accounts\.google\.com", RegexOptions.IgnoreCase)
        };

        public GoogleFormsDetector(
            SiteDataDownloader siteDataDownloader,
            MonitoringDbContext dbContext,
            ILogger<GoogleFormsDetector> logger = null)
        {
            _siteDataDownloader = siteDataDownloader ?? throw new ArgumentNullException(nameof(siteDataDownloader));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger;
        }

        public async Task<GoogleFormsDetectionResult> DetectGoogleFormsFromHtmlAsync(string htmlContent, string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL не может быть пустым", nameof(url));

            var result = new GoogleFormsDetectionResult
            {
                Id = Guid.NewGuid(),
                Url = url,
                DetectionTime = DateTime.UtcNow
            };

            try
            {
                if (string.IsNullOrEmpty(htmlContent))
                {
                    result.ErrorMessage = "HTML контент пуст";
                    result.HasGoogleForms = false;
                    result.HtmlLoaded = false;
                    return result;
                }

                result.HtmlLoaded = true;
                result.HtmlLength = htmlContent.Length;

                // Проверка наличия Google Forms
                result.HasGoogleForms = DetectGoogleFormsInHtml(htmlContent, out var detectionMethod);

                // Проверка на вредоносные формы (включая фишинговые ссылки)
                result.IsPotentiallyMalicious = CheckForMaliciousForms(htmlContent);

                // Дополнительная проверка на фишинговые Google формы
                result.HasGoogleForms = result.HasGoogleForms || DetectGooglePhishingLinks(htmlContent);

                _logger?.LogInformation("Google Forms detection for {Url}: HasGoogleForms={Has}, Method={Method}, IsMalicious={Malicious}",
                    url, result.HasGoogleForms, detectionMethod, result.IsPotentiallyMalicious);

                // Сохраняем результат в БД
                await SaveDetectionResultToDatabaseAsync(result);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error detecting Google Forms for {Url}", url);
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

            string lowerHtml = html.ToLower();

            // 1. Проверка по регулярным выражениям
            foreach (var pattern in _googleFormsPatterns)
            {
                if (pattern.IsMatch(html))
                {
                    detectionMethod = "url_pattern";
                    return true;
                }
            }

            // 2. Проверка индикаторов
            foreach (var indicator in _googleFormsIndicators)
            {
                if (lowerHtml.Contains(indicator))
                {
                    detectionMethod = "indicator";
                    return true;
                }
            }

            // 3. Проверка action атрибутов форм
            var formMatches = Regex.Matches(html, @"<form[^>]*action=[""']([^""']+)[""'][^>]*>", RegexOptions.IgnoreCase);
            foreach (Match match in formMatches)
            {
                string action = match.Groups[1].Value.ToLower();
                if (action.Contains("google.com/forms") || action.Contains("docs.google.com/forms"))
                {
                    detectionMethod = "form_action";
                    return true;
                }
            }

            // 4. Проверка iframe src
            var iframeMatches = Regex.Matches(html, @"<iframe[^>]*src=[""']([^""']+)[""'][^>]*>", RegexOptions.IgnoreCase);
            foreach (Match match in iframeMatches)
            {
                string src = match.Groups[1].Value.ToLower();
                if (src.Contains("google.com/forms") || src.Contains("docs.google.com/forms"))
                {
                    detectionMethod = "iframe_src";
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Проверяет наличие фишинговых ссылок на Google Forms
        /// </summary>
        private bool DetectGooglePhishingLinks(string html)
        {
            if (string.IsNullOrEmpty(html))
                return false;

            string lowerHtml = html.ToLower();

            // Проверка на паттерны фишинга
            foreach (var pattern in _googlePhishingPatterns)
            {
                if (pattern.IsMatch(html))
                {
                    _logger?.LogWarning("Detected Google phishing pattern: {Pattern}", pattern.ToString());
                    return true;
                }
            }

            // Проверка на ссылки вида accounts.google.com с упоминанием форм
            if (lowerHtml.Contains("accounts.google.com") &&
                (lowerHtml.Contains("docs.google.com/forms") ||
                 lowerHtml.Contains("google.com/forms") ||
                 lowerHtml.Contains("create") ||
                 lowerHtml.Contains("wise")))
            {
                // Дополнительная проверка контекста
                var googleAuthLinks = Regex.Matches(html, @"<a[^>]*href=[""'][^""']*accounts\.google\.com[^""']*[""'][^>]*>.*?</a>", RegexOptions.IgnoreCase);

                foreach (Match match in googleAuthLinks)
                {
                    string linkText = match.Value.ToLower();
                    string linkUrl = Regex.Match(match.Value, @"href=[""']([^""']+)[""']", RegexOptions.IgnoreCase).Groups[1].Value;

                    // Если текст ссылки призывает к действию
                    if (linkText.Contains("click here") ||
                        linkText.Contains("нажмите") ||
                        linkText.Contains("verify") ||
                        linkText.Contains("confirm") ||
                        linkText.Contains("подтвердите") ||
                        linkText.Contains("войти") ||
                        linkText.Contains("sign in"))
                    {
                        _logger?.LogWarning("Found suspicious Google auth link: {Url} with text: {Text}", linkUrl, linkText);
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CheckForMaliciousForms(string html)
        {
            string lowerHtml = html.ToLower();

            // Проверка на фишинговые индикаторы
            bool hasPhishingIndicators = (lowerHtml.Contains("verify") || lowerHtml.Contains("confirm") ||
                                         lowerHtml.Contains("подтвердите") || lowerHtml.Contains("верификация")) &&
                                         lowerHtml.Contains("google.com/forms");

            // Проверка на запрос敏感 данных
            bool requestsSensitiveData = (lowerHtml.Contains("password") || lowerHtml.Contains("пароль") ||
                                         lowerHtml.Contains("credit card") || lowerHtml.Contains("банковская карта") ||
                                         lowerHtml.Contains("ssn") || lowerHtml.Contains("passport") ||
                                         lowerHtml.Contains("паспорт")) &&
                                         (lowerHtml.Contains("entry.") || lowerHtml.Contains("google.com/forms"));

            // Проверка на срочные действия
            bool urgentAction = (lowerHtml.Contains("immediately") || lowerHtml.Contains("urgent") ||
                                lowerHtml.Contains("as soon as possible") || lowerHtml.Contains("срочно") ||
                                lowerHtml.Contains("немедленно")) &&
                                (lowerHtml.Contains("verify") || lowerHtml.Contains("confirm") ||
                                 lowerHtml.Contains("click here") || lowerHtml.Contains("нажмите"));

            // Проверка на ссылки авторизации Google
            bool hasGoogleAuth = lowerHtml.Contains("accounts.google.com") &&
                                 lowerHtml.Contains("docs.google.com/forms") &&
                                 (lowerHtml.Contains("signin") || lowerHtml.Contains("login"));

            return hasPhishingIndicators || requestsSensitiveData || urgentAction || hasGoogleAuth;
        }

        private async Task SaveDetectionResultToDatabaseAsync(GoogleFormsDetectionResult result)
        {
            try
            {
                _logger?.LogInformation("Saving Google Forms detection result to DB for {Url}", result.Url);

                await _dbContext.GoogleFormsDetectionResults.AddAsync(result);
                await _dbContext.SaveChangesAsync();

                _logger?.LogInformation("Successfully saved Google Forms result with ID: {Id}, HasGoogleForms: {HasForms}, IsMalicious: {IsMalicious}",
                    result.Id, result.HasGoogleForms, result.IsPotentiallyMalicious);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving Google Forms result to database for {Url}", result.Url);
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}