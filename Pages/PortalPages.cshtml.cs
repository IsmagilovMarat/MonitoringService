using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringService.Database.MonitoringPortalResources;
using MonitoringServiceCore.Database.BadWord;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.MonitoringPortalResources;
using MonitoringServiceCore.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MonitoringServiceCore.Pages
{
    public class PortalPagesModel : PageModel
    {
        private readonly MonitoringDbContext _dbContext;
        private readonly SiteDataDownloader _siteDataDownloader;
        private readonly BadWordAnalyzer _badWordAnalyzer;
        private readonly GoogleFormsDetector _googleFormsDetector;
        private readonly PersonalDataConsentService _consentService;
        private readonly ExtremistMaterialChecker _extremistChecker;

        public PortalPagesModel(
            MonitoringDbContext dbContext,
            SiteDataDownloader siteDataDownloader,
            BadWordAnalyzer badWordAnalyzer,
            GoogleFormsDetector googleFormsDetector,
            PersonalDataConsentService consentService,
            ExtremistMaterialChecker extremistChecker)
        {
            _dbContext = dbContext;
            _siteDataDownloader = siteDataDownloader;
            _badWordAnalyzer = badWordAnalyzer;
            _googleFormsDetector = googleFormsDetector;
            _consentService = consentService;
            _extremistChecker = extremistChecker;
        }

        [BindProperty]
        public ResourceInputModel NewResource { get; set; } = new();

        public List<MonitoringResource> Resources { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        // Хранилище десериализованных результатов проверки для текущей сессии
        public Dictionary<Guid, ResourceCheckResult> CheckResults { get; set; } = new();

        public void OnGet()
        {
            LoadResources();
            DeserializeAllCheckResults();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadResources();
                DeserializeAllCheckResults();
                return Page();
            }

            try
            {
                var resource = new MonitoringResource
                {
                    Id = Guid.NewGuid(),
                    Name = NewResource.Name,
                    Url = NewResource.Url,
                    TypePortal = PortalType.Government,
                    IsActive = true
                };

                _dbContext.Resources.Add(resource);
                await _dbContext.SaveChangesAsync();

                SuccessMessage = $"Ресурс \"{resource.Name}\" успешно добавлен!";
                NewResource = new ResourceInputModel();
                LoadResources();
                DeserializeAllCheckResults();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при добавлении ресурса: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            try
            {
                var resource = await _dbContext.Resources.FindAsync(id);
                if (resource != null)
                {
                    _dbContext.Resources.Remove(resource);
                    await _dbContext.SaveChangesAsync();
                    SuccessMessage = $"Ресурс \"{resource.Name}\" удален!";
                }
                LoadResources();
                DeserializeAllCheckResults();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при удалении: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(Guid id)
        {
            try
            {
                var resource = await _dbContext.Resources.FindAsync(id);
                if (resource != null)
                {
                    resource.IsActive = !resource.IsActive;
                    await _dbContext.SaveChangesAsync();
                    SuccessMessage = $"Статус ресурса \"{resource.Name}\" изменен на {(resource.IsActive ? "Активен" : "Неактивен")}";
                }
                LoadResources();
                DeserializeAllCheckResults();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при изменении статуса: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCheckResourceAsync(Guid id)
        {
            try
            {
                var resource = await _dbContext.Resources.FindAsync(id);
                if (resource == null)
                {
                    ErrorMessage = "Ресурс не найден";
                    LoadResources();
                    DeserializeAllCheckResults();
                    return Page();
                }

                var result = new ResourceCheckResult
                {
                    ResourceId = id,
                    ResourceName = resource.Name,
                    CheckTime = DateTime.Now,
                    IsExpanded = true
                };

                try
                {
                    // Скачиваем HTML
                    var htmlContent = await _siteDataDownloader.DownloadHtmlAsync(resource.Url);

                    // Проверка нецензурной лексики
                    var analysisResult = _badWordAnalyzer.AnalyzeContent(htmlContent);
                    result.HasBadWords = analysisResult.HasBadWords;
                    result.BadWordsCount = analysisResult.TotalBadWordsCount;
                    result.BadWordsList = analysisResult.BadWordsFound;

                    // Проверка Google Forms
                    //var googleFormsResult = await _googleFormsDetector.DetectGoogleFormsAsync(resource.Url,resource.Url);
                    //result.HasGoogleForms = googleFormsResult.HasGoogleForms;
                    //result.GoogleFormsCount = googleFormsResult.FormUrls?.Count ?? 0;
                    //result.GoogleFormsList = googleFormsResult.FormUrls;
                    //result.IsPotentiallyMalicious = googleFormsResult.IsPotentiallyMalicious;

                    // Проверка экстремистских материалов
                    var extremistResult = await _extremistChecker.CheckContentAsync(htmlContent, resource.Url);
                    result.HasExtremistMaterials = extremistResult.HasExtremistMaterials;
                    result.ExtremistCount = extremistResult.FoundMaterials?.Count ?? 0;

                    // Проверка согласия на обработку ПД
                    var consentResult = await _consentService.CheckConsentAsync(resource.Url);
                    result.HasConsent = consentResult?.IsCompliant ?? false;

                    // Расчет общей оценки
                    result.OverallScore = CalculateOverallScore(result);

                    // Сохраняем результат в БД
                    resource.CheckResults = JsonSerializer.Serialize(result);
                    resource.LastCheckDate = DateTime.Now;
                    await _dbContext.SaveChangesAsync();

                    SuccessMessage = $"Проверка ресурса \"{resource.Name}\" завершена!";
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = $"Ошибка при проверке: {ex.Message}";
                    resource.CheckResults = JsonSerializer.Serialize(result);
                    resource.LastCheckDate = DateTime.Now;
                    await _dbContext.SaveChangesAsync();
                }

                LoadResources();
                DeserializeAllCheckResults();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка: {ex.Message}";
                LoadResources();
                DeserializeAllCheckResults();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostToggleResultAsync(Guid id)
        {
            var resource = await _dbContext.Resources.FindAsync(id);
            if (resource != null && !string.IsNullOrEmpty(resource.CheckResults))
            {
                var result = JsonSerializer.Deserialize<ResourceCheckResult>(resource.CheckResults);
                if (result != null)
                {
                    result.IsExpanded = !result.IsExpanded;
                    resource.CheckResults = JsonSerializer.Serialize(result);
                    await _dbContext.SaveChangesAsync();
                }
            }

            LoadResources();
            DeserializeAllCheckResults();
            return Page();
        }

        private int CalculateOverallScore(ResourceCheckResult result)
        {
            int score = 100;
            if (result.HasBadWords) score -= 30;
            if (result.HasExtremistMaterials) score -= 50;
            if (result.HasGoogleForms) score -= 20;
            if (!result.HasConsent) score -= 20;
            return Math.Max(0, score);
        }

        private void LoadResources()
        {
            Resources = _dbContext.Resources
                .Where(r => r.TypePortal == PortalType.Government)
                .OrderByDescending(r => r.IsActive)
                .ThenBy(r => r.Name)
                .ToList();
        }

        private void DeserializeAllCheckResults()
        {
            CheckResults.Clear();
            foreach (var resource in Resources)
            {
                if (!string.IsNullOrEmpty(resource.CheckResults))
                {
                    try
                    {
                        var result = JsonSerializer.Deserialize<ResourceCheckResult>(resource.CheckResults);
                        if (result != null)
                        {
                            CheckResults[resource.Id] = result;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка десериализации для {resource.Id}: {ex.Message}");
                    }
                }
            }
        }
    }

    public class ResourceInputModel
    {
        [Required(ErrorMessage = "Введите название ресурса")]
        [Display(Name = "Название ресурса")]
        [StringLength(100, ErrorMessage = "Название не должно превышать 100 символов")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите URL ресурса")]
        [Url(ErrorMessage = "Введите корректный URL (например, https://example.com)")]
        [Display(Name = "URL адрес")]
        public string Url { get; set; } = string.Empty;
    }

    public class ResourceCheckResult
    {
        public Guid ResourceId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public DateTime CheckTime { get; set; }
        public bool IsExpanded { get; set; }
        public string? ErrorMessage { get; set; }

        // Результаты проверок
        public bool HasBadWords { get; set; }
        public int BadWordsCount { get; set; }
        public Dictionary<string, int>? BadWordsList { get; set; }

        public bool HasGoogleForms { get; set; }
        public int GoogleFormsCount { get; set; }
        public List<string>? GoogleFormsList { get; set; }
        public bool IsPotentiallyMalicious { get; set; }

        public bool HasExtremistMaterials { get; set; }
        public int ExtremistCount { get; set; }
        public List<string>? ExtremistList { get; set; }

        public bool HasConsent { get; set; }
        public int OverallScore { get; set; }

        public bool HasViolations => HasBadWords || HasGoogleForms || HasExtremistMaterials || !HasConsent;
    }
}