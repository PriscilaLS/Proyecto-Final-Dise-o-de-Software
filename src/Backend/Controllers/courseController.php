<?php
/*
 * Controlador de cursos.
 * Maneja crear cursos, listar cursos del usuario y matricular estudiantes con código.
 */
require_once __DIR__ . '/../Services/courseService.php';
require_once __DIR__ . '/../Middleware/authMiddleware.php';

class CourseController {
    private CourseService $courseService;
    
    public function __construct() {
        $this->courseService = new CourseService();
    }

    public function create(): void {
        // Crear cursos requiere token válido y rol teacher.
        $payload = AuthMiddleware::handle();
        AuthMiddleware::requireRole($payload, 'teacher');

        $data = json_decode(file_get_contents('php://input'), true);

        if (!$data || !isset($data['name'])) {
            http_response_code(400);
            echo json_encode(['error' => 'Nombre del curso es requerido']);
            return;
        }
        try {
            $result = $this->courseService->createCourse($data, $payload['id']);
            http_response_code(200);
            echo json_encode($result);
        } catch (Exception $e) {
            http_response_code(400);
            echo json_encode(['error' => $e->getMessage()]);
        }
    }

    public function getMyCourses(): void {
        // Con el role del JWT, el service decide si busca cursos creados o matriculados.
        $payload = AuthMiddleware::handle();
        try {
            $courses = $this->courseService->getMyCourses(
                $payload['id'],
                $payload['role']
            );
            http_response_code(200);
            echo json_encode($courses);
        } catch (Exception $e) {
            http_response_code(400);
            echo json_encode(['error' => $e->getMessage()]);
        }
    }

    public function join(): void {
        // Solo estudiantes pueden matricularse con código de curso.
        $payload = AuthMiddleware::handle();
        AuthMiddleware::requireRole($payload, 'student');

        $data = json_decode(file_get_contents('php://input'), true);

        if (!$data || !isset($data['join_code'])) {
            http_response_code(400);
            echo json_encode(['error' => 'Código de curso es requerido']);
            return;
        }

        try {
            $this->courseService->joinCourse($data['join_code'], $payload['id']);
            http_response_code(200);
            echo json_encode(['message' => 'Te has unido al curso exitosamente']);
        } catch (Exception $e) {
            http_response_code(400);
            echo json_encode(['error' => $e->getMessage()]);
        }
    }

        
}
