<?php

require_once 'Components/BasePage.php';
require_once 'Services/CourseService.php';

class CoursesPage extends BasePage
{
    protected function getTitle(): string
    {
        return "Cursos";
    }

    protected function renderContent(): string
    {
        $service = new CourseService();
        $courses = $service->getCourses();
        $isTeacher = $this->isTeacher();
        $isStudent = $this->isStudent();

        $primaryActions = "";
        if ($isTeacher) {
            $primaryActions .= "<a class='button-link' href='index.php?page=create-course'>Crear curso</a>";
        }
        if ($isStudent) {
            $primaryActions .= "<a class='button-link' href='index.php?page=join-course'>Unirse a curso</a>";
        }

        $html = "
        <div class='page-panel'>
            <div class='page-actions'>
                <h1>Cursos</h1>
                <div>{$primaryActions}</div>
            </div>
        ";

        if (isset($courses['error'])) {
            $html .= "<p class='error'>{$courses['error']}</p></div>";
            return $html;
        }

        $html .= "<div class='courses-grid'>";

        if (!$courses) {
            $html .= "<p>No hay cursos disponibles.</p>";
        } else {
            foreach ($courses as $course) {
                $id = $course['id'];
                $name = htmlspecialchars($course['name'] ?? '', ENT_QUOTES, 'UTF-8');
                $description = htmlspecialchars($course['description'] ?? '', ENT_QUOTES, 'UTF-8');
                $joinCode = htmlspecialchars($course['join_code'] ?? '', ENT_QUOTES, 'UTF-8');

                $teacherAction = $isTeacher
                    ? "<a href='index.php?page=create-task&course_id={$id}'>Crear tarea</a>"
                    : "";

                $html .= "
                <div class='course-card'>
                    <h2>{$name}</h2>
                    <p>{$description}</p>
                    " . ($isTeacher ? "<p><strong>Codigo:</strong> {$joinCode}</p>" : "") . "
                    <a href='index.php?page=tasks&id={$id}'>Ver tareas</a>
                    {$teacherAction}
                </div>
                ";
            }
        }

        $html .= "</div></div>";
        return $html;
    }
}
