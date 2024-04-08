[CmdletBinding()] Param()
process {
    & C:\ProgramData\Alkami\Installer\Widget\uninstall.ps1 $PSScriptRoot;
    return;
}
