using DotnetBadWordDetector;
using MonitoringServiceCore.Emuns;
using System.Text;
using System.Text.RegularExpressions;

namespace MonitoringServiceCore.Services
{
    public class BadWordAnalyzer
    {
        private List<string> _badWords = new List<string>();
        private readonly object _lockObject = new object();
        private bool _isDictionaryLoaded = false;
        private readonly ProfanityDetector? _mlDetector;
        private readonly bool _useMLDetector;

        public BadWordAnalyzer(bool useMLDetector = true)
        {
            _useMLDetector = useMLDetector;

            if (_useMLDetector)
            {
                try
                {
                    _mlDetector = new ProfanityDetector(allLocales: true);
                    Console.WriteLine("ML детектор нецензурной лексики успешно загружен");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки ML детектора: {ex.Message}");
                    Console.WriteLine("Будет использоваться только словарный метод");
                }
            }

            LoadBadWordsDictionary();
        }

        private void LoadBadWordsDictionary()
        {
            try
            {
                string dictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "Data", "russian_bad_words.txt");

                if (File.Exists(dictionaryPath))
                {
                    Encoding[] encodingsToTry =
                    {
                        Encoding.GetEncoding(1251),
                        Encoding.UTF8,
                        Encoding.GetEncoding(20866),
                        Encoding.Default
                    };

                    foreach (var encoding in encodingsToTry)
                    {
                        try
                        {
                            var lines = File.ReadAllLines(dictionaryPath, encoding);
                            if (lines.Length > 0)
                            {
                                _badWords = lines
                                    .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                                    .Select(line => line.Trim().ToLower())
                                    .ToList();

                                if (_badWords.Count > 0)
                                {
                                    Console.WriteLine($"Загружено {_badWords.Count} слов из файла. Кодировка: {encoding.EncodingName}");
                                    _isDictionaryLoaded = true;
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                if (!_isDictionaryLoaded)
                {
                    Console.WriteLine("Файл словаря не найден. Используется встроенный словарь.");
                    LoadDefaultBadWords();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке словаря: {ex.Message}");
                LoadDefaultBadWords();
            }
        }

        private void LoadDefaultBadWords()
        {
            _badWords = Enum.GetNames(typeof(BadWords)).ToList();
            _isDictionaryLoaded = true;
            Console.WriteLine($"Загружено {_badWords.Count} стандартных слов");
        }

        // Основной метод анализа контента (только нецензурная лексика)
        public async Task<AnalysisResult> AnalyzeContentAsync(string content)
        {
            var result = new AnalysisResult
            {
                TotalCharacters = content.Length,
                Content = content
            };

            // Анализ нецензурных слов через словарь
            if (_isDictionaryLoaded && _badWords.Count > 0)
            {
                var badWordsAnalysis = AnalyzeBadWords(content);
                result.BadWordsFound = badWordsAnalysis.FoundWords;
                result.TotalBadWordsCount = badWordsAnalysis.TotalCount;
                result.BadWordsWithContext = badWordsAnalysis.WordsWithContext;
            }

            // ML анализ
            if (_useMLDetector && _mlDetector != null)
            {
                try
                {
                    string plainText = Regex.Replace(content, "<.*?>", string.Empty);

                    bool hasProfanity = _mlDetector.IsPhraseProfane(plainText);
                    float profanityProbability = _mlDetector.GetPhraseProfanityProbability(plainText);

                    var words = plainText.Split(new[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?' },
                        StringSplitOptions.RemoveEmptyEntries);

                    var mlFoundWords = new Dictionary<string, int>();
                    foreach (var word in words)
                    {
                        if (_mlDetector.IsProfane(word))
                        {
                            string wordLower = word.ToLower();
                            if (mlFoundWords.ContainsKey(wordLower))
                                mlFoundWords[wordLower]++;
                            else
                                mlFoundWords[wordLower] = 1;
                        }
                    }

                    result.MLDetectionResult = new MLDetectionResult
                    {
                        HasProfanity = hasProfanity,
                        Probability = profanityProbability,
                        FoundWords = mlFoundWords,
                        IsMLBased = true
                    };

                    // Объединяем результаты из словаря и ML
                    if (hasProfanity && !result.HasBadWords)
                    {
                        result.MLOnlyDetected = true;
                        result.TotalBadWordsCount = Math.Max(result.TotalBadWordsCount, mlFoundWords.Sum(x => x.Value));

                        foreach (var word in mlFoundWords)
                        {
                            if (!result.BadWordsFound.ContainsKey(word.Key))
                            {
                                result.BadWordsFound[word.Key] = word.Value;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка ML анализа: {ex.Message}");
                }
            }

            return result;
        }

        // Синхронная версия для обратной совместимости
        public AnalysisResult AnalyzeContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentException("Контент не может быть пустым", nameof(content));

            var result = new AnalysisResult
            {
                TotalCharacters = content.Length,
                Content = content
            };

            if (_isDictionaryLoaded && _badWords.Count > 0)
            {
                var badWordsAnalysis = AnalyzeBadWords(content);
                result.BadWordsFound = badWordsAnalysis.FoundWords;
                result.TotalBadWordsCount = badWordsAnalysis.TotalCount;
                result.BadWordsWithContext = badWordsAnalysis.WordsWithContext;
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
                        int count = CountWordOccurrences(contentLower, badWord);

                        if (count > 0)
                        {
                            analysis.FoundWords[badWord] = count;
                            analysis.TotalCount += count;

                            var context = GetWordContext(content, badWord);
                            if (context != null)
                            {
                                analysis.WordsWithContext.Add(context);
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

            analysis.WordsWithContext = analysis.WordsWithContext
                .OrderByDescending(w => w.Count)
                .ToList();

            return analysis;
        }

        public string MaskProfanity(string text, char maskChar = '*')
        {
            if (string.IsNullOrEmpty(text) || (!_isDictionaryLoaded && _mlDetector == null))
                return text;

            string result = text;

            if (_useMLDetector && _mlDetector != null)
            {
                try
                {
                    result = _mlDetector.MaskProfanity(result, maskChar);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка ML маскировки: {ex.Message}");
                }
            }

            lock (_lockObject)
            {
                foreach (var badWord in _badWords)
                {
                    string masked = new string(maskChar, badWord.Length);
                    result = Regex.Replace(result, $@"\b{Regex.Escape(badWord)}\b", masked,
                        RegexOptions.IgnoreCase);
                }
            }

            return result;
        }

        public float GetProfanityProbability(string text)
        {
            if (_useMLDetector && _mlDetector != null)
            {
                try
                {
                    return _mlDetector.GetPhraseProfanityProbability(text);
                }
                catch
                {
                    return ContainsBadWord(text) ? 0.8f : 0.1f;
                }
            }
            return ContainsBadWord(text) ? 0.8f : 0.1f;
        }

        private int CountWordOccurrences(string text, string word)
        {
            int count = 0;
            int index = 0;

            while ((index = text.IndexOf(word, index, StringComparison.Ordinal)) != -1)
            {
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
            context = context.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");
            context = context.Replace(word, $"<strong>{word}</strong>", StringComparison.OrdinalIgnoreCase);

            return new WordContext
            {
                Word = word,
                Count = 1,
                Context = $"...{context}..."
            };
        }

        public DictionaryInfo GetDictionaryInfo()
        {
            return new DictionaryInfo
            {
                TotalWords = _badWords.Count,
                IsLoaded = _isDictionaryLoaded,
                Source = _badWords.Count > 0 ? "Dictionary" : "Not Loaded",
                SampleWords = _badWords.Take(10).ToList(),
                MLEnabled = _useMLDetector && _mlDetector != null
            };
        }

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
    }

    public class MLDetectionResult
    {
        public bool HasProfanity { get; set; }
        public float Probability { get; set; }
        public Dictionary<string, int> FoundWords { get; set; } = new();
        public bool IsMLBased { get; set; }
    }

    public class AnalysisResult
    {
        public int TotalCharacters { get; set; }
        public string? Content { get; set; }

        public Dictionary<string, int> BadWordsFound { get; set; } = new();
        public int TotalBadWordsCount { get; set; }
        public List<WordContext> BadWordsWithContext { get; set; } = new();
        public bool HasBadWords => TotalBadWordsCount > 0;

        public MLDetectionResult? MLDetectionResult { get; set; }
        public bool MLOnlyDetected { get; set; }

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
            }
            else
            {
                Console.WriteLine("\n✓ Нецензурные слова не обнаружены");
            }

            if (MLDetectionResult != null && MLDetectionResult.HasProfanity)
            {
                Console.WriteLine($"\n=== ML ДЕТЕКЦИЯ ===");
                Console.WriteLine($"Вероятность наличия нецензурной лексики: {MLDetectionResult.Probability:P}");
            }

            Console.WriteLine($"\nОбщее количество символов: {TotalCharacters}");
        }
    }

    public class DictionaryInfo
    {
        public int TotalWords { get; set; }
        public bool IsLoaded { get; set; }
        public string Source { get; set; } = string.Empty;
        public List<string> SampleWords { get; set; } = new();
        public bool MLEnabled { get; set; }
    }

    public class BadWordsAnalysis
    {
        public Dictionary<string, int> FoundWords { get; set; } = new();
        public int TotalCount { get; set; }
        public List<WordContext> WordsWithContext { get; set; } = new();
        public bool HasBadWords => TotalCount > 0;
    }

    public class WordContext
    {
        public string Word { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Context { get; set; } = string.Empty;
    }
}