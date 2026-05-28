<?php
/*
 * Middleware de autenticación y autorización.
 * Valida tokens JWT y comprueba roles antes de permitir acciones protegidas.
 */
require_once __DIR__ . '/../config.php';

class AuthMiddleware {
    private static string $secret = Config::JWT_SECRET;

    public static function handle(): array {
        $headers = getallheaders();

        // Las rutas protegidas esperan: Authorization: Bearer {token}.
        $authHeader = $headers['Authorization'] ?? $headers['authorization'] ?? '';

        if (substr($authHeader, 0, 7) !== 'Bearer ') {
            http_response_code(401);
            echo json_encode(['error' => 'Token requerido']);
            exit();
        }

        $token = substr($authHeader, 7);

        // Si el token es válido, aqui recuperamos id, role y exp del usuario.
        $payload = self::validateJWT($token);
        if (!$payload) {
            http_response_code(401);
            echo json_encode(['error' => 'Token invalido o expirado']);
            exit();
        }

        return $payload;
    }

    public static function requireRole(array $payload, string $role): void {
        // Protege acciones por rol, por ejemplo crear cursos solo como teacher.
        if ($payload['role'] !== $role) {
            http_response_code(403);
            echo json_encode(['error' => "Solo los {$role}s pueden hacer esto"]);
            exit();
        }
    }

    private static function validateJWT(string $token): ?array {
        $parts = explode('.', $token);
        if (count($parts) !== 3) {
            return null;
        }

        [$header, $payload, $signature] = $parts;

        $expectedSig = rtrim(strtr(base64_encode(
            hash_hmac('sha256', "$header.$payload", self::$secret, true)
        ), '+/', '-_'), '=');

        if ($signature !== $expectedSig) {
            return null;
        }

        // El payload es la parte del JWT donde guardamos datos del usuario.
        $data = json_decode(base64_decode(strtr($payload, '-_', '+/')), true);

        if (!$data || !isset($data['exp']) || $data['exp'] < time()) {
            return null;
        }

        return $data;
    }
}
