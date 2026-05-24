<?php

session_start();

require_once 'Pages/LoginPage.php';
require_once 'Pages/RegisterPage.php';
require_once 'Pages/CoursesPage.php';
require_once 'Pages/TasksPage.php';

$page = $_GET['page'] ?? 'login';

switch ($page) {

    case 'register':
        $view = new RegisterPage();
        break;

    case 'courses':
        $view = new CoursesPage();
        break;

    case 'tasks':
        $view = new TasksPage();
        break;

    default:
        $view = new LoginPage();
        break;
}

$view->render();