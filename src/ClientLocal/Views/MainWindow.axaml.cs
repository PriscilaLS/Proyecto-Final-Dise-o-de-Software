using System;
using Avalonia.Controls;
using ClientLocal.Models.Courses;
using ClientLocal.Models.Tasks;
using ClientLocal.Services.Session;
using ClientLocal.Views.Auth;
using ClientLocal.Views.Decorator;

namespace ClientLocal.Views;

public partial class MainWindow : Window
{
    private SessionService? _sessionService;
    private CourseDto? _currentCourse;

    public MainWindow()
    {
        Title = "EduIDE";
        Width = 1280;
        Height = 800;
        MinWidth = 800;
        MinHeight = 600;

        ShowLogin();
    }

    private void ShowLogin()
    {
        var login = new LoginView();

        login.LoginSucceeded += () =>
        {
            _sessionService = login.GetSessionService();
            ShowCourses();
        };

        login.RegisterRequested += ShowRegister;
        Content = login;
    }

    private void ShowRegister()
    {
        var register = new RegisterView();
        register.BackRequested += ShowLogin;
        Content = register;
    }

    private void ShowCourses()
    {
        var courses = new CoursesTestView(_sessionService!);
        courses.CourseSelected += ShowTasks;
        courses.IdeRequested += OpenIdeWindow;
        courses.DecoratorRequested += OpenDecoratorDemo;
        Content = courses;
    }

    private void OpenIdeWindow()
    {
        var editor = new EditorView();
        editor.HomeRequested += () => editor.Close();
        editor.Show();
    }

    private void OpenDecoratorDemo()
    {
        var demo = new DecoratorHostWindow();
        demo.Show();
    }

    private void ShowTasks(CourseDto course)
    {
        _currentCourse = course;

        var tasks = new TasksTestView(_sessionService!, course);
        tasks.SubmitRequested += ShowSubmit;
        Content = tasks;
    }

    private void ShowSubmit(TaskDto task)
    {
        var submit = new SubmitTestView(_sessionService!, task);

        submit.BackRequested += () =>
        {
            if (_currentCourse != null)
                ShowTasks(_currentCourse);
            else
                ShowCourses();
        };

        submit.ClipboardTestRequested += ShowClipboardTest;
        Content = submit;
    }

    private void ShowClipboardTest()
    {
        var clipboardTest = new ClipboardTestView();

        clipboardTest.BackRequested += () =>
        {
            if (_currentCourse != null)
                ShowTasks(_currentCourse);
            else
                ShowCourses();
        };

        Content = clipboardTest;
    }
}
