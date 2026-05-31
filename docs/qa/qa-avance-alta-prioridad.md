# QA del avance de requerimientos de alta prioridad

Este documento resume las pruebas realizadas sobre los componentes principales del sistema EduIDE para validar el avance de funcionalidades criticas. La validación cubre el IDE local, seguridad e integridad, autenticación, cursos, tareas, entregas, backend remoto y cliente web.

## Estado general

Estado del avance: funcional para los requerimientos mínimos de alta prioridad.

Componentes validados:

- IDE local para crear, editar, guardar y ejecutar scripts Python.
- Terminal interactiva integrada.
- Integridad local mediante firmas HMAC.
- Detección de modificaciones externas.
- Bloqueo de pegado externo.
- Autenticación contra backend.
- Cursos matriculados y tareas.
- Entrega de proyectos en ZIP.
- Backend PHP desplegable y probado en Oracle Cloud.
- Cliente web para cuenta, cursos, tareas, entregas y descargas.

## IDE local

Estado general: 90% listo.

### Aprobado

- Crear proyecto local: APROBADO.
- Abrir proyecto existente: APROBADO.
- Explorador de archivos con TreeView: APROBADO.
- Mostrar carpetas anidadas en TreeView: APROBADO.
- Crear carpetas: APROBADO.
- Crear scripts `.py`: APROBADO.
- Editar scripts: APROBADO.
- Guardar scripts: APROBADO.
- Firmar scripts con HMAC al guardar: APROBADO.
- Ejecutar scripts Python: APROBADO.
- Mostrar output en consola: APROBADO.
- Usar terminal interactiva de Python: APROBADO.
- Enviar comandos a la terminal interactiva: APROBADO.
- Detectar modificación externa con `FileSystemWatcher`: APROBADO.
- Marcar archivo corrupto cuando cambia fuera del IDE: APROBADO.
- Mostrar advertencia de integridad: APROBADO.
- Restaurar ultima version firmada: APROBADO.
- Renombrar scripts: APROBADO.
- Eliminar scripts: APROBADO.
- Bloquear paste externo con `Ctrl+V`: APROBADO.
- Bloquear paste externo con click derecho > Pegar: APROBADO.
- Permitir paste interno con `Ctrl+C` / `Ctrl+V`: APROBADO.
- Permitir paste interno con click derecho: APROBADO.
- Compilar cliente local: APROBADO.

### Corregido en QA

- El IDE no abría directamente desde cursos: CORREGIDO.
- El botón Ir a entrega cerraba la app: CORREGIDO.
- La ejecución fallaba con rutas Windows por `\U`: CORREGIDO.
- La consola mostraba caracteres raros al ejecutar scripts: CORREGIDO.
- El `ClipboardGuard` permitía bypass con click derecho: CORREGIDO.
- El `ClipboardGuard` no permitía pegar varias veces texto interno: CORREGIDO.
- El menú Archivo dejo de desplegarse: CORREGIDO.
- Renombrar carpetas fuera del IDE podia crashear el explorador: PROTECCION AGREGADA.

### Pendientes o mejoras

- Syntax highlighting visual: PENDIENTE / NO IMPLEMENTADO.
- Confirmación antes de eliminar scripts: MEJORA.
- Renombrar carpetas desde el IDE: NO IMPLEMENTADO.
- Mostrar ruta completa del proyecto abierto: MEJORA.

## Seguridad e integridad local

Estado general: funcional para el avance.

### Aprobado

- Firmar archivos del proyecto: APROBADO.
- Validar firmas antes de entregar: APROBADO.
- Detectar archivo modificado despues de firmar: APROBADO.
- Bloquear entrega cuando una firma no coincide: APROBADO.
- Restaurar ultima versión válida desde advertencia de integridad: APROBADO.
- Bloquear pegado de texto externo desde teclado: APROBADO.
- Bloquear pegado de texto externo desde menu contextual: APROBADO.
- Permitir copiar y pegar texto generado dentro del IDE: APROBADO.

### Observacion de diseno

La firma HMAC no prueba por sí sola que el código fue escrito sin ayuda externa. La firma valida que el archivo no cambio despues de ser firmado. Para fortalecer el modelo académico, la firma debería generarse automáticamente al guardar desde el IDE y la pantalla de entrega debería enfocarse en validar y enviar.

Estado de esta observación: RIESGO DE DISEÑO / DOCUMENTAR.

## Autenticacion, cursos y tareas en cliente local

Estado general: 95% listo.

### Aprobado

- Login conectado al backend: APROBADO.
- Login con credenciales correctas: APROBADO.
- Login con contraseña incorrecta: APROBADO.
- Mostrar error de login invalido: APROBADO.
- Register conectado al backend: APROBADO.
- Registrar usuario desde cliente local: APROBADO.
- Registro desktop como estudiante por defecto: APROBADO / DECISION DE DISENO.
- Autenticación con JWT: APROBADO.
- Guardar sesión/token para llamadas posteriores: APROBADO.
- Cargar cursos matriculados desde backend real: APROBADO.
- No mostrar cursos falsos o mock cuando no hay matrícula: APROBADO.
- Unirse a curso con `join_code`: APROBADO.
- Detectar `join_code` invalido: APROBADO.
- Refrescar lista de cursos despues de matricularse: APROBADO.
- Seleccionar curso matriculado: APROBADO.
- Listar tareas del curso seleccionado: APROBADO.
- Mostrar título, descripción y fecha límite de tarea: APROBADO.

### Corregido en QA

- La pantalla de cursos mostraba cursos mock cuando no habia cursos reales: CORREGIDO.
- Un estudiante nuevo no tenía forma de matricularse desde cliente local: CORREGIDO.
- El botón Ir a entrega cerraba la app: CORREGIDO.

### Pendientes o mejoras

- Selector de rol en register desktop: NO NECESARIO SEGUN ENUNCIADO / DOCUMENTAR.
- Registro de profesores desde cliente local: NO REQUERIDO.
- Crear cursos desde cliente local: NO REQUERIDO, corresponde al cliente web.
- Crear tareas desde cliente local: NO REQUERIDO, corresponde al cliente web.

## Entrega de tareas desde cliente local

Estado general: funcional.

### Aprobado

- Abrir pantalla de entrega desde una tarea: APROBADO.
- Seleccionar carpeta del proyecto a entregar: APROBADO.
- Detectar carpeta no seleccionada: APROBADO.
- Detectar carpeta sin archivos `.py`: APROBADO.
- Firmar proyecto antes de entregar: APROBADO.
- Validar firmas antes de entregar: APROBADO.
- Bloquear entrega si hay firma invalida: APROBADO.
- Bloquear entrega si el archivo fue modificado despues de firmar: APROBADO.
- Comprimir proyecto en ZIP automaticamente: APROBADO.
- Excluir carpetas pesadas o generadas al comprimir: APROBADO.
- Enviar ZIP al backend: APROBADO.
- Recibir respuesta de entrega con ID, fecha y estado tardio: APROBADO.
- Mostrar mensaje de entrega exitosa: APROBADO.

### Corregido en QA

- Enviar entrega se quedaba congelado en "Preparando ZIP": CORREGIDO.
- La compresión ZIP podia trabarse al comprimir carpetas completas sin filtrar: CORREGIDO.
- El boton Enviar entrega no mostraba estado intermedio: CORREGIDO.
- El boton Enviar entrega podia parecer que no hacia nada: CORREGIDO.
- Se agrego timeout para evitar espera indefinida contra backend: CORREGIDO.

### Evidencia validada

- Entrega a `Tarea Web QA`: APROBADO.
- Entrega registrada con ID `4`: APROBADO.
- Estado tardio `False`: APROBADO.
- Fecha recibida desde backend: APROBADO.

## Backend remoto, API REST y base de datos

Estado general: completo para el avance.

Ambiente validado:

- Oracle Cloud Compute.
- Ubuntu 20.04.
- Apache.
- PHP 7.4.
- MySQL.
- API publicada por IP pública.

URL base pública probada:

```text
http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php
```

### Aprobado

- Base de datos MySQL completa: APROBADO.
- `schema.sql`: APROBADO.
- Conexion PDO a MySQL: APROBADO.
- API REST con router central: APROBADO.
- Middleware de autenticacion JWT: APROBADO.
- Validacion de roles `teacher` y `student`: APROBADO.
- Registro de usuario: APROBADO.
- Registro de profesor: APROBADO.
- Registro de estudiante: APROBADO.
- Generacion de llave privada/certificado en registro: APROBADO.
- Login con JWT: APROBADO.
- Login como profesor: APROBADO.
- Login como estudiante: APROBADO.
- Crear curso: APROBADO.
- Generar `join_code` al crear curso: APROBADO.
- Ver cursos del usuario segun rol: APROBADO.
- Unirse a curso con codigo: APROBADO.
- Crear tarea: APROBADO.
- Listar tareas por curso: APROBADO.
- Entregar ZIP: APROBADO.
- Guardar ZIPs de entregas: APROBADO.
- Ver entregas por tarea: APROBADO.
- Ver historial propio de entregas como estudiante: APROBADO.
- Descargar entrega: APROBADO.
- Descargar ZIP y validar contenido: APROBADO.
- Validaciones de permisos por rol: APROBADO.
- Errores negativos detectados correctamente: APROBADO.
- Despliegue en Oracle Cloud: APROBADO.
- Endpoint publico probado: APROBADO.

### Pruebas funcionales realizadas

#### Health check de ruta protegida

- Endpoint probado: `GET /courses/me`.
- Resultado esperado: rechazar peticion sin token.
- Resultado obtenido:

```json
{"error":"Token requerido"}
```

Estado: APROBADO.

Esta prueba confirma que Apache responde, PHP ejecuta el backend, el router carga correctamente y el middleware JWT protege rutas privadas.

#### Registro de usuarios

- Endpoint probado: `POST /auth/register`.
- Registro de profesor: APROBADO.
- Registro de estudiante: APROBADO.
- Respuesta esperada: mensaje de registro exitoso y llave privada.
- Resultado validado:

```json
{
  "message": "Registro exitoso",
  "private_key": "-----BEGIN PRIVATE KEY-----..."
}
```

Estado: APROBADO.

#### Login y generación de JWT

- Endpoint probado: `POST /auth/login`.
- Login como profesor: APROBADO.
- Login como estudiante: APROBADO.
- Respuesta esperada: token JWT y datos del usuario.
- Resultado validado:

```json
{
  "token": "jwt...",
  "user": {
    "id": 2,
    "name": "Teacher QA Oracle",
    "role": "teacher"
  }
}
```

Estado: APROBADO.

#### Crear curso como profesor

- Endpoint probado: `POST /courses`.
- Valida que solo usuarios `teacher` puedan crear cursos.
- Valida que el curso se guarde en MySQL.
- Valida que se genere un `join_code`.
- Curso creado durante QA: `Curso QA Oracle`.
- `COURSE_ID` validado: `1`.
- `JOIN_CODE` validado: `YCNTSO`.

Estado: APROBADO.

#### Matricular estudiante con codigo

- Endpoint probado: `POST /courses/join`.
- Valida que un estudiante pueda unirse usando `join_code`.
- Resultado validado:

```json
{"message":"Te has unido al curso exitosamente"}
```

Estado: APROBADO.

#### Listar cursos del estudiante

- Endpoint probado: `GET /courses/me`.
- Valida que el backend devuelva los cursos donde el estudiante esta matriculado.
- Resultado validado:

```json
[
  {
    "id": "1",
    "name": "Curso QA Oracle",
    "description": "Curso creado durante QA en Oracle",
    "teacher_id": "2",
    "join_code": "YCNTSO"
  }
]
```

Estado: APROBADO.

#### Crear tarea como profesor

- Endpoint probado: `POST /courses/{id}/tasks`.
- Valida que solo el profesor propietario pueda crear tareas.
- Tarea creada durante QA: `Tarea QA Oracle`.
- `TASK_ID` validado: `1`.
- Resultado validado:

```json
{
  "id": 1,
  "course_id": 1,
  "title": "Tarea QA Oracle",
  "description": "Subir ZIP de prueba",
  "due_date": "2026-06-30 23:59:00"
}
```

Estado: APROBADO.

#### Listar tareas como estudiante matriculado

- Endpoint probado: `GET /courses/{id}/tasks`.
- Valida que el estudiante matriculado pueda ver tareas del curso.
- Resultado validado: lista con la tarea creada.

Estado: APROBADO.

#### Subir ZIP como estudiante

- Endpoint probado: `POST /tasks/{id}/submit`.
- Valida recepcion de `multipart/form-data`.
- Valida archivo ZIP.
- Valida guardado fisico del ZIP.
- Valida registro en tabla de entregas.
- Resultado validado:

```json
{
  "id": 1,
  "is_late": false,
  "submitted_at": "2026-05-28 22:34:14"
}
```

Estado: APROBADO.

#### Ver entregas como profesor

- Endpoint probado: `GET /tasks/{id}/submissions`.
- Valida que el profesor pueda ver entregas de una tarea propia.
- Resultado validado:

```json
[
  {
    "id": "1",
    "task_id": "1",
    "student_id": "3",
    "student": "Student QA Oracle",
    "file_path": "Submissions/task_1_student_3_20260528_223414.zip",
    "submitted_at": "2026-05-28 22:34:14",
    "is_late": "0"
  }
]
```

Estado: APROBADO.

#### Ver historial propio como estudiante

- Endpoint probado: `GET /tasks/{id}/my-submissions`.
- Valida que el estudiante pueda consultar sus propias entregas.
- Resultado validado: lista de entregas propias del estudiante autenticado.

Estado: APROBADO.

#### Descargar entrega como profesor

- Endpoint probado: `GET /submissions/{id}/download`.
- Valida descarga del ZIP guardado.
- Valida que solo el profesor autorizado pueda descargar.
- Valida que la ruta guardada en MySQL apunte a un archivo real.
- Archivo descargado validado: `downloaded_submission.zip`.
- Contenido validado con `unzip -l`: archivo `.py` dentro del ZIP.

Estado: APROBADO.

### Pruebas negativas realizadas

- Ruta protegida sin token devuelve `Token requerido`: APROBADO.
- Crear curso como estudiante devuelve `Solo los teachers pueden hacer esto`: APROBADO.
- Crear tarea como estudiante devuelve `Solo los teachers pueden hacer esto`: APROBADO.
- Subir entrega como profesor devuelve `Solo los students pueden hacer esto`: APROBADO.
- Ver entregas como estudiante devuelve `Solo los teachers pueden hacer esto`: APROBADO.
- Subir archivo que no es ZIP devuelve `Solo se aceptan archivos ZIP`: APROBADO.
- `join_code` invalido devuelve error controlado: APROBADO.
- Token faltante o mal enviado devuelve error controlado: APROBADO.

### Corregido en QA

- Rutas no encontradas por formato de URL: CORREGIDO / DOCUMENTADO.
- Error PHP 7.4 por uso de `match`: CORREGIDO.
- Error PHP 7.4 por uso de `str_starts_with`: CORREGIDO.
- Problemas de permisos/firewall en Oracle Cloud: CORREGIDO.
- Apache accesible por IP publica: CORREGIDO.
- Conexion MySQL en Oracle con usuario correcto: CORREGIDO.
- Endpoints publicos respondieron correctamente: CORREGIDO.
- Diferencias entre Windows/XAMPP y Oracle/PHP 7.4 documentadas: CORREGIDO.
- Problemas de nombres de archivos/clases por mayusculas en Linux revisados: CORREGIDO.

### Observaciones

- La hora del servidor puede diferir por zona horaria. Esto puede afectar el campo `is_late` si la fecha limite esta cerca.
- Para produccion real, conviene mover credenciales y secretos a variables de entorno.

## Cliente web

Estado general: 85% listo.

### Aprobado

- Crear cuenta desde web: APROBADO.
- Seleccionar rol estudiante/profesor en registro: APROBADO.
- Login desde web: APROBADO.
- Crear curso como profesor: APROBADO.
- Mostrar codigo de curso: APROBADO.
- Unirse a curso como estudiante: APROBADO.
- Ver cursos del usuario autenticado: APROBADO.
- Crear tarea con descripcion y fecha limite: APROBADO.
- Listar tareas de un curso: APROBADO.
- Ver entregas de una tarea como profesor: APROBADO.
- Bloquear ver entregas si el usuario es estudiante: APROBADO.
- Descargar ZIP de entrega: APROBADO.
- ZIP descargado contiene archivo entregado: APROBADO.
- Cliente web conectado al backend local: APROBADO.
- Rutas web principales conectadas: APROBADO.
- Sintaxis PHP validada con `php -l`: APROBADO.

### Corregido en QA

- `ApiClient.php` apuntaba a una URL incorrecta: CORREGIDO.
- `CreateCoursePage.php` existia pero no estaba conectada al router: CORREGIDO.
- Faltaba pagina de unirse a curso: CORREGIDO.
- Faltaba pagina de crear tarea: CORREGIDO.
- Faltaba pagina de ver entregas: CORREGIDO.
- Faltaba descarga de entregas: CORREGIDO.
- `TasksPage.php` usaba `deadline` en vez de `due_date`: CORREGIDO.
- `RegisterPage.php` esperaba `success`, pero backend devuelve `message`: CORREGIDO.
- Register no enviaba rol: CORREGIDO.
- `Header.php` / `header.php` podia fallar en Oracle por mayusculas: CORREGIDO.
- Login podia redirigir despues de imprimir HTML: CORREGIDO.

### Pendientes o mejoras

- Ocultar botones docentes cuando entra un estudiante: MEJORA.
- Ocultar botones de estudiante cuando entra un profesor: MEJORA.
- Desplegar cliente web en Oracle o dejar enlace remoto definitivo: PENDIENTE.
- Mejorar mensajes visuales y navegacion segun rol: MEJORA.

## Flujo completo validado

Flujo de punta a punta validado:

1. Profesor crea cuenta desde cliente web.
2. Profesor inicia sesion.
3. Profesor crea curso.
4. Sistema genera codigo de curso.
5. Profesor crea tarea con descripcion y fecha limite.
6. Estudiante crea cuenta o inicia sesion.
7. Estudiante se une al curso con codigo.
8. Estudiante ve curso y tarea desde cliente local.
9. Estudiante selecciona carpeta de proyecto.
10. Estudiante firma y valida proyecto.
11. Cliente local comprime y envia ZIP al backend.
12. Backend guarda entrega.
13. Profesor entra al cliente web.
14. Profesor ve entregas de la tarea.
15. Profesor descarga ZIP.
16. ZIP descargado contiene el archivo `.py` entregado.

Resultado del flujo completo: APROBADO.

## Pendientes generales para cerrar el informe

- Diagrama de clases actualizado.
- Discusion de patrones de diseno usados.
- Instrucciones de instalacion.
- Enlace al ejecutable compilado del cliente local.
- Enlace al cliente web remoto.
- Capturas de pantalla del flujo funcionando.
- Despliegue final del cliente web si se requiere URL publica.

## Conclusión de QA

El avance cubre las funcionalidades minimas de alta prioridad indicadas en el enunciado. El IDE local, backend, autenticacion, cursos, tareas, entregas y cliente web fueron probados con flujos positivos y negativos principales. Existen mejoras visuales y de diseno pendientes, pero el flujo critico del sistema funciona de punta a punta.
