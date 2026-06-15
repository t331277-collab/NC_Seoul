@echo off
setlocal

cd /d "%~dp0"

set "CODEX_CMD=C:\Users\t3312\AppData\Roaming\npm\codex.cmd"

echo Project: %CD%
echo.

if exist "%CODEX_CMD%" (
    call "%CODEX_CMD%" %*
) else (
    codex %*
)

set "CODEX_EXIT=%ERRORLEVEL%"
echo.
echo Codex exited with code %CODEX_EXIT%.
pause
exit /b %CODEX_EXIT%
