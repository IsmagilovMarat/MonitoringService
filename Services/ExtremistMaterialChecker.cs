using global::MonitoringServiceCore.Database.dbContext;
using Microsoft.EntityFrameworkCore;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.ExtremistMaterialPackage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitoringServiceCore.Services
{
    public class ExtremistMaterialChecker
    {
        private readonly MonitoringDbContext _dbContext;

        public ExtremistMaterialChecker(MonitoringDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Проверяет HTML-контент на наличие упоминаний экстремистских материалов
        /// </summary>
        public async Task<ExtremistCheckResult> CheckContentAsync(string htmlContent, string url)
        {
            var result = new ExtremistCheckResult
            {
                Url = url,
                CheckTime = DateTime.UtcNow
            };

            var materials = await _dbContext.ExtremistMaterials.ToListAsync();
            if (!materials.Any())
            {
                result.ErrorMessage = "Список экстремистских материалов не загружен";
                return result;
            }

            var lowerContent = htmlContent.ToLowerInvariant();

            foreach (var material in materials)
            {
                // Ищем точное совпадение описания или его части
                var description = material.Description?.ToLowerInvariant();
                if (string.IsNullOrEmpty(description)) continue;

                // Разбиваем описание на ключевые слова (например, по пробелам)
                var keywords = description.Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Where(w => w.Length > 3)
                                            .Distinct()
                                            .Take(10); // берём первые 10 значимых слов

                bool found = false;
                string matchedKeyword = null;

                foreach (var kw in keywords)
                {
                    if (lowerContent.Contains(kw))
                    {
                        found = true;
                        matchedKeyword = kw;
                        break;
                    }
                }

                if (found)
                {
                    result.FoundMaterials.Add(new FoundMaterial
                    {
                        Number = material.Number,
                        Description = material.Description,
                        MatchedKeyword = matchedKeyword,
                        DecisionDate = material.DecisionDate
                    });
                }
            }

            result.HasExtremistMaterials = result.FoundMaterials.Any();
            return result;
        }
    }

    public class ExtremistCheckResult
    {
        public string Url { get; set; }
        public DateTime CheckTime { get; set; }
        public bool HasExtremistMaterials { get; set; }
        public List<FoundMaterial> FoundMaterials { get; set; } = new();
        public string ErrorMessage { get; set; }
        public bool HasErrors => !string.IsNullOrEmpty(ErrorMessage);
    }

    public class FoundMaterial
    {
        public int Number { get; set; }
        public string Description { get; set; }
        public string MatchedKeyword { get; set; }
        public DateTime? DecisionDate { get; set; }
    }
}
