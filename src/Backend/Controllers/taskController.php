<?php
/*
 * Controlador de tareas.
 * Maneja la creación de tareas por curso y la consulta de tareas disponibles.
 */
require_once __DIR__ . '/../Services/taskService.php';
require_once __DIR__ . '/../Middleware/authMiddleware.php';

class TaskController {
    private TaskService $taskService;

    public function __construct() {
        $this->taskService = new TaskService();
    }

    public function getByCourse(int $courseId): void {
        // Primero se valida el JWT. Sin token válido, AuthMiddleware corta la respuesta.
        $payload = AuthMiddleware::handle();

        try {
            // El service decide si el usuario puede ver tareas de este curso.
            $tasks = $this->taskService->getTasksByCourse($courseId, $payload);
            http_response_code(200);
            echo json_encode($tasks);
        } catch (Exception $e) {
            http_response_code(403);
            echo json_encode(['error' => $e->getMessage()]);
        }
    }

    public function create(int $courseId): void {
        $payload = AuthMiddleware::handle();
        AuthMiddleware::requireRole($payload, 'teacher');

        $contentType = $_SERVER['CONTENT_TYPE'] ?? '';
        $isMultipart = stripos($contentType, 'multipart/form-data') !== false;
        $file = null;

        if ($isMultipart) {
            $data = $_POST;
            $file = $_FILES['attachment'] ?? null;
        } else {
            // En PHP, php://input contiene el JSON enviado en el body.
            $data = json_decode(file_get_contents('php://input'), true);
        }

        if (!$data || !isset($data['title'], $data['due_date'])) {
            http_response_code(400);
            echo json_encode(['error' => 'Debe ingresar un título y una fecha límite para crear la tarea.']);
            return;
        }

        try {
            // La tarea solo se crea si el teacher es duenio del curso.
            $task = $this->taskService->createTask($courseId, $data, $payload, $file);
            http_response_code(200);
            echo json_encode($task);
        } catch (Exception $e) {
            http_response_code(403);
            echo json_encode(['error' => $e->getMessage()]);
        }
    }

    public function downloadAttachment(int $taskId): void {
        $payload = AuthMiddleware::handle();

        try {
            $path = $this->taskService->getAttachmentDownloadPath($taskId, $payload);
            $contentType = function_exists('mime_content_type')
                ? (mime_content_type($path) ?: 'application/octet-stream')
                : 'application/octet-stream';

            header('Content-Type: ' . $contentType);
            header('Content-Disposition: attachment; filename="' . basename($path) . '"');
            header('Content-Length: ' . filesize($path));
            readfile($path);
            exit;
        } catch (Exception $e) {
            http_response_code(404);
            echo json_encode(['error' => $e->getMessage()]);
        }
    }
}
