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

        $html = "<div class='tasks-container'>";

        if (!$tasks) {

            $html .= "<p>No hay tareas disponibles</p>";

        } else {

            foreach ($tasks as $task) {

                $html .= "

                <div class='task-card'>

                    <h2>{$task['title']}</h2>

                    <p>{$task['description']}</p>

                    <p>
                        <strong>Fecha límite:</strong>
                        {$task['deadline']}
                    </p>

                </div>
                ";
            }
        }

        $html .= "</div>";

        return $html;
    }
}