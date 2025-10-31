# 🤖 Chatbox AI Setup Guide

## 📋 Prerequisites

1. **Gemini API Key** - Get from [Google AI Studio](https://makersuite.google.com/app/apikey)
2. **.NET 8.0** - Make sure you have .NET 8.0 installed
3. **SQL Server** - Database connection configured

## 🚀 Quick Setup

### 1. Configure API Key
Update `appsettings.json`:
```json
{
  "Gemini": {
    "ApiKey": "YOUR_ACTUAL_GEMINI_API_KEY"
  }
}
```

### 2. Restore and Build
Run the setup script:
```bash
# Windows
restore-and-build.bat

# Or manually
dotnet restore
dotnet build
```

### 3. Run the Project
```bash
dotnet run
```

## 🧪 Testing

### 1. Test API Endpoints
Use `TestChatbox.http` file with your JWT token:
```http
POST http://localhost:5000/api/Chat/ask
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN

{
  "message": "Từ 'acquire' nghĩa là gì?",
  "userId": 1,
  "conversationType": "vocabulary"
}
```

### 2. Test Frontend
1. Start Angular frontend: `ng serve`
2. Open browser: `http://localhost:4200`
3. Click "AI Tutor" in header
4. Start chatting!

## 🎯 Features

### 4 Conversation Types:
- 📚 **Vocabulary** - Ask about words, generate vocabulary lists
- 📝 **Grammar** - Grammar explanations and examples
- 🎯 **TOEIC Strategy** - Test-taking tips and strategies
- 🏃 **Practice** - Exercise recommendations and study plans

### Special Features:
- 🤖 **AI-powered responses** using Gemini API
- 💾 **Auto-save vocabulary** to user folders
- 📱 **Responsive design** for mobile and desktop
- 🔐 **Authentication required** for security

## 🔧 Troubleshooting

### Common Issues:

1. **"Gemini API key is not configured"**
   - Update `appsettings.json` with your API key

2. **"Unable to find package Google.AI.GenerativeAI"**
   - Run `dotnet restore` to restore packages

3. **"The type or namespace name 'GenerativeAI' does not exist"**
   - Make sure you're using the correct package: `Google.AI.GenerativeAI`

4. **Frontend not loading chatbox**
   - Check browser console for errors
   - Ensure backend is running on port 5000

## 📚 API Endpoints

### POST `/api/Chat/ask`
Ask questions to AI tutor
```json
{
  "message": "Your question",
  "userId": 1,
  "conversationType": "vocabulary"
}
```

### POST `/api/Chat/save-vocabularies`
Save generated vocabulary to user folder
```json
{
  "userId": 1,
  "folderName": "Business Vocabulary",
  "vocabularies": [...]
}
```

## 🎉 Success!

If everything is working, you should see:
- ✅ Backend running on `http://localhost:5000`
- ✅ Frontend running on `http://localhost:4200`
- ✅ Chatbox accessible via "AI Tutor" button in header
- ✅ AI responses working with Gemini API
- ✅ Vocabulary generation and saving working

Happy coding! 🚀
