using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClientLocal.Models.Courses;
using ClientLocal.Services.Api;
using ClientLocal.Services.Session;

namespace ClientLocal.Views.Auth
{
    public partial class CoursesTestView : UserControl
    {
        private readonly SessionService _sessionService;
        private readonly CourseRepository _courseRepository;

        private ListBox? _coursesListBox;
        private TextBlock? _statusTextBlock;

        public event Action<CourseDto>? CourseSelected;
        public event Action? IdeRequested;

        public CoursesTestView(SessionService sessionService)
        {
            InitializeComponent();

            _sessionService = sessionService;
            _courseRepository = new CourseRepository(ApiClientFactory.Create(_sessionService));

            _coursesListBox = this.FindControl<ListBox>("CoursesListBox");
            _statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");

            Loaded += CoursesTestView_Loaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void CoursesTestView_Loaded(object? sender, RoutedEventArgs e)
        {
            if (_statusTextBlock != null)
                _statusTextBlock.Text = string.Empty;

            try
            {
                var courses = await _courseRepository.GetEnrolledCoursesAsync();

                if (courses.Count == 0)
                {
                    courses = GetMockCourses();

                    if (_statusTextBlock != null)
                        _statusTextBlock.Text = "No hay cursos reales matriculados. Mostrando cursos de prueba.";
                }

                if (_coursesListBox != null)
                    _coursesListBox.ItemsSource = courses;
            }
            catch (Exception ex)
            {
                if (_statusTextBlock != null)
                    _statusTextBlock.Text = $"No se pudieron cargar cursos reales. Mostrando cursos de prueba. Detalle: {ex.Message}";

                if (_coursesListBox != null)
                    _coursesListBox.ItemsSource = GetMockCourses();
            }
        }

        private void OpenTasksButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_coursesListBox?.SelectedItem is not CourseDto selectedCourse)
            {
                if (_statusTextBlock != null)
                    _statusTextBlock.Text = "Selecciona un curso.";
                return;
            }

            CourseSelected?.Invoke(selectedCourse);
        }

        private void OpenIdeButton_Click(object? sender, RoutedEventArgs e)
        {
            IdeRequested?.Invoke();
        }

        private List<CourseDto> GetMockCourses()
        {
            return new List<CourseDto>
            {
                new CourseDto
                {
                    Id = 101,
                    Name = "Diseño de Software",
                    Description = "Curso de prueba para navegación",
                    JoinCode = "MOCK101"
                },
                new CourseDto
                {
                    Id = 102,
                    Name = "Programación Python",
                    Description = "Curso temporal de ejemplo",
                    JoinCode = "MOCK102"
                }
            };
        }
    }
}
