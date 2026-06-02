# Instalacion del cliente local en Windows

## Artefacto generado

El cliente local fue publicado como aplicación Windows self-contained en:

```text
artifacts/ClientLocal-win-x64/
```

También se generó un ZIP listo para compartir:

```text
artifacts/ClientLocal-win-x64.zip
```

Ejecutable principal:

```text
artifacts/ClientLocal-win-x64/ClientLocal.exe
```

## Requisitos

- Windows 10 o superior.
- Python instalado y disponible como `python` en PATH.
- Conexión al backend configurado en el cliente local.

El build fue generado como self-contained, por lo que no debería requerir instalar .NET manualmente.

## Cómo ejecutar

1. Descomprimir `ClientLocal-win-x64.zip`.
2. Abrir la carpeta descomprimida.
3. Ejecutar:

```text
ClientLocal.exe
```

## Comando usado para generar el ejecutable

Desde la raíz del repositorio:

```powershell
dotnet publish .\src\ClientLocal\ClientLocal.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\ClientLocal-win-x64
```

Luego se comprimió la carpeta publicada:

```powershell
Compress-Archive -Path artifacts\ClientLocal-win-x64\* -DestinationPath artifacts\ClientLocal-win-x64.zip
```

## Prueba rápida

1. Abrir `ClientLocal.exe`.
2. Iniciar sesión.
3. Abrir el IDE.
4. Crear o abrir un proyecto.
5. Crear un script `.py`.
6. Ejecutar el script.
7. Confirmar que el resultado aparece en la consola.
