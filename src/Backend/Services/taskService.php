<?php
/*
 * Servicio de tareas.
 * Aplica permisos para listar o crear tareas dentro de cursos.
 */
require_once __DIR__ . '/../Repositories/taskRepository.php';

class TaskService {
    private TaskRepository $taskRepo;
    private string $attachmentDir;

    public function __construct() {
        $this->taskRepo = new TaskRepository();
        $this->attachmentDir = __DIR__ . '/../uploads/task_attachments';
    }

    public function getTasksByCourse(int $courseId, array $payload): array {
        // payload viene del JWT. Aqui trae id, role y exp del usuario.
        if (!$this->taskRepo->userCanAccessCourse($courseId, (int) $payload['id'], $payload['role'])) {
            throw new Exception('No tienes acceso a este curso');
        }

        return $this->taskRepo->findByCourseId($courseId);
    }

    public function createTask(int $courseId, array $data, array $payload, ?array $file = null): array {
        // Regla de negocio: solo el teacher propietario puede crear tareas.
        if (!$this->taskRepo->teacherOwnsCourse($courseId, (int) $payload['id'])) {
            throw new Exception('No puedes crear tareas en este curso');
        }

        $attachmentPath = $this->storeAttachment($file);

        try {
            $taskId = $this->taskRepo->save([
                'course_id' => $courseId,
                'title' => $data['title'],
                'description' => $data['description'] ?? null,
                'due_date' => $data['due_date'],
                'attachment_path' => $attachmentPath
            ]);
        } catch (Exception $e) {
            if ($attachmentPath !== null) {
                $this->deleteStoredAttachment($attachmentPath);
            }

            throw $e;
        }

        return [
            'id' => $taskId,
            'course_id' => $courseId,
            'title' => $data['title'],
            'description' => $data['description'] ?? null,
            'due_date' => $data['due_date'],
            'attachment_path' => $attachmentPath
        ];
    }

    public function getAttachmentDownloadPath(int $taskId, array $payload): string {
        $task = $this->taskRepo->findById($taskId);
        if (!$task) {
            throw new Exception('Tarea no encontrada');
        }

        if (!$this->taskRepo->userCanAccessCourse((int) $task['course_id'], (int) $payload['id'], $payload['role'])) {
            throw new Exception('No tienes acceso a esta tarea');
        }

        $attachmentPath = $task['attachment_path'] ?? null;
        if (!$attachmentPath) {
            throw new Exception('La tarea no tiene archivo de apoyo');
        }

        return $this->resolveAttachmentPath($attachmentPath);
    }

    private function storeAttachment(?array $file): ?string {
        if ($file === null || !isset($file['error']) || $file['error'] === UPLOAD_ERR_NO_FILE) {
            return null;
        }

        if ($file['error'] !== UPLOAD_ERR_OK) {
            throw new Exception('Archivo adjunto invalido');
        }

        if (empty($file['tmp_name']) || !is_uploaded_file($file['tmp_name'])) {
            throw new Exception('Archivo adjunto invalido');
        }

        if (!is_dir($this->attachmentDir) && !mkdir($this->attachmentDir, 0775, true)) {
            throw new Exception('No se pudo crear la carpeta de adjuntos');
        }

        $filename = $this->buildAttachmentFilename($file['name'] ?? 'archivo');
        $targetPath = $this->attachmentDir . DIRECTORY_SEPARATOR . $filename;

        if (!move_uploaded_file($file['tmp_name'], $targetPath)) {
            throw new Exception('No se pudo guardar el archivo adjunto');
        }

        return 'uploads/task_attachments/' . $filename;
    }

    private function buildAttachmentFilename(string $originalName): string {
        $basename = basename($originalName);
        $extension = pathinfo($basename, PATHINFO_EXTENSION);
        $name = pathinfo($basename, PATHINFO_FILENAME);

        $safeName = preg_replace('/[^A-Za-z0-9_-]/', '_', $name);
        $safeExtension = preg_replace('/[^A-Za-z0-9]/', '', $extension);

        $safeName = trim((string) $safeName, '_');
        if ($safeName === '') {
            $safeName = 'archivo';
        }

        $safeName = substr($safeName, 0, 80);
        $suffix = $safeExtension !== '' ? '.' . strtolower($safeExtension) : '';

        return date('Ymd_His') . '_' . bin2hex(random_bytes(4)) . '_' . $safeName . $suffix;
    }

    private function resolveAttachmentPath(string $relativePath): string {
        $filename = basename($relativePath);
        if ($relativePath !== 'uploads/task_attachments/' . $filename) {
            throw new Exception('Ruta de archivo invalida');
        }

        $baseDir = realpath($this->attachmentDir);
        if ($baseDir === false) {
            throw new Exception('Archivo de apoyo no encontrado');
        }

        $fullPath = $baseDir . DIRECTORY_SEPARATOR . $filename;
        $realPath = realpath($fullPath);
        if ($realPath === false || strpos($realPath, $baseDir . DIRECTORY_SEPARATOR) !== 0 || !is_file($realPath)) {
            throw new Exception('Archivo de apoyo no encontrado');
        }

        return $realPath;
    }

    private function deleteStoredAttachment(string $relativePath): void {
        $filename = basename($relativePath);
        if ($relativePath !== 'uploads/task_attachments/' . $filename) {
            return;
        }

        $path = $this->attachmentDir . DIRECTORY_SEPARATOR . $filename;
        if (is_file($path)) {
            unlink($path);
        }
    }
}
