<?php
/*
 * Modelo de usuarios.
 * Representa la tabla users y contiene la inserción de nuevas cuentas.
 */
require_once __DIR__ . '/baseModel.php';

class UserModel extends BaseModel {
    public function getTableName(): string {
        return 'users';
    }

    public function create(array $data): int {
        $stmt = $this->db->prepare(
            "INSERT INTO users (name, email, password, role, public_key)
             VALUES (?, ?, ?, ?, ?)"
        );
        $stmt->execute([
            $data['name'],
            $data['email'],
            $data['password'],
            $data['role'],
            $data['public_key'] ?? null
        ]);
        return (int) $this->db->lastInsertId();
    }
}
