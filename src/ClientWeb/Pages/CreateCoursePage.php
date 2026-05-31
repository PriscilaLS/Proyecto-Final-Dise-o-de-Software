<?php

require_once 'Components/BasePage.php';
require_once 'Services/CourseService.php';

class CreateCoursePage extends BasePage
{
    protected function getTitle(): string
    {
        return "Crear Curso";
    }

    protected function renderContent(): string
    {
        $message = "";

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $name = $_POST['name'] ?? '';
            $description = $_POST['description'] ?? '';

            $service = new CourseService();
            $response = $service->createCourse($name, $description);

            if (isset($response['error'])) {
                $message = "<p class='error'>{$response['error']}</p>";
            } else {
                $joinCode = htmlspecialchars($response['join_code'] ?? '', ENT_QUOTES, 'UTF-8');
                $message = "<p class='success'>Curso creado correctamente. Codigo: {$joinCode}</p>";
            }
        }

        return "
        <div class='card'>
            <h1>Crear Curso</h1>
            {$message}
            <form method='POST'>
                <input type='text' name='name' placeholder='Nombre del curso' required>
                <textarea name='description' placeholder='Descripcion' required></textarea>
                <button type='submit'>Crear Curso</button>
            </form>
            <a class='back-link' href='index.php?page=courses'>Volver a cursos</a>
        </div>
        ";
    }
}
