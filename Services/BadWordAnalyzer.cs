using MonitoringServiceCore.Database.BadWord;
using MonitoringServiceCore.Emuns;

namespace MonitoringServiceCore.Services
{
    public class BadWordAnalyzer
    {
        private List<string> _badWords = new List<string>();
        public BadWordAnalyzer()
        {

            LoadDefaultBadWords();
        }
        private void LoadDefaultBadWords()
        {
            _badWords = Enum.GetNames(typeof(BadWords)).ToList();
        }
        public AnalysisResult AnalyzeContent(string content)
        {
            var result = new AnalysisResult
            {
                TotalCharacters = content.Length,
                Content = content
            };

            var badWordsAnalysis = AnalyzeBadWordsWithPositions(content);
            result.BadWordsFound = badWordsAnalysis.FoundWords;
            result.TotalBadWordsCount = badWordsAnalysis.TotalCount;
            result.BadWordsWithContext = badWordsAnalysis.WordsWithContext;
            result.WordPositions = badWordsAnalysis.WordPositions;
            return result;
        }
        public BadWordsAnalysis AnalyzeBadWordsWithPositions(string content)
        {
            var analysis = new BadWordsAnalysis();
            string contentLower = content.ToLower();
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

            analysis.WordPositions = analysis.WordPositions.OrderBy(p => p.Position).ToList();
            analysis.WordsWithContext = analysis.WordsWithContext
                .OrderByDescending(w => w.Count)
                .ToList();

            return analysis;
        }
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
        private string GetContextAround(string text, int position, int wordLength, int contextChars = 50)
        {
            int start = Math.Max(0, position - contextChars);
            int end = Math.Min(text.Length, position + wordLength + contextChars);

            string context = text.Substring(start, end - start);
            context = context.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");

            int wordPosInContext = position - start;
            string wordInContext = text.Substring(position, wordLength);
            context = context.Substring(0, wordPosInContext) +
                     $"<mark class='bad-word'>{wordInContext}</mark>" +
                     context.Substring(wordPosInContext + wordLength);

            return $"...{context}...";
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
      
    }
    

   
}