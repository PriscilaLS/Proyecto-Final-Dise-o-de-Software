<?php

require_once 'Components/BasePage.php';
require_once 'Services/AuthService.php';

class LoginPage extends BasePage
{
    protected function getTitle(): string
    {
        return "Login";
    }

    protected function renderContent(): string
    {
        $message = "";

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $email = $_POST['email'] ?? '';
            $password = $_POST['password'] ?? '';

            $service = new AuthService();
            $response = $service->login($email, $password);

            if (isset($response['token'])) {
                $_SESSION['token'] = $response['token'];
                $_SESSION['user'] = $response['user'] ?? [];
                header('Location: index.php?page=courses');
                exit;
            }

            $error = $response['error'] ?? 'Credenciales invalidas';
            $message = "<p class='error'>{$error}</p>";
        }

        return "
        <div class='card'>
            <h1>Iniciar Sesion</h1>
            {$message}
            <form method='POST'>
                <input type='email' name='email' placeholder='Correo' required>
                <input type='password' name='password' placeholder='Contrasena' required>
                <button type='submit'>Ingresar</button>
            </form>
        </div>
        ";
    }
}
