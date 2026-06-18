using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ClientLocal.Models.Submission;
using ClientLocal.Models.Tasks;
using ClientLocal.Services.Api;
using ClientLocal.Services.Editor;
using ClientLocal.Services.Files;
using ClientLocal.Services.Session;

namespace ClientLocal.Views.Auth
{
    public partial class SubmitTestView : UserControl
    {
        private readonly SessionService _sessionService;
        private readonly TaskDto _task;
        private readonly SubmissionRepository _submissionRepository;
        private readonly CompressionService _compressionService;
        private readonly IIntegrityService _integrityService;

        private TextBlock? _titleTextBlock;
        private TextBox? _projectPathTextBox;
        private TextBlock? _statusTextBlock;
        private TextBlock? _resultTextBlock;
        private TextBlock? _historyStatusTextBlock;
        private ListBox? _versionsListBox;

        public event Action? BackRequested;
        public event Action? BackToCoursesRequested;
        public event Action? IdeRequested;

        public SubmitTestView(SessionService sessionService, TaskDto task)
        {
            InitializeComponent();

            _sessionService = sessionService;
            _task = task;
            _submissionRepository = new SubmissionRepository(ApiClientFactory.Create(_sessionService));
            _compressionService = new CompressionService();
            _integrityService = FileIntegrityService.Instance;

            _titleTextBlock = this.FindControl<TextBlock>("TitleTextBlock");
            _projectPathTextBox = this.FindControl<TextBox>("ProjectPathTextBox");
            _statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
            _resultTextBlock = this.FindControl<TextBlock>("ResultTextBlock");
            _historyStatusTextBlock = this.FindControl<TextBlock>("HistoryStatusTextBlock");
            _versionsListBox = this.FindControl<ListBox>("VersionsListBox");

            if (_titleTextBlock != null)
                _titleTextBlock.Text = $"Entregar: {_task.Title}";

            Loaded += SubmitTestView_Loaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void SubmitTestView_Loaded(object? sender, RoutedEventArgs e)
        {
            await LoadHistoryAsync();
        }

        private async void PickFolderButton_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null)
                return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Selecciona la carpeta del proyecto",
                AllowMultiple = false
            });

            var selected = folders.FirstOrDefault();
            if (selected != null && _projectPathTextBox != null)
                _projectPathTextBox.Text = selected.Path.LocalPath;
        }

        private void SignButton_Click(object? sender, RoutedEventArgs e)
        {
            ResetMessages(clearResult: true);

            var projectPath = _projectPathTextBox?.Text?.Trim() ?? string.Empty;

            if (!ValidateProjectFolder(projectPath, out var pythonFiles))
                return;

            foreach (var file in pythonFiles)
            {
                var content = File.ReadAllText(file);
                _integrityService.Sign(file, content);
            }

            SetSuccess($"Proyecto firmado correctamente. Archivos firmados: {pythonFiles.Length}");
        }

        private void ValidateButton_Click(object? sender, RoutedEventArgs e)
        {
            ResetMessages(clearResult: true);

            var projectPath = _projectPathTextBox?.Text?.Trim() ?? string.Empty;

            if (!ValidateProjectFolder(projectPath, out var pythonFiles))
                return;

            foreach (var file in pythonFiles)
            {
                var content = File.ReadAllText(file);
                var isValid = _integrityService.Validate(file, content);

                if (!isValid)
                {
                    SetError($"Archivo con firma invalida: {Path.GetFileName(file)}");
                    return;
                }
            }

            SetSuccess("Validacion correcta. Todas las firmas son validas.");
        }

        private async void SubmitButton_Click(object? sender, RoutedEventArgs e)
        {
            ResetMessages(clearResult: true);
            if (sender is Button submitButton)
                submitButton.IsEnabled = false;

            var projectPath = _projectPathTextBox?.Text?.Trim() ?? string.Empty;

            if (!ValidateProjectFolder(projectPath, out var pythonFiles))
            {
                if (sender is Button invalidButton)
                    invalidButton.IsEnabled = true;
                return;
            }

            foreach (var file in pythonFiles)
            {
                var content = File.ReadAllText(file);
                var isValid = _integrityService.Validate(file, content);

                if (!isValid)
                {
                    SetError($"No se puede enviar. Firma invalida en: {Path.GetFileName(file)}");
                    if (sender is Button invalidSignatureButton)
                        invalidSignatureButton.IsEnabled = true;
                    return;
                }
            }

            string? zipPath = null;

            try
            {
                SetSuccess("Preparando ZIP para la entrega...");
                zipPath = await Task.Run(() => _compressionService.CreateProjectZip(projectPath));

                SetSuccess("Enviando entrega al backend...");
                var response = await _submissionRepository.SubmitProjectAsync(_task.Id, zipPath);

                if (!string.IsNullOrWhiteSpace(response.Error))
                {
                    SetError(response.Error);
                    return;
                }

                if (_resultTextBlock != null)
                {
                    _resultTextBlock.Text =
                        $"Entrega registrada correctamente.\n\n" +
                        $"ID de entrega: {response.SubmissionId}\n" +
                        $"ID de version: {response.VersionId}\n" +
                        $"Version enviada: v{response.VersionNumber}\n" +
                        $"Fecha y hora: {response.SubmittedAt}\n" +
                        $"Puntualidad: {response.StatusText}";
                }

                SetSuccess("Entrega enviada correctamente.");
                await LoadHistoryAsync();
            }
            catch (TaskCanceledException)
            {
                SetError("Error al enviar la entrega: el backend no respondio a tiempo.");
            }
            catch (Exception ex)
            {
                SetError($"Error al enviar la entrega: {ex.Message}");
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(zipPath))
                {
                    try { File.Delete(zipPath); } catch { }
                }

                if (sender is Button finalButton)
                    finalButton.IsEnabled = true;
            }
        }

        private async void RefreshHistoryButton_Click(object? sender, RoutedEventArgs e)
        {
            await LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            if (_historyStatusTextBlock != null)
                _historyStatusTextBlock.Text = "Cargando historial...";

            try
            {
                var versions = await _submissionRepository.GetMySubmissionsByTaskAsync(_task.Id);

                if (_versionsListBox != null)
                    _versionsListBox.ItemsSource = versions;

                if (_historyStatusTextBlock != null)
                {
                    if (versions.Count == 0)
                    {
                        _historyStatusTextBlock.Text = "Todavia no hay entregas para esta tarea.";
                    }
                    else
                    {
                        var latest = versions[0];
                        _historyStatusTextBlock.Text =
                            $"Version mas reciente: v{latest.VersionNumber} | {latest.SubmittedAt} | {latest.StatusText}\n" +
                            $"Total de versiones: {versions.Count}";
                    }
                }
            }
            catch (Exception ex)
            {
                if (_versionsListBox != null)
                    _versionsListBox.ItemsSource = new List<SubmissionVersionDto>();

                if (_historyStatusTextBlock != null)
                    _historyStatusTextBlock.Text = $"No se pudo cargar el historial: {ex.Message}";
            }
        }

        private void OpenIdeButton_Click(object? sender, RoutedEventArgs e)
        {
            IdeRequested?.Invoke();
        }

        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke();
        }

        private void BackToCoursesButton_Click(object? sender, RoutedEventArgs e)
        {
            BackToCoursesRequested?.Invoke();
        }

        private bool ValidateProjectFolder(string projectPath, out string[] pythonFiles)
        {
            pythonFiles = Array.Empty<string>();

            if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
            {
                SetError("Selecciona una carpeta valida.");
                return false;
            }

            pythonFiles = Directory.GetFiles(projectPath, "*.py", SearchOption.AllDirectories);

            if (pythonFiles.Length == 0)
            {
                SetError("La carpeta no contiene archivos .py.");
                return false;
            }

            return true;
        }

        private void ResetMessages(bool clearResult)
        {
            if (_statusTextBlock != null)
            {
                _statusTextBlock.Text = string.Empty;
                _statusTextBlock.Foreground = Brushes.Red;
            }

            if (clearResult && _resultTextBlock != null)
                _resultTextBlock.Text = string.Empty;
        }

        private void SetError(string message)
        {
            if (_statusTextBlock != null)
            {
                _statusTextBlock.Foreground = Brushes.Red;
                _statusTextBlock.Text = message;
            }
        }

        private void SetSuccess(string message)
        {
            if (_statusTextBlock != null)
            {
                _statusTextBlock.Foreground = Brushes.Green;
                _statusTextBlock.Text = message;
            }
        }
    }
}
