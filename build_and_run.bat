@echo off
cd /d "O:\Projects\Visual StudioCodeProjects\Scada"
dotnet build Scada.Client
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Starting application...
    echo ========================================
    echo.
    dotnet run --project Scada.Client
)
