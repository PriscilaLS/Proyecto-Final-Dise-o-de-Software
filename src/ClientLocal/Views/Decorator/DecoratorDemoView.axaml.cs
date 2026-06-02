using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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
        private bool _isRefreshingView;
        private bool _isHandlingExternalChange;
        private DateTime _lastEditorSaveUtc = DateTime.MinValue;
        private FileSystemWatcher? _fileWatcher;

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
            StartWatchingFile(path);

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

            if (!ReloadScriptFromDisk())
                return;

            if (_signedScript!.HasStoredSignature())
            {
                SetStatus("Estado de firma: Ya existe una firma. Usa Regenerar Firma para reemplazarla.", Brushes.Orange);
                return;
            }

            _signedScript!.Sign();
            RefreshView();
            SetStatus("Estado de firma: Script firmado correctamente", Brushes.LightGreen);
        }

        private void VerifyButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_signedScript == null)
            {
                SetStatus("Primero debes cargar un script.", Brushes.Red);
                return;
            }

            if (!ReloadScriptFromDisk())
                return;

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

            if (!ReloadScriptFromDisk())
                return;

            _signedScript!.RegenerateSignature();
            RefreshView();
            SetStatus("Estado de firma: Firma regenerada correctamente", Brushes.LightGreen);
        }

        private bool ReloadScriptFromDisk()
        {
            if (_basicScript == null)
                return false;

            var path = _basicScript.GetPath();
            if (!File.Exists(path))
            {
                SetStatus("El archivo ya no existe en la ruta original.", Brushes.Red);
                return false;
            }

            try
            {
                RebuildDecorators(path);
                RefreshView();
                return true;
            }
            catch (IOException)
            {
                SetStatus("El archivo está siendo usado por otra aplicación. Intenta verificar de nuevo.", Brushes.Orange);
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                SetStatus("No se tienen permisos para leer el archivo.", Brushes.Red);
                return false;
            }
        }

        private void RefreshView()
        {
            if (_basicScript == null)
                return;

            _isRefreshingView = true;

            if (_pathTextBlock != null)
                _pathTextBlock.Text = $"Ruta: {_basicScript.GetPath()}";

            if (_plainTextBox != null)
                _plainTextBox.Text = _basicScript.GetText();

            _isRefreshingView = false;

            RefreshHighlightedText();
            RefreshSignatureInfo();
        }

        private void PlainTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_isRefreshingView || _basicScript == null || _plainTextBox == null)
                return;

            var path = _basicScript.GetPath();

            if (!File.Exists(path))
            {
                SetStatus("El archivo ya no existe en la ruta original.", Brushes.Red);
                return;
            }

            try
            {
                _lastEditorSaveUtc = DateTime.UtcNow;
                File.WriteAllText(path, _plainTextBox.Text ?? string.Empty);
                RebuildDecorators(path);
                RefreshHighlightedText();
                RefreshSignatureInfo();
                SetStatus("Estado de firma: Cambios guardados. Verifica o regenera la firma.", Brushes.Orange);
            }
            catch (IOException)
            {
                SetStatus("No se pudo guardar porque el archivo está siendo usado por otra aplicación.", Brushes.Orange);
            }
            catch (UnauthorizedAccessException)
            {
                SetStatus("No se tienen permisos para guardar el archivo.", Brushes.Red);
            }
        }

        private void RebuildDecorators(string path)
        {
            _basicScript = new BasicScript(path);
            _signedScript = new SignedScriptDecorator(_basicScript);
            _highlightedScript = new HighlightedScriptDecorator(_basicScript);
        }

        private void StartWatchingFile(string path)
        {
            _fileWatcher?.Dispose();
            _fileWatcher = null;

            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);

            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
                return;

            _fileWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.FileName |
                               NotifyFilters.LastWrite |
                               NotifyFilters.Size |
                               NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += (_, _) => QueueExternalRefresh(path);
            _fileWatcher.Renamed += (_, _) => QueueExternalRefresh(path);
            _fileWatcher.Deleted += (_, _) => QueueExternalRefresh(path);
        }

        private async void QueueExternalRefresh(string path)
        {
            if ((DateTime.UtcNow - _lastEditorSaveUtc).TotalMilliseconds < 800)
                return;

            await Task.Delay(300);
            await Dispatcher.UIThread.InvokeAsync(() => RefreshFromExternalChange(path));
        }

        private void RefreshFromExternalChange(string path)
        {
            if (_isHandlingExternalChange || _basicScript == null)
                return;

            if (!string.Equals(_basicScript.GetPath(), path, StringComparison.OrdinalIgnoreCase))
                return;

            if (!File.Exists(path))
            {
                SetStatus("El archivo fue eliminado fuera de la aplicación.", Brushes.Red);
                return;
            }

            try
            {
                _isHandlingExternalChange = true;
                RebuildDecorators(path);
                RefreshView();

                if (_signedScript != null && _signedScript.HasStoredSignature())
                {
                    var isValid = _signedScript.VerifySignature();
                    SetStatus(
                        isValid
                            ? "Archivo actualizado desde disco. La firma sigue siendo válida."
                            : "Estado de firma: Firma inválida. El archivo cambió fuera de la aplicación.",
                        isValid ? Brushes.LightGreen : Brushes.Red);
                }
                else
                {
                    SetStatus("Archivo actualizado desde disco. Firma pendiente.", Brushes.Orange);
                }
            }
            catch (IOException)
            {
                SetStatus("El archivo cambió, pero todavía está siendo usado por otra aplicación. Intenta verificar de nuevo.", Brushes.Orange);
            }
            catch (UnauthorizedAccessException)
            {
                SetStatus("El archivo cambió, pero no se tienen permisos para leerlo.", Brushes.Red);
            }
            finally
            {
                _isHandlingExternalChange = false;
            }
        }

        private void RefreshHighlightedText()
        {
            if (_highlightedScript == null || _highlightedTextBlock == null)
                return;

            _highlightedTextBlock.Inlines ??= new InlineCollection();
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
