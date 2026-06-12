using MonitoringServiceCore.Database.ConsentCheckResults;
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

                foreach (var keyword in _consentKeywords)
                {
                    if (lowerHtml.Contains(keyword.ToLower()))
                    {
                        result.FoundKeywords.Add(keyword);
                    }
                }
                result.HasConsentMechanism = result.FoundKeywords.Any();

                result.HasCheckboxConsent = _checkboxRegex.IsMatch(html) &&
                    (lowerHtml.Contains("согласие") || lowerHtml.Contains("consent") || lowerHtml.Contains("agree"));

                result.HasButtonConsent = _consentButtonRegex.IsMatch(html);

                result.HasPrivacyPolicyLink = _privacyLinkRegex.IsMatch(html);

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
    }
}
