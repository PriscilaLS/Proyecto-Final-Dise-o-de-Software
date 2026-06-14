<?php
/*
 * Repositorio de entregas.
 * Encapsula consultas SQL para entregas principales y sus versiones.
 */
require_once __DIR__ . '/baseRepository.php';
require_once __DIR__ . '/../Models/submissionModel.php';
require_once __DIR__ . '/../Models/submissionVersionModel.php';

class SubmissionRepository extends BaseRepository {
    private SubmissionModel $submissionModel;
    private SubmissionVersionModel $versionModel;

    public function __construct() {
        $this->submissionModel = new SubmissionModel();
        $this->versionModel = new SubmissionVersionModel();
        parent::__construct($this->submissionModel);
    }

    public function save(array $data): int {
        return $this->submissionModel->create($data);
    }

    public function findAll(): array {
        $stmt = $this->submissionModel->db->prepare("SELECT * FROM submissions ORDER BY updated_at DESC");
        $stmt->execute();
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    public function findByTaskId(int $taskId): array {
        $stmt = $this->submissionModel->db->prepare(
            "SELECT s.id AS id,
                    s.id AS submission_id,
                    s.task_id,
                    s.student_id,
                    u.name AS student,
                    latest.id AS latest_version_id,
                    latest.version_number AS latest_version_number,
                    latest.file_path,
                    latest.submitted_at,
                    latest.is_late,
                    counts.total_versions
             FROM submissions s
             JOIN users u ON u.id = s.student_id
             JOIN (
                SELECT sv.*
                FROM submission_versions sv
                JOIN (
                    SELECT submission_id, MAX(version_number) AS version_number
                    FROM submission_versions
                    GROUP BY submission_id
                ) mx ON mx.submission_id = sv.submission_id
                    AND mx.version_number = sv.version_number
             ) latest ON latest.submission_id = s.id
             JOIN (
                SELECT submission_id, COUNT(*) AS total_versions
                FROM submission_versions
                GROUP BY submission_id
             ) counts ON counts.submission_id = s.id
             WHERE s.task_id = ?
             ORDER BY latest.submitted_at DESC"
        );
        $stmt->execute([$taskId]);
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    public function findByTaskAndStudent(int $taskId, int $studentId): array {
        $stmt = $this->submissionModel->db->prepare(
            "SELECT sv.id AS id,
                    s.id AS submission_id,
                    sv.id AS version_id,
                    sv.version_number,
                    s.task_id,
                    s.student_id,
                    sv.file_path,
                    sv.submitted_at,
                    sv.is_late
             FROM submissions s
             JOIN submission_versions sv ON sv.submission_id = s.id
             WHERE s.task_id = ? AND s.student_id = ?
             ORDER BY sv.version_number DESC"
        );
        $stmt->execute([$taskId, $studentId]);
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    public function findSubmissionByTaskAndStudent(int $taskId, int $studentId): ?array {
        return $this->submissionModel->findByTaskAndStudent($taskId, $studentId);
    }

    public function createSubmission(int $taskId, int $studentId): int {
        return $this->submissionModel->create([
            'task_id' => $taskId,
            'student_id' => $studentId
        ]);
    }

    public function touchSubmission(int $submissionId): void {
        $stmt = $this->submissionModel->db->prepare(
            "UPDATE submissions SET updated_at = CURRENT_TIMESTAMP WHERE id = ?"
        );
        $stmt->execute([$submissionId]);
    }

    public function getNextVersionNumber(int $submissionId): int {
        $stmt = $this->submissionModel->db->prepare(
            "SELECT COALESCE(MAX(version_number), 0) + 1
             FROM submission_versions
             WHERE submission_id = ?"
        );
        $stmt->execute([$submissionId]);
        return (int) $stmt->fetchColumn();
    }

    public function saveVersion(array $data): int {
        return $this->versionModel->create($data);
    }

    public function findVersionsBySubmissionId(int $submissionId): array {
        return $this->versionModel->findBySubmissionId($submissionId);
    }

    public function findLatestVersionBySubmissionId(int $submissionId): ?array {
        return $this->versionModel->findLatestBySubmissionId($submissionId);
    }

    public function findVersionById(int $versionId): ?array {
        return $this->versionModel->findById($versionId);
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

    public function teacherOwnsSubmission(int $submissionId, int $teacherId): bool {
        $stmt = $this->submissionModel->db->prepare(
            "SELECT COUNT(*)
             FROM submissions s
             JOIN tasks t ON t.id = s.task_id
             JOIN courses c ON c.id = t.course_id
             WHERE s.id = ? AND c.teacher_id = ?"
        );
        $stmt->execute([$submissionId, $teacherId]);
        return $stmt->fetchColumn() > 0;
    }

    public function studentOwnsSubmission(int $submissionId, int $studentId): bool {
        $stmt = $this->submissionModel->db->prepare(
            "SELECT COUNT(*)
             FROM submissions
             WHERE id = ? AND student_id = ?"
        );
        $stmt->execute([$submissionId, $studentId]);
        return $stmt->fetchColumn() > 0;
    }

    public function teacherOwnsVersion(int $versionId, int $teacherId): bool {
        $stmt = $this->submissionModel->db->prepare(
            "SELECT COUNT(*)
             FROM submission_versions sv
             JOIN submissions s ON s.id = sv.submission_id
             JOIN tasks t ON t.id = s.task_id
             JOIN courses c ON c.id = t.course_id
             WHERE sv.id = ? AND c.teacher_id = ?"
        );
        $stmt->execute([$versionId, $teacherId]);
        return $stmt->fetchColumn() > 0;
    }

    public function studentOwnsVersion(int $versionId, int $studentId): bool {
        $stmt = $this->submissionModel->db->prepare(
            "SELECT COUNT(*)
             FROM submission_versions sv
             JOIN submissions s ON s.id = sv.submission_id
             WHERE sv.id = ? AND s.student_id = ?"
        );
        $stmt->execute([$versionId, $studentId]);
        return $stmt->fetchColumn() > 0;
    }

    public function beginTransaction(): void {
        $this->submissionModel->db->beginTransaction();
    }

    public function commit(): void {
        $this->submissionModel->db->commit();
    }

    public function rollBack(): void {
        if ($this->submissionModel->db->inTransaction()) {
            $this->submissionModel->db->rollBack();
        }
    }
}
