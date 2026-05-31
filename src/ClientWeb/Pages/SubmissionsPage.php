<?php

require_once 'Components/BasePage.php';
require_once 'Services/TaskService.php';

class SubmissionsPage extends BasePage
{
    protected function getTitle(): string
    {
        return "Entregas";
    }

    protected function renderContent(): string
    {
        $taskId = $_GET['task_id'] ?? 0;
        $service = new TaskService();
        $submissions = $service->getSubmissions($taskId);

        $html = "<div class='page-panel'><h1>Entregas</h1>";

        if (isset($submissions['error'])) {
            $html .= "<p class='error'>{$submissions['error']}</p></div>";
            return $html;
        }

        if (!$submissions) {
            $html .= "<p>No hay entregas registradas.</p>";
        } else {
            $html .= "<table class='data-table'>
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Estudiante</th>
                        <th>Fecha</th>
                        <th>Tardia</th>
                        <th>Archivo</th>
                    </tr>
                </thead>
                <tbody>";

            foreach ($submissions as $submission) {
                $id = $submission['id'];
                $student = htmlspecialchars($submission['student'] ?? '', ENT_QUOTES, 'UTF-8');
                $submittedAt = htmlspecialchars($submission['submitted_at'] ?? '', ENT_QUOTES, 'UTF-8');
                $isLate = !empty($submission['is_late']) ? 'Si' : 'No';
                $downloadUrl = "download.php?id={$id}";

                $html .= "
                <tr>
                    <td>{$id}</td>
                    <td>{$student}</td>
                    <td>{$submittedAt}</td>
                    <td>{$isLate}</td>
                    <td><a href='{$downloadUrl}'>Descargar ZIP</a></td>
                </tr>";
            }

            $html .= "</tbody></table>";
        }

        $html .= "<a class='back-link' href='index.php?page=courses'>Volver a cursos</a></div>";
        return $html;
    }
}
