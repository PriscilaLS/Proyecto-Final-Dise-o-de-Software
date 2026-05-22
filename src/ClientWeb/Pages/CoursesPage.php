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

        $html = "<div class='courses-grid'>";

        if (!$courses) {

            $html .= "<p>No hay cursos disponibles</p>";

        } else {

            foreach ($courses as $course) {

                $html .= "

                <div class='course-card'>

                    <h2>{$course['name']}</h2>

                    <p>{$course['description']}</p>

                    <a href='index.php?page=tasks&id={$course['id']}'>
                        Ver tareas
                    </a>

                </div>
                ";
            }
        }

        $html .= "</div>";

        return $html;
    }
}