<?php
/*
 * Controlador de entregas.
 * Recibe ZIPs de proyectos, lista entregas de una tarea y permite descargar archivos enviados.
 */
require_once __DIR__ . '/../Services/submissionService.php';
require_once __DIR__ . '/../Repositories/submissionRepository.php';
require_once __DIR__ . '/../Middleware/authMiddleware.php';

class SubmissionController {
    private SubmissionService $submissionService;

    public function __construct() {
        $this->submissionService = new SubmissionService();
    }

    public function submit(int $taskId): void {
        $payload = AuthMiddleware::handle();
        AuthMiddleware::requireRole($payload, 'student');

        // Los archivos enviados por multipart/form-data llegan en $_FILES.
        if (!isset($_FILES['project'])) {
            http_response_code(400);
            echo json_encode(['error' => 'Archivo ZIP requerido']);
            return;
        }

        try {
            // El service valida permisos, valida que sea ZIP y guarda el archivo.
            $result = $this->submissionService->submitProject($taskId, $_FILES['project'], $payload);
            http_response_code(200);
            echo json_encode($result);
        } catch (Exception $e) {
            http_response_code(400);
            echo json_encode(['error' => $e->getMessage()]);
        }
    }

    public function getByTask(int $taskId): void {
        $payload = AuthMiddleware::handle();
        AuthMiddleware::requireRole($payload, 'teacher');

        try {
            $submissions = $this->submissionService->getSubmissionsByTask($taskId, $payload);
            http_response_code(200);
            echo json_encode($submissions);
        } catch (Exception $e) {
            http_response_code(403);
            echo json_encode(['error' => $e->getMessage()]);
        }
    }

    public function getMineByTask(int $taskId): void {
        $payload = AuthMiddleware::handle();
        AuthMiddleware::requireRole($payload, 'student');

        try {
            $submissionRepo = new SubmissionRepository();
            $submissions = $submissionRepo->findByTaskAndStudent($taskId, (int) $payload['id']);
            http_response_code(200);
            echo json_encode($submissions);
        } catch (Exception $e) {
            http_response_code(403);
            echo json_encode(['error' => $e->getMessage()]);
        }
    }

    public function download(int $submissionId): void {
        $payload = AuthMiddleware::handle();
        AuthMiddleware::requireRole($payload, 'teacher');

        try {
            $path = $this->submissionService->getDownloadPath($submissionId, $payload);
            // Para descargar ZIP, la respuesta ya no es JSON: es el archivo.
            header('Content-Type: application/zip');
            header('Content-Disposition: attachment; filename="' . basename($path) . '"');
            header('Content-Length: ' . filesize($path));
            readfile($path);
        } catch (Exception $e) {
            http_response_code(404);
            echo json_encode(['error' => $e->getMessage()]);
        }
    }
}
