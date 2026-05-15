using System;
using System.Collections.Generic;
using System.Windows;
using ClientLocal.Models.Courses;
using ClientLocal.Services.Api;
using ClientLocal.Services.Session;
using ClientLocal.Views.Tasks;

namespace ClientLocal.Views.Courses
{
    public partial class CoursesView : Window
    {
        private readonly SessionService _sessionService;
        private readonly CourseRepository _courseRepository;
        private List<CourseDto> _courses = new();

        public CoursesView(SessionService sessionService)
        {
            InitializeComponent();

            _sessionService = sessionService;
            var httpClient = ApiClientFactory.Create(_sessionService);
            _courseRepository = new CourseRepository(httpClient);

            Loaded += CoursesView_Loaded;
        }

        private async void CoursesView_Loaded(object sender, RoutedEventArgs e)
        {
            EstadoTextBlock.Text = string.Empty;

            try
            {
                _courses = await _courseRepository.GetEnrolledCoursesAsync();
                CoursesListBox.ItemsSource = _courses;

                if (_courses.Count == 0)
                    EstadoTextBlock.Text = "No hay cursos matriculados.";
            }
            catch (Exception ex)
            {
                EstadoTextBlock.Text = $"No se pudieron cargar los cursos: {ex.Message}";
            }
        }

        private void AbrirTareas_Click(object sender, RoutedEventArgs e)
        {
            if (CoursesListBox.SelectedItem is not CourseDto selectedCourse)
            {
                EstadoTextBlock.Text = "Selecciona un curso.";
                return;
            }

            var tasksView = new TasksView(_sessionService, selectedCourse);
            tasksView.Show();
            Close();
        }
    }
}