<?php
/*
 * Modelo de tareas.
 * Representa la tabla tasks y permite crear/listar tareas por curso.
 */
require_once __DIR__ . '/baseModel.php';

class TaskModel extends BaseModel {
    public function getTableName(): string {
        return 'tasks';
    }

    public function create(array $data): int {
        $stmt = $this->db->prepare(
            "INSERT INTO tasks (course_id, title, description, due_date, attachment_path)
             VALUES (?, ?, ?, ?)"
        );
        $stmt->execute([
            $data['course_id'],
            $data['title'],
            $data['description'] ?? null,
            $data['due_date'],
            $data['attachment_path'] ?? null
            
        ]);
        return (int) $this->db->lastInsertId();
    }

    public function findByCourseId(int $courseId): array {
        $stmt = $this->db->prepare(
            "SELECT id, course_id, title, description, due_date, created_at, attachment_path
             FROM tasks
             WHERE course_id = ?
             ORDER BY due_date ASC"
        );
        $stmt->execute([$courseId]);
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }
}
