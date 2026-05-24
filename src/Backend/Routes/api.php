<?php
require_once __DIR__ . '/../Controllers/authController.php';
require_once __DIR__ . '/../Controllers/courseController.php';
require_once __DIR__ . '/../Controllers/taskController.php';
require_once __DIR__ . '/../Controllers/submissionController.php';

$method = $_SERVER['REQUEST_METHOD'];
$path   = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);

// Apache entrega la URL completa. Quitamos el prefijo para comparar solo
// la ruta de la API, por ejemplo: /courses/me.
$path = str_replace('/ProyectoFinalDS/src/Backend/app.php', '', $path);



$authController = new AuthController();
$courseController = new CourseController();
$taskController = new TaskController();
$submissionController = new SubmissionController();

// Este archivo funciona como router: recibe metodo + URL y llama
// al controlador que sabe manejar esa accion.
match(true) {
    // Rutas de autenticación
    $method === 'POST' && $path === '/auth/register' 
        => $authController->register(),
    $method === 'POST' && $path === '/auth/login'    
        => $authController->login(),

    // Cursos
    $method === 'POST' && $path === '/courses' 
        => $courseController->create(),    
    $method === 'GET' && ($path === '/courses/me' || $path === '/courses/mine')
        => $courseController->getMyCourses(),
    $method === 'POST' && $path === '/courses/join' 
        => $courseController->join(),

    // Tareas
    $method === 'GET' && preg_match('#^/courses/(\d+)/tasks$#', $path, $matches)
        => $taskController->getByCourse((int) $matches[1]),
    $method === 'POST' && preg_match('#^/courses/(\d+)/tasks$#', $path, $matches)
        => $taskController->create((int) $matches[1]),

    // Entregas
    $method === 'POST' && preg_match('#^/tasks/(\d+)/submit$#', $path, $matches)
        => $submissionController->submit((int) $matches[1]),
    $method === 'GET' && preg_match('#^/tasks/(\d+)/submissions$#', $path, $matches)
        => $submissionController->getByTask((int) $matches[1]),
    $method === 'GET' && preg_match('#^/submissions/(\d+)/download$#', $path, $matches)
        => $submissionController->download((int) $matches[1]),
    default => (function() {
        http_response_code(404);
        echo json_encode(['error' => 'Ruta no encontrada']);
    })()
};
