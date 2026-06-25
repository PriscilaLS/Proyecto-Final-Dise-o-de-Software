<?php

require_once 'Components/BasePage.php';
require_once 'Services/TaskService.php';

class TasksPage extends BasePage
{
    protected function getTitle(): string
    {
        return "Tareas";
    }

    protected function renderContent(): string
    {
        $courseId = $_GET['id'] ?? 0;
        $service = new TaskService();
        $tasks = $service->getTasks($courseId);
        $createTaskAction = $this->isTeacher()
            ? "<a class='button-link' href='index.php?page=create-task&course_id={$courseId}'>Crear tarea</a>"
            : "";

        $html = "
        <div class='page-panel'>
            <div class='page-actions'>
                <h1>Tareas</h1>
                {$createTaskAction}
            </div>
        ";

        if (isset($tasks['error'])) {
            $html .= "<p class='error'>{$tasks['error']}</p></div>";
            return $html;
        }

        $html .= "<div class='tasks-container'>";

        if (!$tasks) {
            $html .= "<p>No hay tareas disponibles.</p>";
        } else {
            foreach ($tasks as $task) {
                $id = $task['id'];
                $title = htmlspecialchars($task['title'] ?? '', ENT_QUOTES, 'UTF-8');
                $description = htmlspecialchars($task['description'] ?? '', ENT_QUOTES, 'UTF-8');
                $dueDate = htmlspecialchars($task['due_date'] ?? '', ENT_QUOTES, 'UTF-8');
                $attachmentPath = htmlspecialchars($task['attachment_path'] ?? '',ENT_QUOTES,'UTF-8'
);

                $submissionAction = $this->isTeacher()
                    ? "<a href='index.php?page=submissions&task_id={$id}'>Ver entregas</a>"
                    : "";

                $html .= "
                <div class='task-card'>
                    <h2>{$title}</h2>
                    <p>{$description}</p>
                    <p><strong>Fecha limite:</strong> {$dueDate}</p>
                ";

                if (!empty($attachmentPath)) {
                    $attachmentUrl = "task_attachment.php?task_id={$id}";
                    $html .= "
                    <p>
                        <strong>Archivo de apoyo:</strong>
                        <a href='{$attachmentUrl}'>Descargar archivo de apoyo</a>
                    </p>
                    ";
                } else {
                    $html .= "
                    <p><strong>Archivo de apoyo:</strong> Sin archivo de apoyo</p>
                    ";
                }

                $html .= "
                    {$submissionAction}
                </div>

                ";
            }
        }

        $html .= "</div><a class='back-link' href='index.php?page=courses'>Volver a cursos</a></div>";
        return $html;
    }
}
