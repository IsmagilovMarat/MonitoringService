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
            try
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                string html = await _httpClient.GetStringAsync(url);

                if (!string.IsNullOrEmpty(filePath))
                {
                    await SaveToFileAsync(html, filePath);
                }
                return html;
            }
            catch (Exception ex)
            {
                throw ex; 
              
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
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(filePath, content);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}