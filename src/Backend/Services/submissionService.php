<?php
/*
 * Servicio de entregas.
 * Valida permisos, valida archivos ZIP, guarda entregas y calcula si fueron tardías.
 */
require_once __DIR__ . '/../Repositories/submissionRepository.php';

class SubmissionService {
    private SubmissionRepository $submissionRepo;
    private string $uploadDir;

    public function __construct() {
        $this->submissionRepo = new SubmissionRepository();
        $this->uploadDir = __DIR__ . '/../Submissions';
    }

    public function submitProject(int $taskId, array $file, array $payload): array {
        // Regla: el estudiante solo puede entregar tareas de cursos donde está matriculado.
        if (!$this->submissionRepo->studentIsEnrolledInTaskCourse($taskId, (int) $payload['id'])) {
            throw new Exception('No puedes entregar esta tarea');
        }

        $task = $this->submissionRepo->findTaskById($taskId);
        if (!$task) {
            throw new Exception('Tarea no encontrada');
        }

        $this->validateZip($file);

        // La carpeta Submissions guarda fisicamente los ZIPs enviados.
        if (!is_dir($this->uploadDir)) {
            mkdir($this->uploadDir, 0775, true);
        }

        $targetPath = null;

        try {
            $this->submissionRepo->beginTransaction();

            $submission = $this->submissionRepo->findSubmissionByTaskAndStudent($taskId, (int) $payload['id']);
            if (!$submission) {
                $submissionId = $this->submissionRepo->createSubmission($taskId, (int) $payload['id']);
            } else {
                $submissionId = (int) $submission['id'];
            }

            $versionNumber = $this->submissionRepo->getNextVersionNumber($submissionId);
            $timestamp = date('Ymd_His');
            $filename = "task_{$taskId}_student_{$payload['id']}_v{$versionNumber}_{$timestamp}.zip";
            $targetPath = $this->uploadDir . DIRECTORY_SEPARATOR . $filename;

            if (!move_uploaded_file($file['tmp_name'], $targetPath)) {
                throw new Exception('No se pudo guardar el archivo');
            }

            // La puntualidad se guarda por version, porque cada reenvio puede ser a tiempo o tarde.
            $submittedAt = date('Y-m-d H:i:s');
            $isLate = strtotime($submittedAt) > strtotime($task['due_date']);
            $relativePath = 'Submissions/' . $filename;

            $versionId = $this->submissionRepo->saveVersion([
                'submission_id' => $submissionId,
                'version_number' => $versionNumber,
                'file_path' => $relativePath,
                'is_late' => $isLate
            ]);

            $this->submissionRepo->touchSubmission($submissionId);
            $this->submissionRepo->commit();
        } catch (Exception $e) {
            $this->submissionRepo->rollBack();

            if ($targetPath && is_file($targetPath)) {
                unlink($targetPath);
            }

            throw $e;
        }

        return [
            // id se conserva como alias para clientes existentes.
            'id' => $submissionId,
            'submission_id' => $submissionId,
            'version_id' => $versionId,
            'version_number' => $versionNumber,
            'is_late' => $isLate,
            'submitted_at' => $submittedAt
        ];
    }

    public function getSubmissionsByTask(int $taskId, array $payload): array {
        if (!$this->submissionRepo->teacherOwnsTask($taskId, (int) $payload['id'])) {
            throw new Exception('No puedes ver las entregas de esta tarea');
        }

        return $this->submissionRepo->findByTaskId($taskId);
    }

    public function getMySubmissionsByTask(int $taskId, array $payload): array {
        if (!$this->submissionRepo->studentIsEnrolledInTaskCourse($taskId, (int) $payload['id'])) {
            throw new Exception('No puedes ver las entregas de esta tarea');
        }

        return $this->submissionRepo->findByTaskAndStudent($taskId, (int) $payload['id']);
    }

    public function getVersionsBySubmission(int $submissionId, array $payload): array {
        $submission = $this->submissionRepo->findById($submissionId);
        if (!$submission) {
            throw new Exception('Entrega no encontrada');
        }

        $role = $payload['role'] ?? '';
        $userId = (int) $payload['id'];
        $canView = false;

        if ($role === 'teacher') {
            $canView = $this->submissionRepo->teacherOwnsSubmission($submissionId, $userId);
        } elseif ($role === 'student') {
            $canView = $this->submissionRepo->studentOwnsSubmission($submissionId, $userId);
        }

        if (!$canView) {
            throw new Exception('No puedes ver las versiones de esta entrega');
        }

        return $this->submissionRepo->findVersionsBySubmissionId($submissionId);
    }

    public function getDownloadPath(int $submissionId, array $payload): string {
        $submission = $this->submissionRepo->findById($submissionId);
        if (!$submission) {
            throw new Exception('Entrega no encontrada');
        }

        if (!$this->submissionRepo->teacherOwnsTask((int) $submission['task_id'], (int) $payload['id'])) {
            throw new Exception('No puedes descargar esta entrega');
        }

        $version = $this->submissionRepo->findLatestVersionBySubmissionId($submissionId);
        if (!$version) {
            throw new Exception('La entrega no tiene versiones');
        }

        $fullPath = __DIR__ . '/../' . $version['file_path'];
        if (!is_file($fullPath)) {
            throw new Exception('Archivo de entrega no encontrado');
        }

        return $fullPath;
    }

    public function getVersionDownloadPath(int $versionId, array $payload): string {
        $version = $this->submissionRepo->findVersionById($versionId);
        if (!$version) {
            throw new Exception('Version de entrega no encontrada');
        }

        $role = $payload['role'] ?? '';
        $userId = (int) $payload['id'];
        $canDownload = false;

        if ($role === 'teacher') {
            $canDownload = $this->submissionRepo->teacherOwnsVersion($versionId, $userId);
        } elseif ($role === 'student') {
            $canDownload = $this->submissionRepo->studentOwnsVersion($versionId, $userId);
        }

        if (!$canDownload) {
            throw new Exception('No puedes descargar esta version');
        }

        $fullPath = __DIR__ . '/../' . $version['file_path'];
        if (!is_file($fullPath)) {
            throw new Exception('Archivo de entrega no encontrado');
        }

        return $fullPath;
    }

    private function validateZip(array $file): void {
        // UPLOAD_ERR_OK significa que PHP recibio el archivo correctamente.
        if (!isset($file['error']) || $file['error'] !== UPLOAD_ERR_OK) {
            throw new Exception('Archivo invalido');
        }

        $extension = strtolower(pathinfo($file['name'], PATHINFO_EXTENSION));
        if ($extension !== 'zip') {
            throw new Exception('Solo se aceptan archivos ZIP');
        }
    }
}
