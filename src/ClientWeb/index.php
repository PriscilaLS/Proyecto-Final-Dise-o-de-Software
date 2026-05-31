<?php

session_start();

require_once 'Pages/loginpage.php';
require_once 'Pages/RegisterPage.php';
require_once 'Pages/CoursesPage.php';
require_once 'Pages/CreateCoursePage.php';
require_once 'Pages/JoinCoursePage.php';
require_once 'Pages/TasksPage.php';
require_once 'Pages/CreateTaskPage.php';
require_once 'Pages/SubmissionsPage.php';

$page = $_GET['page'] ?? 'login';

if ($page === 'logout') {
    session_destroy();
    header('Location: index.php?page=login');
    exit;
}

switch ($page) {
    case 'register':
        $view = new RegisterPage();
        break;

    case 'courses':
        $view = new CoursesPage();
        break;

    case 'create-course':
        $view = new CreateCoursePage();
        break;

    case 'join-course':
        $view = new JoinCoursePage();
        break;

    case 'tasks':
        $view = new TasksPage();
        break;

    case 'create-task':
        $view = new CreateTaskPage();
        break;

    case 'submissions':
        $view = new SubmissionsPage();
        break;

    default:
        $view = new LoginPage();
        break;
}

$view->render();
