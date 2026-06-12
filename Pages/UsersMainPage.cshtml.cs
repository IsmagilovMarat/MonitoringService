using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringServiceCore.Database.BadWord;
using MonitoringServiceCore.Database.ConsentCheckResults;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.ExtremistMaterials;
using MonitoringServiceCore.Database.GoogleForms;
using MonitoringServiceCore.Services;
using System.ComponentModel.DataAnnotations;

namespace MonitoringServiceCore.Pages
{
    public class UsersMainPageModel : PageModel
    {
        private readonly MonitoringDbContext _dbContext;
        private readonly SiteDataDownloader _siteDataDownloader;
        private readonly BadWordAnalyzer _badWordAnalyzer;
        private readonly GoogleFormsDetector _googleFormsDetector;
        private readonly PersonalDataConsentService _consentService;
        private readonly ExtremistMaterialChecker _extremistChecker;
        public UsersMainPageModel(
            MonitoringDbContext dbContext,
            SiteDataDownloader siteDataDownloader,
            BadWordAnalyzer badWordAnalyzer,
            GoogleFormsDetector googleFormsDetector,
            PersonalDataConsentService consentService,
            ExtremistMaterialChecker extremistChecker,
            ILogger<UsersMainPageModel> logger)
        {
            _dbContext = dbContext;
            _siteDataDownloader = siteDataDownloader;
            _badWordAnalyzer = badWordAnalyzer;
            _googleFormsDetector = googleFormsDetector;
            _consentService = consentService;
            _extremistChecker = extremistChecker;
        }

        [BindProperty]
        [Required(ErrorMessage = "Введите URL сайта")]
        [Url(ErrorMessage = "Введите корректный URL (например, https://example.com)")]
        public string? SiteUrl { get; set; }

        // Результаты всех проверок
        public AnalysisResult? AnalysisResult { get; set; }
        public GoogleFormsDetectionResult? GoogleFormsResult { get; set; }
        public ExtremistCheckResult? ExtremistCheckResult { get; set; }
        public ConsentCheckResult? ConsentResult { get; set; }

        public string? ErrorMessage { get; set; }
        public bool HasResults => AnalysisResult != null || GoogleFormsResult != null || ExtremistCheckResult != null || ConsentResult != null;
        public bool IsAnalyzing { get; set; }

        public async Task OnGetAsync()
        {
            AnalysisResult = null;
            GoogleFormsResult = null;
            ExtremistCheckResult = null;
            ConsentResult = null;
        }

        public async Task<IActionResult> OnPostAnalyzeAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            IsAnalyzing = true;

            try
            {
                var htmlContent = await _siteDataDownloader.DownloadHtmlAsync(SiteUrl!);

                AnalysisResult = _badWordAnalyzer.AnalyzeContent(htmlContent);

                GoogleFormsResult = await _googleFormsDetector.DetectGoogleFormsFromHtmlAsync(htmlContent, SiteUrl!);

                var extremistCheck = await _extremistChecker.CheckContentWithContextAsync(htmlContent, SiteUrl!);
                ExtremistCheckResult = new ExtremistCheckResult
                {
                    Id = Guid.NewGuid(),
                    Url = SiteUrl!,
                    CheckTime = DateTime.UtcNow,
                    HasExtremistMaterials = extremistCheck.HasExtremistMaterials,
                    ErrorMessage = extremistCheck.ErrorMessage,
                    FoundMaterials = extremistCheck.FoundMaterials?.Select(m => new FoundMaterial
                    {
                        Id = Guid.NewGuid(),
                        Number = m.Number,
                        Count = 1,
                        Description = m.Description,
                        MatchedKeyword = m.MatchedKeyword,
                        MatchType = m.MatchType,
                        Context = m.Context,
                        DecisionDate = m.DecisionDate
                    }).ToList() ?? new List<FoundMaterial>()
                };

                ConsentResult = await _consentService.CheckConsentAsync(SiteUrl!);

                var messages = new List<string>();

                if (AnalysisResult != null && AnalysisResult.HasBadWords)
                {
                    messages.Add($"Обнаружено {AnalysisResult.TotalBadWordsCount} нецензурных слов");
                }

                if (GoogleFormsResult != null && GoogleFormsResult.HasGoogleForms)
                {
                    if (GoogleFormsResult.IsPotentiallyMalicious)
                    {
                        messages.Add("Обнаружены потенциально вредоносные Google формы!");
                    }
                    else
                    {
                        messages.Add($"Обнаружено {ExtremistCheckResult.FoundMaterials.Count} Google форм");
                    }
                }

                if (ExtremistCheckResult != null && ExtremistCheckResult.HasExtremistMaterials)
                {
                    messages.Add($" Обнаружено {ExtremistCheckResult.FoundMaterials.Count} экстремистских материалов!");
                }

                if (ConsentResult != null && !ConsentResult.IsCompliant)
                {
                    messages.Add(" Отсутствует явное согласие на обработку персональных данных");
                }

                if (messages.Any())
                {
                    TempData["WarningMessage"] = string.Join(". ", messages);
                }
                else
                {
                    TempData["SuccessMessage"] = " Анализ завершен! Нарушений не обнаружено.";
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при анализе: {ex.Message}";
                TempData["ErrorMessage"] = ErrorMessage;
            }
            finally
            {
                IsAnalyzing = false;
            }

            return Page();
        }

        public bool HasBadWordsViolations => AnalysisResult?.HasBadWords == true;
        public int BadWordsCount => AnalysisResult?.TotalBadWordsCount ?? 0;

        public bool HasGoogleForms => GoogleFormsResult?.HasGoogleForms == true;
        public bool IsGoogleFormsMalicious => GoogleFormsResult?.IsPotentiallyMalicious == true;

        public bool HasExtremistViolations => ExtremistCheckResult?.HasExtremistMaterials == true;
        public int ExtremistCount => ExtremistCheckResult?.FoundMaterials?.Count ?? 0;

        public bool HasConsentCompliance => ConsentResult?.IsCompliant == true;

        public int TotalViolationsCount => (HasBadWordsViolations ? 1 : 0) +
                                           (HasGoogleForms ? 1 : 0) +
                                           (HasExtremistViolations ? 1 : 0) +
                                           (!HasConsentCompliance ? 1 : 0);
        public int OverallScore
        {
            get
            {
                if (!HasResults) return 0;

                int score = 100;
                if (HasBadWordsViolations) score -= 25;
                if (HasExtremistViolations) score -= 40;
                if (!HasConsentCompliance) score -= 20;
                if (HasGoogleForms && IsGoogleFormsMalicious) score -= 15;
                else if (HasGoogleForms) score -= 5;

                return Math.Max(0, score);
            }
        }

        public string OverallStatus
        {
            get
            {
                if (OverallScore >= 80) return "Безопасно";
                if (OverallScore >= 60) return "Требует внимания";
                if (OverallScore >= 40) return "Высокий риск";
                return "Критический риск";
            }
        }

        public string OverallStatusColor
        {
            get
            {
                if (OverallScore >= 80) return "success";
                if (OverallScore >= 60) return "warning";
                if (OverallScore >= 40) return "danger";
                return "danger";
            }
        }
    }
}