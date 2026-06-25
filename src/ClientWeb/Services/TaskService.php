<?php

require_once 'Services/ApiClient.php';

class TaskService
{
    private ApiClient $client;

    public function __construct()
    {
        $this->client = new ApiClient();
    }

    public function getTasks($courseId)
    {
        return $this->client->get('/courses/' . $courseId . '/tasks', true);
    }

    public function createTask($courseId, $title, $description, $dueDate, $attachmentPath)
    {
        return $this->client->post('/courses/' . $courseId . '/tasks', [
            'title' => $title,
            'description' => $description,
            'due_date' => $dueDate,
            'attachment_path' => $attachmentPath
            

        ], true);
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
