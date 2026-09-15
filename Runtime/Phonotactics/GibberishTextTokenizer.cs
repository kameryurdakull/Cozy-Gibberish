using System;
using System.Collections.Generic;
using System.Globalization;

namespace CozyGibberish
{
    internal enum GibberishPause
    {
        None,
        Word,
        Comma,
        Sentence,
        Question,
        Exclamation
    }

    internal readonly struct GibberishTextToken
    {
        public int CharacterCount { get; }
        public int SourceStart { get; }
        public int SourceLength { get; }
        public int WordIndex { get; }
        public int PhraseIndex { get; }
        public GibberishPause Pause { get; }

        public GibberishTextToken(
            int characterCount,
            int sourceStart,
            int sourceLength,
            int wordIndex,
            int phraseIndex,
            GibberishPause pause)
        {
            CharacterCount = characterCount;
            SourceStart = sourceStart;
            SourceLength = sourceLength;
            WordIndex = wordIndex;
            PhraseIndex = phraseIndex;
            Pause = pause;
        }

        public GibberishTextToken WithPause(GibberishPause pause)
        {
            return new GibberishTextToken(
                CharacterCount,
                SourceStart,
                SourceLength,
                WordIndex,
                PhraseIndex,
                pause);
        }
    }

    internal static class GibberishTextTokenizer
    {
        private const char TagOpen = '<';
        private const char TagClose = '>';
        private const char Apostrophe = '\'';
        private const char RightApostrophe = '\u2019';
        private const char Hyphen = '-';

        public static List<GibberishTextToken> Parse(
            string text,
            GibberishTokenizerMode mode)
        {
            var tokens = new List<GibberishTextToken>();
            if (string.IsNullOrEmpty(text))
            {
                return tokens;
            }

            var elementOffsets = StringInfo.ParseCombiningCharacters(text);
            var wordStart = -1;
            var wordEnd = -1;
            var graphemeCount = 0;
            var wordIndex = 0;
            var phraseIndex = 0;
            var insideTag = false;

            for (var elementIndex = 0; elementIndex < elementOffsets.Length; elementIndex++)
            {
                var offset = elementOffsets[elementIndex];
                var nextOffset = elementIndex + 1 < elementOffsets.Length
                    ? elementOffsets[elementIndex + 1]
                    : text.Length;
                var character = text[offset];

                if (character == TagOpen)
                {
                    insideTag = true;
                    continue;
                }

                if (insideTag)
                {
                    if (character == TagClose)
                    {
                        insideTag = false;
                    }

                    continue;
                }

                if (IsWordElement(text, offset, character, mode))
                {
                    if (wordStart < 0)
                    {
                        wordStart = offset;
                    }

                    wordEnd = nextOffset;
                    graphemeCount++;
                    continue;
                }

                if (graphemeCount > 0)
                {
                    var pause = ClassifyPause(character);
                    tokens.Add(new GibberishTextToken(
                        graphemeCount,
                        wordStart,
                        wordEnd - wordStart,
                        wordIndex++,
                        phraseIndex,
                        pause));
                    wordStart = -1;
                    wordEnd = -1;
                    graphemeCount = 0;

                    if (IsPhraseEnding(pause))
                    {
                        phraseIndex++;
                    }

                    continue;
                }

                if (tokens.Count == 0)
                {
                    continue;
                }

                var classifiedPause = ClassifyPause(character);
                var lastIndex = tokens.Count - 1;
                var previous = tokens[lastIndex];
                var strongest = (GibberishPause)Math.Max((int)previous.Pause, (int)classifiedPause);
                tokens[lastIndex] = previous.WithPause(strongest);
                if (IsPhraseEnding(classifiedPause))
                {
                    phraseIndex++;
                }
            }

            if (graphemeCount > 0)
            {
                tokens.Add(new GibberishTextToken(
                    graphemeCount,
                    wordStart,
                    wordEnd - wordStart,
                    wordIndex,
                    phraseIndex,
                    GibberishPause.Word));
            }

            return tokens;
        }

        private static bool IsWordElement(
            string text,
            int offset,
            char character,
            GibberishTokenizerMode mode)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(text, offset);
            var isLetterOrDigit =
                category == UnicodeCategory.UppercaseLetter ||
                category == UnicodeCategory.LowercaseLetter ||
                category == UnicodeCategory.TitlecaseLetter ||
                category == UnicodeCategory.ModifierLetter ||
                category == UnicodeCategory.OtherLetter ||
                category == UnicodeCategory.DecimalDigitNumber ||
                category == UnicodeCategory.LetterNumber ||
                category == UnicodeCategory.NonSpacingMark ||
                category == UnicodeCategory.SpacingCombiningMark;

            if (isLetterOrDigit || character == Apostrophe || character == RightApostrophe)
            {
                return true;
            }

            return mode == GibberishTokenizerMode.Fantasy && character == Hyphen;
        }

        private static GibberishPause ClassifyPause(char character)
        {
            switch (character)
            {
                case '.':
                case ':':
                case ';':
                    return GibberishPause.Sentence;
                case '?':
                    return GibberishPause.Question;
                case '!':
                    return GibberishPause.Exclamation;
                case ',':
                    return GibberishPause.Comma;
                default:
                    return char.IsWhiteSpace(character) ? GibberishPause.Word : GibberishPause.None;
            }
        }

        private static bool IsPhraseEnding(GibberishPause pause)
        {
            return pause == GibberishPause.Sentence ||
                   pause == GibberishPause.Question ||
                   pause == GibberishPause.Exclamation;
        }
    }
}
