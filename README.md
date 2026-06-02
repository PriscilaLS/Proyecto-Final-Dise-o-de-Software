# EduIDE

EduIDE es una plataforma educativa para cursos de programacion en Python. El
proyecto combina un IDE local de escritorio, un backend PHP con MySQL y un
cliente web para la gestión remota de cursos, tareas y entregas.

El objetivo principal es permitir que un estudiante trabaje en scripts Python
desde un entorno local controlado, firme su proyecto, lo entregue al backend y
que un profesor pueda revisar y descargar esas entregas desde la web.

## Componentes del proyecto

```text
ProyectoFinalDS/
  artifacts/              Ejecutable compilado del cliente local.
  docs/                   Documentacion, QA e instrucciones de instalacion.
  samples/                Archivos de ejemplo para demos.
  src/
    Backend/              API REST en PHP.
    ClientLocal/          Cliente de escritorio en C# / Avalonia.
    ClientWeb/            Cliente web en PHP orientado a objetos.
    Shared/ApiContracts/  Contratos de API documentados.
```

## Funcionalidades principales

### Cliente local

- Registro e inicio de sesión para estudiantes.
- Visualización de cursos matriculados.
- Unión a cursos mediante código.
- Listado de tareas por curso.
- Apertura del IDE local.
- Creación, apertura, edición y guardado de scripts `.py`.
- Ejecución de scripts Python y visualización de salida en consola.
- Terminal interactiva de Python.
- Firma local de scripts y validación de integridad.
- Detección de modificaciones externas mediante `FileSystemWatcher`.
- Bloqueo de pegado externo en el editor.
- Entrega de tareas mediante ZIP al backend.
- Demo del patrón Decorator aplicado a `Script`.

### Backend PHP

- `POST /auth/register`
- `POST /auth/login`
- `POST /courses`
- `GET /courses/mine`
- `POST /courses/join`
- `GET /courses/{id}/tasks`
- `POST /courses/{id}/tasks`
- `POST /tasks/{id}/submit`
- `GET /tasks/{id}/submissions`
- `GET /submissions/{id}/download`

### Cliente web

- Registro e inicio de sesión.
- Creación de cursos para profesores.
- Unión a cursos para estudiantes.
- Creación de tareas con descripcion y fecha límite.
- Visualización de tareas.
- Visualización y descarga de entregas.
- Navegación ajustada por rol.

## URLs de prueba

Backend remoto en Oracle Cloud:

```text
http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php
```

Cliente web remoto:

```text
http://141.148.68.5/ProyectoFinalDS/src/ClientWeb/index.php
```

Cliente web local con XAMPP:

```text
http://localhost/ProyectoFinalDS/src/ClientWeb/index.php
```

Backend local con XAMPP:

```text
http://localhost/ProyectoFinalDS/src/Backend/app.php
```

## Ejecutable del cliente local

El ejecutable publicado para Windows esta en:

```text
artifacts/ClientLocal-win-x64/ClientLocal.exe
```

Para regenerarlo desde la raiz del repositorio:

```powershell
dotnet publish .\src\ClientLocal\ClientLocal.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\ClientLocal-win-x64
```

Requisitos para usar el cliente local:

- Windows 10 o superior.
- Python instalado y disponible como `python` en el PATH.
- Conexión al backend remoto o local configurada en el cliente.

## Ejecucion local

### Backend y cliente web

1. Copiar el repositorio dentro de `C:\xampp\htdocs\ProyectoFinalDS`.
2. Encender Apache y MySQL desde XAMPP.
3. Crear la base de datos `ide_educativo`.
4. Importar:

```text
src/Backend/Database/schema.sql
```

5. Revisar credenciales en:

```text
src/Backend/Database/connection.php
```

6. Probar el backend:

```text
http://localhost/ProyectoFinalDS/src/Backend/app.php/courses/me
```

Respuesta esperada sin token:

```json
{"error":"Token requerido"}
```

7. Abrir el cliente web:

```text
http://localhost/ProyectoFinalDS/src/ClientWeb/index.php
```

### Cliente local

Ejecutar:

```text
artifacts/ClientLocal-win-x64/ClientLocal.exe
```

También se puede ejecutar desde código con:

```powershell
dotnet run --project .\src\ClientLocal\ClientLocal.csproj
```

## Flujo recomendado de prueba

1. Entrar al cliente web como profesor.
2. Crear un curso.
3. Crear una tarea dentro del curso.
4. Registrar o entrar al cliente local como estudiante.
5. Unirse al curso con el código generado.
6. Ver tareas del curso.
7. Abrir el IDE local y crear un script Python.
8. Ejecutar el script y verificar salida en consola.
9. Firmar el proyecto.
10. Entregar la tarea desde el cliente local.
11. Volver al cliente web como profesor.
12. Ver entregas y descargar el ZIP.

## Roles

El cliente local esta orientado al estudiante. Desde ahpi se crean cuentas de
estudiante y solo se permite iniciar sesion con rol `student`.

El cliente web se usa para la gestión remota. Los profesores pueden crear
cursos, crear tareas, ver entregas y descargar archivos. Los estudiantes pueden
unirse a cursos y ver sus tareas.

## Documentación incluida

- [Funcionamiento del backend](docs/api/backend-funcionamiento.md)
- [QA del backend en Oracle](docs/api/QA_Oracle.md)
- [QA de funcionalidades de alta prioridad](docs/qa/qa-avance-alta-prioridad.md)
- [Instalación del cliente local](docs/installation/cliente-local-windows.md)
- [Despliegue del cliente web](docs/installation/cliente-web-oracle.md)
- [Despliegue del backend](docs/installation/backend-oracle-cloud.md)
- [Patron Decorator aplicado a Script](docs/decorator/patron-decorator-script.md)

## Patrones de diseño

El proyecto documenta y utiliza patrones de diseño como:

- Fachada, para separar operaciones de alto nivel en servicios.
- Singleton, para servicios compartidos como integridad local.
- Repository, para acceso a datos en backend.
- Decorator, aplicado al objeto `Script` para agregar firma y syntax highlight
  sin modificar el objeto base.

## Estado general

El proyecto incluye las funcionalidades críticas del avance estipulado para la fecha 1 de junio 2026:

- IDE local funcional.
- Backend desplegado en Oracle Cloud.
- Cliente web remoto funcional.
- Entrega de tareas desde cliente local.
- Descarga de entregas desde cliente web.
- Documentación y QA de las partes principales.
