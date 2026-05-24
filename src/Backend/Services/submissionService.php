<?php
require_once __DIR__ . '/../Repositories/submissionRepository.php';

class SubmissionService {
    private SubmissionRepository $submissionRepo;
    private string $uploadDir;

    public function __construct() {
        $this->submissionRepo = new SubmissionRepository();
        $this->uploadDir = __DIR__ . '/../Submissions';
    }

    public function submitProject(int $taskId, array $file, array $payload): array {
        // Regla: el estudiante solo puede entregar tareas de cursos donde esta matriculado.
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

        $timestamp = date('Ymd_His');
        $filename = "task_{$taskId}_student_{$payload['id']}_{$timestamp}.zip";
        $targetPath = $this->uploadDir . DIRECTORY_SEPARATOR . $filename;

        if (!move_uploaded_file($file['tmp_name'], $targetPath)) {
            throw new Exception('No se pudo guardar el archivo');
        }

        // La BD guarda la ruta relativa y si la entrega fue tarde.
        $isLate = strtotime(date('Y-m-d H:i:s')) > strtotime($task['due_date']);
        $relativePath = 'Submissions/' . $filename;

        $submissionId = $this->submissionRepo->save([
            'task_id' => $taskId,
            'student_id' => (int) $payload['id'],
            'file_path' => $relativePath,
            'is_late' => $isLate
        ]);

        return [
            'id' => $submissionId,
            'is_late' => $isLate,
            'submitted_at' => date('Y-m-d H:i:s')
        ];
    }

    public function getSubmissionsByTask(int $taskId, array $payload): array {
        if (!$this->submissionRepo->teacherOwnsTask($taskId, (int) $payload['id'])) {
            throw new Exception('No puedes ver las entregas de esta tarea');
        }

        return $this->submissionRepo->findByTaskId($taskId);
    }

    public function getDownloadPath(int $submissionId, array $payload): string {
        $submission = $this->submissionRepo->findById($submissionId);
        if (!$submission) {
            throw new Exception('Entrega no encontrada');
        }

        if (!$this->submissionRepo->teacherOwnsTask((int) $submission['task_id'], (int) $payload['id'])) {
            throw new Exception('No puedes descargar esta entrega');
        }

        $fullPath = __DIR__ . '/../' . $submission['file_path'];
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
