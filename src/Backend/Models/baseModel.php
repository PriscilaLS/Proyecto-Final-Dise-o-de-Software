<?php
require_once __DIR__ . '/../Database/connection.php';
abstract class BaseModel {
    public PDO $db;
    public function __construct() {
        // Todos los modelos comparten la misma conexion PDO.
        $this->db = Connection::getInstance();
    }

    abstract public function getTableName(): string;
    public function findById(int $id): ?array {
        // El ? es un parametro preparado: evita concatenar valores del usuario en SQL.
        $stmt = $this->db->prepare("SELECT * FROM " . $this->getTableName() . " WHERE id = ?");
        $stmt->execute([$id]);
        return $stmt->fetch(PDO::FETCH_ASSOC) ?: null;
    }
    public function findByField(string $field, $value): ?array {
        $stmt = $this->db->prepare("SELECT * FROM " . $this->getTableName() . " WHERE $field = ?");
        $stmt->execute([$value]);
        return $stmt->fetch(PDO::FETCH_ASSOC) ?: null;
    }
}
