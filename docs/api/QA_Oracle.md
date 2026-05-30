# QA Backend PHP - Oracle Cloud

Fecha: 2026-05-26

## Ambiente

- Oracle Cloud Compute
- Ubuntu 20.04
- Apache
- PHP 7.4
- MySQL

URL base publica:

```text
http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php
```

## Datos Del QA Actual

Durante las pruebas se deben reemplazar los valores escritos en mayúscula por datos reales.
Si se pega `COURSE_ID`, `TASK_ID`, `TOKEN_TEACHER`, `TOKEN_STUDENT` o `SUBMISSION_ID` literalmente, la API puede responder `Ruta no encontrada` o `Token inválido`.

Valores confirmados hasta ahora:

```text
COURSE_ID = 1
JOIN_CODE = YCNTSO
```

Ejemplo correcto:

```text
/courses/1/tasks
```

Ejemplo incorrecto:

```text
/courses/COURSE_ID/tasks
```

## Objetivo

Validar que el backend desplegado publicamente funcione correctamente para autenticacion, cursos, tareas, entregas, historial y descarga de archivos.

Esta guia tambien explica por que cada prueba es util para confirmar que una parte del backend esta funcionando.

---

## 1. Health Check / Ruta Protegida Sin Token

**Endpoint**

```text
GET /courses/me
```

**Por qué es útil**

Esta prueba confirma que:

- La IP publica responde.
- Apache esta sirviendo el proyecto.
- PHP esta ejecutando el backend.
- El router `api.php` esta cargando.
- El middleware JWT esta funcionando.
- Las rutas protegidas rechazan peticiones sin token.

**Comando**

```bash
curl http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/courses/me
```

**Resultado esperado**

```json
{"error":"Token requerido"}
```

**Resultado obtenido**

```json
{"error":"Token requerido"}
```

**Estado:** APROBADO

---

## 2. Registro Teacher

**Endpoint**

```text
POST /auth/register
```

**Por qué es útil**

Valida que el backend pueda:

- Recibir JSON.
- Crear usuarios en MySQL.
- Validar roles.
- Generar llaves RSA.
- Devolver la clave privada al cliente.

**Comando**

```bash
curl -X POST "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/auth/register" \
-H "Content-Type: application/json" \
-d '{"name":"Teacher QA Oracle","email":"teacher.qa.oracle@test.com","password":"123456","role":"teacher"}'
```

**Resultado esperado**

```json
{
  "message": "Registro exitoso",
  "private_key": "-----BEGIN PRIVATE KEY-----..."
}
```

**Resultado obtenido**

```json
{
 "message": "Registro exitoso",
  "private_key": "-----BEGIN PRIVATE KEY-----..."
}
```

**Estado:** APROBADO

---

## 3. Login Teacher

**Endpoint**

```text
POST /auth/login
```

**Por qué es útil**

Confirma que:

- El password se valida correctamente contra el hash guardado.
- El backend genera JWT.
- El rol `teacher` queda incluido en la respuesta.
- El token servira para probar rutas protegidas de profesor.

**Comando**

```bash
curl -X POST "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/auth/login" \
-H "Content-Type: application/json" \
-d '{"email":"teacher.qa.oracle@test.com","password":"123456"}'
```

**Resultado esperado**

```json
{
  "token": "jwt...",
  "user": {
    "id": 1,
    "name": "Teacher QA Oracle",
    "role": "teacher"
  }
}
```

**Resultado obtenido**

```json
{"token":"eyJ...",
"user":{"id":"2",
"name":"Teacher QA Oracle",
"role":"teacher"}}
```

**Estado:** APROBADO

---

## 4. Registro Student

**Endpoint**

```text
POST /auth/register
```

**Por qué es útil**

Valida el segundo rol del sistema y confirma que estudiantes tambien pueden registrarse correctamente.

**Comando**

```bash
curl -X POST "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/auth/register" \
-H "Content-Type: application/json" \
-d '{"name":"Student QA Oracle","email":"student.qa.oracle@test.com","password":"123456","role":"student"}'
```

**Resultado esperado**

```json
{
  "message": "Registro exitoso",
  "private_key": "-----BEGIN PRIVATE KEY-----..."
}
```
**Resultado obtenido**
```json
{
  "message": "Registro exitoso",
  "private_key": "-----BEGIN PRIVATE KEY-----..."
}
```
**Estado:** APROBADO

---

## 5. Login Student

**Endpoint**

```text
POST /auth/login
```

**Por qué es útil**

Confirma que el estudiante puede autenticarse y obtener un JWT para probar matricula, consulta de tareas y entregas.

**Comando**

```bash
curl -X POST "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/auth/login" \
-H "Content-Type: application/json" \
-d '{"email":"student.qa.oracle@test.com","password":"123456"}'
```

**Resultado esperado**

```json
{
  "token": "jwt...",
  "user": {
    "id": 2,
    "name": "Student QA Oracle",
    "role": "student"
  }
}
```

**Resultado obtenido**

```json
{"token":"eyJ...",
"user":{"id":"3",
"name":"Student QA Oracle",
"role":"student"}}
```

**Estado:** APROBADO

---

## 6. Crear Curso Como Teacher

**Endpoint**

```text
POST /courses
```

**Por qué es útil**

Valida que:

- El JWT se envia correctamente.
- El middleware reconoce el rol `teacher`.
- El backend crea cursos en MySQL.
- Se genera un `join_code` para matricula.

**Comando**

```bash
curl -X POST "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/courses" \
-H "Content-Type: application/json" \
-H "Authorization: Bearer TOKEN_TEACHER" \
-d '{"name":"Curso QA Oracle","description":"Curso creado durante QA en Oracle"}'
```

**Resultado esperado**

```json
{
  "id": 1,
  "name": "Curso QA Oracle",
  "description": "Curso creado durante QA en Oracle",
  "teacher_id": 1,
  "join_code": "ABC123"
}
```

**COURSE_ID**

```text
COURSE_ID= 1
```

**JOIN_CODE**

```text
YCNTSO
```

**Estado:** APROBADO **

---

## 7. Student Se Une Al Curso

**Endpoint**

```text
POST /courses/join
```

**Por qué es útil**

Confirma que:

- El rol `student` puede matricularse.
- El `join_code` se valida.
- Se crea el registro en `enrollments`.
- El estudiante queda relacionado con el curso.

**Comando**

```bash
curl -X POST "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/courses/join" \
-H "Content-Type: application/json" \
-H "Authorization: Bearer TOKEN_STUDENT" \
-d '{"join_code":"JOIN_CODE"}'
```

**Resultado esperado**

```json
{"message":"Te has unido al curso exitosamente"}
```

**Resultado obtenido**

```json
{"message":"Te has unido al curso exitosamente"}
```

**Estado:** APROBADO

---

## 8. Listar Cursos Del Student

**Endpoint**

```text
GET /courses/me
```

**Por qué es útil**

Valida que el backend puede consultar cursos segun el rol del usuario. Para estudiantes, debe devolver los cursos donde esta matriculado.

**Comando**

```bash
curl "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/courses/me" \
-H "Authorization: Bearer TOKEN_STUDENT"
```

**Resultado esperado**

Lista con el curso creado y unido por `join_code`.
**Resultado obtenido**

```json
[{"id":"1",
"name":"Curso QA Oracle",
"description":"Curso creado durante QA en Oracle","teacher_id":"2",
"join_code":"YCNTSO",
"created_at":"2026-05-26 22:13:15"}]
```

**Estado:** APROBADO

---

## 9. Crear Tarea Como Teacher

**Endpoint**

```text
POST /courses/{id}/tasks
```

**Por qué es útil**

Confirma que:

- Solo el profesor propietario puede crear tareas.
- La tarea se guarda con titulo, descripcion y fecha limite.
- La tarea queda asociada al curso correcto.

**Comando**

Reemplazar:

```text
COURSE_ID -> 1
TOKEN_TEACHER -> token real del login teacher
```

```bash
curl -X POST "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/courses/1/tasks" \
-H "Content-Type: application/json" \
-H "Authorization: Bearer TOKEN_TEACHER" \
-d '{"title":"Tarea QA Oracle","description":"Subir ZIP de prueba","due_date":"2026-06-30 23:59:00"}'
```

**Resultado esperado**

JSON con `id`, `course_id`, `title`, `description` y `due_date`.

**TASK_ID**

```text
TASK ID = 1
```

**Resultado obtenido**
```json
{"id":1,
"course_id":1,
"title":"Tarea QA Oracle",
"description":"Subir ZIP de prueba","due_date":"2026-06-30 23:59:00"}
```
**Estado:** APROBADO

---

## 10. Listar Tareas Como Student

**Endpoint**

```text
GET /courses/{id}/tasks
```

**Por qué es útil**

Valida que el estudiante matriculado puede consultar las tareas disponibles del curso.

**Comando**

Reemplazar:

```text
COURSE_ID -> 1
TOKEN_STUDENT -> token real del login student
```

```bash
curl "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/courses/1/tasks" \
-H "Authorization: Bearer TOKEN_STUDENT"
```

**Resultado esperado**

Lista con la tarea creada.
**Resultado obtenido**
```json
[{"id":"1",
"course_id":"1",
"title":"Tarea QA Oracle",
"description":"Subir ZIP de prueba","due_date":"2026-06-30 23:59:00","created_at":"2026-05-28 21:57:49"}]
```
**Estado:** APROBADO

---

## 11. Subir ZIP Como Student

**Endpoint**

```text
POST /tasks/{id}/submit
```

**Por qué es útil**

Valida el flujo principal de entrega:

- El backend recibe `multipart/form-data`.
- Se valida que el archivo sea ZIP.
- Se guarda el archivo en `Submissions`.
- Se registra la entrega en MySQL.
- Se calcula si fue tardia.

**Preparar ZIP de prueba en la VM**

```bash
echo "print('QA Oracle')" > /home/ubuntu/test.py
zip /home/ubuntu/test.zip /home/ubuntu/test.py
```

**Comando**

Reemplazar:

```text
TASK_ID -> id real de la tarea creada
TOKEN_STUDENT -> token real del login student
```

```bash
curl -X POST "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/tasks/TASK_ID/submit" \
-H "Authorization: Bearer TOKEN_STUDENT" \
-F "project=@/home/ubuntu/test.zip;type=application/zip"
```

**Resultado esperado**

```json
{
  "id": 1,
  "is_late": false,
  "submitted_at": "2026-05-26 ..."
}
```

**SUBMISSION_ID**
ID: 1

**Resultado obtenido**

```json
{"id":1,
"is_late":false,
"submitted_at":"2026-05-28 22:34:14"}
```

**Estado:** APROBADO

---

## 12. Ver Entregas Como Teacher

**Endpoint**

```text
GET /tasks/{id}/submissions
```

**Por qué es útil**

Confirma que el profesor puede revisar las entregas de una tarea propia, incluyendo estudiante, fecha y estado de puntualidad.

**Comando**

Reemplazar:

```text
TASK_ID -> id real de la tarea creada
TOKEN_TEACHER -> token real del login teacher
```

```bash
curl "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/tasks/TASK_ID/submissions" \
-H "Authorization: Bearer TOKEN_TEACHER"
```

**Resultado esperado**

Lista de entregas con `student`, `submitted_at` e `is_late`.

**Resultado obtenido**
```json
task
[{"id":"1",
"task_id":"1",
"student_id":"3",
"student":"Student QA Oracle","file_path":"Submissions\/task_1_student_3_20260528_223414.zip","submitted_at":"2026-05-28 22:34:14","is_late":"0"}]
```
**Estado:** APROBADO

---

## 13. Ver Historial Propio Como Student

**Endpoint**

```text
GET /tasks/{id}/my-submissions
```

**Por qué es útil**

Valida el historial propio de entregas. Cada reenvio crea una entrega nueva, y el estudiante puede consultar sus versiones enviadas.

**Comando**

Reemplazar:

```text
TASK_ID -> id real de la tarea creada
TOKEN_STUDENT -> token real del login student
```

```bash
curl "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/tasks/TASK_ID/my-submissions" \
-H "Authorization: Bearer TOKEN_STUDENT"
```

**Resultado esperado**

Lista de entregas del estudiante autenticado para esa tarea.

**Resultado obtenido**
```json
[{"id":"1",
"task_id":"1",
"student_id":"3",
"file_path":"Submissions\
/task_1_student_3_20260528_223414.zip","submitted_at":"2026-05-28 22:34:14","is_late":"0"}]
```
**Estado:** APROBADO

---

## 14. Descargar Entrega Como Teacher

**Endpoint**

```text
GET /submissions/{id}/download
```

**Por qué es útil**

Confirma que:

- El archivo ZIP guardado puede descargarse.
- Solo el profesor autorizado puede descargar entregas.
- La ruta almacenada en MySQL apunta a un archivo real.

**Comando**

Reemplazar:

```text
SUBMISSION_ID -> id real de la entrega creada
TOKEN_TEACHER -> token real del login teacher
```

```bash
curl -L "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/submissions/SUBMISSION_ID/download" \
-H "Authorization: Bearer TOKEN_TEACHER" \
-o /home/ubuntu/downloaded_submission.zip
```

**Validacion**

```bash
ls -lh /home/ubuntu/downloaded_submission.zip
```

**Resultado esperado**

Archivo ZIP descargado.
```text
  % Total    % Received % Xferd  Average Speed   Time    Time     Time  Current
                                 Dload  Upload   Total   Spent    Left  Speed
  0     0    0     0    0     0      0      0 --:--:-- --:--:-- --:-100   207  100   207    0     0  17250      0 --:--:-- --:--:-- --:--:-- 18818
ubuntu@eduide-backend:~$ ls -lh /home/ubuntu/downloaded_submission.zip
-rw-rw-r-- 1 ubuntu ubuntu 207 May 28 22:43 /home/ubuntu/downloaded_submission.zip
ubuntu@eduide-backend:~$ unzip -l /home/ubuntu/downloaded_submission.zip
Archive:  /home/ubuntu/downloaded_submission.zip
  Length      Date    Time    Name
---------  ---------- -----   ----
       19  2026-05-26 22:28   home/ubuntu/test.py
---------                     -------
       19                     1 file
```

**Estado:** APROBADO

---

## 15. Pruebas Negativas

Las pruebas negativas confirman que el backend no solo funciona en el camino feliz, sino que tambien bloquea acciones invalidas.

### 15.1 Crear curso como student

**Por qué es útil**

Valida autorizacion por rol.

**Comando**
```bash
curl -X POST "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/courses" \
-H "Content-Type: application/json" \
-H "Authorization: TOKEN_STUDENT" \
-d '{"name":"Curso X","description":"Probar si estudiante puede crear curso"}'

```
**Resultado obtenido**
```json
{"error":"Solo los teachers pueden hacer esto"}
```


### 15.2 Crear tarea como student

**Por qué es útil**

Confirma que solo profesores pueden crear tareas.

**Comando**
```bash
curl -X POST "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/courses/1/tasks" \
-H "Content-Type: application/json" \
-H "Authorization: Bearer TOKEN_STUDENT" \
-d '{"title":"Tarea X","description":"Student no deberia crear tareas","due_date":"2026-06-30 23:59:00"}'

```
**Resultado obtenido**
```json
{"error":"Solo los teachers pueden hacer esto"}
```

### 15.3 Subir entrega como teacher

**Por qué es útil**

Confirma que solo estudiantes pueden enviar entregas.

**Comando**
```bash
curl -X POST "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/tasks/TASK_ID/submit" \
-H "Authorization: Bearer TOKEN_TEACHER" \
-F "project=@/home/ubuntu/test.zip;type=application/zip"

```
**Resultado obtenido**
```json
{"error":"Solo los students pueden hacer esto"}
```

### 15.4 Ver entregas como student

**Por qué es útil**

Confirma que no pueden ver entregas de todos los estudiantes.

**Comando**
```bash
curl "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/tasks/TASK_ID/submissions" \
-H "Authorization: Bearer TOKEN_STUDENT"

```
**Resultado obtenido**
```json
{"error":"Solo los teachers pueden hacer esto"}
```

### 15.5 Subir archivo no ZIP

**Por qué es útil**

Confirma que el backend valida tipo de archivo antes de guardar entregas.

```bash
echo "esto no es un zip" > /home/ubuntu/no_zip.txt
```
**Comando**
```bash
curl -X POST "http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/tasks/TASK_ID/submit" \
-H "Authorization: Bearer TOKEN_STUDENT" \
-F "project=@/home/ubuntu/no_zip.txt;type=text/plain"
```
**Resultado obtenido**
```json
{"error":"Solo se aceptan archivos ZIP"}
```

**Estado:** APROBADO
