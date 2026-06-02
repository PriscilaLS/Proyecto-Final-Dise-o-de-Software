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

        $error = $response['error'] ?? 'Credenciales inválidas';
        $message = "<p class='error'>{$error}</p>";
    }

    return "
    <div class='card'>
        <h1>Iniciar sesión</h1>
        <p class='card-subtitle'>Ingresá con tu cuenta de EduIDE</p>
        {$message}
        <form method='POST'>
            <div class='form-group'>
                <label>Correo electrónico</label>
                <input type='email' name='email' placeholder='usuario@ejemplo.com' required>
            </div>
            <div class='form-group'>
                <label>Contraseña</label>
                <input type='password' name='password' placeholder='••••••••' required>
            </div>
            <button type='submit'>Ingresar</button>
        </form>
        <p style='text-align:center; margin-top:20px; font-size:13px; color:var(--text-secondary)'>
            ¿No tenés cuenta? <a href='index.php?page=register' style='color:var(--accent); text-decoration:none;'>Registrate</a>
        </p>
    </div>
    ";
}
}
