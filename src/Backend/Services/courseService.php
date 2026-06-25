<?php
/*
 * Servicio de cursos.
 * Aplica reglas para crear cursos, listar cursos segun rol y unir estudiantes por codigo.
 */
require_once __DIR__ . '/../Repositories/courseRepository.php';

class CourseService {
    private CourseRepository $courseRepo;

    public function __construct() {
        $this->courseRepo = new CourseRepository();
    }

    public function createCourse(array $data, int $teacherId): array {
        // join_code es el código que usa un estudiante para unirse al curso.
        $joinCode = $this->generateJoinCode();
        $courseId = $this->courseRepo->save([
            'name' => $data['name'],
            'description' => $data['description'] ?? null,
            'teacher_id' => $teacherId,
            'join_code' => $joinCode
        ]);

        return [
            'id' => $courseId,
            'name' => $data['name'],
            'description' => $data['description'] ?? null,
            'teacher_id' => $teacherId,
            'join_code' => $joinCode
        ];
    }

    public function getMyCourses(int $userID, string $role): array {
        // Teacher ve cursos que creó, student ve cursos donde está matriculado.
        if ($role === 'teacher') {
            return $this->courseRepo->findByTeacherId($userID);
        } else {
            return $this->courseRepo->findByStudentId($userID);
        }
    }

    public function joinCourse(string $joinCode, int $studentId): void {
        $joinCode = strtoupper(trim($joinCode));
        $joinCode = preg_replace('/\s+/u', '', $joinCode);

        // Busca el curso por código y evita matriculas duplicadas.
        $course = $this->courseRepo->findByJoinCode($joinCode);
        if (!$course) {
            throw new Exception('Código de curso inválido');
        }
        if ($this->courseRepo->isEnrolled($studentId, $course['id'])) {
            throw new Exception('Ya estás inscrito en este curso');
        }
        $this->courseRepo->enroll($studentId, $course['id']);
    }

    private function generateJoinCode(): string {
        $chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
        $code = '';
        for ($i = 0; $i < 6; $i++) {
            $code .= $chars[rand(0, strlen($chars) - 1)];
        }
        return $code;
    }
}
