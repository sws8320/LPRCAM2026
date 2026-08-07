@echo off
rem ===================================================================
rem  Build KyungsinLPR installer (Inno Setup 6)
rem  Output: SETUP\Output\KyungsinLPR_Setup_<version>.exe
rem
rem  Version auto-increments each build via SETUP\version.txt (patch +1).
rem    build_setup.bat            -> auto bump (e.g. 1.0.1 -> 1.0.2)
rem    build_setup.bat 2.0.0      -> use given version (also saved)
rem ===================================================================
setlocal enabledelayedexpansion
set ISCC="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist %ISCC% set ISCC="C:\Program Files\Inno Setup 6\ISCC.exe"

set VERFILE=%~dp0version.txt

rem --- read current version (default 1.0.0) ---
set CUR=1.0.0
if exist "%VERFILE%" set /p CUR=<"%VERFILE%"
if "%CUR%"=="" set CUR=1.0.0

rem --- decide new version: arg overrides, else bump patch ---
if not "%~1"=="" (
  set VER=%~1
) else (
  for /f "tokens=1-3 delims=." %%a in ("!CUR!") do (
    set /a PATCH=%%c+1
    set VER=%%a.%%b.!PATCH!
  )
)

rem --- save new version for next build ---
>"%VERFILE%" echo !VER!

echo Building KyungsinLPR installer v!VER! ...
%ISCC% /DMyAppVersion=!VER! "%~dp0KyungsinLPR.iss"
if errorlevel 1 (
  echo.
  echo [FAILED] Inno Setup compile error.
  exit /b 1
)
echo.
echo [OK] Output: %~dp0Output\KyungsinLPR_Setup_!VER!.exe
