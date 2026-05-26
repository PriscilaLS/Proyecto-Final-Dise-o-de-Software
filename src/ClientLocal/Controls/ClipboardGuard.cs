using Avalonia.Controls;
using Avalonia.Input;
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

        if (isCmd && (e.Key == Key.C || e.Key == Key.X))
        {
            _internalClipboard = _editor.SelectedText;
            return;
        }

        if (isCmd && e.Key == Key.V)
        {
            if (!string.IsNullOrEmpty(_internalClipboard))
            {
                e.Handled = true;
                int caret = _editor.CaretIndex;
                string current = _editor.Text ?? "";
                _editor.Text = current.Insert(caret, _internalClipboard);
                _editor.CaretIndex = caret + _internalClipboard.Length;
                _internalClipboard = null;
            }
            else
            {
                e.Handled = true;
                var owner = TopLevel.GetTopLevel(_editor) as Window;
                if (owner != null)
                {
                    _ = AlertDialog.Show(
                        owner,
                        "Pegado externo no permitido",
                        "Esta aplicación solo permite pegar texto que hayas copiado dentro del editor. " +
                        "No se permite pegar contenido externo para garantizar la integridad de tu trabajo."
                    );
                }
            }
        }
    }
}