using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CutsceneEngine
{
    public class TypingEffect
    {
        public static IEnumerable<string> GetTypingText(string text)
        {
            var sb = new StringBuilder();
            var combinationBuilder = new StringBuilder();

            var enumerator = StringInfo.GetTextElementEnumerator(text);
            while (enumerator.MoveNext())
            {
                var grapheme = enumerator.GetTextElement();

                if (IsDecomposable(grapheme))
                {
                    var decomposed = grapheme.Normalize(NormalizationForm.FormKD);
                    combinationBuilder.Clear();

                    for (int i = 0; i < decomposed.Length; i++)
                    {
                        string combined;
                        if (i == 0)
                        {
                            combinationBuilder.Append(NormalizeChar(decomposed[i]));
                            combined = combinationBuilder.ToString().Normalize(NormalizationForm.FormC);
                        }
                        else
                        {
                            combinationBuilder.Append(decomposed[i]);
                            combined = combinationBuilder.ToString().Normalize(NormalizationForm.FormKC);
                        }
                        yield return sb + combined;
                    }

                    sb.Append(grapheme);
                }
                else
                {
                    sb.Append(grapheme);
                    yield return sb.ToString();
                }
            }
        }

        public static float CalcDuration(string text, TypingEffectParameter param)
        {
            var normalizedText = text.Normalize(NormalizationForm.FormD);
            var normalizedTextLength = normalizedText.Length;
            var duration = 0f;
            
            var timePerChar = param.timeMethod == TypingEffectParameter.TimeMethod.PerChar
                ? param.timePerChar
                : param.totalDuration / normalizedTextLength;

            duration += timePerChar * normalizedTextLength;
            duration += param.fadeOutTime;
            
            foreach (var c in normalizedText)
            {
                if (param.HasAdditionalCharSetting(c, out var time))
                {
                    duration += time - timePerChar;
                }
            }

            return duration;
        }

        static bool IsDecomposable(string grapheme)
        {
            if (string.IsNullOrEmpty(grapheme))
            {
                return false;
            }

            return grapheme.Normalize(NormalizationForm.FormD).Length > 1;
        }

        static readonly Dictionary<char, char> KoreanConsonantToCompatible = new Dictionary<char, char>
        {
            { '\u1100', '\u3131' },
            { '\u1101', '\u3132' },
            { '\u1102', '\u3134' },
            { '\u1103', '\u3137' },
            { '\u1104', '\u3138' },
            { '\u1105', '\u3139' },
            { '\u1106', '\u3141' },
            { '\u1107', '\u3142' },
            { '\u1108', '\u3143' },
            { '\u1109', '\u3145' },
            { '\u110A', '\u3146' },
            { '\u110B', '\u3147' },
            { '\u110C', '\u3148' },
            { '\u110D', '\u3149' },
            { '\u110E', '\u314A' },
            { '\u110F', '\u314B' },
            { '\u1110', '\u314C' },
            { '\u1111', '\u314D' },
            { '\u1112', '\u314E' },
        };
        
        static char NormalizeChar(char input)
        {
            return KoreanConsonantToCompatible.GetValueOrDefault(input, input);
        }
    }
}