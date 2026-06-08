namespace MonitoringServiceCore.Database.ExtremistMaterialPackage
{
    public class ExtremistMaterial
    {
        public Guid Id { get; set; }
        public int Number { get; set; }            // Номер в списке
        public string? Text { get; set; }   // Слова заключенные в двойные кавычки  перечисленнные через запятую, которые есть в пронумированном блоке, который мы обрабатываем
        public string Description { get; set; }    // Описание материала
        public DateTime? DecisionDate { get; set; } // Дата решения суда
        public string RawText { get; set; }        // Исходный текст
        public int PageNumber { get; set; }        // Номер страницы, откуда взят материал
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
