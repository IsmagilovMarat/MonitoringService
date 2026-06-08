using MonitoringServiceCore.Database.dbContext;

namespace MonitoringServiceCore.Services
{
    public class ExtremistMaterialUpdateService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ExtremistMaterialUpdateService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromHours(24); // раз в сутки

        public ExtremistMaterialUpdateService(IServiceProvider services, ILogger<ExtremistMaterialUpdateService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // При запуске – сразу обновляем
            await UpdateMaterials();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_updateInterval, stoppingToken);
                await UpdateMaterials();
            }
        }

        private async Task UpdateMaterials()
        {
            try
            {
                using var scope = _services.CreateScope();
                var docxService = scope.ServiceProvider.GetRequiredService<ExtremistMaterialsParser>();
                var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();

                // Скачиваем файл, если он устарел
                if (docxService.IsFileOutdated())
                {
                    _logger.LogInformation("Файл устарел, начинаем загрузку...");
                    var downloaded = await docxService.DownloadFileAsync();
                    if (!downloaded) return;
                }

                // Парсим файл и обновляем базу
                var newMaterials = docxService.ParseMaterialsFromDocx();
                if (!newMaterials.Any()) return;

                // Обновляем БД (удаляем старые и добавляем новые, или обновляем существующие)
                foreach (var material in newMaterials)
                {
                    var existing = dbContext.ExtremistMaterials.FirstOrDefault(m => m.Number == material.Number);
                    if (existing != null)
                    {
                        existing.Text = material.Text;
                        existing.Description = material.Description;
                        existing.DecisionDate = material.DecisionDate;
                        existing.RawText = material.RawText;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        dbContext.ExtremistMaterials.Add(material);
                    }
                }
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("База данных обновлена: {Count} материалов", newMaterials.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении материалов");
            }
        }
    }
}