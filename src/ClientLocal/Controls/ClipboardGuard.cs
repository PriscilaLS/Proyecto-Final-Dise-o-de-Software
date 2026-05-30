using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace ClientLocal.Controls;

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

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        bool isCmd = e.KeyModifiers.HasFlag(KeyModifiers.Meta) ||
                     e.KeyModifiers.HasFlag(KeyModifiers.Control);

        if (isCmd && e.Key == Key.C)
        {
            e.Handled = true;
            CopySelection();
            return;
        }

        if (isCmd && e.Key == Key.X)
        {
            e.Handled = true;
            CutSelection();
            return;
        }

        if (isCmd && e.Key == Key.V)
        {
            e.Handled = true;
            PasteInternalOrWarn();
        }
    }

    public async void CopySelection()
    {
        _internalClipboard = _editor.SelectedText;

        if (!string.IsNullOrEmpty(_internalClipboard))
        {
            var topLevel = TopLevel.GetTopLevel(_editor);
            if (topLevel?.Clipboard != null)
                await topLevel.Clipboard.SetTextAsync(_internalClipboard);
        }
    }

    public async void CutSelection()
    {
        CopySelection();

        if (string.IsNullOrEmpty(_internalClipboard))
            return;

        string current = _editor.Text ?? "";
        int start = Math.Min(_editor.SelectionStart, _editor.SelectionEnd);
        int end = Math.Max(_editor.SelectionStart, _editor.SelectionEnd);

        _editor.Text = current.Remove(start, end - start);
        _editor.CaretIndex = start;

        var topLevel = TopLevel.GetTopLevel(_editor);
        if (topLevel?.Clipboard != null)
            await topLevel.Clipboard.SetTextAsync(_internalClipboard);
    }

    public async void PasteInternalOrWarn()
    {
        var topLevel = TopLevel.GetTopLevel(_editor);
        string? systemClipboard = null;

        if (topLevel?.Clipboard != null)
            systemClipboard = await topLevel.Clipboard.TryGetTextAsync();

        if (!string.IsNullOrEmpty(_internalClipboard) && systemClipboard == _internalClipboard)
        {
            int caret = _editor.CaretIndex;
            string current = _editor.Text ?? "";
            _editor.Text = current.Insert(caret, _internalClipboard);
            _editor.CaretIndex = caret + _internalClipboard.Length;
            return;
        }

        var owner = topLevel as Window;
        if (owner != null)
        {
            await AlertDialog.Show(
                owner,
                "Pegado externo no permitido",
                "Esta aplicacion solo permite pegar texto que hayas copiado dentro del editor. " +
                "No se permite pegar contenido externo para garantizar la integridad de tu trabajo."
            );
        }
    }
}
