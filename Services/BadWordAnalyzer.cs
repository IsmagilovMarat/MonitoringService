using DotnetBadWordDetector;
using MonitoringServiceCore.Database.BadWord;
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
                var badWordsAnalysis = AnalyzeBadWordsWithPositions(content);
                result.BadWordsFound = badWordsAnalysis.FoundWords;
                result.TotalBadWordsCount = badWordsAnalysis.TotalCount;
                result.BadWordsWithContext = badWordsAnalysis.WordsWithContext;
                result.WordPositions = badWordsAnalysis.WordPositions;
            }

            // ML анализ
            if (_useMLDetector && _mlDetector != null)
            {
                try
                {
                    string plainText = Regex.Replace(content, "<.*?>", string.Empty);

                    // Получаем позиции матных слов от ML детектора
                    var mlPositions = GetMLWordPositions(plainText, content);

                    result.MLDetectionResult = new MLDetectionResult
                    {
                        HasProfanity = mlPositions.Any(),
                        Probability = mlPositions.Any() ? _mlDetector.GetPhraseProfanityProbability(plainText) : 0,
                        FoundWords = mlPositions.GroupBy(p => p.Word).ToDictionary(g => g.Key, g => g.Count()),
                        WordPositions = mlPositions,
                        IsMLBased = true
                    };

                    // Объединяем результаты из словаря и ML
                    if (mlPositions.Any() && !result.HasBadWords)
                    {
                        result.MLOnlyDetected = true;
                        result.TotalBadWordsCount = Math.Max(result.TotalBadWordsCount, mlPositions.Count);

                        foreach (var pos in mlPositions)
                        {
                            if (!result.BadWordsFound.ContainsKey(pos.Word))
                            {
                                result.BadWordsFound[pos.Word] = 1;
                            }
                            else
                            {
                                result.BadWordsFound[pos.Word]++;
                            }
                            result.WordPositions.Add(pos);
                        }
                    }
                    else if (mlPositions.Any())
                    {
                        // Добавляем ML позиции к существующим
                        foreach (var pos in mlPositions)
                        {
                            result.WordPositions.Add(pos);
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
                var badWordsAnalysis = AnalyzeBadWordsWithPositions(content);
                result.BadWordsFound = badWordsAnalysis.FoundWords;
                result.TotalBadWordsCount = badWordsAnalysis.TotalCount;
                result.BadWordsWithContext = badWordsAnalysis.WordsWithContext;
                result.WordPositions = badWordsAnalysis.WordPositions;
            }

            return result;
        }

        // НОВЫЙ МЕТОД: Получение позиций матных слов от ML детектора
        private List<WordPosition> GetMLWordPositions(string plainText, string originalHtml)
        {
            var positions = new List<WordPosition>();
            var words = plainText.Split(new[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '(', ')', '[', ']', '{', '}' },
                StringSplitOptions.RemoveEmptyEntries);

            int currentIndex = 0;

            foreach (var word in words)
            {
                if (_mlDetector!.IsProfane(word))
                {
                    // Ищем позицию слова в оригинальном HTML
                    int posInHtml = originalHtml.IndexOf(word, currentIndex, StringComparison.OrdinalIgnoreCase);
                    if (posInHtml >= 0)
                    {
                        positions.Add(new WordPosition
                        {
                            Word = word.ToLower(),
                            Position = posInHtml,
                            LineNumber = GetLineNumber(originalHtml, posInHtml),
                            Context = GetContextAround(originalHtml, posInHtml, word.Length)
                        });
                        currentIndex = posInHtml + 1;
                    }
                }
            }

            return positions;
        }

        // НОВЫЙ МЕТОД: Анализ с позициями слов
        public BadWordsAnalysis AnalyzeBadWordsWithPositions(string content)
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
                        var positions = FindWordPositions(contentLower, badWord);
                        int count = positions.Count;

                        if (count > 0)
                        {
                            analysis.FoundWords[badWord] = count;
                            analysis.TotalCount += count;

                            foreach (var pos in positions)
                            {
                                analysis.WordPositions.Add(new WordPosition
                                {
                                    Word = badWord,
                                    Position = pos,
                                    LineNumber = GetLineNumber(content, pos),
                                    Context = GetContextAround(content, pos, badWord.Length)
                                });
                            }

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

            analysis.WordPositions = analysis.WordPositions.OrderBy(p => p.Position).ToList();
            analysis.WordsWithContext = analysis.WordsWithContext
                .OrderByDescending(w => w.Count)
                .ToList();

            return analysis;
        }

        // НОВЫЙ МЕТОД: Поиск всех позиций слова в тексте
        private List<int> FindWordPositions(string text, string word)
        {
            var positions = new List<int>();
            int index = 0;

            while ((index = text.IndexOf(word, index, StringComparison.Ordinal)) != -1)
            {
                if (IsWholeWord(text, index, word.Length))
                {
                    positions.Add(index);
                }
                index += word.Length;
            }

            return positions;
        }

        // НОВЫЙ МЕТОД: Получение номера строки по позиции
        private int GetLineNumber(string text, int position)
        {
            if (position < 0 || position >= text.Length)
                return 0;

            int lineNumber = 1;
            for (int i = 0; i < position; i++)
            {
                if (text[i] == '\n')
                    lineNumber++;
            }
            return lineNumber;
        }

        // НОВЫЙ МЕТОД: Получение контекста вокруг позиции
        private string GetContextAround(string text, int position, int wordLength, int contextChars = 50)
        {
            int start = Math.Max(0, position - contextChars);
            int end = Math.Min(text.Length, position + wordLength + contextChars);

            string context = text.Substring(start, end - start);
            context = context.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");

            // Выделяем найденное слово
            int wordPosInContext = position - start;
            string wordInContext = text.Substring(position, wordLength);
            context = context.Substring(0, wordPosInContext) +
                     $"<mark class='bad-word'>{wordInContext}</mark>" +
                     context.Substring(wordPosInContext + wordLength);

            return $"...{context}...";
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
    

   
}