<?php
require_once __DIR__ . '/../Models/UserModel.php';
require_once __DIR__ . '/CertificateService.php';

class AuthService {
    private UserModel $userModel;
    private CertificateService $certService;

    public function __construct() {
        $this->userModel   = new UserModel();
        $this->certService = new CertificateService();
    }

    public function register(array $data): array {
        // Verificar si el email ya existe
        $existing = $this->userModel->findByField('email', $data['email']);
        if ($existing) {
            throw new Exception("El correo ya está registrado");
        }

        // Generar certificado
        $keys = $this->certService->generateKeyPair();

        // Crear usuario
        $id = $this->userModel->create([
            'name'       => $data['name'],
            'email'      => $data['email'],
            'password'   => password_hash($data['password'], PASSWORD_BCRYPT),
            'role'       => $data['role'],
            'public_key' => $keys['public_key']
        ]);

        return [
            'id'          => $id,
            'private_key' => $keys['private_key']
        ];
    }

    public function login(array $data): array {
        $user = $this->userModel->findByField('email', $data['email']);

        if (!$user || !password_verify($data['password'], $user['password'])) {
            throw new Exception("Credenciales inválidas");
        }

        $token = $this->generateJWT($user);

        return [
            'token' => $token,
            'user'  => [
                'id'   => $user['id'],
                'name' => $user['name'],
                'role' => $user['role']
            ]
        ];
    }

    private function generateJWT(array $user): string {
        $secret  = 'TU_SECRET_KEY_CAMBIALA';
        $header  = base64_encode(json_encode(['alg' => 'HS256', 'typ' => 'JWT']));
        $payload = base64_encode(json_encode([
            'id'   => $user['id'],
            'role' => $user['role'],
            'exp'  => time() + 86400
        ]));
        $signature = base64_encode(
            hash_hmac('sha256', "$header.$payload", $secret, true)
        );
        return "$header.$payload.$signature";
    }
}