namespace MonitoringServiceCore.Database.GoogleForms
{
    public class GoogleFormsDetectionResult
    {
        public string Url { get; set; }
        public DateTime DetectionTime { get; set; }
        public bool HasGoogleForms { get; set; }
        public bool HtmlLoaded { get; set; }
        public int HtmlLength { get; set; }
        public List<string> FormUrls { get; set; } = new List<string>();
        public List<string> FormTypes { get; set; } = new List<string>();
        public SurroundingContentAnalysis SurroundingContentAnalysis { get; set; }
        public bool IsPotentiallyMalicious { get; set; }
        public List<FormDetail> FormDetails { get; set; } = new List<FormDetail>();
        public SecurityAnalysis SecurityAnalysis { get; set; }
        public string ErrorMessage { get; set; }

        public bool HasErrors => !string.IsNullOrEmpty(ErrorMessage);

        public void PrintReport()
        {
            Console.WriteLine("=== ОТЧЕТ О ПРОВЕРКЕ GOOGLE FORMS ===");
            Console.WriteLine($"URL: {Url}");
            Console.WriteLine($"Время проверки: {DetectionTime}");
            Console.WriteLine($"HTML загружен: {(HtmlLoaded ? "Да" : "Нет")}");

            if (HasErrors)
            {
                Console.WriteLine($"Ошибка: {ErrorMessage}");
                return;
            }

            Console.WriteLine($"Найдены Google Forms: {(HasGoogleForms ? "ДА" : "НЕТ")}");

            if (HasGoogleForms)
            {
                Console.WriteLine($"\nНайдено форм: {FormUrls.Count}");
                Console.WriteLine($"Типы форм: {string.Join(", ", FormTypes)}");

                if (FormUrls.Any())
                {
                    Console.WriteLine("\nURL форм:");
                    foreach (var formUrl in FormUrls)
                    {
                        Console.WriteLine($"  - {formUrl}");
                    }
                }

                if (FormDetails.Any())
                {
                    Console.WriteLine("\nДетали форм:");
                    foreach (var detail in FormDetails)
                    {
                        Console.WriteLine($"  Форма #{detail.Index}:");
                        Console.WriteLine($"    Action: {detail.Action}");
                        Console.WriteLine($"    Method: {detail.Method}");
                        Console.WriteLine($"    Полей ввода: {detail.InputFieldsCount}");
                        Console.WriteLine($"    Кнопка отправки: {(detail.HasSubmitButton ? "Да" : "Нет")}");
                    }
                }

                if (IsPotentiallyMalicious)
                {
                    Console.WriteLine("\n⚠️ ВНИМАНИЕ: Обнаружены потенциально вредоносные формы!");
                }

                if (SurroundingContentAnalysis?.ContainsProfanity == true)
                {
                    Console.WriteLine($"\n⚠️ Обнаружена нецензурная лексика в формах: {SurroundingContentAnalysis.ProfanityCount} вхождений");
                }
            }

            if (SecurityAnalysis != null)
            {
                Console.WriteLine($"\n=== АНАЛИЗ БЕЗОПАСНОСТИ ===");
                Console.WriteLine($"Уровень безопасности: {SecurityAnalysis.SecurityLevel}");
                Console.WriteLine($"HTTPS: {(SecurityAnalysis.HasHttps ? "Да" : "Нет")}");
                Console.WriteLine($"Content Security Policy: {(SecurityAnalysis.HasCSP ? "Да" : "Нет")}");
                Console.WriteLine($"Смешанный контент: {(SecurityAnalysis.HasMixedContent ? "Да" : "Нет")}");
                Console.WriteLine($"Внешних скриптов: {SecurityAnalysis.ExternalScriptsCount}");
            }
        }
    }
}
