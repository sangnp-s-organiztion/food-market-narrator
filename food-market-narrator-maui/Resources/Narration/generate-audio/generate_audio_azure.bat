@echo off
setlocal enabledelayedexpansion
echo ================================================
echo    Food Market Narrator - Audio Generator
echo    Using Azure Speech Services
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

REM Check if azure speech SDK is installed
python -c "import azure.cognitiveservices.speech" >nul 2>&1
if errorlevel 1 (
    echo Installing Azure Speech SDK...
    pip install -r requirements-azure.txt
    echo.
)

REM Check for API key
if "%AZURE_SPEECH_KEY%"=="" (
    if NOT "%1"=="" (
        set AZURE_SPEECH_KEY=%1
    ) else (
        echo.
        echo Azure Speech Key chua duoc cai dat.
        echo.
        set /p AZURE_SPEECH_KEY="Nhap Azure Speech Key cua ban: "
        if "!AZURE_SPEECH_KEY!"=="" (
            echo.
            echo ERROR: Khong the de trong API key!
            pause
            exit /b 1
        )
    )
)

REM Set region if not set
if "%AZURE_SPEECH_REGION%"=="" (
    if NOT "%2"=="" (
        set AZURE_SPEECH_REGION=%2
    ) else (
        echo.
        set /p AZURE_SPEECH_REGION="Nhap Azure Region (mac dinh: southeastasia): "
        if "!AZURE_SPEECH_REGION!"=="" (
            set AZURE_SPEECH_REGION=southeastasia
            echo Su dung region mac dinh: southeastasia
        )
    )
)

echo Starting audio generation with Azure Speech...
echo API Key: %AZURE_SPEECH_KEY:~0,10%...
echo Region: %AZURE_SPEECH_REGION%
echo.
python generate_audio_azure.py

echo.
echo ================================================
echo Generation complete!
echo ================================================
pause
