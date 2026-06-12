using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MonitoringServiceCore.Database;
using MonitoringServiceCore.Database.BadWord;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.ExtremistMaterials;
using MonitoringServiceCore.Database.GoogleForms;
using MonitoringServiceCore.Database.Roles;
using MonitoringServiceCore.Email.Interface;
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
        private readonly ILogger<IndexModel> _logger;

        public ExtremistCheckResult? ExtremistCheckResult { get; set; }
        public List<User> Users { get; set; } = new List<User>();
        public AnalysisResult? AnalysisResult { get; set; }
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
            IEmailService emailService,
            ILogger<IndexModel> logger)
        {
            _emailService = emailService;
            _dbContext = dbContext;
            _siteDataDownloader = siteDataDownloader;
            _badWordAnalyzer = badWordAnalyzer;
            _googleFormsDetector = googleFormsDetector;
            _consentService = consentService;
            _extremistChecker = extremistChecker;
            _logger = logger;
        }

        public void OnGet()
        {
            try
            {
                Users = _dbContext.Users.ToList();

                AnalysisResult = new AnalysisResult();
                GoogleFormsResult = new GoogleFormsDetectionResult();
                ExtremistCheckResult = new ExtremistCheckResult();
                ConsentResult = new ConsentCheckResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке страницы");
                Users = new List<User>();
            }
        }

        public async Task<IActionResult> OnPostAnalyzeSiteAsync()
        {
            if (!ModelState.IsValid)
            {
                Users = _dbContext.Users.ToList();
                return Page();
            }

            try
            {
                HasResults = true;

                _logger.LogInformation("Начинаем анализ сайта: {SiteUrl}", SiteUrl);

                var htmlContent = await _siteDataDownloader.DownloadHtmlAsync(SiteUrl!);

                var checkResult = await _extremistChecker.CheckContentWithContextAsync(htmlContent, SiteUrl!);

                var displayResult = new ExtremistCheckResult
                {
                    Id = Guid.NewGuid(),
                    Url = SiteUrl!,
                    CheckTime = DateTime.UtcNow,
                    HasExtremistMaterials = checkResult.HasExtremistMaterials,
                    ErrorMessage = checkResult.ErrorMessage,
                    FoundMaterials = new List<FoundMaterial>()
                };

                if (checkResult.HasExtremistMaterials && checkResult.FoundMaterials.Any())
                {
                    foreach (var material in checkResult.FoundMaterials)
                    {
                        displayResult.FoundMaterials.Add(new FoundMaterial
                        {
                            Id = Guid.NewGuid(),
                            Number = material.Number,
                            Count = 1,
                            Description = material.Description,
                            MatchedKeyword = material.MatchedKeyword,
                            MatchType = material.MatchType,
                            Context = material.Context,
                            DecisionDate = material.DecisionDate,
                            CheckResultId = displayResult.Id
                        });
                    }

                    foreach (var material in displayResult.FoundMaterials)
                    {
                        _logger.LogWarning("Обнаружен экстремистский материал #{Number}: {Description} (совпадение: {Keyword})",
                            material.Number, material.Description, material.MatchedKeyword);
                    }
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var savedResult = new ExtremistCheckResult
                        {
                            Id = displayResult.Id,
                            Url = displayResult.Url,
                            CheckTime = displayResult.CheckTime,
                            HasExtremistMaterials = displayResult.HasExtremistMaterials,
                            ErrorMessage = displayResult.ErrorMessage,
                        };

                        await _dbContext.ExtremistCheckResults.AddAsync(savedResult);

                        for(int i= 0;i< displayResult.FoundMaterials.Count; i++)
                        {
                            if (displayResult.FoundMaterials.Any())
                            {
                                FoundMaterial fm = displayResult.FoundMaterials[i];
                                await _dbContext.FoundMaterials.AddAsync(fm);
                                ;
                            }
                        }
                       

                        await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("Результаты проверки сохранены в БД для URL {SiteUrl}", SiteUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при сохранении результатов в БД для URL {SiteUrl}", SiteUrl);
                    }
                });

                ExtremistCheckResult = displayResult;

                AnalysisResult = _badWordAnalyzer.AnalyzeContent(htmlContent);

                GoogleFormsResult = await _googleFormsDetector.DetectGoogleFormsFromHtmlAsync(htmlContent, SiteUrl!);

                ConsentResult = await _consentService.CheckConsentAsync(SiteUrl!);

                Users = _dbContext.Users.ToList();

                var messages = new List<string>();

                if (ExtremistCheckResult != null && ExtremistCheckResult.HasExtremistMaterials)
                {
                    messages.Add($"Обнаружено {ExtremistCheckResult.FoundMaterials.Count} экстремистских материалов");
                }

                if (AnalysisResult != null && AnalysisResult.HasBadWords)
                {
                    messages.Add($"Обнаружено {AnalysisResult.TotalBadWordsCount} нецензурных слов");
                }

                if (GoogleFormsResult != null && GoogleFormsResult.HasGoogleForms)
                {
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

                await SendEmailNotificationAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при анализе сайта {SiteUrl}", SiteUrl);
                ErrorMessage = $"Ошибка при анализе сайта: {ex.Message}";
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
                messageBuilder.AppendLine("<hr/>");

                bool hasViolations = false;

                if (ExtremistCheckResult != null && ExtremistCheckResult.HasExtremistMaterials)
                {
                    hasViolations = true;
                    messageBuilder.AppendLine($"<h3 style='color:red'>⚠️ Экстремистские материалы ({ExtremistCheckResult.FoundMaterials.Count})</h3>");
                    foreach (var material in ExtremistCheckResult.FoundMaterials)
                    {
                        messageBuilder.AppendLine($"<div style='border:1px solid #ff4444; margin:10px; padding:10px; border-radius:5px'>");
                        messageBuilder.AppendLine($"<p><strong>№{material.Number}</strong> - <em>Найдено по: {material.MatchedKeyword}</em></p>");
                        messageBuilder.AppendLine($"<p>{material.Description}</p>");
                        if (!string.IsNullOrEmpty(material.Context))
                        {
                            messageBuilder.AppendLine($"<p><strong>Контекст:</strong><br/>{material.Context}</p>");
                        }
                        messageBuilder.AppendLine($"</div>");
                    }
                }

                if (AnalysisResult != null && AnalysisResult.HasBadWords)
                {
                    hasViolations = true;
                    messageBuilder.AppendLine($"<p style='color:orange'>⚠️ Нецензурные слова: {AnalysisResult.TotalBadWordsCount}</p>");
                }

                if (ConsentResult != null && !ConsentResult.IsCompliant)
                {
                    hasViolations = true;
                    messageBuilder.AppendLine($"<p style='color:red'>⚠️ Нет согласия на обработку ПД</p>");
                }

                if (!hasViolations)
                {
                    messageBuilder.AppendLine("<p style='color:green; font-size:1.2em'>✅ Нарушений не обнаружено.</p>");
                }

                await _emailService.SendEmailAsync("fullstack_web_developer@mail.ru", subject, messageBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки email для сайта {SiteUrl}", SiteUrl);
            }
        }
    }
}