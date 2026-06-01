<?php

require_once 'Components/BasePage.php';
require_once 'Services/TaskService.php';

class CreateTaskPage extends BasePage
{
    protected function getTitle(): string
    {
        return "Crear Tarea";
    }

    protected function renderContent(): string
    {
        $courseId = $_GET['course_id'] ?? 0;
        $message = "";

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $title = $_POST['title'] ?? '';
            $description = $_POST['description'] ?? '';
            $dueDate = $_POST['due_date'] ?? '';

            $service = new TaskService();
            $response = $service->createTask($courseId, $title, $description, $dueDate);

            if (isset($response['error'])) {
                $message = "<p class='error'>{$response['error']}</p>";
            } else {
                $message = "<p class='success'>Tarea creada correctamente.</p>";
            }
        }

        return "
        <div class='card'>
            <h1>Crear Tarea</h1>
            {$message}
            <form method='POST'>
                <input type='text' name='title' placeholder='Titulo' required>
                <textarea name='description' placeholder='Descripcion' required></textarea>
                <input type='datetime-local' name='due_date' required>
                <button type='submit'>Crear Tarea</button>
            </form>
            <a class='back-link' href='index.php?page=tasks&id={$courseId}'>Volver a tareas</a>
        </div>
        ";
    }
}
