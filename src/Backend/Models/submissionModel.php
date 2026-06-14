<?php
/*
 * Modelo de entregas.
 * Representa la entrega principal entre una tarea y un estudiante.
 */
require_once __DIR__ . '/baseModel.php';

class SubmissionModel extends BaseModel {
    public function getTableName(): string {
        return 'submissions';
    }

    public function create(array $data): int {
        $stmt = $this->db->prepare(
            "INSERT INTO submissions (task_id, student_id)
             VALUES (?, ?)"
        );
        $stmt->execute([
            $data['task_id'],
            $data['student_id']
        ]);
        return (int) $this->db->lastInsertId();
    }

    public function findByTaskAndStudent(int $taskId, int $studentId): ?array {
        $stmt = $this->db->prepare(
            "SELECT id, task_id, student_id, created_at, updated_at
             FROM submissions
             WHERE task_id = ? AND student_id = ?"
        );
        $stmt->execute([$taskId, $studentId]);
        return $stmt->fetch(PDO::FETCH_ASSOC) ?: null;
    }
}
