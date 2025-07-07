@echo off
echo Searching for n8n process on port 5678...
FOR /F "tokens=5" %%G IN ('netstat -a -n -o ^| find "5678"') DO (
    FOR /F "tokens=1" %%H IN ("%%G") DO (
        IF "%%H" NEQ "0" (
            echo Found n8n process with PID %%H. Stopping it...
            taskkill /PID %%H /F
        )
    )
)

echo Waiting for 3 seconds...
timeout /t 3 /nobreak >nul

echo Setting environment variables...
set N8N_EDITOR_BASE_URL=https://renderfin.com/api-n8n
set N8N_PUBLIC_API_BASE_URL=https://renderfin.com/api-n8n
rem N8N_PATH should NOT be set. n8n will run in root mode.
set N8N_RUNNERS_ENABLED=true

echo Starting n8n...
start "n8n" cmd /k "n8n start"

echo n8n restarted successfully!
echo Access it at: https://renderfin.com/api-n8n/
pause 