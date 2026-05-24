# API Contracts

Base URL local:   http://localhost/ProyectoFinalDS/src/Backend/app.php
Base URL remota:  http://TU-IP-ORACLE/src/Backend/app.php

---

## AUTH

### POST /auth/register
Body:
{
  "name": "...",
  "email": "...",
  "password": "...",
  "role": "student | teacher"
}

Respuesta 200:
{
  "message": "Registro exitoso",
  "private_key": "-----BEGIN PRIVATE KEY-----..."
}

Respuesta 400:
{
  "error": "El correo ya está registrado"
}

---

### POST /auth/login
Body:
{
  "email": "...",
  "password": "..."
}

Respuesta 200:
{
  "token": "jwt...",
  "user": { "id": 1, "name": "...", "role": "student | teacher" }
}

Respuesta 401:
{
  "error": "Credenciales inválidas"
}

---

## COURSES

### POST /courses
Header: Authorization: Bearer {token}  (solo teacher)
Body:
{
  "name": "...",
  "description": "..."
}

Respuesta 200:
{
  "id": 1,
  "join_code": "ABC123"
}

Respuesta 403:
{
  "error": "Solo los teachers pueden hacer esto"
}

---

### GET /courses/me
Header: Authorization: Bearer {token}

Respuesta 200:
[
  { "id": 1, "name": "...", "description": "...", "join_code": "..." }
]

---

### POST /courses/join
Header: Authorization: Bearer {token}  (solo estudiante)
Body:
{
  "join_code": "ABC123"
}

Respuesta 200:
{
  "message": "Matrícula exitosa"
}

Respuesta 400:
{
  "error": "Código inválido"
}

---

## TASKS

### GET /courses/{id}/tasks
Header: Authorization: Bearer {token}

Respuesta 200:
[
  {
    "id": 1,
    "title": "...",
    "description": "...",
    "due_date": "2026-05-20 23:59:00"
  }
]

---

### POST /courses/{id}/tasks
Header: Authorization: Bearer {token}  (solo teacher)
Body:
{
  "title": "...",
  "description": "...",
  "due_date": "2026-05-20 23:59:00"
}

Respuesta 200:
{
  "id": 1,
  "title": "..."
}

---

## SUBMISSIONS

### POST /tasks/{id}/submit
Header: Authorization: Bearer {token}  (solo estudiante)
Body: multipart/form-data
  - campo "project": archivo ZIP del proyecto

Respuesta 200:
{
  "id": 1,
  "is_late": false,
  "submitted_at": "2026-05-14 10:30:00"
}

Respuesta 400:
{
  "error": "Archivo inválido"
}

---

### GET /tasks/{id}/submissions
Header: Authorization: Bearer {token}  (solo teacher)

Respuesta 200:
[
  {
    "id": 1,
    "student": "Juan Perez",
    "submitted_at": "2026-05-14 10:30:00",
    "is_late": false
  }
]

---

### GET /submissions/{id}/download
Header: Authorization: Bearer {token}  (solo teacher)

Respuesta 200: archivo ZIP descargable
Respuesta 404:
{
  "error": "Entrega no encontrada"
}
