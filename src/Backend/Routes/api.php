<?php
/*
 * Router principal de la API REST.
 * Lee el metodo HTTP y la ruta solicitada para enviar la peticion al controlador correcto.
 * Esta version usa if/elseif para ser compatible con PHP 7.4 en Ubuntu 20.04.
 */
require_once __DIR__ . '/../Controllers/authController.php';
require_once __DIR__ . '/../Controllers/courseController.php';
require_once __DIR__ . '/../Controllers/taskController.php';
require_once __DIR__ . '/../Controllers/submissionController.php';

$method = $_SERVER['REQUEST_METHOD'];
$path = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);

// Apache entrega la URL completa. Quitamos el prefijo para comparar solo
// la ruta de la API, por ejemplo: /courses/me.
$path = str_replace('/ProyectoFinalDS/src/Backend/app.php', '', $path);
$path = str_replace('/app.php', '', $path);

$authController = new AuthController();
$courseController = new CourseController();
$taskController = new TaskController();
$submissionController = new SubmissionController();

// Este archivo funciona como router: recibe metodo + URL y llama
// al controlador que sabe manejar esa accion.
if ($method === 'POST' && $path === '/auth/register') {
    $authController->register();
} elseif ($method === 'POST' && $path === '/auth/login') {
    $authController->login();
} elseif ($method === 'POST' && $path === '/courses') {
    $courseController->create();
} elseif ($method === 'GET' && ($path === '/courses/me' || $path === '/courses/mine')) {
    $courseController->getMyCourses();
} elseif ($method === 'POST' && $path === '/courses/join') {
    $courseController->join();
} elseif ($method === 'GET' && preg_match('#^/courses/(\d+)/tasks$#', $path, $matches)) {
    $taskController->getByCourse((int) $matches[1]);
} elseif ($method === 'POST' && preg_match('#^/courses/(\d+)/tasks$#', $path, $matches)) {
    $taskController->create((int) $matches[1]);
} elseif ($method === 'GET' && preg_match('#^/tasks/(\d+)/attachment$#', $path, $matches)) {
    $taskController->downloadAttachment((int) $matches[1]);
} elseif ($method === 'POST' && preg_match('#^/tasks/(\d+)/submit$#', $path, $matches)) {
    $submissionController->submit((int) $matches[1]);
} elseif ($method === 'GET' && preg_match('#^/tasks/(\d+)/submissions$#', $path, $matches)) {
    $submissionController->getByTask((int) $matches[1]);
} elseif ($method === 'GET' && preg_match('#^/tasks/(\d+)/my-submissions$#', $path, $matches)) {
    $submissionController->getMineByTask((int) $matches[1]);
} elseif ($method === 'GET' && preg_match('#^/submissions/(\d+)/versions$#', $path, $matches)) {
    $submissionController->getVersions((int) $matches[1]);
} elseif ($method === 'GET' && preg_match('#^/submissions/(\d+)/download$#', $path, $matches)) {
    $submissionController->download((int) $matches[1]);
} elseif ($method === 'GET' && preg_match('#^/submission-versions/(\d+)/download$#', $path, $matches)) {
    $submissionController->downloadVersion((int) $matches[1]);
} else {
    http_response_code(404);
    echo json_encode(['error' => 'Ruta no encontrada']);
}
