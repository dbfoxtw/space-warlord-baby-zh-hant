@echo off
rem Build the mod from source and install it (needs MelonLoader 0.7.3 in the game folder; see README.md).
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
%PY% tools\install.py install %*
:end
echo.
pause
