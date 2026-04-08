using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.Roles;
using MonitoringServiceCore.Services;
using System.ComponentModel.DataAnnotations;

namespace MonitoringServiceCore.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly MonitoringDbContext _dbContext;
        private readonly SiteDataDownloader _siteDataDownloader;

        private readonly NetWordAnalyzer _netWordAnalyzer;

        public List<User> Users { get; set; } = new List<User>();

        [BindProperty]
        [Required(ErrorMessage = "������� URL �����")]
        [Url(ErrorMessage = "������� ���������� URL")]
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
                HtmlContent = await _siteDataDownloader.DownloadHtmlAsync(SiteUrl!);

                AnalysisResult = _netWordAnalyzer.AnalyzeContent(HtmlContent, "NET");

                DictionaryInfo = _netWordAnalyzer.GetDictionaryInfo();

                Users = _dbContext.Users.ToList();

                if (AnalysisResult.HasBadWords)
                {
                    TempData["WarningMessage"] =
                        $"���������� {AnalysisResult.TotalBadWordsCount} ����������� ���� " +
                        $"({AnalysisResult.BadWordsFound.Count} ����������)";
                }
                else
                {
                    TempData["SuccessMessage"] =
                        $"������ ��������! ������� {AnalysisResult.CountAllVariants} ��������� NET. " +
                        "����������� ����� �� ����������.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"������ ��� ������� �����: {ex.Message}";
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