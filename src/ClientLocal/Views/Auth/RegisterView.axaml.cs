using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ClientLocal.Models.Auth;
using ClientLocal.Services.Api;

namespace ClientLocal.Views.Auth
{
    public partial class RegisterView : UserControl
    {
        private readonly AuthRepository _authRepository;

        private TextBox? _nameTextBox;
        private TextBox? _emailTextBox;
        private TextBox? _passwordTextBox;
        private TextBlock? _statusTextBlock;

        public event Action? BackRequested;

        public RegisterView()
        {
            InitializeComponent();

            _authRepository = new AuthRepository();

            _nameTextBox = this.FindControl<TextBox>("NameTextBox");
            _emailTextBox = this.FindControl<TextBox>("EmailTextBox");
            _passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
            _statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void RegisterButton_Click(object? sender, RoutedEventArgs e)
        {
            var name = _nameTextBox?.Text?.Trim() ?? string.Empty;
            var email = _emailTextBox?.Text?.Trim() ?? string.Empty;
            var password = _passwordTextBox?.Text ?? string.Empty;

            if (_statusTextBlock != null)
            {
                _statusTextBlock.Text = string.Empty;
                _statusTextBlock.Foreground = Brushes.Red;
            }

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                if (_statusTextBlock != null)
                    _statusTextBlock.Text = "Completa todos los campos.";
                return;
            }

            try
            {
                var response = await _authRepository.RegisterAsync(new RegisterRequest
                {
                    Name = name,
                    Email = email,
                    Password = password,
                    Role = "student"
                });

                if (!string.IsNullOrWhiteSpace(response.Error))
                {
                    if (_statusTextBlock != null)
                        _statusTextBlock.Text = response.Error;
                    return;
                }

                if (_statusTextBlock != null)
                {
                    _statusTextBlock.Foreground = Brushes.Green;
                    _statusTextBlock.Text = response.Message ?? "Registro exitoso. Ya puedes iniciar sesión.";
                }

                // Volver al login automáticamente
                BackRequested?.Invoke();
            }
            catch (Exception ex)
            {
                if (_statusTextBlock != null)
                {
                    _statusTextBlock.Foreground = Brushes.Red;
                    _statusTextBlock.Text = $"Error de conexión: {ex.Message}";
                }
            }
        }

        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke();
        }
    }
}