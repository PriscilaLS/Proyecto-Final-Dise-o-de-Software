<?php
require_once __DIR__ . '/../Services/AuthService.php';

class AuthController {
    private AuthService $authService;

    public function __construct() {
        $this->authService = new AuthService();
    }

    public function register(): void {
        $data = json_decode(file_get_contents('php://input'), true);

        if (!$data || !isset($data['name'], $data['email'],
                                $data['password'], $data['role'])) {
            http_response_code(400);
            echo json_encode(['error' => 'Faltan campos obligatorios']);
            return;
        }

        try {
            $result = $this->authService->register($data);
            http_response_code(200);
            echo json_encode([
                'message'     => 'Registro exitoso',
                'private_key' => $result['private_key']
            ]);
        } catch (Exception $e) {
            http_response_code(400);
            echo json_encode(['error' => $e->getMessage()]);
        }
    }

    public function login(): void {
        $data = json_decode(file_get_contents('php://input'), true);

        if (!$data || !isset($data['email'], $data['password'])) {
            http_response_code(400);
            echo json_encode(['error' => 'Faltan campos obligatorios']);
            return;
        }

        try {
            $result = $this->authService->login($data);
            http_response_code(200);
            echo json_encode($result);
        } catch (Exception $e) {
            http_response_code(401);
            echo json_encode(['error' => $e->getMessage()]);
        }
    }
}