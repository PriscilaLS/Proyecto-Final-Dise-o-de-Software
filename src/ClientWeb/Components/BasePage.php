<?php

abstract class BasePage
{
    abstract protected function getTitle(): string;

    abstract protected function renderContent(): string;

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
