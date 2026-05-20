using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClientLocal.Helpers;

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

        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke();
        }
    }
}