using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringServiceCore.Database;
using MonitoringServiceCore.Services;
using System.ComponentModel.DataAnnotations;

namespace MonitoringServiceCore.Pages
{
    public class UsersMainPageModel : PageModel
    {
        private readonly SiteDataDownloader _siteDataDownloader;
        private readonly BadWordAnalyzer _BadWordAnalyzer;

        public UsersMainPageModel(
            SiteDataDownloader siteDataDownloader,
            BadWordAnalyzer BadWordAnalyzer)
        {
            _siteDataDownloader = siteDataDownloader;
            _BadWordAnalyzer = BadWordAnalyzer;
        }

        [BindProperty]
        [Required(ErrorMessage = "Введите URL сайта")]
        [Url(ErrorMessage = "Введите корректный URL (например, https://example.com)")]
        public string? SiteUrl { get; set; }

        public AnalysisResult? AnalysisResult { get; set; }
        public string? ErrorMessage { get; set; }
        public bool HasAnalysis => AnalysisResult != null;

        public async Task OnGetAsync()
        {
            // Здесь должна быть загрузка списка сайтов из базы данных
            // SitesList = _dbContext.Sites.ToList();
        }

        public async Task<IActionResult> OnPostAnalyzeAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Скачиваем HTML содержимое сайта
                var htmlContent = await _siteDataDownloader.DownloadHtmlAsync(SiteUrl!);

                // Анализируем содержимое (только нецензурная лексика, без .NET)
                AnalysisResult = _BadWordAnalyzer.AnalyzeContent(htmlContent); // Убрали параметр "NET"

                if (AnalysisResult.HasBadWords)
                {
                    TempData["WarningMessage"] = $"Обнаружено {AnalysisResult.TotalBadWordsCount} нецензурных слов";
                }
                else
                {
                    TempData["SuccessMessage"] = "Нецензурные слова не обнаружены";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при анализе: {ex.Message}";
            }

            return Page();
        }
    }
}