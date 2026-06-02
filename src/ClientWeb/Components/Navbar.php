<nav class="navbar">
    <div class="logo">EduIDE</div>

    <div class="nav-links">
        <?php $role = $_SESSION['user']['role'] ?? ''; ?>

        <?php if (empty($_SESSION['token'])): ?>
            <a href="index.php?page=login">Login</a>
            <a href="index.php?page=register">Registro</a>
        <?php else: ?>
            <a href="index.php?page=courses">Cursos</a>

            <?php if ($role === 'teacher'): ?>
                <a href="index.php?page=create-course">Crear curso</a>
            <?php endif; ?>

            <?php if ($role === 'student'): ?>
                <a href="index.php?page=join-course">Unirse</a>
            <?php endif; ?>

            <a href="index.php?page=logout">Salir</a>
        <?php endif; ?>
    </div>
</nav>
