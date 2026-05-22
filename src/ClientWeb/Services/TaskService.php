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
}