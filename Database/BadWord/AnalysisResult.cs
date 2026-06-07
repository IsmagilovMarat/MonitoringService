using MonitoringServiceCore.Services;

namespace MonitoringServiceCore.Database.BadWord
{
    public class AnalysisResult
    {
        public int TotalCharacters { get; set; }
        public string? Content { get; set; }
        public DateTime AnalyzedDate { get; set; }
        public int CountOfViolations { get; set; }
        public bool HasExtimistMaterial {get;set;}
        public bool HasGoogleForms { get;set;}
public Dictionary<string, int> BadWordsFound { get; set; } = new();
        public int TotalBadWordsCount { get; set; }
        public List<WordContext> BadWordsWithContext { get; set; } = new();
        public List<WordPosition> WordPositions { get; set; } = new();
        public bool HasBadWords => TotalBadWordsCount > 0;

        public MLDetectionResult? MLDetectionResult { get; set; }
        public bool MLOnlyDetected { get; set; }
        public List<string>? GoogleFormsFound { get; set; }
        public List<string>? ExtremismFound { get; set; }
        public int OverallScore { get; set; }
        public bool HasPrivacyPolicy { get; set; }
        public bool HasConsent { get; set; }
        public void PrintResults()
        {
            Console.WriteLine("=== РЕЗУЛЬТАТЫ АНАЛИЗА НЕЦЕНЗУРНОЙ ЛЕКСИКИ ===");

            if (HasBadWords)
            {
                Console.WriteLine($"\n=== НЕЦЕНЗУРНЫЕ СЛОВА ===");
                Console.WriteLine($"Общее количество вхождений: {TotalBadWordsCount}");
                Console.WriteLine($"Уникальных слов: {BadWordsFound.Count}");

                foreach (var word in BadWordsFound.OrderByDescending(w => w.Value).Take(20))
                {
                    Console.WriteLine($"  {word.Key}: {word.Value} вхождений");
                }

                if (BadWordsFound.Count > 20)
                {
                    Console.WriteLine($"  ... и еще {BadWordsFound.Count - 20} слов");
                }

                Console.WriteLine($"\n=== ПОЗИЦИИ МАТНЫХ СЛОВ ===");
                foreach (var pos in WordPositions.Take(20))
                {
                    Console.WriteLine($"  Строка {pos.LineNumber}, позиция {pos.Position}: '{pos.Word}'");
                    Console.WriteLine($"    Контекст: {pos.Context}");
                }
            }
            else
            {
                Console.WriteLine("\n✓ Нецензурные слова не обнаружены");
            }

            if (MLDetectionResult != null && MLDetectionResult.HasProfanity)
            {
                Console.WriteLine($"\n=== ML ДЕТЕКЦИЯ ===");
                Console.WriteLine($"Вероятность наличия нецензурной лексики: {MLDetectionResult.Probability:P}");
                Console.WriteLine($"Найдено ML слов: {MLDetectionResult.FoundWords.Sum(x => x.Value)}");

                foreach (var pos in MLDetectionResult.WordPositions.Take(10))
                {
                    Console.WriteLine($"  ML: строка {pos.LineNumber}, слово '{pos.Word}'");
                }
            }

            Console.WriteLine($"\nОбщее количество символов: {TotalCharacters}");
        }
    }
}
