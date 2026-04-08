using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.Roles;
using MonitoringServiceCore.Services;
using System.ComponentModel.DataAnnotations;
using MonitoringServiceCore.Database.GoogleForms;

namespace MonitoringServiceCore.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly MonitoringDbContext _dbContext;
        private readonly SiteDataDownloader _siteDataDownloader;
        private readonly NetWordAnalyzer _netWordAnalyzer;
        private readonly GoogleFormsDetector _googleFormsDetector;

        public List<User> Users { get; set; } = new List<User>();

        [BindProperty]
        [Required(ErrorMessage = "Введите URL сайта")]
        [Url(ErrorMessage = "Введите корректный URL")]
        public string? SiteUrl { get; set; }

        public AnalysisResult? AnalysisResult { get; set; }
        public DictionaryInfo? DictionaryInfo { get; set; }
        public GoogleFormsDetectionResult? GoogleFormsResult { get; set; }

        public string? HtmlContent { get; set; }
        public string? ErrorMessage { get; set; }
        public bool HasAnalysis => AnalysisResult != null;
        public bool HasGoogleFormsCheck => GoogleFormsResult != null;
        public bool ShowBadWordsDetails { get; set; }
        public bool ShowGoogleFormsDetails { get; set; }

        public IndexModel(
            MonitoringDbContext dbContext,
            SiteDataDownloader siteDataDownloader,
            NetWordAnalyzer netWordAnalyzer,
            GoogleFormsDetector googleFormsDetector)
        {
            _dbContext = dbContext;
            _siteDataDownloader = siteDataDownloader;
            _netWordAnalyzer = netWordAnalyzer;
            _googleFormsDetector = googleFormsDetector;
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
                HtmlContent = await _siteDataDownloader.DownloadHtmlAsync(SiteUrl!);

                AnalysisResult = _netWordAnalyzer.AnalyzeContent(HtmlContent, "NET");

                GoogleFormsResult = await _googleFormsDetector.DetectGoogleFormsAsync(SiteUrl!);

                DictionaryInfo = _netWordAnalyzer.GetDictionaryInfo();
                Users = _dbContext.Users.ToList();

                var messages = new List<string>();

                if (AnalysisResult.HasBadWords)
                {
                    messages.Add($"Обнаружено {AnalysisResult.TotalBadWordsCount} нецензурных слов ({AnalysisResult.BadWordsFound.Count} уникальных)");
                }

                if (GoogleFormsResult.HasGoogleForms)
                {
                    messages.Add($"Обнаружено {GoogleFormsResult.FormUrls.Count} Google Form(s)");
                    if (GoogleFormsResult.IsPotentiallyMalicious)
                    {
                        messages.Add("⚠️ ВНИМАНИЕ: Обнаружены потенциально вредоносные формы!");
                    }
                }

                if (messages.Any())
                {
                    TempData["WarningMessage"] = string.Join(". ", messages);
                }
                else
                {
                    TempData["SuccessMessage"] = $"Анализ завершен! Найдено {AnalysisResult.CountAllVariants} вхождений NET. Нецензурные слова и Google Forms не обнаружены.";
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

        public IActionResult OnPostToggleGoogleFormsDetails()
        {
            ShowGoogleFormsDetails = !ShowGoogleFormsDetails;
            Users = _dbContext.Users.ToList();
            DictionaryInfo = _netWordAnalyzer.GetDictionaryInfo();
            return Page();
        }
    }
}