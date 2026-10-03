@echo off
rem Remove the mod (MelonLoader itself is left alone).
rem This file is ASCII-only on purpose: cmd.exe parses .bat files byte by byte,
rem and non-ASCII text (UTF-8 or Big5) can break the parser.
setlocal
cd /d "%~dp0"
chcp 65001 >nul
set PYTHONUTF8=1
set PY=
where py >nul 2>nul
if not errorlevel 1 (
    set PY=py -3
) else (
    where python >nul 2>nul
    if not errorlevel 1 set PY=python
)
if "%PY%"=="" (
    echo Python 3.10+ is required: https://www.python.org/downloads/
    goto end
)
%PY% tools\install.py uninstall %*
:end
echo.
pause
