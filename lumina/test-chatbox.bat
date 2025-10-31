@echo off
echo Testing Chatbox AI Backend...

echo.
echo Step 1: Restoring packages...
dotnet restore

echo.
echo Step 2: Building project...
dotnet build

if %ERRORLEVEL% EQU 0 (
    echo ✅ Build successful!
    echo.
    echo Next steps:
    echo 1. Update Gemini API key in appsettings.json
    echo 2. Run: dotnet run
    echo 3. Test with TestChatbox.http
) else (
    echo ❌ Build failed! Check errors above.
)

pause
