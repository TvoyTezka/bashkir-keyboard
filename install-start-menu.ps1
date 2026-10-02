param([string]$ExecutablePath)
$ErrorActionPreference = 'Stop'
if (-not $ExecutablePath) {
    $ExecutablePath = Join-Path $PSScriptRoot 'BashkortKeyboard.exe'
    if (-not (Test-Path -LiteralPath $ExecutablePath -PathType Leaf)) {
        $ExecutablePath = Join-Path $PSScriptRoot 'artifacts\win-x64\BashkortKeyboard.exe'
    }
}
$ExecutablePath = (Resolve-Path -LiteralPath $ExecutablePath).Path
$programsDirectory = [Environment]::GetFolderPath('Programs')
$shortcutPath = Join-Path $programsDirectory 'Bashkort Keyboard.lnk'
$shellObject = New-Object -ComObject WScript.Shell
try {
    $shortcut = $shellObject.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $ExecutablePath
    $shortcut.WorkingDirectory = [IO.Path]::GetDirectoryName($ExecutablePath)
    $shortcut.IconLocation = "$ExecutablePath,0"
    $shortcut.Description = 'Башкирские буквы через удержание русских клавиш'
    $shortcut.Save()
    Write-Host "Start menu shortcut: $shortcutPath"
}
finally {
    if ($shortcut) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shortcut) }
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shellObject)
}
