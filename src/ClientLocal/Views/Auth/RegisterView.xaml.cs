using System;
using System.Windows;
using ClientLocal.Models.Auth;
using ClientLocal.Services.Api;
using ClientLocal.Services.Session;

namespace ClientLocal.Views.Auth
{
    public partial class RegisterView : Window
    {
        private readonly SessionService _sessionService;
        private readonly AuthRepository _authRepository;

        public RegisterView(SessionService sessionService)
        {
            InitializeComponent();

            _sessionService = sessionService;
            var httpClient = ApiClientFactory.Create(_sessionService);
            _authRepository = new AuthRepository(httpClient);
        }

        private async void Registrar_Click(object sender, RoutedEventArgs e)
        {
            EstadoTextBlock.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(NombreTextBox.Text) ||
                string.IsNullOrWhiteSpace(Apellido1TextBox.Text) ||
                string.IsNullOrWhiteSpace(CarnetTextBox.Text) ||
                string.IsNullOrWhiteSpace(CorreoTextBox.Text) ||
                string.IsNullOrWhiteSpace(ContrasenaPasswordBox.Password))
            {
                EstadoTextBlock.Text = "Completa todos los campos obligatorios.";
                return;
            }

            try
            {
                var request = new RegisterRequest
                {
                    Nombre = NombreTextBox.Text.Trim(),
                    Apellido1 = Apellido1TextBox.Text.Trim(),
                    Apellido2 = string.IsNullOrWhiteSpace(Apellido2TextBox.Text)
                        ? null
                        : Apellido2TextBox.Text.Trim(),
                    Carnet = CarnetTextBox.Text.Trim(),
                    Correo = CorreoTextBox.Text.Trim(),
                    Contrasena = ContrasenaPasswordBox.Password
                };

                var response = await _authRepository.RegisterAsync(request);

                if (!response.Success)
                {
                    EstadoTextBlock.Text = response.Mensaje ?? "No se pudo registrar el usuario.";
                    return;
                }

                MessageBox.Show("Registro exitoso. Ahora puedes iniciar sesión.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                var loginView = new LoginView(_sessionService);
                loginView.Show();
                Close();
            }
            catch (Exception ex)
            {
                EstadoTextBlock.Text = $"Error de conexión: {ex.Message}";
            }
        }

        private void VolverLogin_Click(object sender, RoutedEventArgs e)
        {
            var loginView = new LoginView(_sessionService);
            loginView.Show();
            Close();
        }
    }
}