using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClientLocal.Controls;

namespace ClientLocal.Views.Auth
{
    public partial class ClipboardTestView : UserControl
    {
        private TextBox? _editorTextBox;
        private TextBlock? _statusTextBlock;
        private ClipboardGuard? _clipboardGuard;

        public event Action? BackRequested;

        public ClipboardTestView()
        {
            InitializeComponent();

            _editorTextBox = this.FindControl<TextBox>("EditorTextBox");
            _statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");

            if (_editorTextBox != null)
            {
                _clipboardGuard = new ClipboardGuard(_editorTextBox);
                _editorTextBox.ContextFlyout = CreateClipboardMenu(_clipboardGuard);
            }

            if (_statusTextBlock != null)
            {
                _statusTextBlock.Text =
                    "Prueba: copia texto dentro del editor y pega. Luego intenta copiar desde fuera y pegar aquí.";
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private MenuFlyout CreateClipboardMenu(ClipboardGuard guard)
        {
            var menu = new MenuFlyout();

            var cut = new MenuItem { Header = "Cortar" };
            cut.Click += (_, _) => guard.CutSelection();

            var copy = new MenuItem { Header = "Copiar" };
            copy.Click += (_, _) => guard.CopySelection();

            var paste = new MenuItem { Header = "Pegar" };
            paste.Click += (_, _) => guard.PasteInternalOrWarn();

            menu.Items.Add(cut);
            menu.Items.Add(copy);
            menu.Items.Add(paste);

            return menu;
        }

        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke();
        }
    }
}
