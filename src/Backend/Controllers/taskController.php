<?php
require_once __DIR__ . '/../Services/taskService.php';
require_once __DIR__ . '/../Middleware/authMiddleware.php';

class TaskController {
    private TaskService $taskService;

    public function __construct() {
        $this->taskService = new TaskService();
    }

    public function getByCourse(int $courseId): void {
        // Primero se valida el JWT. Sin token valido, AuthMiddleware corta la respuesta.
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

        // En PHP, php://input contiene el JSON enviado en el body.
        $data = json_decode(file_get_contents('php://input'), true);
        if (!$data || !isset($data['title'], $data['due_date'])) {
            http_response_code(400);
            echo json_encode(['error' => 'Titulo y fecha limite son requeridos']);
            return;
        }

        try {
            // La tarea solo se crea si el teacher es duenio del curso.
            $task = $this->taskService->createTask($courseId, $data, $payload);
            http_response_code(200);
            echo json_encode($task);
        } catch (Exception $e) {
            http_response_code(403);
            echo json_encode(['error' => $e->getMessage()]);
        }
    }
}
