@echo off
echo Restoring NuGet packages and building Chatbox AI...

echo.
echo Step 1: Restoring packages...
dotnet restore

echo.
echo Step 2: Building project...
dotnet build

echo.
echo Step 3: Checking for errors...
if %ERRORLEVEL% EQU 0 (
    echo ✅ Build successful!
    echo.
    echo To run the project:
    echo   dotnet run
    echo.
    echo To test the chatbox:
    echo   1. Update Gemini API key in appsettings.json
    echo   2. Run the project
    echo   3. Test with TestChatbox.http file
) else (
    echo ❌ Build failed! Check the errors above.
)

pause
