<?php
/*
 * Repositorio de cursos.
 * Encapsula consultas de cursos, matrículas y validaciones de inscripción.
 */
require_once __DIR__ . '/baseRepository.php';
require_once __DIR__ . '/../Models/courseModel.php';

class CourseRepository extends BaseRepository {
    private CourseModel $courseModel;
    public function __construct() {
        $this->courseModel = new CourseModel();
        parent::__construct($this->courseModel);
    }

    public function save (array $data): int {
        return $this->courseModel->create($data);
    }

    public function findAll():array {
        $stmt = $this->courseModel->db->prepare("SELECT * FROM courses ORDER BY created_at DESC");
        $stmt->execute();
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    public function findByJoinCode(string $code): ?array {
        return $this->courseModel->findByJoinCode($code);
    }

    public function findByTeacherId(int $teacherId): array {
        return $this->courseModel->findByTeacherId($teacherId);
    }

    public function findByStudentId(int $studentId): array {
        return $this->courseModel->findByStudentId($studentId);
    }

    public function enroll(int $studentId, int $courseId): bool {
        $stmt = $this->courseModel->db->prepare(
            "INSERT INTO enrollments (student_id, course_id) VALUES (?, ?)"
        );
        return $stmt->execute([$studentId, $courseId]);
    }

    public function isEnrolled(int $studentId, int $courseId): bool {
        $stmt = $this->courseModel->db->prepare(
            "SELECT COUNT(*) FROM enrollments WHERE student_id = ? AND course_id = ?"
        );
        $stmt->execute([$studentId, $courseId]);
        return $stmt->fetchColumn() > 0;
    }
}

