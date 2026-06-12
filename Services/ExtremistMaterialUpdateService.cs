using MonitoringServiceCore.Database.dbContext;

namespace MonitoringServiceCore.Services
{
    public class ExtremistMaterialUpdateService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ExtremistMaterialUpdateService> _logger;
        private Timer? _timer;
        private readonly TimeSpan _updateInterval = TimeSpan.FromDays(7); // Раз в неделю

        public ExtremistMaterialUpdateService(
            IServiceProvider services,
            ILogger<ExtremistMaterialUpdateService> logger)
        {
            _services = services;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Сервис обновления экстремистских материалов запущен");

            // Выполняем первое обновление сразу при запуске
            _ = Task.Run(async () => await UpdateMaterialsAsync());

            // Настраиваем таймер для последующих обновлений
            _timer = new Timer(
                async _ => await UpdateMaterialsAsync(),
                null,
                _updateInterval,
                _updateInterval);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Сервис обновления экстремистских материалов остановлен");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private async Task UpdateMaterialsAsync()
        {
            _logger.LogInformation("=== НАЧАЛО ОБНОВЛЕНИЯ ЭКСТРЕМИСТСКИХ МАТЕРИАЛОВ ===");

            try
            {
                using var scope = _services.CreateScope();
                var parser = scope.ServiceProvider.GetRequiredService<ExtremistMaterialsParser>();
                var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();

                // 1. Скачиваем файл
                _logger.LogInformation("Шаг 1: Скачивание файла...");
                var downloaded = await parser.DownloadFileAsync();

                if (!downloaded)
                {
                    _logger.LogWarning("Не удалось загрузить файл. Обновление отменено.");
                    return;
                }

                _logger.LogInformation("Файл успешно скачан. Размер: {Size} байт", parser.GetFileSize());

                // 2. Парсим файл
                _logger.LogInformation("Шаг 2: Парсинг файла...");
                var materials = parser.ParseMaterialsFromDocx();

                if (!materials.Any())
                {
                    _logger.LogWarning("Не удалось извлечь материалы из файла. Обновление отменено.");
                    return;
                }

                _logger.LogInformation("Извлечено {Count} материалов", materials.Count);

                // 3. Очищаем старые данные
                _logger.LogInformation("Шаг 3: Очистка старых данных...");
                var oldCount = dbContext.ExtremistMaterials.Count();
                dbContext.ExtremistMaterials.RemoveRange(dbContext.ExtremistMaterials);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Удалено {Count} старых записей", oldCount);

                // 4. Сохраняем новые данные
                _logger.LogInformation("Шаг 4: Сохранение новых данных...");

                // Сохраняем пачками по 1000 записей для оптимизации
                const int batchSize = 1000;
                for (int i = 0; i < materials.Count; i += batchSize)
                {
                    var batch = materials.Skip(i).Take(batchSize);
                    await dbContext.ExtremistMaterials.AddRangeAsync(batch);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Сохранено {Count} материалов (партия {Batch})",
                        batch.Count(), (i / batchSize) + 1);
                }

                _logger.LogInformation("Сохранено {Count} материалов в базу данных", materials.Count);

                // 5. Выводим подробную статистику
                var savedMaterials = dbContext.ExtremistMaterials.ToList();
                var withTextCount = savedMaterials.Count(m => !string.IsNullOrEmpty(m.Text));
                var withDateCount = savedMaterials.Count(m => m.DecisionDate.HasValue);

                _logger.LogInformation("=== ОБНОВЛЕНИЕ ЗАВЕРШЕНО ===");
                _logger.LogInformation("Всего материалов в БД: {Count}", savedMaterials.Count);
                _logger.LogInformation("Материалов с текстом: {Count}", withTextCount);
                _logger.LogInformation("Материалов с датой: {Count}", withDateCount);

                if (savedMaterials.Any())
                {
                    _logger.LogInformation("Диапазон номеров: {Min} - {Max}",
                        savedMaterials.Min(m => m.Number),
                        savedMaterials.Max(m => m.Number));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ОШИБКА при обновлении экстремистских материалов");
            }
        }

    }
}