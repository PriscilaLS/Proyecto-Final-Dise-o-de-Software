using Avalonia.Media;

namespace ClientLocal.Services.Decorator;

public class HighlightSegment
{
    public string Text { get; }
    public IBrush Color { get; }
    public FontWeight Weight { get; }

    public HighlightSegment(string text, IBrush color, FontWeight weight)
    {
        Text = text;
        Color = color;
        Weight = weight;
    }
}