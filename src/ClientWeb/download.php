<?php

session_start();

$submissionId = $_GET['id'] ?? null;
$versionId = $_GET['version_id'] ?? null;

if ((!$submissionId && !$versionId) || !isset($_SESSION['token'])) {
    http_response_code(400);
    echo 'Solicitud invalida.';
    exit;
}

$host = $_SERVER['HTTP_HOST'] ?? 'localhost';
$scheme = (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off') ? 'https' : 'http';
$downloadId = $versionId ?: $submissionId;
$filenamePrefix = $versionId ? 'submission_version_' : 'submission_';
$endpoint = $versionId
    ? '/submission-versions/' . urlencode($versionId) . '/download'
    : '/submissions/' . urlencode($submissionId) . '/download';
$url = 'http://localhost:5500/app.php' . $endpoint;
$ch = curl_init($url);

curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
curl_setopt($ch, CURLOPT_HTTPHEADER, [
    'Authorization: Bearer ' . $_SESSION['token']
]);

$response = curl_exec($ch);
$status = curl_getinfo($ch, CURLINFO_HTTP_CODE);
$contentType = curl_getinfo($ch, CURLINFO_CONTENT_TYPE) ?: 'application/zip';
curl_close($ch);

if ($status >= 400 || $response === false) {
    http_response_code($status ?: 500);
    echo $response ?: 'No se pudo descargar la entrega.';
    exit;
}

header('Content-Type: ' . $contentType);
header('Content-Disposition: attachment; filename="' . $filenamePrefix . $downloadId . '.zip"');
header('Content-Length: ' . strlen($response));
echo $response;
