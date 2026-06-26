@echo off
setlocal
chcp 65001 >nul
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0generateData.ps1"
if errorlevel 1 (
    echo [generateData] FAILED
    exit /b 1
)
echo [generateData] DONE
