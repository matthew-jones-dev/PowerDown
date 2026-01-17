<#
.SYNOPSIS
Syncs the PowerDown repo from WSL into a Windows folder so `dotnet` can build/run on Windows.

.DESCRIPTION
This script mirrors a WSL source directory into a Windows destination using robocopy.
It excludes build artifacts (bin/obj/publish) and .git for faster syncs.
Run it from PowerShell when you want to launch the Windows UI or CLI from a Windows path.

.PARAMETER Distro
The WSL distro name shown by `wsl -l -v` (default: Ubuntu).

.PARAMETER SourcePath
Absolute path inside WSL (default: /home/<user>/linux/projects/PowerDown).

.PARAMETER DestinationPath
Windows destination directory (default: C:\dev\PowerDown).

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\sync-wsl.ps1 -Distro Ubuntu -SourcePath /home/<user>/linux/projects/PowerDown -DestinationPath C:\dev\PowerDown

.EXAMPLE
.\sync-wsl.ps1 -Distro Ubuntu -SourcePath /home/<user>/linux/projects/PowerDown -DestinationPath C:\dev\PowerDown
#>
param(
    [string]$Distro = "Ubuntu",
    [string]$SourcePath = "/home/$env:USERNAME/linux/projects/PowerDown",
    [string]$DestinationPath = "C:\dev\PowerDown"
)

$wslRoot = "\\wsl$\$Distro"
$source = Join-Path $wslRoot ($SourcePath.TrimStart("/").Replace("/", "\"))

Write-Host "Syncing from $source to $DestinationPath"
robocopy $source $DestinationPath /MIR /XD bin obj publish .git /NFL /NDL /NJH /NJS /NS /NC

if ($LASTEXITCODE -ge 8) {
    throw "robocopy failed with exit code $LASTEXITCODE"
}
