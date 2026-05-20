using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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
        private TextBlock? _statusTextBlock;

        public event Action<TaskDto>? SubmitRequested;

        public TasksTestView(SessionService sessionService, CourseDto course)
        {
            InitializeComponent();

            _sessionService = sessionService;
            _course = course;
            _taskRepository = new TaskRepository(ApiClientFactory.Create(_sessionService));

            _titleTextBlock = this.FindControl<TextBlock>("TitleTextBlock");
            _tasksListBox = this.FindControl<ListBox>("TasksListBox");
            _detailsTextBlock = this.FindControl<TextBlock>("DetailsTextBlock");
            _statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");

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
                if (_detailsTextBlock != null)
                    _detailsTextBlock.Text = string.Empty;
                return;
            }

            if (_detailsTextBlock != null)
            {
                _detailsTextBlock.Text =
                    $"Título: {selectedTask.Title}\n\n" +
                    $"Descripción: {selectedTask.Description}\n\n" +
                    $"Fecha límite: {selectedTask.DueDate}";
            }
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

        private List<TaskDto> GetMockTasks()
        {
            return new List<TaskDto>
            {
                new TaskDto
                {
                    Id = 201,
                    Title = "Tarea 1 - Autenticación",
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