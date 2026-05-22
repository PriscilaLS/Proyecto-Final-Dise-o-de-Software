// Conexion con persona 3 esto se modifica despues -


<?php

class ApiClient
{
    private string $baseUrl = "http://localhost/Backend";

    private function request(string $method, string $endpoint, array $data = [], bool $auth = false)
    {
        $url = $this->baseUrl . $endpoint;

        $ch = curl_init($url);

        $headers = [
            'Content-Type: application/json'
        ];

        if ($auth && isset($_SESSION['token'])) {

            $headers[] = 'Authorization: Bearer ' . $_SESSION['token'];
        }

        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);

        curl_setopt($ch, CURLOPT_CUSTOMREQUEST, $method);

        curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);

        if (!empty($data)) {

            curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
        }

        $response = curl_exec($ch);

        curl_close($ch);

        return json_decode($response, true);
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