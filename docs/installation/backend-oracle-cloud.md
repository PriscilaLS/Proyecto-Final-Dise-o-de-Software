# Despliegue Backend PHP en Oracle Cloud

## Requisitos

- Instancia Ubuntu en Oracle Cloud.
- Apache, PHP, MySQL y extensiones `pdo_mysql` y `openssl`.
- Puerto 80 abierto en la VCN/security list.

## Instalacion base

```bash
sudo apt update
sudo apt install -y apache2 mysql-server php php-mysql php-curl php-xml unzip
sudo systemctl enable apache2 mysql
sudo systemctl start apache2 mysql
```

## Base de datos

```bash
mysql -u root -p < src/Backend/Database/schema.sql
```

Si el usuario de MySQL no es `root`, actualizar credenciales en:

```text
src/Backend/Database/connection.php
```

Tambien se debe cambiar el secreto JWT antes de publicar:

```text
src/Backend/config.php
```

El valor recomendado debe ser largo, privado y distinto al usado en desarrollo.

## Publicacion del proyecto

Copiar el proyecto a:

```text
/var/www/html/ProyectoFinalDS
```

El endpoint principal queda:

```text
http://IP_PUBLICA/ProyectoFinalDS/src/Backend/app.php
```

## Permisos para entregas

```bash
sudo mkdir -p /var/www/html/ProyectoFinalDS/src/Backend/Submissions
sudo chown -R www-data:www-data /var/www/html/ProyectoFinalDS/src/Backend/Submissions
sudo chmod -R 775 /var/www/html/ProyectoFinalDS/src/Backend/Submissions
```

## Validacion rapida

```bash
curl http://IP_PUBLICA/ProyectoFinalDS/src/Backend/app.php/courses/me
```

Respuesta esperada sin token:

```json
{"error":"Token requerido"}
```
