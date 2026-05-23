namespace MonitoringServiceCore.Database
{
    public class ConsentCheckResult
    {
        public string Url { get; set; }
        public DateTime CheckTime { get; set; }
        public bool HasConsentMechanism { get; set; }      // Есть ли вообще форма/виджет согласия
        public bool HasCheckboxConsent { get; set; }       // Чекбокс "Я согласен"
        public bool HasButtonConsent { get; set; }         // Кнопка "Принять"
        public bool HasPrivacyPolicyLink { get; set; }     // Ссылка на политику конфиденциальности
        public string ConsentText { get; set; }            // Найденный текст согласия
        public List<string> FoundKeywords { get; set; }    // Какие ключевые слова найдены
        public string ErrorMessage { get; set; }

        public bool IsCompliant => HasConsentMechanism && (HasCheckboxConsent || HasButtonConsent) && HasPrivacyPolicyLink;
        public bool HasErrors => !string.IsNullOrEmpty(ErrorMessage);
    }
}
