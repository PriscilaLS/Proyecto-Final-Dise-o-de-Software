<?php
require_once __DIR__ . '/../Controllers/AuthController.php';

$method = $_SERVER['REQUEST_METHOD'];
$path   = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
$path = str_replace('/ProyectoFinalDS/src/Backend/app.php', '', $path);

require_once __DIR__ . '/../Controllers/AuthController.php';

$authController = new AuthController();

match(true) {
    $method === 'POST' && $path === '/auth/register' 
        => $authController->register(),
    $method === 'POST' && $path === '/auth/login'    
        => $authController->login(),
    default => (function() {
        http_response_code(404);
        echo json_encode(['error' => 'Ruta no encontrada']);
    })()
};