using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.SiteAnalysisNamespace;
using System.Text.RegularExpressions;
using MonitoringServiceCore.Database.GoogleForms;

namespace MonitoringServiceCore.Services
{
    public class GoogleFormsDetector
    {
        private readonly SiteDataDownloader _siteDataDownloader;
        private readonly NetWordAnalyzer _netWordAnalyzer;
        private readonly MonitoringDbContext _dbContext;

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

        private readonly List<string> _googleFormsIndicators = new List<string>
        {
            "google forms",
            "forms.google",
            "docs.google.com/forms",
            "forms.gle",
            "google-form",
            "gform",
            "entry.",
            "formResponse",
            "viewform"
        };

        public GoogleFormsDetector(
            SiteDataDownloader siteDataDownloader,
            NetWordAnalyzer netWordAnalyzer,
            MonitoringDbContext dbContext)
        {
            _siteDataDownloader = siteDataDownloader ?? throw new ArgumentNullException(nameof(siteDataDownloader));
            _netWordAnalyzer = netWordAnalyzer ?? throw new ArgumentNullException(nameof(netWordAnalyzer));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Основной метод для проверки наличия Google Forms на сайте
        /// </summary>
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

                result.HasGoogleForms = DetectGoogleFormsInHtml(htmlContent);

                if (result.HasGoogleForms)
                {
                    result.FormUrls = ExtractGoogleFormUrls(htmlContent);

                    result.FormTypes = DetermineFormTypes(htmlContent);

                    result.SurroundingContentAnalysis = AnalyzeSurroundingContent(htmlContent);

                    result.IsPotentiallyMalicious = CheckForMaliciousForms(htmlContent);

                    result.FormDetails = ExtractFormDetails(htmlContent);
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

       
        private bool DetectGoogleFormsInHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return false;

            foreach (var pattern in _googleFormsPatterns)
            {
                if (pattern.IsMatch(html))
                    return true;
            }

            string htmlLower = html.ToLower();
            foreach (var indicator in _googleFormsIndicators)
            {
                if (htmlLower.Contains(indicator))
                    return true;
            }

            return false;
        }

        private List<string> ExtractGoogleFormUrls(string html)
        {
            var urls = new List<string>();

            foreach (var pattern in _googleFormsPatterns)
            {
                var matches = pattern.Matches(html);
                foreach (Match match in matches)
                {
                    string url = match.Value;
                    if (!urls.Contains(url))
                    {
                        urls.Add(url);
                    }
                }
            }

            return urls;
        }
        private List<string> DetermineFormTypes(string html)
        {
            var types = new List<string>();

            if (html.Contains("viewform", StringComparison.OrdinalIgnoreCase))
                types.Add("Анкета/опрос");

            if (html.Contains("formResponse", StringComparison.OrdinalIgnoreCase))
                types.Add("Форма отправки данных");

            if (html.Contains("embedded", StringComparison.OrdinalIgnoreCase))
                types.Add("Встроенная форма");

            if (html.Contains("prefill", StringComparison.OrdinalIgnoreCase))
                types.Add("Форма с предзаполнением");

            if (html.Contains("template", StringComparison.OrdinalIgnoreCase))
                types.Add("Шаблон формы");

            return types.Distinct().ToList();
        }

        private SurroundingContentAnalysis AnalyzeSurroundingContent(string html)
        {
            var analysis = new SurroundingContentAnalysis();

            var formMatches = Regex.Matches(html, @"<form[^>]*>([\s\S]*?)</form>", RegexOptions.IgnoreCase);

            foreach (Match formMatch in formMatches)
            {
                string formContent = formMatch.Groups[1].Value;

                var badWordsAnalysis = _netWordAnalyzer.AnalyzeBadWords(formContent);

                if (badWordsAnalysis.HasBadWords)
                {
                    analysis.ContainsProfanity = true;
                    analysis.ProfanityCount += badWordsAnalysis.TotalCount;
                    analysis.ProfanityExamples.AddRange(
                        badWordsAnalysis.FoundWords.Keys.Take(5));
                }

                if (formContent.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                    formContent.Contains("credit card", StringComparison.OrdinalIgnoreCase) ||
                    formContent.Contains("ssn", StringComparison.OrdinalIgnoreCase))
                {
                    analysis.ContainsSensitiveFields = true;
                }
            }

            return analysis;
        }
        private bool CheckForMaliciousForms(string html)
        {
            bool hasPhishingIndicators = html.Contains("verify your account", StringComparison.OrdinalIgnoreCase) ||
                                        html.Contains("confirm your identity", StringComparison.OrdinalIgnoreCase) ||
                                        html.Contains("update your information", StringComparison.OrdinalIgnoreCase);

            bool requestsSensitiveData = html.Contains("password", StringComparison.OrdinalIgnoreCase) &&
                                        (html.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                                         html.Contains("sign in", StringComparison.OrdinalIgnoreCase));

            bool suspiciousDomain = html.Contains("google.com") &&
                                   (html.Contains("gmail.com", StringComparison.OrdinalIgnoreCase) ||
                                    html.Contains("drive.google.com", StringComparison.OrdinalIgnoreCase));

            return hasPhishingIndicators || requestsSensitiveData || suspiciousDomain;
        }

        private List<FormDetail> ExtractFormDetails(string html)
        {
            var formDetails = new List<FormDetail>();

            var formMatches = Regex.Matches(html, @"<form[^>]*>([\s\S]*?)</form>", RegexOptions.IgnoreCase);

            for (int i = 0; i < formMatches.Count; i++)
            {
                var match = formMatches[i];
                var detail = new FormDetail
                {
                    Index = i + 1,
                    Action = Regex.Match(match.Value, @"action=[""']([^""']+)[""']", RegexOptions.IgnoreCase).Groups[1].Value,
                    Method = Regex.Match(match.Value, @"method=[""']([^""']+)[""']", RegexOptions.IgnoreCase).Groups[1].Value,
                    InputFieldsCount = Regex.Matches(match.Value, @"<input", RegexOptions.IgnoreCase).Count,
                    HasSubmitButton = Regex.IsMatch(match.Value, @"<button[^>]*type=[""']submit[""']|<input[^>]*type=[""']submit[""']", RegexOptions.IgnoreCase)
                };

                formDetails.Add(detail);
            }

            return formDetails;
        }

        private SecurityAnalysis AnalyzePageSecurity(string html)
        {
            var analysis = new SecurityAnalysis();

            analysis.HasHttps = html.Contains("https://");

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
                var siteAnalysis = new SiteAnalysis
                {
                    Url = url,
                    DomainUrl = new Uri(url).Host,
                };


                Console.WriteLine($"Результат проверки для {url} сохранен в БД");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения в БД: {ex.Message}");
            }
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

            return stats;
        }
    }
}
