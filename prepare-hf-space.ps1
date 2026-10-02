$ErrorActionPreference = 'Stop'
$spaceFolder = Join-Path $PSScriptRoot 'artifacts/hf-space'
New-Item -ItemType Directory -Path $spaceFolder -Force | Out-Null
foreach ($name in @('index.html', 'style.css', 'site.js', 'demo.js', 'i18n.js')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "docs/$name") -Destination $spaceFolder
}
New-Item -ItemType Directory -Path (Join-Path $spaceFolder 'assets') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs/assets/app-icon.png') -Destination (Join-Path $spaceFolder 'assets/app-icon.png')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'huggingface/README.md') -Destination (Join-Path $spaceFolder 'README.md')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE') -Destination (Join-Path $spaceFolder 'LICENSE')
'{"repository":"TvoyTezka/bashkir-keyboard"}' | Set-Content -LiteralPath (Join-Path $spaceFolder 'repository.json') -Encoding utf8
Write-Output "Prepared: $spaceFolder"
