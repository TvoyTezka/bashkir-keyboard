param([ValidateSet('win-x64', 'win-arm64')][string]$Runtime = 'win-x64', [switch]$LoadTestLayout)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet build BashkortKeyboard.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
    dotnet run --project tests/BashkortKeyboard.Tests -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Logic scenarios failed.' }
    $nativeTestArguments = @('run', '--project', 'tests/BashkortKeyboard.NativeTests', '-c', 'Release', '--no-build')
    if ($LoadTestLayout) { $nativeTestArguments += @('--', '--load-test-layout') }
    dotnet @nativeTestArguments
    if ($LASTEXITCODE -ne 0) { throw 'Native tests failed or Russian layout is unavailable.' }
    dotnet publish src/BashkortKeyboard/BashkortKeyboard.csproj -c Release -r $Runtime --self-contained true -o "artifacts/$Runtime"
    if ($LASTEXITCODE -ne 0) { throw 'Publishing failed.' }
    Copy-Item -LiteralPath README.md -Destination "artifacts/$Runtime/README.md"
    Copy-Item -LiteralPath TECHNICAL.md -Destination "artifacts/$Runtime/TECHNICAL.md"
    Copy-Item -LiteralPath CONTRIBUTING.md -Destination "artifacts/$Runtime/CONTRIBUTING.md"
    Copy-Item -LiteralPath CHANGELOG.md -Destination "artifacts/$Runtime/CHANGELOG.md"
    Copy-Item -LiteralPath install-start-menu.ps1 -Destination "artifacts/$Runtime/install-start-menu.ps1"
    Copy-Item -LiteralPath LICENSE -Destination "artifacts/$Runtime/LICENSE.txt"
    Copy-Item -LiteralPath PRIVACY.md -Destination "artifacts/$Runtime/PRIVACY.md"
    Write-Host "Ready: $PSScriptRoot\artifacts\$Runtime\BashkortKeyboard.exe"
}
finally { Pop-Location }
