@echo off
set "EDITOR=C:\Program Files\Unity 2022.3.62f3c1\2022.3.62f3c1\Editor\Unity.exe"
set "PROJECT=%~dp0"
if "%PROJECT:~-1%"=="\" set "PROJECT=%PROJECT:~0,-1%"
start "" "%EDITOR%" -projectPath "%PROJECT%"
