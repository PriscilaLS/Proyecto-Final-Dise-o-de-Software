<?php
require_once __DIR__ . '/baseRepository.php';
require_once __DIR__ . '/../Models/taskModel.php';
require_once __DIR__ . '/../Models/courseModel.php';

class TaskRepository extends BaseRepository {
    private TaskModel $taskModel;
    private CourseModel $courseModel;

    public function __construct() {
        $this->taskModel = new TaskModel();
        $this->courseModel = new CourseModel();
        parent::__construct($this->taskModel);
    }

    public function save(array $data): int {
        return $this->taskModel->create($data);
    }

    public function findAll(): array {
        $stmt = $this->taskModel->db->prepare("SELECT * FROM tasks ORDER BY due_date ASC");
        $stmt->execute();
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    public function findByCourseId(int $courseId): array {
        return $this->taskModel->findByCourseId($courseId);
    }

    public function userCanAccessCourse(int $courseId, int $userId, string $role): bool {
        // Teacher: tiene acceso si el curso le pertenece.
        if ($role === 'teacher') {
            $stmt = $this->taskModel->db->prepare(
                "SELECT COUNT(*) FROM courses WHERE id = ? AND teacher_id = ?"
            );
            $stmt->execute([$courseId, $userId]);
            return $stmt->fetchColumn() > 0;
        }

        // Student: tiene acceso si esta matriculado en enrollments.
        $stmt = $this->taskModel->db->prepare(
            "SELECT COUNT(*) FROM enrollments WHERE course_id = ? AND student_id = ?"
        );
        $stmt->execute([$courseId, $userId]);
        return $stmt->fetchColumn() > 0;
    }

    public function teacherOwnsCourse(int $courseId, int $teacherId): bool {
        $stmt = $this->taskModel->db->prepare(
            "SELECT COUNT(*) FROM courses WHERE id = ? AND teacher_id = ?"
        );
        $stmt->execute([$courseId, $teacherId]);
        return $stmt->fetchColumn() > 0;
    }
}
