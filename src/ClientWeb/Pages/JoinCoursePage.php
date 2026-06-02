<?php

require_once 'Components/BasePage.php';
require_once 'Services/CourseService.php';

class JoinCoursePage extends BasePage
{
    protected function getTitle(): string
    {
        return "Unirse a Curso";
    }

    protected function renderContent(): string
    {
        if (!$this->isStudent()) {
            return "
            <div class='card'>
                <h1>Acceso restringido</h1>
                <p class='error'>Solo los estudiantes pueden unirse a cursos.</p>
                <a class='back-link' href='index.php?page=courses'>Volver a cursos</a>
            </div>
            ";
        }

        $message = "";

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $joinCode = $_POST['join_code'] ?? '';
            $service = new CourseService();
            $response = $service->joinCourse($joinCode);

            if (isset($response['error'])) {
                $message = "<p class='error'>{$response['error']}</p>";
            } else {
                $message = "<p class='success'>{$response['message']}</p>";
            }
        }

        return "
        <div class='card'>
            <h1>Unirse a Curso</h1>
            {$message}
            <form method='POST'>
                <input type='text' name='join_code' placeholder='Codigo del curso' required>
                <button type='submit'>Unirse</button>
            </form>
            <a class='back-link' href='index.php?page=courses'>Volver a cursos</a>
        </div>
        ";
    }
}
