<?php
/*
 * Conexion centralizada a MySQL usando PDO.
 * Implementa una única instancia reutilizable para que los modelos compartan la misma conexión.
 */
class Connection {
    public static ?PDO $instance = null;
    public static function getInstance(): PDO {
        if (self::$instance === null) {
            $host = 'localhost';
            $db = 'ide_educativo';
            $user = 'root';
            $pass = '';
            self::$instance = new PDO(
                "mysql:host=$host;dbname=$db;charset=utf8mb4", 
                $user, 
                $pass, 
                [
                    PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
                    PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC
                ]
            );
        }
        return self::$instance;
    }
    private function __construct() {}
    private function __clone() {}
}
