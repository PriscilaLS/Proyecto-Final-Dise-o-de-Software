<?php
/*
 * Punto de entrada del backend.
 * Configura respuestas JSON, habilita CORS y carga el router principal de la API.
 */
date_default_timezone_set('America/Costa_Rica');

header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type, Authorization');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit();
}

require_once __DIR__ . '/Routes/api.php';
