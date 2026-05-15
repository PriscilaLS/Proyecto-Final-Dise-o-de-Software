using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ClientLocal.Helpers;

public class ClipboardGuard
{
    private string? _internalClipboard;
    private readonly TextBox _editor;

    public ClipboardGuard(TextBox editor)
    {
        _editor = editor;
        _editor.AddHandler(
            InputElement.KeyDownEvent,
            OnEditorKeyDown,
            RoutingStrategies.Tunnel
        );
    }

    private async void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        bool isCmd = e.KeyModifiers.HasFlag(KeyModifiers.Meta) ||
                     e.KeyModifiers.HasFlag(KeyModifiers.Control);

        if (isCmd && e.Key == Key.C || isCmd && e.Key == Key.X)
        {
            _internalClipboard = _editor.SelectedText;
        }

        if (isCmd && e.Key == Key.V)
        {
            e.Handled = true;

            var clipboard = TopLevel.GetTopLevel(_editor)!.Clipboard as Avalonia.Input.IAsyncDataTransfer;
            string? clipText = clipboard != null ? await clipboard.TryGetTextAsync() : null;

            if (clipText != null && clipText == _internalClipboard)
            {
                int caret = _editor.CaretIndex;
                string current = _editor.Text ?? "";
                _editor.Text = current.Insert(caret, clipText);
                _editor.CaretIndex = caret + clipText.Length;
            }
        }
    }
}