using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace ClientLocal.Services.Decorator;

public class HighlightedScriptDecorator : ScriptDecorator
{
    public HighlightedScriptDecorator(IScript script) : base(script)
    {
    }

    public List<HighlightSegment> GetHighlightedSegments()
    {
        var result = new List<HighlightSegment>();
        var text = GetText().Replace("\r\n", "\n");
        var lines = text.Split('\n');

        foreach (var line in lines)
        {
            AppendHighlightedLine(result, line);
            result.Add(new HighlightSegment("\n", Brushes.White, FontWeight.Normal));
        }

        return result;
    }

    private void AppendHighlightedLine(List<HighlightSegment> result, string line)
    {
        var keywords = new[]
        {
            "def", "class", "if", "else", "elif", "for", "while",
            "return", "import", "from", "try", "except", "with",
            "as", "pass", "break", "continue", "lambda", "in",
            "is", "not", "and", "or", "True", "False", "None"
        };

        var i = 0;

        while (i < line.Length)
        {
            if (line[i] == '#')
            {
                result.Add(new HighlightSegment(line[i..], Brushes.LightGreen, FontWeight.Normal));
                return;
            }

            if (line[i] == '"' || line[i] == '\'')
            {
                var quote = line[i];
                var start = i;
                i++;

                while (i < line.Length)
                {
                    if (line[i] == quote && line[i - 1] != '\\')
                    {
                        i++;
                        break;
                    }
                    i++;
                }

                result.Add(new HighlightSegment(line[start..i], Brushes.Goldenrod, FontWeight.Normal));
                continue;
            }

            if (char.IsDigit(line[i]))
            {
                var start = i;
                i++;

                while (i < line.Length && (char.IsDigit(line[i]) || line[i] == '.'))
                    i++;

                result.Add(new HighlightSegment(line[start..i], Brushes.HotPink, FontWeight.Normal));
                continue;
            }

            if (char.IsLetter(line[i]) || line[i] == '_')
            {
                var start = i;
                i++;

                while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_'))
                    i++;

                var token = line[start..i];
                var isKeyword = Array.Exists(keywords, k => k == token);

                result.Add(new HighlightSegment(
                    token,
                    isKeyword ? Brushes.LightSkyBlue : Brushes.White,
                    isKeyword ? FontWeight.Bold : FontWeight.Normal
                ));

                continue;
            }

            result.Add(new HighlightSegment(line[i].ToString(), Brushes.White, FontWeight.Normal));
            i++;
        }
    }
}