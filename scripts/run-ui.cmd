@echo off
setlocal

set "ROOT=%~dp0.."
dotnet run --project "%ROOT%\src\PowerDown.UI"

endlocal
