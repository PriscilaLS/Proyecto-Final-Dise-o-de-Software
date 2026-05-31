# Despliegue del cliente web en Oracle Cloud

## Objetivo

Publicar el cliente web PHP en la misma instancia Oracle Cloud donde esta desplegado el backend.

URL esperada:

```text
http://141.148.68.5/ProyectoFinalDS/src/ClientWeb/index.php
```

## Preparacion local

El cliente web fue ajustado para detectar automaticamente el host desde donde se abre.

Si se abre en XAMPP:

```text
http://localhost/ProyectoFinalDS/src/ClientWeb/index.php
```

llama al backend local:

```text
http://localhost/ProyectoFinalDS/src/Backend/app.php
```

Si se abre en Oracle:

```text
http://141.148.68.5/ProyectoFinalDS/src/ClientWeb/index.php
```

llama al backend remoto:

```text
http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php
```

## Subir archivos a Oracle

Desde PowerShell en Windows, en la raiz del proyecto:

```powershell
scp -i "C:\Users\prisc\Downloads\eduide-backend.key" -r .\src\ClientWeb ubuntu@141.148.68.5:/home/ubuntu/ClientWeb
```

Luego entrar por SSH:

```powershell
ssh -i "C:\Users\prisc\Downloads\eduide-backend.key" ubuntu@141.148.68.5
```

## Instalar en Apache

Dentro de la VM:

```bash
sudo mkdir -p /var/www/html/ProyectoFinalDS/src
sudo rm -rf /var/www/html/ProyectoFinalDS/src/ClientWeb
sudo mv /home/ubuntu/ClientWeb /var/www/html/ProyectoFinalDS/src/ClientWeb
sudo chown -R www-data:www-data /var/www/html/ProyectoFinalDS/src/ClientWeb
sudo find /var/www/html/ProyectoFinalDS/src/ClientWeb -type d -exec chmod 755 {} \;
sudo find /var/www/html/ProyectoFinalDS/src/ClientWeb -type f -exec chmod 644 {} \;
```

## Probar en navegador

Abrir:

```text
http://141.148.68.5/ProyectoFinalDS/src/ClientWeb/index.php
```

## Prueba minima

1. Registrar usuario profesor.
2. Iniciar sesion como profesor.
3. Crear curso.
4. Crear tarea.
5. Registrar usuario estudiante.
6. Iniciar sesion como estudiante.
7. Unirse al curso con el codigo.
8. Ver tareas.
9. Enviar entrega desde cliente local.
10. Volver al cliente web como profesor.
11. Ver entregas.
12. Descargar ZIP.

## Nota

El backend debe estar funcionando antes de probar el cliente web remoto. La prueba rapida del backend es:

```bash
curl http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/courses/me
```

Resultado esperado:

```json
{"error":"Token requerido"}
```
