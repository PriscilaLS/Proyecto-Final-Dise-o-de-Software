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

            $email = $_POST['email'];
            $password = $_POST['password'];

            $service = new AuthService();

            $response = $service->login($email, $password);

            if (isset($response['token'])) {

                $_SESSION['token'] = $response['token'];

                $message = "<p class='success'>Login exitoso</p>";

            } else {

                $message = "<p class='error'>Credenciales inválidas</p>";
            }
        }

        return "

        <div class='card'>

            <h1>Iniciar Sesión</h1>

            {$message}

            <form method='POST'>

                <input type='email' name='email' placeholder='Correo' required>

                <input type='password' name='password' placeholder='Contraseña' required>

                <button type='submit'>Ingresar</button>

            </form>

        </div>
        ";
    }
}