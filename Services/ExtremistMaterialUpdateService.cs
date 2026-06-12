using MonitoringServiceCore.Database.dbContext;

namespace MonitoringServiceCore.Services
{
    public class ExtremistMaterialUpdateService : IHostedService
    {
        private readonly IServiceProvider _services;
        private Timer? _timer;
        private readonly TimeSpan _updateInterval = TimeSpan.FromDays(7); // Раз в неделю

        public ExtremistMaterialUpdateService(
            IServiceProvider services,
            ILogger<ExtremistMaterialUpdateService> logger)
        {
            _services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {

            _ = Task.Run(async () => await UpdateMaterialsAsync());

            _timer = new Timer(
                async _ => await UpdateMaterialsAsync(),
                null,
                _updateInterval,
                _updateInterval);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private async Task UpdateMaterialsAsync()
        {
            try
            {
                using var scope = _services.CreateScope();
                var parser = scope.ServiceProvider.GetRequiredService<ExtremistMaterialsParser>();
                var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();

                var downloaded = await parser.DownloadFileAsync();

                if (!downloaded)
                {
                    return;
                }


                var materials = parser.ParseMaterialsFromDocx();

                if (!materials.Any())
                {
                    return;
                }

                var oldCount = dbContext.ExtremistMaterials.Count();
                dbContext.ExtremistMaterials.RemoveRange(dbContext.ExtremistMaterials);
                await dbContext.SaveChangesAsync();

                // Сохраняем пачками по 1000 записей для оптимизации
                const int batchSize = 1000;
                for (int i = 0; i < materials.Count; i += batchSize)
                {
                    var batch = materials.Skip(i).Take(batchSize);
                    await dbContext.ExtremistMaterials.AddRangeAsync(batch);
                    await dbContext.SaveChangesAsync();
                }

                var savedMaterials = dbContext.ExtremistMaterials.ToList();
                var withTextCount = savedMaterials.Count(m => !string.IsNullOrEmpty(m.Text));
                var withDateCount = savedMaterials.Count(m => m.DecisionDate.HasValue);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}