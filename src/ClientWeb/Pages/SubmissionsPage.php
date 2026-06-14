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
        $taskId = (int) ($_GET['task_id'] ?? 0);
        $historySubmissionId = (int) ($_GET['history'] ?? 0);
        $service = new TaskService();
        $submissions = $service->getSubmissions($taskId);
        $historyVersions = [];
        $historyError = null;

        if ($historySubmissionId > 0) {
            $historyVersions = $service->getSubmissionVersions($historySubmissionId);
            if (isset($historyVersions['error'])) {
                $historyError = $historyVersions['error'];
                $historyVersions = [];
            }
        }

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
                        <th>Entrega</th>
                        <th>Estudiante</th>
                        <th>Última versión</th>
                        <th>Fecha</th>
                        <th>Puntualidad</th>
                        <th>Total versiones</th>
                        <th>Acciones</th>
                    </tr>
                </thead>
                <tbody>";

            $selectedSubmission = null;

            foreach ($submissions as $submission) {
                $id = (int) ($submission['submission_id'] ?? $submission['id']);
                $student = htmlspecialchars($submission['student'] ?? '', ENT_QUOTES, 'UTF-8');
                $submittedAt = htmlspecialchars($submission['submitted_at'] ?? '', ENT_QUOTES, 'UTF-8');
                $latestVersion = (int) ($submission['latest_version_number'] ?? $submission['version_number'] ?? 1);
                $totalVersions = (int) ($submission['total_versions'] ?? 1);
                $isLate = !empty($submission['is_late']) ? 'Tardía' : 'A tiempo';
                $downloadUrl = "download.php?id={$id}";
                $historyUrl = "index.php?page=submissions&task_id={$taskId}&history={$id}";

                if ($historySubmissionId === $id) {
                    $selectedSubmission = [
                        'id' => $id,
                        'student' => $student,
                        'latest_version' => $latestVersion,
                        'total_versions' => $totalVersions,
                        'submitted_at' => $submittedAt,
                        'is_late' => $isLate
                    ];
                }

                $html .= "
                <tr>
                    <td>{$id}</td>
                    <td>{$student}</td>
                    <td>Versión {$latestVersion}</td>
                    <td>{$submittedAt}</td>
                    <td>{$isLate}</td>
                    <td>{$totalVersions}</td>
                    <td>
                        <a href='{$downloadUrl}'>Descargar última</a>
                        &nbsp;|&nbsp;
                        <a class='table-button' href='{$historyUrl}'>Ver historial</a>
                    </td>
                </tr>";
            }

            $html .= "</tbody></table>";

            if ($selectedSubmission !== null) {
                $html .= "
                <div class='history-summary'>
                    <h2>Historial de {$selectedSubmission['student']}</h2>";

                if ($historyError !== null) {
                    $safeError = htmlspecialchars($historyError, ENT_QUOTES, 'UTF-8');
                    $html .= "<p class='error'>{$safeError}</p>";
                } elseif (!$historyVersions) {
                    $html .= "<p>No hay versiones registradas para esta entrega.</p>";
                } else {
                    $html .= "
                    <table class='data-table history-table'>
                        <thead>
                            <tr>
                                <th>Versión</th>
                                <th>Fecha</th>
                                <th>Puntualidad</th>
                                <th>Archivo</th>
                            </tr>
                        </thead>
                        <tbody>";

                    foreach ($historyVersions as $version) {
                        $versionId = (int) ($version['version_id'] ?? $version['id']);
                        $versionNumber = (int) ($version['version_number'] ?? 0);
                        $versionDate = htmlspecialchars($version['submitted_at'] ?? '', ENT_QUOTES, 'UTF-8');
                        $versionLate = !empty($version['is_late']) ? 'Tardía' : 'A tiempo';
                        $versionDownloadUrl = "download.php?version_id={$versionId}";

                        $html .= "
                            <tr>
                                <td>Versión {$versionNumber}</td>
                                <td>{$versionDate}</td>
                                <td>{$versionLate}</td>
                                <td><a href='{$versionDownloadUrl}'>Descargar ZIP</a></td>
                            </tr>";
                    }

                    $html .= "
                        </tbody>
                    </table>";
                }

                $html .= "</div>";
            }
        }

        $html .= "<a class='back-link' href='index.php?page=courses'>Volver a cursos</a></div>";
        return $html;
    }
}
