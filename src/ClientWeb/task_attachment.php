<?php

session_start();

function taskAttachmentHeaderValue(string $headers, string $name): ?string
{
    foreach (explode("\r\n", $headers) as $line) {
        if (stripos($line, $name . ':') === 0) {
            return trim(substr($line, strlen($name) + 1));
        }
    }

    return null;
}

$taskId = (int) ($_GET['task_id'] ?? 0);

if ($taskId <= 0 || !isset($_SESSION['token'])) {
    http_response_code(400);
    echo 'Solicitud invalida.';
    exit;
}

$url = 'http://localhost:5500/app.php/tasks/' . urlencode((string) $taskId) . '/attachment';
$ch = curl_init($url);

curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
curl_setopt($ch, CURLOPT_HEADER, true);
curl_setopt($ch, CURLOPT_HTTPHEADER, [
    'Authorization: Bearer ' . $_SESSION['token']
]);

$response = curl_exec($ch);
$status = curl_getinfo($ch, CURLINFO_HTTP_CODE);
$headerSize = curl_getinfo($ch, CURLINFO_HEADER_SIZE);
$fallbackContentType = curl_getinfo($ch, CURLINFO_CONTENT_TYPE) ?: 'application/octet-stream';
$error = curl_error($ch);
if (PHP_VERSION_ID < 80000 && is_resource($ch)) {
    curl_close($ch);
}

if ($response === false) {
    http_response_code(500);
    echo 'No se pudo descargar el archivo de apoyo: ' . $error;
    exit;
}

$headers = substr($response, 0, $headerSize);
$body = substr($response, $headerSize);

if ($status >= 400) {
    http_response_code($status);
    echo $body ?: 'No se pudo descargar el archivo de apoyo.';
    exit;
}

$contentType = taskAttachmentHeaderValue($headers, 'Content-Type') ?: $fallbackContentType;
$contentDisposition = taskAttachmentHeaderValue($headers, 'Content-Disposition')
    ?: 'attachment; filename="task_attachment_' . $taskId . '"';

header('Content-Type: ' . $contentType);
header('Content-Disposition: ' . $contentDisposition);
header('Content-Length: ' . strlen($body));
echo $body;
