using System.Text;
using System.Text.RegularExpressions;

namespace MonitoringServiceCore.Services
{
    public class NetWordAnalyzer
    {
        private List<string> _badWords = new List<string>();
        private readonly object _lockObject = new object();
        private bool _isDictionaryLoaded = false;

        public NetWordAnalyzer()
        {
            LoadBadWordsDictionary();
        }

        private void LoadBadWordsDictionary()
        {
            try
            {
                // Сначала пробуем загрузить из файла (если существует)
                string dictionaryPath = @"C:\Users\Marat2\source\repos\MonitoringService\MonitoringServiceCore\Словарь нецензурных выражений\Русский матерный словарь.txt";

                if (File.Exists(dictionaryPath))
                {
                    // Пробуем разные кодировки для русского текста
                    Encoding[] encodingsToTry =
                    {
                        Encoding.GetEncoding(1251),     // Windows-1251 (самая распространенная)
                        Encoding.UTF8,                  // UTF-8
                        Encoding.GetEncoding(20866),    // KOI8-R
                        Encoding.Default               // Системная кодировка
                    };

                    bool loadedFromFile = false;

                    foreach (var encoding in encodingsToTry)
                    {
                        try
                        {
                            var lines = File.ReadAllLines(dictionaryPath, encoding);

                            if (lines.Length > 0)
                            {
                                _badWords = lines
                                    .Where(line => !string.IsNullOrWhiteSpace(line))
                                    .Select(line => line.Trim().ToLower())
                                    .ToList();

                                if (_badWords.Count > 0)
                                {
                                    loadedFromFile = true;
                                    Console.WriteLine($"Загружено {_badWords.Count} слов из файла. Кодировка: {encoding.EncodingName}");
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (!loadedFromFile)
                    {
                        Console.WriteLine("Не удалось прочитать файл словаря. Используется enum.");
                        LoadFromEnum();
                    }
                }
                else
                {
                    Console.WriteLine($"Файл словаря не найден: {dictionaryPath}");
                    Console.WriteLine("Используется enum BadWords");
                    LoadFromEnum();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке словаря: {ex.Message}");
                LoadFromEnum();
            }

            // Если все еще не загружено, загружаем из enum
            if (_badWords.Count == 0)
            {
                LoadFromEnum();
            }

            _isDictionaryLoaded = true;
        }

        private void LoadFromEnum()
        {
            try
            {
                // Получаем все значения из enum BadWords
                var enumValues = Enum.GetValues(typeof(BadWords));
                _badWords = new List<string>();

                foreach (BadWords word in enumValues)
                {
                    // Преобразуем enum в строку в нижнем регистре
                    string wordStr = word.ToString().ToLower();
                    _badWords.Add(wordStr);
                }

                Console.WriteLine($"Загружено {_badWords.Count} слов из enum BadWords");

                // Выводим первые 10 слов для проверки
                Console.WriteLine("Первые 10 слов из enum:");
                foreach (var word in _badWords.Take(10))
                {
                    Console.WriteLine($"  - {word}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке из enum: {ex.Message}");
                LoadDefaultBadWords();
            }
        }

        private void LoadDefaultBadWords()
        {
            // Резервный список на случай ошибок
            string[] defaultBadWords = {
                "мат1", "мат2", "мат3", "брань1", "брань2"
            };

            _badWords = defaultBadWords.ToList();
            Console.WriteLine($"Загружено {_badWords.Count} слов по умолчанию");
        }

        public AnalysisResult AnalyzeFile(string filePath, string searchWord = "NET")
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл {filePath} не найден");

            try
            {
                string content = File.ReadAllText(filePath, Encoding.UTF8);
                return AnalyzeContent(content, searchWord);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке файла {filePath}: {ex.Message}");
                throw;
            }
        }

        public AnalysisResult AnalyzeContent(string content, string searchWord = "NET")
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentException("Контент не может быть пустым", nameof(content));

            var result = new AnalysisResult
            {
                TotalCharacters = content.Length,
                Content = content
            };

            // 1. Поиск .NET и вариантов
            result.CountIgnoreCase = CountOccurrencesIgnoreCase(content, searchWord);
            result.CountWholeWord = CountWithRegex(content, $@"\b{Regex.Escape(searchWord)}\b", RegexOptions.IgnoreCase);
            result.CountCaseSensitive = CountOccurrences(content, searchWord);
            result.CountAllVariants = CountWithRegex(content, $@"\.?{Regex.Escape(searchWord)}\b", RegexOptions.IgnoreCase);
            result.CountDotNet = CountWithRegex(content, @"\.NET", RegexOptions.IgnoreCase);
            result.CountAspDotNet = CountWithRegex(content, @"ASP\.NET", RegexOptions.IgnoreCase);

            // 2. Проверка на нецензурные слова
            if (_isDictionaryLoaded && _badWords.Count > 0)
            {
                var badWordsAnalysis = AnalyzeBadWords(content);
                result.BadWordsFound = badWordsAnalysis.FoundWords;
                result.TotalBadWordsCount = badWordsAnalysis.TotalCount;
                result.BadWordsWithContext = badWordsAnalysis.WordsWithContext;
            }
            else
            {
                Console.WriteLine("Предупреждение: Словарь нецензурных слов не загружен");
            }

            return result;
        }

        public BadWordsAnalysis AnalyzeBadWords(string content)
        {
            var analysis = new BadWordsAnalysis();

            if (!_isDictionaryLoaded || _badWords.Count == 0)
            {
                Console.WriteLine("Предупреждение: Анализ нецензурных слов невозможен - словарь пуст");
                return analysis;
            }

            string contentLower = content.ToLower();

            lock (_lockObject)
            {
                foreach (var badWord in _badWords)
                {
                    try
                    {
                        // Ищем вхождения слова
                        int count = CountWordOccurrences(contentLower, badWord);

                        if (count > 0)
                        {
                            analysis.FoundWords[badWord] = count;
                            analysis.TotalCount += count;

                            // Получаем контекст для первого вхождения
                            var context = GetWordContext(content, badWord);
                            if (context != null)
                            {
                                analysis.WordsWithContext.Add(new WordContext
                                {
                                    Word = badWord,
                                    Count = count,
                                    Context = context.Context
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при анализе слова '{badWord}': {ex.Message}");
                        continue;
                    }
                }
            }

            // Сортируем по количеству вхождений
            analysis.WordsWithContext = analysis.WordsWithContext
                .OrderByDescending(w => w.Count)
                .ToList();

            return analysis;
        }

        private int CountWordOccurrences(string text, string word)
        {
            int count = 0;
            int index = 0;

            // Оптимизированный поиск с учетом того, что текст уже в нижнем регистре
            while ((index = text.IndexOf(word, index, StringComparison.Ordinal)) != -1)
            {
                // Проверяем, что это отдельное слово
                if (IsWholeWord(text, index, word.Length))
                {
                    count++;
                }
                index += word.Length;
            }

            return count;
        }

        private bool IsWholeWord(string text, int index, int wordLength)
        {
            // Проверяем символы до и после слова
            bool startOk = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            bool endOk = (index + wordLength >= text.Length) ||
                        !char.IsLetterOrDigit(text[index + wordLength]);

            return startOk && endOk;
        }

        private WordContext? GetWordContext(string text, string word, int contextLength = 30)
        {
            int index = text.ToLower().IndexOf(word);

            if (index == -1)
                return null;

            int start = Math.Max(0, index - contextLength);
            int end = Math.Min(text.Length, index + word.Length + contextLength);

            string context = text.Substring(start, end - start);

            // Заменяем переносы строк
            context = context.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");

            // Подсвечиваем найденное слово
            context = context.Replace(word, $"<strong>{word}</strong>", StringComparison.OrdinalIgnoreCase);

            return new WordContext
            {
                Word = word,
                Count = 1,
                Context = $"...{context}..."
            };
        }

        // Метод 1: Без учета регистра
        private int CountOccurrencesIgnoreCase(string text, string searchWord)
        {
            int count = 0;
            int index = 0;

            while ((index = text.IndexOf(searchWord, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += searchWord.Length;
            }

            return count;
        }

        // Метод 2: С учетом регистра
        private int CountOccurrences(string text, string searchWord)
        {
            int count = 0;
            int index = 0;

            while ((index = text.IndexOf(searchWord, index)) != -1)
            {
                count++;
                index += searchWord.Length;
            }

            return count;
        }

        // Метод 3: С использованием регулярных выражений
        private int CountWithRegex(string text, string pattern, RegexOptions options = RegexOptions.None)
        {
            return Regex.Matches(text, pattern, options).Count;
        }

        // Метод для получения статистики словаря
        public DictionaryInfo GetDictionaryInfo()
        {
            return new DictionaryInfo
            {
                TotalWords = _badWords.Count,
                IsLoaded = _isDictionaryLoaded,
                Source = _badWords.Count > 0 ?
                    (_badWords.Any(w => w.Contains("мат1") || w.Contains("брань1")) ?
                    "Default" : "File or Enum") : "Not Loaded",
                SampleWords = _badWords.Take(5).ToList()
            };
        }

        // Дополнительные методы для работы со словарем
        public List<string> GetAllBadWords()
        {
            return new List<string>(_badWords);
        }

        public bool ContainsBadWord(string text)
        {
            if (string.IsNullOrEmpty(text) || !_isDictionaryLoaded)
                return false;

            string textLower = text.ToLower();

            foreach (var badWord in _badWords)
            {
                if (textLower.Contains(badWord))
                {
                    return true;
                }
            }

            return false;
        }

        public int CountBadWordsInText(string text)
        {
            if (string.IsNullOrEmpty(text) || !_isDictionaryLoaded)
                return 0;

            string textLower = text.ToLower();
            int total = 0;

            foreach (var badWord in _badWords)
            {
                int index = 0;
                while ((index = textLower.IndexOf(badWord, index)) != -1)
                {
                    if (IsWholeWord(textLower, index, badWord.Length))
                    {
                        total++;
                    }
                    index += badWord.Length;
                }
            }

            return total;
        }
    }

    // Классы для хранения результатов остаются без изменений
    public class BadWordsAnalysis
    {
        public Dictionary<string, int> FoundWords { get; set; } = new Dictionary<string, int>();
        public int TotalCount { get; set; }
        public List<WordContext> WordsWithContext { get; set; } = new List<WordContext>();
        public bool HasBadWords => TotalCount > 0;
    }

    public class WordContext
    {
        public string Word { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Context { get; set; } = string.Empty;
    }

    public class DictionaryInfo
    {
        public int TotalWords { get; set; }
        public bool IsLoaded { get; set; }
        public string Source { get; set; } = string.Empty;
        public List<string> SampleWords { get; set; } = new List<string>();
    }

    public class AnalysisResult
    {
        // Старые свойства для .NET анализа
        public int CountIgnoreCase { get; set; }
        public int CountWholeWord { get; set; }
        public int CountCaseSensitive { get; set; }
        public int CountAllVariants { get; set; }
        public int CountDotNet { get; set; }
        public int CountAspDotNet { get; set; }
        public int TotalCharacters { get; set; }
        public string? Content { get; set; }

        // Новые свойства для нецензурных слов
        public Dictionary<string, int> BadWordsFound { get; set; } = new Dictionary<string, int>();
        public int TotalBadWordsCount { get; set; }
        public List<WordContext> BadWordsWithContext { get; set; } = new List<WordContext>();
        public bool HasBadWords => TotalBadWordsCount > 0;

        public void PrintResults()
        {
            Console.WriteLine("=== РЕЗУЛЬТАТЫ АНАЛИЗА ===");
            Console.WriteLine($"1. Все варианты (.NET, NET): {CountAllVariants} вхождений");
            Console.WriteLine($"2. Только '.NET': {CountDotNet} вхождений");
            Console.WriteLine($"3. Только 'ASP.NET': {CountAspDotNet} вхождений");

            if (HasBadWords)
            {
                Console.WriteLine($"\n=== НЕЦЕНЗУРНЫЕ СЛОВА ===");
                Console.WriteLine($"Общее количество: {TotalBadWordsCount}");
                Console.WriteLine($"Уникальных слов: {BadWordsFound.Count}");

                foreach (var word in BadWordsFound.OrderByDescending(w => w.Value).Take(10))
                {
                    Console.WriteLine($"  {word.Key}: {word.Value} вхождений");
                }

                if (BadWordsFound.Count > 10)
                {
                    Console.WriteLine($"  ... и еще {BadWordsFound.Count - 10} слов");
                }
            }
            else
            {
                Console.WriteLine("\n✓ Нецензурные слова не обнаружены");
            }

            Console.WriteLine($"\nОбщее количество символов: {TotalCharacters}");
        }
    }
}