<?php

abstract class BasePage
{
    abstract protected function getTitle(): string;

    abstract protected function renderContent(): string;

    public function render()
    {
        include 'Components/Header.php';
        include 'Components/Navbar.php';

        echo "<div class='main-container'>";
        echo $this->renderContent();
        echo "</div>";

        include 'Components/Footer.php';
    }
}