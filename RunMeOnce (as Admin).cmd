@echo off
set "CertFolder=%~dp0"
:: necessary when run elevated
cd /d "%CertFolder%"

net session >nul 2>&1 || (echo must be run as administrator to properly install Apple certs & pause & exit /b)

for %%f in (*.cer) do (
    echo Importing: %%f
    certutil -addstore -f "Root" "%%f" >nul
) 

dotnet run WinAerials.cs
timeout /t 5