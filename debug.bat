@echo off
call cls
echo *Tail wagging* Checking for existing processes... Woof!

taskkill /F /IM "Sensor Stream.exe" /T 2>nul
if %ERRORLEVEL% EQU 0 (
    echo *Happy bark* Found and closed existing Sensor Stream process!
) else (
    echo *Sniffs around* No existing process found!
)

echo *Tail wagging* Starting SensorStream in debug mode! Woof!

cd /d "%~dp0"
powershell -Command "Start-Process cmd -ArgumentList '/c dotnet run --project SensorsStream\SensorStream.csproj' -Verb RunAs"

echo *Excited barking* Waiting for service to start up...
timeout /t 5 /nobreak > nul

echo *Playful bark* Connecting to WebSocket and sending test command...
powershell -ExecutionPolicy Bypass -File "%~dp0websocket_test.ps1"

echo *Panting happily* Debug session running! Press any key to close everything...
pause

taskkill /F /IM "Sensor Stream.exe" /T 2>nul