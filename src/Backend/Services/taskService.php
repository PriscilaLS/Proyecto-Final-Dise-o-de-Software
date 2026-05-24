<?php
/*
 * Servicio de tareas.
 * Aplica permisos para listar o crear tareas dentro de cursos.
 */
require_once __DIR__ . '/../Repositories/taskRepository.php';

class TaskService {
    private TaskRepository $taskRepo;

    public function __construct() {
        $this->taskRepo = new TaskRepository();
    }

    public function getTasksByCourse(int $courseId, array $payload): array {
        // payload viene del JWT. Aqui trae id, role y exp del usuario.
        if (!$this->taskRepo->userCanAccessCourse($courseId, (int) $payload['id'], $payload['role'])) {
            throw new Exception('No tienes acceso a este curso');
        }

        return $this->taskRepo->findByCourseId($courseId);
    }

    public function createTask(int $courseId, array $data, array $payload): array {
        // Regla de negocio: solo el teacher propietario puede crear tareas.
        if (!$this->taskRepo->teacherOwnsCourse($courseId, (int) $payload['id'])) {
            throw new Exception('No puedes crear tareas en este curso');
        }

        $taskId = $this->taskRepo->save([
            'course_id' => $courseId,
            'title' => $data['title'],
            'description' => $data['description'] ?? null,
            'due_date' => $data['due_date']
        ]);

        return [
            'id' => $taskId,
            'course_id' => $courseId,
            'title' => $data['title'],
            'description' => $data['description'] ?? null,
            'due_date' => $data['due_date']
        ];
    }
}
