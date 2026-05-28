# Despliegue Backend PHP en Oracle Cloud

## Requisitos

- Instancia Ubuntu en Oracle Cloud.
- Apache, PHP, MySQL y extensiones `pdo_mysql` y `openssl`.
- Puerto 80 abierto en la VCN/security list.

## Instalación base

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

## Publicación del proyecto

Copiar el proyecto a:

```text
/var/www/html/ProyectoFinalDS
```

El endpoint principal queda:

```text
http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php
```

## Permisos para entregas

```bash
sudo mkdir -p /var/www/html/ProyectoFinalDS/src/Backend/Submissions
sudo chown -R www-data:www-data /var/www/html/ProyectoFinalDS/src/Backend/Submissions
sudo chmod -R 775 /var/www/html/ProyectoFinalDS/src/Backend/Submissions
```

## Validacion rápida

```bash
curl http://141.148.68.5/ProyectoFinalDS/src/Backend/app.php/courses/me
```

Respuesta esperada sin token:

```json
{"error":"Token requerido"}
```
