<?php

require_once 'Services/ApiClient.php';

class AuthService
{
    private ApiClient $client;

    public function __construct()
    {
        $this->client = new ApiClient();
    }

    public function login(string $email, string $password)
    {
        return $this->client->post('/auth/login', [

            'email' => $email,
            'password' => $password

        ]);
    }

    public function register(string $name, string $email, string $password)
    {
        return $this->client->post('/auth/register', [

            'name' => $name,
            'email' => $email,
            'password' => $password

        ]);
    }
}