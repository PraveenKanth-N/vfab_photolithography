@echo off
title Semiconductor Digital Twin - FastAPI Backend Server
echo =====================================================================
echo Starting Semiconductor Digital Twin Microservices Backend...
echo Module 9: High-Throughput Telemetry & Wafer Serialization Service
echo =====================================================================
cd /d "%~dp0"

echo Checking Python environment...
python -c "import fastapi, uvicorn, sqlalchemy; print('All core modules verified!')"
if %errorlevel% neq 0 (
    echo [ERROR] Required Python packages are missing. Installing...
    python -m pip install -r requirements.txt
)

echo Starting FastAPI server at http://127.0.0.1:8005
echo Swagger UI docs available at: http://127.0.0.1:8005/docs
echo Press Ctrl+C to stop the server.
echo.
python -m uvicorn main:app --host 0.0.0.0 --port 8005 --reload
pause
