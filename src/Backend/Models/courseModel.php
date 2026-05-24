<?php
/*
 * Modelo de cursos.
 * Representa la tabla courses y consultas relacionadas con cursos de profesores/estudiantes.
 */
require_once __DIR__ . '/baseModel.php';

class CourseModel extends BaseModel {
    public function getTableName(): string {
        return 'courses';
    }

    public function create(array $data): int {
        $stmt = $this->db->prepare(
            "INSERT INTO courses (name, description, teacher_id, join_code)
             VALUES (?, ?, ?, ?)"
        );
        $stmt->execute([
            $data['name'],
            $data['description'],
            $data['teacher_id'],
            $data['join_code']
        ]);
        return (int) $this->db->lastInsertId();
    }

    public function findByJoinCode(string $code): ?array {
        return $this->findByField('join_code', $code);
    }

    public function findByTeacherId(int $teacherId): array {
        $stmt = $this->db->prepare("SELECT * FROM courses WHERE teacher_id = ? ORDER BY created_at DESC");
        $stmt->execute([$teacherId]);
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    public function findByStudentId(int $studentId): array {
        $stmt = $this->db->prepare(
            "SELECT c.* FROM courses c
             JOIN enrollments cs ON c.id = cs.course_id
             WHERE cs.student_id = ? ORDER BY c.created_at DESC"
        );
        $stmt->execute([$studentId]);
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    public function update(int $id, array $data): bool {
        $fields = [];
        $values = [];
        foreach ($data as $key => $value) {
            $fields[] = "$key = ?";
            $values[] = $value;
        }
        $values[] = $id;
        $stmt = $this->db->prepare("UPDATE courses SET " . implode(', ', $fields) . " WHERE id = ?");
        return $stmt->execute($values);
    }
}
