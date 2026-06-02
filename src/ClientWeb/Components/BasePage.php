<?php

abstract class BasePage
{
    abstract protected function getTitle(): string;

    abstract protected function renderContent(): string;

    protected function getCurrentUserRole(): string
    {
        return $_SESSION['user']['role'] ?? '';
    }

    protected function isTeacher(): bool
    {
        return $this->getCurrentUserRole() === 'teacher';
    }

    protected function isStudent(): bool
    {
        return $this->getCurrentUserRole() === 'student';
    }

    public function render()
    {
        $content = $this->renderContent();

        include 'Components/header.php';
        include 'Components/Navbar.php';

        echo "<div class='main-container'>";
        echo $content;
        echo "</div>";

        include 'Components/footer.php';
    }
}
