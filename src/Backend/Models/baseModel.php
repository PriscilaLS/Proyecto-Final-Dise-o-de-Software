<?php
require_once __DIR__ . '/../Database/connection.php';
abstract class BaseModel {
    protected PDO $db;
    public function __construct() {
        $this->db = Connection::getInstance();
    }

    abstract public function getTableName(): string;
    public function findById(int $id): ?array {
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
