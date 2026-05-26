using System;
using Avalonia.Controls;
using ClientLocal.Models.Courses;
using ClientLocal.Models.Tasks;
using ClientLocal.Services.Session;
using ClientLocal.Views.Auth;

namespace ClientLocal.Views;

public partial class MainWindow : Window
{
    private SessionService? _sessionService;

    public MainWindow()
    {
        Title    = "EduIDE";
        Width    = 1280;
        Height   = 800;
        MinWidth = 800;
        MinHeight = 600;

        ShowLogin();
    }

    // ── Login ─────────

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

    // ── Register ────────

    private void ShowRegister()
    {
        var register = new RegisterView();
        register.BackRequested += ShowLogin;
        Content = register;
    }

    // ── Courses ────

    private void ShowCourses()
    {
        var courses = new CoursesTestView(_sessionService!);

        courses.CourseSelected += (course) => ShowTasks(course);

        Content = courses;
    }

    // ── Tasks ────

    private void ShowTasks(CourseDto course)
    {
        var tasks = new TasksTestView(_sessionService!, course);

        tasks.SubmitRequested += (task) => ShowEditor(task);

        Content = tasks;
    }

    // ── Editor ────

    private void ShowEditor(TaskDto task)
    {
        var editor = new EditorView();
        editor.HomeRequested += ShowCourses;
        Content = editor;
    }
}