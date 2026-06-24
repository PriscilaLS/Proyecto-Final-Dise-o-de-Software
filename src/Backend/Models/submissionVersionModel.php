<?php
/*
 * Modelo de versiones de entrega.
 * Cada registro representa un envio o reenvio asociado a una entrega principal.
 */
require_once __DIR__ . '/baseModel.php';

class SubmissionVersionModel extends BaseModel {
    public function getTableName(): string {
        return 'submission_versions';
    }

    public function create(array $data): int {
        $stmt = $this->db->prepare(
            "INSERT INTO submission_versions (submission_id, version_number, file_path, is_late)
             VALUES (?, ?, ?, ?)"
        );
        $stmt->execute([
            $data['submission_id'],
            $data['version_number'],
            $data['file_path'],
            $data['is_late'] ? 1 : 0
        ]);
        return (int) $this->db->lastInsertId();
    }

    public function findById(int $versionId): ?array {
        $stmt = $this->db->prepare(
            "SELECT id, submission_id, version_number, file_path, submitted_at, is_late
             FROM submission_versions
             WHERE id = ?"
        );
        $stmt->execute([$versionId]);
        return $stmt->fetch(PDO::FETCH_ASSOC) ?: null;
    }

    public function findBySubmissionId(int $submissionId): array {
        $stmt = $this->db->prepare(
            "SELECT id, submission_id, version_number, file_path, submitted_at, is_late
             FROM submission_versions
             WHERE submission_id = ?
             ORDER BY version_number DESC"
        );
        $stmt->execute([$submissionId]);
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    public function findLatestBySubmissionId(int $submissionId): ?array {
        $stmt = $this->db->prepare(
            "SELECT id, submission_id, version_number, file_path, submitted_at, is_late
             FROM submission_versions
             WHERE submission_id = ?
             ORDER BY version_number DESC
             LIMIT 1"
        );
        $stmt->execute([$submissionId]);
        return $stmt->fetch(PDO::FETCH_ASSOC) ?: null;
    }
}
