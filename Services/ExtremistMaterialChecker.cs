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

        public ExtremistMaterialChecker(MonitoringDbContext dbContext, ILogger<ExtremistMaterialChecker> logger)
        {
            _dbContext = dbContext;
        }

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
                    return result;
                }

                var cleanText = StripHtml(htmlContent);
                var lowerCleanText = cleanText.ToLowerInvariant();
                var lowerHtml = htmlContent.ToLowerInvariant();

                foreach (var material in materials)
                {
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
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Ошибка при проверке: {ex.Message}";
            }

            return result;
        }

        private FoundMaterial? CheckMaterialText(ExtremistMaterial material, string cleanText, string htmlText)
        {
            if (string.IsNullOrEmpty(material.Text)) return null;

            var textParts = material.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in textParts)
            {
                var trimmedPart = part.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(trimmedPart) || trimmedPart.Length < 3) continue;

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

                    foundMaterial.Context = GetContextAroundKeyword(cleanText, trimmedPart);

                    return foundMaterial;
                }
            }

            return null;
        }

        private string GetContextAroundKeyword(string text, string keyword, int contextLength = 150)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword)) return string.Empty;

            var index = text.ToLowerInvariant().IndexOf(keyword);
            if (index < 0) return string.Empty;

            var start = Math.Max(0, index - contextLength);
            var length = Math.Min(text.Length - start, contextLength * 2);
            var context = text.Substring(start, length);

            if (start > 0) context = "..." + context;
            if (start + length < text.Length) context = context + "...";

            return context;
        }
        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            var result = Regex.Replace(html, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            result = Regex.Replace(result, @"<style[^>]*>.*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            result = Regex.Replace(result, @"<[^>]+>", " ");

            result = System.Net.WebUtility.HtmlDecode(result);

            result = Regex.Replace(result, @"\s+", " ");

            return result.Trim();
        }

        public async Task<ExtremistCheckResult> CheckContentWithContextAsync(string htmlContent, string url)
        {
            return await CheckContentAsync(htmlContent, url);
        }
    }
}

  