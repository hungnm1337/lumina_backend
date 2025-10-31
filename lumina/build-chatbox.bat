@echo off
echo Building Chatbox AI Backend...

echo Restoring NuGet packages...
dotnet restore

echo Building project...
dotnet build

echo Build completed!
echo.
echo To run the project:
echo dotnet run
echo.
echo To test the chatbox:
echo 1. Update Gemini API key in appsettings.json
echo 2. Run the project
echo 3. Test with TestChatbox.http file
