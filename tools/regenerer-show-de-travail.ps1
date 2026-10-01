# Régénère « samples/Show de travail » à partir de « samples/Show de référence » (doc 32 §4, essais manuels).
# - refuse de travailler si LuXia est ouvert (fichiers verrouillés, verrou mono-instance) ;
# - met l'ancien show de travail de côté (dossier temporaire daté) au lieu de le supprimer ;
# - renomme le projet en « Show de travail » (sinon la barre de titre affiche « Show de référence »).
# Usage : powershell -File tools/regenerer-show-de-travail.ps1
$ErrorActionPreference = 'Stop'
$racine = Split-Path -Parent $PSScriptRoot
$reference = Join-Path $racine 'samples\Show de référence'
$travail = Join-Path $racine 'samples\Show de travail'

if (Get-Process -Name 'LuXia' -ErrorAction SilentlyContinue) {
    Write-Error 'LuXia est ouvert : fermez-le avant de régénérer le show de travail.'
}
if (-not (Test-Path $reference)) {
    Write-Error "Show de référence introuvable : $reference"
}

if (Test-Path $travail) {
    $sauvegarde = Join-Path ([System.IO.Path]::GetTempPath()) ('luxia-show-de-travail-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    Move-Item -LiteralPath $travail -Destination $sauvegarde
    Write-Host "Ancien show de travail mis de côté : $sauvegarde"
}

Copy-Item -LiteralPath $reference -Destination $travail -Recurse
# Les versions automatiques et enregistrements de la référence ne suivent pas la copie.
foreach ($dossier in 'Versions') {
    $chemin = Join-Path $travail $dossier
    if (Test-Path $chemin) { Remove-Item -LiteralPath $chemin -Recurse -Force }
}

$projet = Join-Path $travail 'projet.json'
$contenu = [System.IO.File]::ReadAllText($projet, [System.Text.Encoding]::UTF8)
$contenu = $contenu -replace '"name":\s*"Show de référence"', '"name": "Show de travail"'
[System.IO.File]::WriteAllText($projet, $contenu, (New-Object System.Text.UTF8Encoding($false)))

Write-Host "Show de travail régénéré : $travail"
Write-Host 'Ouvrez ce dossier dans LuXia (Projet → Ouvrir) et vérifiez le dossier dans Aide → À propos.'
