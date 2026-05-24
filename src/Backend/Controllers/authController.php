<?php
/*
 * Controlador de autenticacion.
 * Recibe peticiones de registro/login, valida datos basicos y delega la logica a AuthService.
 */
require_once __DIR__ . '/../Services/authService.php';

class AuthController {
    private AuthService $authService;

    public function __construct() {
        $this->authService = new AuthService();
    }

    public function register(): void {
        // Lee el JSON del body y lo convierte a arreglo asociativo de PHP.
        $data = json_decode(file_get_contents('php://input'), true);

        if (!$data || !isset($data['name'], $data['email'], $data['password'])) {
            http_response_code(400);
            echo json_encode(['error' => 'Faltan campos obligatorios']);
            return;
        }

        $data['role'] = $data['role'] ?? 'student';

        try {
            // El controlador no guarda directo en BD: delega la regla al service.
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
        // Para login esperamos JSON con email y password.
        $data = json_decode(file_get_contents('php://input'), true);

        if (!$data || !isset($data['email'], $data['password'])) {
            http_response_code(400);
            echo json_encode(['error' => 'Faltan campos obligatorios']);
            return;
        }

        try {
            // Si las credenciales son validas, el service devuelve token + user.
            $result = $this->authService->login($data);
            http_response_code(200);
            echo json_encode($result);
        } catch (Exception $e) {
            http_response_code(401);
            echo json_encode(['error' => $e->getMessage()]);
        }
    }
}
