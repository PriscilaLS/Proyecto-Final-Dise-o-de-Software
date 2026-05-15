using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ClientLocal.Models.Courses;
using ClientLocal.Models.Tasks;
using ClientLocal.Services.Api;
using ClientLocal.Services.Session;

namespace ClientLocal.Views.Tasks
{
    public partial class TasksView : Window
    {
        private readonly SessionService _sessionService;
        private readonly TaskRepository _taskRepository;
        private readonly CourseDto _course;
        private List<TaskDto> _tasks = new();

        public TasksView(SessionService sessionService, CourseDto course)
        {
            InitializeComponent();

            _sessionService = sessionService;
            _course = course;
            TituloCursoTextBlock.Text = $"Curso: {_course.Nombre}";

            var httpClient = ApiClientFactory.Create(_sessionService);
            _taskRepository = new TaskRepository(httpClient);

            Loaded += TasksView_Loaded;
        }

        private async void TasksView_Loaded(object sender, RoutedEventArgs e)
        {
            EstadoTextBlock.Text = string.Empty;

            try
            {
                _tasks = await _taskRepository.GetTasksByCourseAsync(_course.Id);
                TasksListBox.ItemsSource = _tasks;

                if (_tasks.Count == 0)
                    EstadoTextBlock.Text = "No hay tareas disponibles en este curso.";
            }
            catch (Exception ex)
            {
                EstadoTextBlock.Text = $"No se pudieron cargar las tareas: {ex.Message}";
            }
        }

        private void TasksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TasksListBox.SelectedItem is not TaskDto selectedTask)
            {
                DetalleTextBlock.Text = string.Empty;
                return;
            }

            DetalleTextBlock.Text =
                $"Nombre: {selectedTask.Nombre}\n\n" +
                $"Descripción: {selectedTask.Descripcion}\n\n" +
                $"Fecha límite: {selectedTask.FechaLimite}";
        }
    }
}