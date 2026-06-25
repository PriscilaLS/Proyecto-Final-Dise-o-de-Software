<?php

require_once 'Services/ApiClient.php';

class TaskService
{
    private ApiClient $client;
    private string $baseUrl;

    public function __construct()
    {
        $this->client = new ApiClient();
        $this->baseUrl = 'http://localhost:5500/app.php';
    }

    public function getTasks($courseId)
    {
        return $this->client->get('/courses/' . $courseId . '/tasks', true);
    }

    public function createTask($courseId, $title, $description, $dueDate, $attachment = null)
    {
        $data = [
            'title' => $title,
            'description' => $description,
            'due_date' => $dueDate
        ];

        if ($this->hasAttachment($attachment)) {
            return $this->postMultipart('/courses/' . $courseId . '/tasks', $data, $attachment);
        }

        if (is_array($attachment) && isset($attachment['error']) && $attachment['error'] !== UPLOAD_ERR_NO_FILE) {
            return ['error' => 'Archivo adjunto invalido'];
        }

        return $this->client->post('/courses/' . $courseId . '/tasks', [
            'title' => $title,
            'description' => $description,
            'due_date' => $dueDate
        ], true);
    }

    private function hasAttachment($attachment): bool
    {
        return is_array($attachment)
            && isset($attachment['error'], $attachment['tmp_name'])
            && $attachment['error'] === UPLOAD_ERR_OK
            && $attachment['tmp_name'] !== '';
    }

    private function postMultipart(string $endpoint, array $data, array $attachment)
    {
        $url = $this->baseUrl . $endpoint;
        $ch = curl_init($url);
        $headers = [];

        if (isset($_SESSION['token'])) {
            $headers[] = 'Authorization: Bearer ' . $_SESSION['token'];
        }

        $data['attachment'] = new CURLFile(
            $attachment['tmp_name'],
            $attachment['type'] ?? 'application/octet-stream',
            $attachment['name'] ?? 'attachment'
        );

        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_POST, true);
        curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);
        curl_setopt($ch, CURLOPT_POSTFIELDS, $data);
        curl_setopt($ch, CURLOPT_TIMEOUT, 30);

        $response = curl_exec($ch);
        $error = curl_error($ch);
        $status = curl_getinfo($ch, CURLINFO_HTTP_CODE);

        if (PHP_VERSION_ID < 80000 && is_resource($ch)) {
            curl_close($ch);
        }

        if ($response === false) {
            return ['error' => 'No se pudo conectar con el backend: ' . $error];
        }

        $decoded = json_decode($response, true);
        if ($decoded === null && json_last_error() !== JSON_ERROR_NONE) {
            return ['error' => 'Respuesta no valida del backend'];
        }

        if ($status >= 400 && is_array($decoded) && !isset($decoded['error'])) {
            $decoded['error'] = 'Error HTTP ' . $status;
        }

        return $decoded;
    }

    public function getSubmissions($taskId)
    {
        return $this->client->get('/tasks/' . $taskId . '/submissions', true);
    }

    public function getSubmissionVersions($submissionId)
    {
        return $this->client->get('/submissions/' . $submissionId . '/versions', true);
    }
}
