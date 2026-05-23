using MonitoringServiceCore.Database.dbContext;

namespace MonitoringServiceCore.Services
{
    public class ExtremistMaterialsUpdateService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ExtremistMaterialsUpdateService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromDays(1); // раз в сутки

        public ExtremistMaterialsUpdateService(IServiceProvider services, ILogger<ExtremistMaterialsUpdateService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var parser = scope.ServiceProvider.GetRequiredService<ExtremistMaterialsParser>();
                    var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();

                    _logger.LogInformation("Начинается обновление списка экстремистских материалов");
                    var result = await parser.ParseAllPagesAsync(1, 55);
                    _logger.LogInformation("Обновление завершено: загружено {Count} материалов", result.TotalMaterialsFound);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обновлении списка экстремистских материалов");
                }

                await Task.Delay(_updateInterval, stoppingToken);
            }
        }
    }
}
