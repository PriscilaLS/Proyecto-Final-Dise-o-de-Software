using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ClientLocal.Models.Courses;
using ClientLocal.Models.Tasks;
using ClientLocal.Services.Api;
using ClientLocal.Services.Session;

namespace ClientLocal.Views.Auth
{
    public partial class TasksTestView : UserControl
    {
        private readonly SessionService _sessionService;
        private readonly CourseDto _course;
        private readonly TaskRepository _taskRepository;

        private TextBlock? _titleTextBlock;
        private ListBox? _tasksListBox;
        private TextBlock? _detailsTextBlock;
        private TextBlock? _attachmentTextBlock;
        private TextBlock? _statusTextBlock;
        private Button? _downloadAttachmentButton;
        private TaskDto? _selectedTask;

        public event Action<TaskDto>? SubmitRequested;
        public event Action? BackToCoursesRequested;

        public TasksTestView(SessionService sessionService, CourseDto course)
        {
            InitializeComponent();

            _sessionService = sessionService;
            _course = course;
            _taskRepository = new TaskRepository(ApiClientFactory.Create(_sessionService));

            _titleTextBlock = this.FindControl<TextBlock>("TitleTextBlock");
            _tasksListBox = this.FindControl<ListBox>("TasksListBox");
            _detailsTextBlock = this.FindControl<TextBlock>("DetailsTextBlock");
            _attachmentTextBlock = this.FindControl<TextBlock>("AttachmentTextBlock");
            _statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
            _downloadAttachmentButton = this.FindControl<Button>("DownloadAttachmentButton");

            if (_titleTextBlock != null)
                _titleTextBlock.Text = $"Tareas de: {_course.Name}";

            Loaded += TasksTestView_Loaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void TasksTestView_Loaded(object? sender, RoutedEventArgs e)
        {
            if (_statusTextBlock != null)
                _statusTextBlock.Text = string.Empty;

            try
            {
                var tasks = await _taskRepository.GetTasksByCourseAsync(_course.Id);

                if (tasks.Count == 0)
                {
                    tasks = GetMockTasks();

                    if (_statusTextBlock != null)
                        _statusTextBlock.Text = "No hay tareas reales en este curso. Mostrando tareas de prueba.";
                }

                if (_tasksListBox != null)
                    _tasksListBox.ItemsSource = tasks;
            }
            catch (Exception ex)
            {
                if (_statusTextBlock != null)
                    _statusTextBlock.Text = $"No se pudieron cargar tareas reales. Mostrando tareas de prueba. Detalle: {ex.Message}";

                if (_tasksListBox != null)
                    _tasksListBox.ItemsSource = GetMockTasks();
            }
        }

        private void TasksListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_tasksListBox?.SelectedItem is not TaskDto selectedTask)
            {
                _selectedTask = null;

                if (_detailsTextBlock != null)
                    _detailsTextBlock.Text = string.Empty;

                if (_attachmentTextBlock != null)
                    _attachmentTextBlock.Text = string.Empty;

                if (_downloadAttachmentButton != null)
                    _downloadAttachmentButton.IsVisible = false;

                return;
            }

            _selectedTask = selectedTask;
            var hasAttachment = !string.IsNullOrWhiteSpace(selectedTask.AttachmentPath);

            if (_detailsTextBlock != null)
            {
                _detailsTextBlock.Text =
                    $"T\u00edtulo: {selectedTask.Title}\n\n" +
                    $"Descripci\u00f3n: {selectedTask.Description}\n\n" +
                    $"Fecha l\u00edmite: {selectedTask.DueDate}";
            }

            if (_attachmentTextBlock != null)
            {
                _attachmentTextBlock.Text = hasAttachment
                    ? "Archivo de apoyo: disponible"
                    : "Archivo de apoyo: Sin archivo de apoyo";
            }

            if (_downloadAttachmentButton != null)
            {
                _downloadAttachmentButton.IsVisible = hasAttachment;
                _downloadAttachmentButton.IsEnabled = hasAttachment;
            }

            if (_statusTextBlock != null)
                _statusTextBlock.Text = string.Empty;
        }

        private void GoToSubmitButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_tasksListBox?.SelectedItem is not TaskDto selectedTask)
            {
                if (_statusTextBlock != null)
                    _statusTextBlock.Text = "Selecciona una tarea.";
                return;
            }

            SubmitRequested?.Invoke(selectedTask);
        }

        private async void DownloadAttachmentButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedTask == null || string.IsNullOrWhiteSpace(_selectedTask.AttachmentPath))
            {
                if (_statusTextBlock != null)
                    _statusTextBlock.Text = "La tarea no tiene archivo de apoyo.";
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null)
            {
                if (_statusTextBlock != null)
                    _statusTextBlock.Text = "No se pudo abrir el dialogo para guardar el archivo.";
                return;
            }

            if (sender is Button button)
                button.IsEnabled = false;

            try
            {
                var download = await _taskRepository.DownloadAttachmentAsync(_selectedTask.Id);
                var destination = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Guardar archivo de apoyo",
                    SuggestedFileName = download.FileName
                });

                if (destination == null)
                    return;

                await using var stream = await destination.OpenWriteAsync();
                await stream.WriteAsync(download.Content, 0, download.Content.Length);

                if (_statusTextBlock != null)
                    _statusTextBlock.Text = "Archivo de apoyo descargado correctamente.";
            }
            catch (Exception ex)
            {
                if (_statusTextBlock != null)
                    _statusTextBlock.Text = $"No se pudo descargar el archivo de apoyo. Detalle: {ex.Message}";
            }
            finally
            {
                if (sender is Button finalButton)
                    finalButton.IsEnabled = true;
            }
        }

        private void BackToCoursesButton_Click(object? sender, RoutedEventArgs e)
        {
            BackToCoursesRequested?.Invoke();
        }

        private List<TaskDto> GetMockTasks()
        {
            return new List<TaskDto>
            {
                new TaskDto
                {
                    Id = 201,
                    Title = "Tarea 1 - Autenticaci\u00f3n",
                    Description = "Implementar login y register conectado al backend.",
                    DueDate = "2026-05-25 23:59:00"
                },
                new TaskDto
                {
                    Id = 202,
                    Title = "Tarea 2 - Entrega de proyecto",
                    Description = "Validar firmas, comprimir el proyecto y enviarlo.",
                    DueDate = "2026-05-30 23:59:00"
                }
            };
        }
    }
}
