@echo off
net session >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    powershell -NoProfile -Command "Start-Process -FilePath 'cmd.exe' -ArgumentList '/c \"\"%~f0\"\"' -Verb RunAs"
    exit /b
)

"%~dp0PsExec.exe" -accepteula -i -s -d "%~dp0SimpleFanControlForAsus.exe"
