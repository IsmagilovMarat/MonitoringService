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
        private readonly BadWordAnalyzer _badWordAnalyzer;
        private readonly GoogleFormsDetector _googleFormsDetector;
        private readonly PersonalDataConsentService _consentService;
        private readonly ExtremistMaterialChecker _extremistChecker;
        private readonly IEmailService _emailService;

        public ExtremistCheckResult? ExtremistCheckResult { get; set; }
        public List<User> Users { get; set; } = new List<User>();
        public AnalysisResult? AnalysisResult { get; set; }
        public DictionaryInfo? DictionaryInfo { get; set; }
        public GoogleFormsDetectionResult? GoogleFormsResult { get; set; }
        public ConsentCheckResult? ConsentResult { get; set; }
        public string? ErrorMessage { get; set; }
        public bool HasResults { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Введите URL сайта")]
        [Url(ErrorMessage = "Введите корректный URL")]
        public string? SiteUrl { get; set; }

        public IndexModel(
            MonitoringDbContext dbContext,
            SiteDataDownloader siteDataDownloader,
            BadWordAnalyzer badWordAnalyzer,
            GoogleFormsDetector googleFormsDetector,
            PersonalDataConsentService consentService,
            ExtremistMaterialChecker extremistChecker,
            IEmailService emailService)
        {
            _emailService = emailService;
            _dbContext = dbContext;
            _siteDataDownloader = siteDataDownloader;
            _badWordAnalyzer = badWordAnalyzer;
            _googleFormsDetector = googleFormsDetector;
            _consentService = consentService;
            _extremistChecker = extremistChecker;
        }

        public void OnGet()
        {
            Users = _dbContext.Users.ToList();
            DictionaryInfo = _badWordAnalyzer.GetDictionaryInfo();
        }

        public async Task<IActionResult> OnPostAnalyzeSiteAsync()
        {
            if (!ModelState.IsValid)
            {
                Users = _dbContext.Users.ToList();
                DictionaryInfo = _badWordAnalyzer.GetDictionaryInfo();
                return Page();
            }

            try
            {
                HasResults = true;

                // Загружаем HTML
                var htmlContent = await _siteDataDownloader.DownloadHtmlAsync(SiteUrl!);

                // 1. Проверка экстремистских материалов
              //  ExtremistCheckResult = await _extremistChecker.CheckContentAsync(htmlContent, SiteUrl!);

                // 2. Проверка нецензурной лексики
                AnalysisResult = _badWordAnalyzer.AnalyzeContent(htmlContent);

                // 3. Проверка Google Forms
                GoogleFormsResult = await _googleFormsDetector.DetectGoogleFormsAsync(SiteUrl!);

                // 4. Проверка согласия на обработку ПД
                ConsentResult = await _consentService.CheckConsentAsync(SiteUrl!);

                // Обновляем информацию о словаре
                DictionaryInfo = _badWordAnalyzer.GetDictionaryInfo();
                Users = _dbContext.Users.ToList();

                // Собираем сообщения
                var messages = new List<string>();

                if (ExtremistCheckResult.HasExtremistMaterials)
                {
                    messages.Add($"Обнаружено {ExtremistCheckResult.FoundMaterials.Count} экстремистских материалов");
                }

                if (AnalysisResult.HasBadWords)
                {
                    messages.Add($"Обнаружено {AnalysisResult.TotalBadWordsCount} нецензурных слов");
                }

                if (GoogleFormsResult.HasGoogleForms)
                {
                    messages.Add($"Обнаружено {GoogleFormsResult.FormUrls.Count} Google Form(s)");
                    if (GoogleFormsResult.IsPotentiallyMalicious)
                    {
                        messages.Add("⚠️ Потенциально вредоносные формы!");
                    }
                }

                if (ConsentResult != null && !ConsentResult.IsCompliant)
                {
                    messages.Add("⚠️ Отсутствует явное согласие на обработку ПД");
                }

                if (messages.Any())
                {
                    TempData["WarningMessage"] = string.Join(". ", messages);
                }
                else
                {
                    TempData["SuccessMessage"] = "Анализ завершен! Нарушений не обнаружено.";
                }

                // Отправка email уведомления
                await SendEmailNotificationAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при анализе сайта: {ex.Message}";
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            return Page();
        }

        private async Task SendEmailNotificationAsync()
        {
            try
            {
                var subject = $"Результаты анализа сайта {SiteUrl}";
                var messageBuilder = new System.Text.StringBuilder();
                messageBuilder.AppendLine($"<h2>Анализ сайта {SiteUrl} завершён</h2>");
                messageBuilder.AppendLine($"<p><strong>Время проверки:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>");

                if (ExtremistCheckResult?.HasExtremistMaterials == true)
                    messageBuilder.AppendLine($"<p style='color:red'>⚠️ Экстремистские материалы: {ExtremistCheckResult.FoundMaterials.Count}</p>");

                if (AnalysisResult?.HasBadWords == true)
                    messageBuilder.AppendLine($"<p style='color:orange'>⚠️ Нецензурные слова: {AnalysisResult.TotalBadWordsCount}</p>");

                if (GoogleFormsResult?.HasGoogleForms == true)
                    messageBuilder.AppendLine($"<p style='color:orange'>⚠️ Google Forms: {GoogleFormsResult.FormUrls.Count}</p>");

                if (ConsentResult?.IsCompliant == false)
                    messageBuilder.AppendLine($"<p style='color:red'>⚠️ Нет согласия на обработку ПД</p>");

                if (!(ExtremistCheckResult?.HasExtremistMaterials == true ||
                      AnalysisResult?.HasBadWords == true ||
                      GoogleFormsResult?.HasGoogleForms == true ||
                      ConsentResult?.IsCompliant == false))
                {
                    messageBuilder.AppendLine("<p style='color:green'>✅ Нарушений не обнаружено.</p>");
                }

                await _emailService.SendEmailAsync("fullstack_web_developer@mail.ru", subject, messageBuilder.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки email: {ex.Message}");
            }
        }
    }
}