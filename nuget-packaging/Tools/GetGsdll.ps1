function Get-MoveGsDllString($toolsPath) {
"
PowerShell -NoProfile -ExecutionPolicy Bypass -Command `"try { ls '`$(SolutionDir)\packages\GhostScriptSharp.*\Tools\gsdll32.dll' | Sort -Descending | Select -First 1 | cp -Destination '`$(TargetDir)' } catch {}`""
}