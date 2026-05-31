using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ClientLocal.Services.Decorator;

namespace ClientLocal.Views.Decorator
{
    public partial class DecoratorDemoView : UserControl
    {
        private TextBlock? _pathTextBlock;
        private TextBlock? _statusTextBlock;
        private TextBlock? _signatureInfoTextBlock;
        private TextBox? _plainTextBox;
        private SelectableTextBlock? _highlightedTextBlock;

        private BasicScript? _basicScript;
        private SignedScriptDecorator? _signedScript;
        private HighlightedScriptDecorator? _highlightedScript;

        public DecoratorDemoView()
        {
            InitializeComponent();

            _pathTextBlock = this.FindControl<TextBlock>("PathTextBlock");
            _statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
            _signatureInfoTextBlock = this.FindControl<TextBlock>("SignatureInfoTextBlock");
            _plainTextBox = this.FindControl<TextBox>("PlainTextBox");
            _highlightedTextBlock = this.FindControl<SelectableTextBlock>("HighlightedTextBlock");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void LoadScriptButton_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null)
                return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar script Python",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Python Script")
                    {
                        Patterns = new[] { "*.py" }
                    }
                ]
            });

            var selected = files.Count > 0 ? files[0] : null;
            if (selected == null)
                return;

            var path = selected.Path.LocalPath;
            if (!File.Exists(path))
                return;

            _basicScript = new BasicScript(path);
            _signedScript = new SignedScriptDecorator(_basicScript);
            _highlightedScript = new HighlightedScriptDecorator(_basicScript);

            RefreshView();
            SetStatus("Estado de firma: No firmado o pendiente de verificación", Brushes.Orange);
        }

        private void SignButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_signedScript == null)
            {
                SetStatus("Primero debes cargar un script.", Brushes.Red);
                return;
            }

            _signedScript.Sign();
            RefreshSignatureInfo();
            SetStatus("Estado de firma: Script firmado correctamente", Brushes.LightGreen);
        }

        private void VerifyButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_signedScript == null)
            {
                SetStatus("Primero debes cargar un script.", Brushes.Red);
                return;
            }

            if (!_signedScript.HasStoredSignature())
            {
                SetStatus("Estado de firma: No firmado", Brushes.Orange);
                RefreshSignatureInfo();
                return;
            }

            var isValid = _signedScript.VerifySignature();

            if (isValid)
                SetStatus("Estado de firma: Firma válida", Brushes.LightGreen);
            else
                SetStatus("Estado de firma: Firma inválida. El archivo cambió fuera de la aplicación.", Brushes.Red);

            RefreshSignatureInfo();
        }

        private void RegenerateButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_signedScript == null)
            {
                SetStatus("Primero debes cargar un script.", Brushes.Red);
                return;
            }

            _signedScript.RegenerateSignature();
            RefreshSignatureInfo();
            SetStatus("Estado de firma: Firma regenerada correctamente", Brushes.LightGreen);
        }

        private void RefreshView()
        {
            if (_basicScript == null)
                return;

            if (_pathTextBlock != null)
                _pathTextBlock.Text = $"Ruta: {_basicScript.GetPath()}";

            if (_plainTextBox != null)
                _plainTextBox.Text = _basicScript.GetText();

            RefreshHighlightedText();
            RefreshSignatureInfo();
        }

        private void RefreshHighlightedText()
        {
            if (_highlightedScript == null || _highlightedTextBlock == null)
                return;

            _highlightedTextBlock.Inlines.Clear();

            var segments = _highlightedScript.GetHighlightedSegments();

            foreach (var segment in segments)
            {
                _highlightedTextBlock.Inlines.Add(new Run(segment.Text)
                {
                    Foreground = segment.Color,
                    FontWeight = segment.Weight
                });
            }
        }

        private void RefreshSignatureInfo()
        {
            if (_signedScript == null || _signatureInfoTextBlock == null)
                return;

            _signatureInfoTextBlock.Text = $"Firma almacenada en: {_signedScript.GetSignatureStorePath()}";
        }

        private void SetStatus(string text, IBrush color)
        {
            if (_statusTextBlock == null)
                return;

            _statusTextBlock.Text = text;
            _statusTextBlock.Foreground = color;
        }
    }
}