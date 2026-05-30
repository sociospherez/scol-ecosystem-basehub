<?php

date_default_timezone_set('Europe/London');

/* =========================================================
   CONFIGURATION
========================================================= */

$uploadDir = "uploads/";
$signatureDir = "signatures/";
$logDir = "logs/";

$adminEmail = "info@solutionscallout.co.uk";

/* =========================================================
   CREATE DIRECTORIES IF NOT EXIST
========================================================= */

if (!file_exists($uploadDir)) {
    mkdir($uploadDir, 0777, true);
}

if (!file_exists($signatureDir)) {
    mkdir($signatureDir, 0777, true);
}

if (!file_exists($logDir)) {
    mkdir($logDir, 0777, true);
}

/* =========================================================
   GENERATE REFERENCE
========================================================= */

$reference = "SCO-NDA-" . date("Ymd-His");

/* =========================================================
   CAPTURE FORM DATA
========================================================= */

$fullName = htmlspecialchars($_POST['full_name'] ?? '');
$company = htmlspecialchars($_POST['company'] ?? '');
$email = htmlspecialchars($_POST['email'] ?? '');
$phone = htmlspecialchars($_POST['phone'] ?? '');
$idReference = htmlspecialchars($_POST['id_reference'] ?? '');

$ipAddress = $_SERVER['REMOTE_ADDR'];
$userAgent = $_SERVER['HTTP_USER_AGENT'];
$timestamp = date("Y-m-d H:i:s");

/* =========================================================
   HANDLE ID DOCUMENT UPLOAD
========================================================= */

$idDocumentPath = "";

if (isset($_FILES['id_upload']) && $_FILES['id_upload']['error'] === 0) {

    $allowedTypes = ['jpg', 'jpeg', 'png', 'pdf'];

    $fileTmp = $_FILES['id_upload']['tmp_name'];
    $fileName = $_FILES['id_upload']['name'];

    $extension = strtolower(pathinfo($fileName, PATHINFO_EXTENSION));

    if (in_array($extension, $allowedTypes)) {

        $newFileName = $reference . "_id." . $extension;

        $destination = $uploadDir . $newFileName;

        move_uploaded_file($fileTmp, $destination);

        $idDocumentPath = $destination;
    }
}

/* =========================================================
   HANDLE SIGNATURE IMAGE
========================================================= */

$signaturePath = "";

if (!empty($_POST['signature_data'])) {

    $signatureData = $_POST['signature_data'];

    $signatureData = str_replace('data:image/png;base64,', '', $signatureData);

    $signatureData = str_replace(' ', '+', $signatureData);

    $signatureBinary = base64_decode($signatureData);

    $signatureFile = $signatureDir . $reference . "_signature.png";

    file_put_contents($signatureFile, $signatureBinary);

    $signaturePath = $signatureFile;
}

/* =========================================================
   SAVE LOG FILE
========================================================= */

$logData = "
====================================================
Reference: $reference
Timestamp: $timestamp

Full Name: $fullName
Company: $company
Email: $email
Phone: $phone
ID Reference: $idReference

IP Address: $ipAddress
Browser: $userAgent

Uploaded ID: $idDocumentPath
Signature: $signaturePath
====================================================
";

file_put_contents(
    $logDir . $reference . ".txt",
    $logData
);

/* =========================================================
   EMAIL CONTENT
========================================================= */

$subject = "New NDA Submission - $reference";

$message = "
New NDA acknowledgement submitted.

Reference: $reference

Full Name: $fullName
Company: $company
Email: $email
Phone: $phone
ID Reference: $idReference

Timestamp: $timestamp
IP Address: $ipAddress
";

/* =========================================================
   EMAIL HEADERS
========================================================= */

$headers = "From: no-reply@solutionscallout.co.uk\r\n";
$headers .= "Reply-To: $email\r\n";

/* =========================================================
   SEND EMAIL
========================================================= */

mail($adminEmail, $subject, $message, $headers);

/* =========================================================
   SUCCESS RESPONSE
========================================================= */

?>

<!DOCTYPE html>
<html>
<head>
    <title>NDA Submitted</title>

    <style>

        body{
            background:#081120;
            color:white;
            font-family:Arial;
            display:flex;
            align-items:center;
            justify-content:center;
            height:100vh;
            margin:0;
        }

        .box{
            background:#111827;
            padding:40px;
            border-radius:20px;
            max-width:600px;
            text-align:center;
            border:1px solid rgba(255,255,255,0.08);
        }

        h1{
            color:#38bdf8;
        }

        p{
            color:#cbd5e1;
            line-height:1.8;
        }

    </style>

</head>

<body>

<div class="box">

    <h1>NDA Successfully Submitted</h1>

    <p>
        Thank you. Your digital acknowledgement and submission
        have been securely recorded.
    </p>

    <p>
        Reference:
        <strong><?php echo $reference; ?></strong>
    </p>

</div>

</body>
</html>