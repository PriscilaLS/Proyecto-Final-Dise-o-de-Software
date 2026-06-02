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
        private TextBox? _joinCodeTextBox;
        private TextBlock? _statusTextBlock;

        public event Action<CourseDto>? CourseSelected;
        public event Action? IdeRequested;
        public event Action? DecoratorRequested;

        public CoursesTestView(SessionService sessionService)
        {
            InitializeComponent();

            _sessionService = sessionService;
            _courseRepository = new CourseRepository(ApiClientFactory.Create(_sessionService));

            _coursesListBox = this.FindControl<ListBox>("CoursesListBox");
            _joinCodeTextBox = this.FindControl<TextBox>("JoinCodeTextBox");
            _statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");

            Loaded += CoursesTestView_Loaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void CoursesTestView_Loaded(object? sender, RoutedEventArgs e)
        {
            await LoadCoursesAsync();
        }

        private async System.Threading.Tasks.Task LoadCoursesAsync()
        {
            if (_statusTextBlock != null)
                _statusTextBlock.Text = string.Empty;

            try
            {
                var courses = await _courseRepository.GetEnrolledCoursesAsync();
                var canSeeJoinCode = _sessionService.UserRole == "teacher";

                foreach (var course in courses)
                    course.ShowJoinCode = canSeeJoinCode;

                if (courses.Count == 0 && _statusTextBlock != null)
                    _statusTextBlock.Text = "No hay cursos matriculados.";

                if (_coursesListBox != null)
                    _coursesListBox.ItemsSource = courses;
            }
            catch (Exception ex)
            {
                if (_statusTextBlock != null)
                    _statusTextBlock.Text = $"No se pudieron cargar los cursos. Detalle: {ex.Message}";

                if (_coursesListBox != null)
                    _coursesListBox.ItemsSource = new List<CourseDto>();
            }
        }

        private async void JoinCourseButton_Click(object? sender, RoutedEventArgs e)
        {
            var joinCode = _joinCodeTextBox?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                if (_statusTextBlock != null)
                    _statusTextBlock.Text = "Ingresa el c\u00f3digo del curso.";
                return;
            }

            if (sender is Button joinButton)
                joinButton.IsEnabled = false;

            try
            {
                var message = await _courseRepository.JoinCourseAsync(joinCode);

                if (_statusTextBlock != null)
                    _statusTextBlock.Text = message;

                if (_joinCodeTextBox != null)
                    _joinCodeTextBox.Text = string.Empty;

                await LoadCoursesAsync();
            }
            catch (Exception ex)
            {
                if (_statusTextBlock != null)
                    _statusTextBlock.Text = ex.Message;
            }
            finally
            {
                if (sender is Button finalButton)
                    finalButton.IsEnabled = true;
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

        private void OpenDecoratorButton_Click(object? sender, RoutedEventArgs e)
        {
            DecoratorRequested?.Invoke();
        }
    }
}

