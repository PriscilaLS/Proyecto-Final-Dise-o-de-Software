<?php
require_once __DIR__ . '/baseRepository.php';
require_once __DIR__ . '/../Models/submissionModel.php';

class SubmissionRepository extends BaseRepository {
    private SubmissionModel $submissionModel;

    public function __construct() {
        $this->submissionModel = new SubmissionModel();
        parent::__construct($this->submissionModel);
    }

    public function save(array $data): int {
        return $this->submissionModel->create($data);
    }

    public function findAll(): array {
        $stmt = $this->submissionModel->db->prepare("SELECT * FROM submissions ORDER BY submitted_at DESC");
        $stmt->execute();
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    public function findByTaskId(int $taskId): array {
        return $this->submissionModel->findByTaskId($taskId);
    }

    public function findTaskById(int $taskId): ?array {
        $stmt = $this->submissionModel->db->prepare(
            "SELECT t.*, c.teacher_id
             FROM tasks t
             JOIN courses c ON c.id = t.course_id
             WHERE t.id = ?"
        );
        $stmt->execute([$taskId]);
        return $stmt->fetch(PDO::FETCH_ASSOC) ?: null;
    }

    public function studentIsEnrolledInTaskCourse(int $taskId, int $studentId): bool {
        $stmt = $this->submissionModel->db->prepare(
            "SELECT COUNT(*)
             FROM tasks t
             JOIN enrollments e ON e.course_id = t.course_id
             WHERE t.id = ? AND e.student_id = ?"
        );
        $stmt->execute([$taskId, $studentId]);
        return $stmt->fetchColumn() > 0;
    }

    public function teacherOwnsTask(int $taskId, int $teacherId): bool {
        $stmt = $this->submissionModel->db->prepare(
            "SELECT COUNT(*)
             FROM tasks t
             JOIN courses c ON c.id = t.course_id
             WHERE t.id = ? AND c.teacher_id = ?"
        );
        $stmt->execute([$taskId, $teacherId]);
        return $stmt->fetchColumn() > 0;
    }
}
