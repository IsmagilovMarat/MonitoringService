using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.Roles;
using MonitoringServiceCore.Services;
using System.ComponentModel.DataAnnotations;

namespace MonitoringServiceCore.Pages
{
    public class IndexModel : PageModel
    {
        private readonly MonitoringDbContext _dbContext;
        private readonly SiteDataDownloader _siteDataDownloader;
        private readonly NetWordAnalyzer _netWordAnalyzer;

        public List<User> Users { get; set; } = new List<User>();

        [BindProperty]
        [Required(ErrorMessage = "Введите URL сайта")]
        [Url(ErrorMessage = "Введите корректный URL")]
        public string? SiteUrl { get; set; }

        public AnalysisResult? AnalysisResult { get; set; }
        public DictionaryInfo? DictionaryInfo { get; set; }

        public string? HtmlContent { get; set; }
        public string? ErrorMessage { get; set; }
        public bool HasAnalysis => AnalysisResult != null;
        public bool ShowBadWordsDetails { get; set; }

        public IndexModel(
            MonitoringDbContext dbContext,
            SiteDataDownloader siteDataDownloader,
            NetWordAnalyzer netWordAnalyzer)
        {
            _dbContext = dbContext;
            _siteDataDownloader = siteDataDownloader;
            _netWordAnalyzer = netWordAnalyzer;
        }

        public void OnGet()
        {
            Users = _dbContext.Users.ToList();
            DictionaryInfo = _netWordAnalyzer.GetDictionaryInfo();
        }

        public async Task<IActionResult> OnPostAnalyzeSiteAsync()
        {
            if (!ModelState.IsValid)
            {
                Users = _dbContext.Users.ToList();
                DictionaryInfo = _netWordAnalyzer.GetDictionaryInfo();
                return Page();
            }

            try
            {
                // Загружаем данные с сайта
                HtmlContent = await _siteDataDownloader.DownloadHtmlAsync(SiteUrl!);

                // Анализируем на наличие слов
                AnalysisResult = _netWordAnalyzer.AnalyzeContent(HtmlContent, "NET");

                // Получаем информацию о словаре
                DictionaryInfo = _netWordAnalyzer.GetDictionaryInfo();

                // Получаем пользователей для таблицы
                Users = _dbContext.Users.ToList();

                if (AnalysisResult.HasBadWords)
                {
                    TempData["WarningMessage"] =
                        $"Обнаружено {AnalysisResult.TotalBadWordsCount} нецензурных слов " +
                        $"({AnalysisResult.BadWordsFound.Count} уникальных)";
                }
                else
                {
                    TempData["SuccessMessage"] =
                        $"Анализ завершен! Найдено {AnalysisResult.CountAllVariants} вхождений NET. " +
                        "Нецензурные слова не обнаружены.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при анализе сайта: {ex.Message}";
                Users = _dbContext.Users.ToList();
                DictionaryInfo = _netWordAnalyzer.GetDictionaryInfo();
            }

            return Page();
        }

        public IActionResult OnPostToggleBadWordsDetails()
        {
            ShowBadWordsDetails = !ShowBadWordsDetails;
            Users = _dbContext.Users.ToList();
            DictionaryInfo = _netWordAnalyzer.GetDictionaryInfo();
            return Page();
        }
    }
}