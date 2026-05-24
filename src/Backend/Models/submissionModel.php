<?php
require_once __DIR__ . '/baseModel.php';

class SubmissionModel extends BaseModel {
    public function getTableName(): string {
        return 'submissions';
    }

    public function create(array $data): int {
        $stmt = $this->db->prepare(
            "INSERT INTO submissions (task_id, student_id, file_path, is_late)
             VALUES (?, ?, ?, ?)"
        );
        $stmt->execute([
            $data['task_id'],
            $data['student_id'],
            $data['file_path'],
            $data['is_late'] ? 1 : 0
        ]);
        return (int) $this->db->lastInsertId();
    }

    public function findByTaskId(int $taskId): array {
        $stmt = $this->db->prepare(
            "SELECT s.id, s.task_id, s.student_id, u.name AS student,
                    s.file_path, s.submitted_at, s.is_late
             FROM submissions s
             JOIN users u ON u.id = s.student_id
             WHERE s.task_id = ?
             ORDER BY s.submitted_at DESC"
        );
        $stmt->execute([$taskId]);
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }
}
