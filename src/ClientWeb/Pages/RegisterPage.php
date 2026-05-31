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
            $name = $_POST['name'] ?? '';
            $email = $_POST['email'] ?? '';
            $password = $_POST['password'] ?? '';
            $role = $_POST['role'] ?? 'student';

            $service = new AuthService();
            $response = $service->register($name, $email, $password, $role);

            if (isset($response['error'])) {
                $message = "<p class='error'>{$response['error']}</p>";
            } else {
                $message = "<p class='success'>Cuenta creada correctamente. Ya puedes iniciar sesion.</p>";
            }
        }

        return "
        <div class='card'>
            <h1>Registro</h1>
            {$message}
            <form method='POST'>
                <input type='text' name='name' placeholder='Nombre' required>
                <input type='email' name='email' placeholder='Correo' required>
                <input type='password' name='password' placeholder='Contrasena' required>
                <select name='role' required>
                    <option value='student'>Estudiante</option>
                    <option value='teacher'>Profesor</option>
                </select>
                <button type='submit'>Crear Cuenta</button>
            </form>
        </div>
        ";
    }
}
