using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MonitoringServiceCore.Services
{
    public class SiteDataDownloader
    {
        private readonly HttpClient _httpClient;

        public SiteDataDownloader(HttpClient httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<string> DownloadHtmlAsync(string url, string filePath = null)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL не может быть пустым", nameof(url));

            try
            {
                // Устанавливаем User-Agent для имитации браузера
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                // Получаем HTML-контент
                string html = await _httpClient.GetStringAsync(url);

                Console.WriteLine($"Успешно загружено {html.Length} символов с {url}");

                // Если указан путь к файлу, сохраняем
                if (!string.IsNullOrEmpty(filePath))
                {
                    await SaveToFileAsync(html, filePath);
                    Console.WriteLine($"HTML сохранен в {filePath}");
                }

                return html;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка при запросе к {url}: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
                throw;
            }
        }

        public async Task SaveToFileAsync(string content, string filePath)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentException("Контент не может быть пустым", nameof(content));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым", nameof(filePath));

            try
            {
                // Создаем директорию, если ее нет
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(filePath, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении файла {filePath}: {ex.Message}");
                throw;
            }
        }
    }
}