# Funcionamiento del Backend

## Objetivo

El backend provee una API REST en PHP para autenticar usuarios, administrar cursos, crear tareas y registrar entregas de proyectos en formato ZIP.

La API se ejecuta sobre XAMPP con Apache y MySQL.

URL base local:

```text
http://localhost/ProyectoFinalDS/src/Backend/app.php
```

## Flujo general

Cuando un cliente llama al backend, el flujo es:

```text
Cliente local o web
  -> Apache/XAMPP
  -> src/Backend/app.php
  -> src/Backend/Routes/api.php
  -> Controller
  -> Service
  -> Repository / Model
  -> MySQL
```

Cada capa tiene una responsabilidad:

- `app.php`: punto de entrada, configura JSON/CORS y carga rutas.
- `Routes/api.php`: compara metodo HTTP y ruta para elegir controlador.
- `Controllers`: leen datos de la peticion y devuelven respuestas JSON.
- `Services`: aplican reglas de negocio y permisos.
- `Repositories`: contienen consultas SQL especificas.
- `Models`: representan tablas y operaciones basicas.
- `Database/connection.php`: crea la conexion PDO a MySQL.
- `config.php`: centraliza valores de configuracion como el secreto JWT.

## Base de datos

El archivo:

```text
src/Backend/Database/schema.sql
```

define la base `ide_educativo` y estas tablas:

- `users`: usuarios del sistema.
- `courses`: cursos creados por profesores.
- `enrollments`: matriculas de estudiantes en cursos.
- `tasks`: tareas asociadas a cursos.
- `submissions`: entregas de estudiantes.

## Autenticacion

### Registro

Endpoint:

```text
POST /auth/register
```

Recibe:

```json
{
  "name": "Nombre",
  "email": "correo@test.com",
  "password": "123456",
  "role": "student"
}
```

Funcionamiento:

1. `authController.php` lee el JSON.
2. `authService.php` valida que el rol sea `student` o `teacher`.
3. `certificateService.php` genera un par de llaves RSA.
4. `userModel.php` guarda el usuario en MySQL.
5. La respuesta devuelve mensaje de exito y la clave privada.

Respuesta esperada:

```json
{
  "message": "Registro exitoso",
  "private_key": "-----BEGIN PRIVATE KEY-----..."
}
```

### Login

Endpoint:

```text
POST /auth/login
```

Funcionamiento:

1. El controlador recibe `email` y `password`.
2. El servicio busca el usuario por correo.
3. `password_verify` compara el password ingresado contra el hash guardado.
4. Si es correcto, se genera un JWT.

Respuesta esperada:

```json
{
  "token": "jwt...",
  "user": {
    "id": 1,
    "name": "Nombre",
    "role": "student"
  }
}
```

## Proteccion con JWT

Las rutas protegidas requieren header:

```text
Authorization: Bearer TOKEN
```

`authMiddleware.php` valida:

- Que exista el header.
- Que el token tenga formato JWT.
- Que la firma sea correcta.
- Que no este expirado.
- Que el rol tenga permiso cuando la ruta lo requiere.

El secreto usado para firmar y validar JWT esta en:

```text
src/Backend/config.php
```

Si se cambia ese secreto, los tokens generados antes dejan de ser validos y se debe iniciar sesion de nuevo.

Ejemplo de ruta protegida:

```text
GET /courses/me
```

Sin token responde:

```json
{
  "error": "Token requerido"
}
```

## Cursos

### Crear curso

Endpoint:

```text
POST /courses
```

Solo usuarios `teacher`.

Body:

```json
{
  "name": "Python Basico",
  "description": "Curso introductorio"
}
```

Funcionamiento:

1. `courseController.php` valida token y rol `teacher`.
2. `courseService.php` genera un `join_code`.
3. `courseRepository.php` guarda el curso.
4. La respuesta devuelve el curso creado.

### Ver mis cursos

Endpoint:

```text
GET /courses/me
```

Funcionamiento:

- Si el usuario es `teacher`, devuelve cursos creados por ese profesor.
- Si el usuario es `student`, devuelve cursos donde esta matriculado.

### Unirse a curso

Endpoint:

```text
POST /courses/join
```

Solo usuarios `student`.

Body:

```json
{
  "join_code": "ABC123"
}
```

Funcionamiento:

1. Busca el curso por codigo.
2. Verifica que el estudiante no este matriculado.
3. Inserta la matricula en `enrollments`.

## Tareas

### Crear tarea

Endpoint:

```text
POST /courses/{id}/tasks
```

Solo usuarios `teacher` propietarios del curso.

Body:

```json
{
  "title": "Tarea 1",
  "description": "Subir proyecto ZIP",
  "due_date": "2026-05-30 23:59:00"
}
```

Funcionamiento:

1. `taskController.php` valida token y rol.
2. `taskService.php` verifica que el curso pertenezca al profesor.
3. `taskRepository.php` guarda la tarea en MySQL.

### Listar tareas

Endpoint:

```text
GET /courses/{id}/tasks
```

Funcionamiento:

- Un `teacher` puede ver tareas de cursos propios.
- Un `student` puede ver tareas de cursos donde esta matriculado.

## Entregas

### Enviar proyecto

Endpoint:

```text
POST /tasks/{id}/submit
```

Solo usuarios `student`.

La peticion debe ser `multipart/form-data` con un campo de archivo:

```text
project = archivo.zip
```

Funcionamiento:

1. `submissionController.php` recibe el archivo desde `$_FILES`.
2. `submissionService.php` verifica que el estudiante este matriculado en el curso de la tarea.
3. Valida que el archivo tenga extension `.zip`.
4. Guarda el ZIP en:

```text
src/Backend/Submissions
```

5. Registra la entrega en `submissions`.
6. Calcula `is_late` comparando la hora actual con `due_date`.

Respuesta esperada:

```json
{
  "id": 1,
  "is_late": false,
  "submitted_at": "2026-05-24 01:27:04"
}
```

### Ver entregas de una tarea

Endpoint:

```text
GET /tasks/{id}/submissions
```

Solo usuarios `teacher` propietarios de la tarea.

Devuelve:

```json
[
  {
    "id": 1,
    "task_id": 1,
    "student_id": 4,
    "student": "Estudiante QA",
    "file_path": "Submissions/task_1_student_4_...",
    "submitted_at": "2026-05-23 17:27:04",
    "is_late": 0
  }
]
```

### Ver historial propio de entregas

Endpoint:

```text
GET /tasks/{id}/my-submissions
```

Solo usuarios `student`.

Permite que un estudiante consulte sus entregas anteriores para una tarea. Esto apoya el historial de reenvios, ya que cada `POST /tasks/{id}/submit` crea una nueva entrega en vez de sobrescribir la anterior.

Devuelve:

```json
[
  {
    "id": 1,
    "task_id": 1,
    "student_id": 4,
    "file_path": "Submissions/task_1_student_4_...",
    "submitted_at": "2026-05-23 17:27:04",
    "is_late": 0
  }
]
```

### Descargar entrega

Endpoint:

```text
GET /submissions/{id}/download
```

Funcionamiento:

1. Valida token y rol `teacher`.
2. Verifica que la entrega pertenezca a una tarea de ese profesor.
3. Devuelve el ZIP como archivo descargable.

## Relacion con el cliente local

El cliente local usa estos endpoints para:

- Iniciar sesion y guardar el JWT en sesion.
- Registrar estudiantes.
- Consultar cursos matriculados.
- Consultar tareas por curso.
- Validar firmas locales antes de entregar.
- Comprimir el proyecto en ZIP.
- Enviar la entrega con `POST /tasks/{id}/submit`.

Archivos relacionados:

```text
src/ClientLocal/Services/Api/AuthRepository.cs
src/ClientLocal/Services/Api/CourseRepository.cs
src/ClientLocal/Services/Api/TaskRepository.cs
src/ClientLocal/Services/Api/SubmissionRepository.cs
```

## Relacion con el cliente web

El cliente web tambien consume la API para login, registro, cursos y tareas.

Archivo central:

```text
src/ClientWeb/Services/ApiClient.php
```

Ese archivo debe apuntar a la URL base correcta del backend.

## Pruebas realizadas

Se valido el flujo principal:

- Login de profesor.
- Creacion de curso.
- Login de estudiante.
- Matricula con `join_code`.
- Consulta de cursos.
- Creacion de tarea.
- Consulta de tareas.
- Subida de ZIP.
- Consulta de entregas.
- Consulta de historial propio de entregas.
- Descarga de ZIP.

Tambien se validaron errores:

- Crear curso como estudiante.
- Crear tarea como estudiante.
- Subir entrega como profesor.
- Ver entregas como estudiante.
- Subir archivo que no es ZIP.
- Llamar ruta protegida sin token.

## Pendientes tecnicos importantes

- Cambiar `Config::JWT_SECRET` por un secreto propio del ambiente antes de despliegue.
- Desplegar en Oracle Cloud y actualizar la URL remota.
- Definir si se requiere endpoint especifico para historial/versiones previas de entregas.
- Definir si las tareas deben aceptar archivos de apoyo.
