using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringServiceCore.Database;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.GoogleForms;
using MonitoringServiceCore.Database.Roles;
using MonitoringServiceCore.Email.Interface;
using MonitoringServiceCore.Email.Jobs;
using MonitoringServiceCore.Services;
using System.ComponentModel.DataAnnotations;

namespace MonitoringServiceCore.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly MonitoringDbContext _dbContext;
        private readonly SiteDataDownloader _siteDataDownloader;
        private readonly BadWordAnalyzer _BadWordAnalyzer;
        private readonly GoogleFormsDetector _googleFormsDetector;
        private readonly PersonalDataConsentService _consentService;
        private readonly ExtremistMaterialChecker _extremistChecker;
        private DataJob _dj;
        public ExtremistCheckResult? ExtremistCheckResult { get; set; }

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

        private readonly IEmailService _emailService;
        public IndexModel(
            MonitoringDbContext dbContext,
            SiteDataDownloader siteDataDownloader,
            BadWordAnalyzer BadWordAnalyzer,
            GoogleFormsDetector googleFormsDetector,
            PersonalDataConsentService consentService,
            ExtremistMaterialChecker extremistChecker,
            IEmailService emailService,DataJob dj)
        {
            _emailService = emailService;
            _dbContext = dbContext;
            _siteDataDownloader = siteDataDownloader;
            _BadWordAnalyzer = BadWordAnalyzer;
            _googleFormsDetector = googleFormsDetector;
            _consentService = consentService;
            _extremistChecker = extremistChecker;
            _dj = dj;
        }
        public ConsentCheckResult? ConsentResult { get; set; }

        public void OnGet()
        {
            Users = _dbContext.Users.ToList();
            DictionaryInfo = _BadWordAnalyzer.GetDictionaryInfo();
        }
        public async Task<IActionResult> OnPostCheckConsentAsync()
        {
            if (string.IsNullOrEmpty(SiteUrl))
            {
                ErrorMessage = "URL не указан";
                return Page();
            }

            try
            {
                ConsentResult = await _consentService.CheckConsentAsync(SiteUrl);
                TempData["SuccessMessage"] = ConsentResult.IsCompliant
                    ? "Согласие на обработку ПД найдено"
                    : "Предупреждение: на странице не обнаружено явного согласия на обработку ПД";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            return Page();
        }
        public async Task<IActionResult> OnPostAnalyzeSiteAsync()
        {
            if (!ModelState.IsValid)
            {
                Users = _dbContext.Users.ToList();
                DictionaryInfo = _BadWordAnalyzer.GetDictionaryInfo();
                return Page();
            }

            try
            {
                HtmlContent = await _siteDataDownloader.DownloadHtmlAsync(SiteUrl!);
                ExtremistCheckResult = await _extremistChecker.CheckContentAsync(HtmlContent, SiteUrl!);

                // Убрали параметр "NET"
                AnalysisResult = _BadWordAnalyzer.AnalyzeContent(HtmlContent);

                GoogleFormsResult = await _googleFormsDetector.DetectGoogleFormsAsync(SiteUrl!);

                DictionaryInfo = _BadWordAnalyzer.GetDictionaryInfo();
                Users = _dbContext.Users.ToList();

                var messages = new List<string>();

                if (ExtremistCheckResult.HasExtremistMaterials)
                {
                    messages.Add($"Обнаружено {ExtremistCheckResult.FoundMaterials.Count} упоминаний материалов из федерального списка экстремистских материалов");
                }

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
                    TempData["SuccessMessage"] = "Анализ завершен! Нецензурные слова и Google Forms не обнаружены.";
                }

                // Отправка email
                string adminEmail = "maratismage@mail.ru";
                string subject = $"Результаты анализа сайта {SiteUrl}";

                var messageBuilder = new System.Text.StringBuilder();
                messageBuilder.AppendLine($"<h2>Анализ сайта {SiteUrl} завершён</h2>");
                messageBuilder.AppendLine($"<p><strong>Время проверки:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>");

                if (ExtremistCheckResult?.HasExtremistMaterials == true)
                {
                    messageBuilder.AppendLine($"<p style='color:red'>⚠️ Обнаружено {ExtremistCheckResult.FoundMaterials.Count} упоминаний экстремистских материалов.</p>");
                }
                if (AnalysisResult?.HasBadWords == true)
                {
                    messageBuilder.AppendLine($"<p style='color:orange'>⚠️ Обнаружено {AnalysisResult.TotalBadWordsCount} нецензурных слов.</p>");
                }
                if (GoogleFormsResult?.HasGoogleForms == true)
                {
                    messageBuilder.AppendLine($"<p style='color:orange'>⚠️ Обнаружено {GoogleFormsResult.FormUrls.Count} Google Form(s).</p>");
                }
                if (ConsentResult?.IsCompliant == false)
                {
                    messageBuilder.AppendLine($"<p style='color:red'>⚠️ Отсутствует явное согласие на обработку персональных данных.</p>");
                }
                if (!(ExtremistCheckResult?.HasExtremistMaterials == true ||
                      AnalysisResult?.HasBadWords == true ||
                      GoogleFormsResult?.HasGoogleForms == true ||
                      ConsentResult?.IsCompliant == false))
                {
                    messageBuilder.AppendLine("<p style='color:green'>✅ Нарушений не обнаружено.</p>");
                }

                SendEmailInBackground("fullstack_web_developer@mail.ru", subject, messageBuilder.ToString());
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при анализе сайта: {ex.Message}";
                Users = _dbContext.Users.ToList();
                DictionaryInfo = _BadWordAnalyzer.GetDictionaryInfo();
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            return Page();
        }
        private void SendEmailInBackground(string to, string subject, string body)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailAsync(to, subject, body);
                }
                catch (Exception ex)
                {
                    // Логируем ошибку, но не показываем пользователю
                    Console.WriteLine($"Ошибка фоновой отправки email: {ex.Message}");
                    // Если у вас есть ILogger, используйте его
                }
            });
        }

        public IActionResult OnPostToggleBadWordsDetails()
        {
            ShowBadWordsDetails = !ShowBadWordsDetails;
            Users = _dbContext.Users.ToList();
            DictionaryInfo = _BadWordAnalyzer.GetDictionaryInfo();
            return Page();
        }

        public IActionResult OnPostToggleGoogleFormsDetails()
        {
            ShowGoogleFormsDetails = !ShowGoogleFormsDetails;
            Users = _dbContext.Users.ToList();
            DictionaryInfo = _BadWordAnalyzer.GetDictionaryInfo();
            return Page();
        }
    }
}