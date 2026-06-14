<?php

class ApiClient
{
    private string $baseUrl;

    public function __construct()
    {
        $host = $_SERVER['HTTP_HOST'] ?? 'localhost';
        $scheme = (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off') ? 'https' : 'http';
        $this->baseUrl = 'http://localhost:5500/app.php';
    }

    private function request(string $method, string $endpoint, array $data = [], bool $auth = false)
    {
        $url = $this->baseUrl . $endpoint;
        $ch = curl_init($url);

        $headers = ['Content-Type: application/json'];

        if ($auth && isset($_SESSION['token'])) {
            $headers[] = 'Authorization: Bearer ' . $_SESSION['token'];
        }

        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_CUSTOMREQUEST, $method);
        curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);
        curl_setopt($ch, CURLOPT_TIMEOUT, 30);

        if (!empty($data)) {
            curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
        }

        $response = curl_exec($ch);
        $error = curl_error($ch);
        $status = curl_getinfo($ch, CURLINFO_HTTP_CODE);

        curl_close($ch);

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

    public function get(string $endpoint, bool $auth = false)
    {
        return $this->request('GET', $endpoint, [], $auth);
    }

    public function post(string $endpoint, array $data = [], bool $auth = false)
    {
        return $this->request('POST', $endpoint, $data, $auth);
    }
}
