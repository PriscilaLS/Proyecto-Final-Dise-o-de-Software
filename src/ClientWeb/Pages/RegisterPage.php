<?php

require_once 'Components/BasePage.php';
require_once 'Services/AuthService.php';

class RegisterPage extends BasePage
{
    protected function getTitle(): string
    {
        return "Registro";
    }

    protected function renderContent(): string
    {
        $message = "";

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {

            $name = $_POST['name'];
            $email = $_POST['email'];
            $password = $_POST['password'];

            $service = new AuthService();

            $response = $service->register($name, $email, $password);

            if (isset($response['success'])) {

                $message = "<p class='success'>Usuario registrado</p>";

            } else {

                $message = "<p class='error'>Error al registrar</p>";
            }
        }

        return "

        <div class='card'>

            <h1>Registro</h1>

            {$message}

            <form method='POST'>

                <input type='text' name='name' placeholder='Nombre' required>

                <input type='email' name='email' placeholder='Correo' required>

                <input type='password' name='password' placeholder='Contraseña' required>

                <button type='submit'>Crear Cuenta</button>

            </form>

        </div>
        ";
    }
}