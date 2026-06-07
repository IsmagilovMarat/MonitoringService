using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringService.Database.MonitoringPortalResources;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.MonitoringPortalResources;
using System.ComponentModel.DataAnnotations;

namespace MonitoringServiceCore.Pages
{
    public class PortalPagesModel : PageModel
    {
        private readonly MonitoringDbContext _dbContext;

        public PortalPagesModel(MonitoringDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [BindProperty]
        public ResourceInputModel NewResource { get; set; } = new();

        public List<MonitoringResource> Resources { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            LoadResources();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadResources();
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
                NewResource = new ResourceInputModel(); // Очищаем форму
                LoadResources();
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
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при изменении статуса: {ex.Message}";
            }

            return Page();
        }

        private void LoadResources()
        {
            Resources = _dbContext.Resources
                .Where(r => r.TypePortal == PortalType.Government)
                .OrderByDescending(r => r.IsActive)
                .ThenBy(r => r.Name)
                .ToList();
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
}