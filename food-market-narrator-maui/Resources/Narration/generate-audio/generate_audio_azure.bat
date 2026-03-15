@echo off
setlocal enabledelayedexpansion
echo ================================================
echo    Food Market Narrator - Audio Generator
echo    Using Edge TTS
echo ================================================
echo.

REM Check if Python is installed
python --version >nul 2>&1
if errorlevel 1 (
    echo Error: Python is not installed or not in PATH
    echo Please install Python 3.8+ from https://www.python.org/
    pause
    exit /b 1
)

REM Check if edge-tts is installed
python -c "import edge_tts" >nul 2>&1
if errorlevel 1 (
    echo Installing Edge TTS dependencies...
    pip install -r requirements.txt
    echo.
)

echo Starting audio generation with Edge TTS...
echo.
if "%1"=="" (
    python generate_audio_azure.py
) else (
    python generate_audio_azure.py --lang %1
)

echo.
echo ================================================
echo Generation complete!
echo ================================================
pause
