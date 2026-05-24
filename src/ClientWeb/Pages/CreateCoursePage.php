<?php

require_once 'Components/BasePage.php';
require_once 'Services/CourseService.php';

class CreateCoursePage extends BasePage
{
    protected function getTitle(): string
    {
        return "Crear Curso";
    }

    protected function renderContent(): string
    {
        $message = "";

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {

            $name = $_POST['name'];
            $description = $_POST['description'];

            $service = new CourseService();

            $response = $service->createCourse($name, $description);

            if ($response) {

                $message = "
                <p class='success'>
                    Curso creado correctamente
                </p>";

            } else {

                $message = "
                <p class='error'>
                    Error al crear el curso
                </p>";
            }
        }

        return "

        <div class='card'>

            <h1>Crear Curso</h1>

            {$message}

            <form method='POST'>

                <input
                    type='text'
                    name='name'
                    placeholder='Nombre del curso'
                    required
                >

                <textarea
                    name='description'
                    placeholder='Descripción'
                    required
                ></textarea>

                <button type='submit'>
                    Crear Curso
                </button>

            </form>

        </div>
        ";
    }
}