using MonitoringServiceCore.Database;
using System.Text.RegularExpressions;

namespace MonitoringServiceCore.Services
{
    public class PersonalDataConsentService
    {
        private readonly SiteDataDownloader _downloader;

        private readonly List<string> _consentKeywords = new List<string>
        {
            "согласие на обработку персональных данных",
            "обработка персональных данных",
            "согласен на обработку",
            "даю согласие",
            "personal data consent",
            "i agree to the processing",
            "gdpr consent",
            "согласие на обработку пд",
            "политика конфиденциальности",
            "пользовательское соглашение",
            "terms of service",
            "privacy policy"
        };

        // Регулярные выражения для поиска чекбоксов и кнопок
        private readonly Regex _checkboxRegex = new Regex(
            @"<input[^>]*type=[""']checkbox[""'][^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly Regex _consentButtonRegex = new Regex(
            @"<(?:button|input)[^>]*(?:принять|accept|agree|согласен|разрешаю)[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly Regex _privacyLinkRegex = new Regex(
            @"<a[^>]*href=[""'][^""']*[""'][^>]*>(?:политика конфиденциальности|политика обработки пд|privacy policy|personal data policy)</a>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public PersonalDataConsentService(SiteDataDownloader downloader)
        {
            _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        }

        /// <summary>
        /// Проверяет HTML-страницу на наличие элементов согласия на обработку ПД
        /// </summary>
        public async Task<ConsentCheckResult> CheckConsentAsync(string url)
        {
            var result = new ConsentCheckResult
            {
                Url = url,
                CheckTime = DateTime.UtcNow,
                FoundKeywords = new List<string>()
            };

            try
            {
                string html = await _downloader.DownloadHtmlAsync(url);
                string lowerHtml = html.ToLower();

                // 1. Поиск ключевых слов
                foreach (var keyword in _consentKeywords)
                {
                    if (lowerHtml.Contains(keyword.ToLower()))
                    {
                        result.FoundKeywords.Add(keyword);
                    }
                }
                result.HasConsentMechanism = result.FoundKeywords.Any();

                // 2. Поиск чекбокса согласия
                result.HasCheckboxConsent = _checkboxRegex.IsMatch(html) &&
                    (lowerHtml.Contains("согласие") || lowerHtml.Contains("consent") || lowerHtml.Contains("agree"));

                // 3. Поиск кнопки принятия
                result.HasButtonConsent = _consentButtonRegex.IsMatch(html);

                // 4. Поиск ссылки на политику конфиденциальности
                result.HasPrivacyPolicyLink = _privacyLinkRegex.IsMatch(html);

                // 5. Извлечение текста около первого найденного элемента (для отчёта)
                if (result.HasConsentMechanism)
                {
                    var firstKeyword = result.FoundKeywords.FirstOrDefault();
                    if (!string.IsNullOrEmpty(firstKeyword))
                    {
                        int index = lowerHtml.IndexOf(firstKeyword.ToLower());
                        int start = Math.Max(0, index - 100);
                        int length = Math.Min(300, html.Length - start);
                        result.ConsentText = html.Substring(start, length).Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Ошибка при проверке: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Проверяет несколько URL
        /// </summary>
        public async Task<List<ConsentCheckResult>> CheckMultipleAsync(IEnumerable<string> urls)
        {
            var results = new List<ConsentCheckResult>();
            foreach (var url in urls)
            {
                var res = await CheckConsentAsync(url);
                results.Add(res);
                await Task.Delay(100); // задержка между запросами
            }
            return results;
        }
    }
}
