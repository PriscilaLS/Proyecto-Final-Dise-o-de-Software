<?php

require_once 'Services/ApiClient.php';

class CourseService
{
    private ApiClient $client;

    public function __construct()
    {
        $this->client = new ApiClient();
    }

    public function getCourses()
    {
        return $this->client->get('/courses/me', true);
    }

    public function createCourse($name, $description)
    {
        return $this->client->post('/courses', [

            'name' => $name,
            'description' => $description

        ], true);
    }
}