<?php
header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');

// Using exchangerate.host (free, no key, supports simple HTTPS)
$url = 'https://api.exchangerate.host/latest?base=USD&symbols=GBP,EUR,JPY';

// Use file_get_contents instead of curl
$response = @file_get_contents($url);

if ($response === FALSE) {
    echo json_encode(["error" => "Failed to fetch rates"]);
    exit;
}

$data = json_decode($response, true);

if (!isset($data['rates'])) {
    echo json_encode(["error" => "Invalid API response"]);
    exit;
}

$rates = $data['rates'];

$result = [
    // Convert USD→GBP into GBP/USD
    "GBPUSD" => 1 / $rates["GBP"],
    // Convert USD→EUR into EUR/USD
    "EURUSD" => 1 / $rates["EUR"],
    // USD→JPY is already correct
    "USDJPY" => $rates["JPY"]
];

echo json_encode($result);
