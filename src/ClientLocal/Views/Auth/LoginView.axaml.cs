using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ClientLocal.Models.Auth;
using ClientLocal.Services.Api;
using ClientLocal.Services.Session;

namespace ClientLocal.Views.Auth
{
    public partial class LoginView : UserControl
    {
        private readonly AuthRepository _authRepository;
        private readonly SessionService _sessionService;

        private TextBox? _emailTextBox;
        private TextBox? _passwordTextBox;
        private TextBlock? _statusTextBlock;

        public event Action? LoginSucceeded;
        public event Action? RegisterRequested;

        public LoginView()
        {
            InitializeComponent();

            _authRepository = new AuthRepository();
            _sessionService = new SessionService();

            _emailTextBox = this.FindControl<TextBox>("EmailTextBox");
            _passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
            _statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
        }

        public SessionService GetSessionService() => _sessionService;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void LoginButton_Click(object? sender, RoutedEventArgs e)
        {
            var email = _emailTextBox?.Text?.Trim() ?? string.Empty;
            var password = _passwordTextBox?.Text ?? string.Empty;

            if (_statusTextBlock != null)
            {
                _statusTextBlock.Text = string.Empty;
                _statusTextBlock.Foreground = Brushes.Red;
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                if (_statusTextBlock != null)
                    _statusTextBlock.Text = "Completa correo y contraseña.";
                return;
            }

            try
            {
                var response = await _authRepository.LoginAsync(new LoginRequest
                {
                    Email = email,
                    Password = password
                });

                if (!string.IsNullOrWhiteSpace(response.Error))
                {
                    if (_statusTextBlock != null)
                    {
                        _statusTextBlock.Text =
                            response.Error == "Credenciales inválidas"
                            ? "El usuario no existe o la contraseña es incorrecta."
                            : response.Error;
                    }
                    return;
                }

                if (string.IsNullOrWhiteSpace(response.Token) || response.User == null)
                {
                    if (_statusTextBlock != null)
                        _statusTextBlock.Text = "Respuesta inválida del servidor.";
                    return;
                }

                _sessionService.SetSession(
                    response.Token,
                    response.User.Id,
                    response.User.Name,
                    response.User.Role
                );

                if (_statusTextBlock != null)
                {
                    _statusTextBlock.Foreground = Brushes.Green;
                    _statusTextBlock.Text = "Inicio de sesión exitoso.";
                }

                LoginSucceeded?.Invoke();
            }
            catch (Exception ex)
            {
                if (_statusTextBlock != null)
                    _statusTextBlock.Text = $"Error de conexión: {ex.Message}";
            }
        }

        private void GoRegisterButton_Click(object? sender, RoutedEventArgs e)
        {
            RegisterRequested?.Invoke();
        }
    }
}