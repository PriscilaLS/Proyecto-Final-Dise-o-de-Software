<?php
class CertificateService {
    public function generateKeyPair(): array {
        $config = [
            "digest_alg"       => "sha256",
            "private_key_bits" => 2048,
            "private_key_type" => OPENSSL_KEYTYPE_RSA,
        ];
        $keyPair = openssl_pkey_new($config);
        openssl_pkey_export($keyPair, $privateKey);
        $publicKey = openssl_pkey_get_details($keyPair)["key"];

        return [
            "private_key" => $privateKey,
            "public_key"  => $publicKey
        ];
    }
}
