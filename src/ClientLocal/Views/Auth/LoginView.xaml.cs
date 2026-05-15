using System;
using System.Windows;
using ClientLocal.Models.Auth;
using ClientLocal.Services.Api;
using ClientLocal.Services.Session;

namespace ClientLocal.Views.Auth
{
    public partial class LoginView : Window
    {
        private readonly SessionService _sessionService;
        private readonly AuthRepository _authRepository;

        public LoginView(SessionService sessionService)
        {
            InitializeComponent();

            _sessionService = sessionService;
            var httpClient = ApiClientFactory.Create(_sessionService);
            _authRepository = new AuthRepository(httpClient);
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            EstadoTextBlock.Text = string.Empty;

            var correo = CorreoTextBox.Text.Trim();
            var contrasena = ContrasenaPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
            {
                EstadoTextBlock.Text = "Debes completar correo y contraseña.";
                return;
            }

            try
            {
                var request = new LoginRequest
                {
                    Correo = correo,
                    Contrasena = contrasena
                };

                var response = await _authRepository.LoginAsync(request);

                if (!response.Success || string.IsNullOrWhiteSpace(response.Token))
                {
                    EstadoTextBlock.Text = response.Mensaje ?? "No se pudo iniciar sesión.";
                    return;
                }

                _sessionService.SetSession(response.Token, response.Rol, correo);

                MessageBox.Show("Inicio de sesión exitoso.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Por ahora solo cerramos. Luego aquí abrimos CoursesView.
                Close();
            }
            catch (Exception ex)
            {
                EstadoTextBlock.Text = $"Error de conexión: {ex.Message}";
            }
        }

        private void IrARegistro_Click(object sender, RoutedEventArgs e)
        {
            var registerView = new RegisterView(_sessionService);
            registerView.Show();
            Close();
        }
    }
}