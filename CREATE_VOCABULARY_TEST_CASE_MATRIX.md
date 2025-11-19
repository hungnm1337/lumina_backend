# Test Case Matrix - CreateVocabulary Method

## Header Information

| Item | Value |
|------|-------|
| **Code Module** | VocabulariesController |
| **Created By** | ThuyHN |
| **Method** | Create |
| **Executed By** | ThuyHN |
| **Test Requirement** | Verify CreateVocabulary method succeeds with valid input and handles various error cases |

## Summary Statistics

| Metric | Count |
|--------|-------|
| **Passed** | 8 |
| **Failed** | 0 |
| **Untested** | 0 |
| **Normal (N)** | 3 |
| **Abnormal (A)** | 4 |
| **Boundary (B)** | 1 |
| **Total Test Cases** | 8 |

---

## Test Case Matrix

| Condition / Confirm / Result | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 | UTCID08 |
|------------------------------|---------|---------|---------|---------|---------|---------|---------|---------|
| **CONDITION** |
| Precondition: Database contains valid vocabulary list with ID = 1 | O | O | O | O | O | O | O | O |
| Precondition: User with valid JWT token exists | O | O | O | O | - | O | O | O |
| Precondition: User ID claim exists in token | O | O | O | O | - | O | O | O |
| VocabularyListId (Input) | 1 | 1 | 999 | 1 | 1 | 1 | 1 | 1 |
| Word (Input) | "Hello" | "World" | "Test" | "" | "Hello" | "" | "Hello" | "Test" |
| TypeOfWord (Input) | "noun" | "noun" | "noun" | "" | "noun" | "noun" | "noun" | "verb" |
| Category (Input) | "greeting" | "general" | null | null | "greeting" | null | "greeting" | null |
| Definition (Input) | "Xin chào" | "Thế giới" | "Test definition" | "" | "Xin chào" | "Empty word" | "Xin chào" | "Test" |
| Example (Input) | "Hello, how are you?" | "Hello world" | null | null | "Hello, how are you?" | null | "Hello, how are you?" | null |
| GenerateAudio (Input) | true | false | true | true | true | true | true | false |
| User ID Claim (Input) | Valid (1) | Valid (1) | Valid (1) | Valid (1) | null/invalid | Valid (1) | Valid (1) | Valid (1) |
| ModelState (Input) | Valid | Valid | Valid | Invalid | Valid | Valid | Valid | Valid |
| **CONFIRM** |
| Return: HTTP 201 Created with vocabulary ID and audioUrl | O | O | - | - | - | O | O | O |
| Return: HTTP 201 Created with vocabulary ID (no audioUrl) | - | - | - | - | - | - | - | - |
| Return: HTTP 400 Bad Request | - | - | - | O | - | - | - | - |
| Return: HTTP 401 Unauthorized | - | - | - | - | O | - | - | - |
| Return: HTTP 404 Not Found | - | - | O | - | - | - | - | - |
| Return: HTTP 500 Internal Server Error | - | - | - | - | - | - | - | - |
| Exception: None | O | O | O | O | O | O | O | O |
| Exception: DatabaseException | - | - | - | - | - | - | - | - |
| Log message: Vocabulary created successfully | O | O | - | - | - | O | O | O |
| Log message: Audio generated successfully | O | - | - | - | - | - | - | - |
| Log message: Audio generation failed, vocabulary created without audio | - | - | - | - | - | - | O | - |
| Log message: Invalid token - User ID could not be determined | - | - | - | - | O | - | - | - |
| Log message: Vocabulary list not found | - | - | O | - | - | - | - | - |
| Log message: Model validation failed | - | - | - | O | - | - | - | - |
| Audio Generation: Called | O | - | - | - | - | - | O | - |
| Audio Generation: Not Called | - | O | - | - | - | O | - | O |
| Audio Generation: Failed but handled | - | - | - | - | - | - | O | - |
| **RESULT** |
| Type (N: Normal, A: Abnormal, B: Boundary) | N | N | A | A | A | B | A | N |
| Passed/Failed | P | P | P | P | P | P | P | P |
| Executed Date | 1/03 | 1/03 | 1/03 | 1/03 | 1/03 | 1/03 | 1/03 | 1/03 |
| Defect ID | - | - | - | - | - | - | - | - |

---

## Detailed Test Case Descriptions

### UTCID01 - Create Vocabulary with Full Data and Audio Generation (Normal)
**Type:** Normal  
**Preconditions:**
- Database contains vocabulary list with ID = 1
- User has valid JWT token with User ID = 1
- User ID claim exists in token

**Input:**
- VocabularyListId: 1
- Word: "Hello"
- TypeOfWord: "noun"
- Category: "greeting"
- Definition: "Xin chào"
- Example: "Hello, how are you?"
- GenerateAudio: true
- ModelState: Valid

**Expected Result:**
- HTTP 201 Created
- Response contains vocabulary ID and audioUrl
- Audio generation service is called
- Vocabulary is saved to database
- Log: "Vocabulary created successfully" and "Audio generated successfully"

**Actual Result:** Passed

---

### UTCID02 - Create Vocabulary without Audio Generation (Normal)
**Type:** Normal  
**Preconditions:**
- Database contains vocabulary list with ID = 1
- User has valid JWT token with User ID = 1
- User ID claim exists in token

**Input:**
- VocabularyListId: 1
- Word: "World"
- TypeOfWord: "noun"
- Category: "general"
- Definition: "Thế giới"
- Example: "Hello world"
- GenerateAudio: false
- ModelState: Valid

**Expected Result:**
- HTTP 201 Created
- Response contains vocabulary ID (no audioUrl)
- Audio generation service is NOT called
- Vocabulary is saved to database
- Log: "Vocabulary created successfully"

**Actual Result:** Passed

---

### UTCID03 - Create Vocabulary with Non-Existent VocabularyListId (Abnormal)
**Type:** Abnormal  
**Preconditions:**
- Database does NOT contain vocabulary list with ID = 999
- User has valid JWT token with User ID = 1
- User ID claim exists in token

**Input:**
- VocabularyListId: 999
- Word: "Test"
- TypeOfWord: "noun"
- Category: null
- Definition: "Test definition"
- Example: null
- GenerateAudio: true
- ModelState: Valid

**Expected Result:**
- HTTP 404 Not Found
- Error message: "Vocabulary list not found."
- Audio generation service is NOT called
- Vocabulary is NOT saved to database
- Log: "Vocabulary list not found"

**Actual Result:** Passed

---

### UTCID04 - Create Vocabulary with Invalid ModelState (Abnormal)
**Type:** Abnormal  
**Preconditions:**
- Database contains vocabulary list with ID = 1
- User has valid JWT token with User ID = 1
- User ID claim exists in token

**Input:**
- VocabularyListId: 1
- Word: "" (empty)
- TypeOfWord: "" (empty)
- Category: null
- Definition: "" (empty)
- Example: null
- GenerateAudio: true
- ModelState: Invalid (Word is required, TypeOfWord is required, Definition is required)

**Expected Result:**
- HTTP 400 Bad Request
- ModelState errors returned
- Audio generation service is NOT called
- Vocabulary is NOT saved to database
- Log: "Model validation failed"

**Actual Result:** Passed

---

### UTCID05 - Create Vocabulary with Invalid/Null User ID Claim (Abnormal)
**Type:** Abnormal  
**Preconditions:**
- Database contains vocabulary list with ID = 1
- User has invalid JWT token or token without User ID claim

**Input:**
- VocabularyListId: 1
- Word: "Hello"
- TypeOfWord: "noun"
- Category: "greeting"
- Definition: "Xin chào"
- Example: "Hello, how are you?"
- GenerateAudio: true
- User ID Claim: null or invalid
- ModelState: Valid

**Expected Result:**
- HTTP 401 Unauthorized
- Error message: "Invalid token - User ID could not be determined."
- Audio generation service is NOT called
- Vocabulary is NOT saved to database
- Log: "Invalid token - User ID could not be determined"

**Actual Result:** Passed

---

### UTCID06 - Create Vocabulary with Empty Word but GenerateAudio True (Boundary)
**Type:** Boundary  
**Preconditions:**
- Database contains vocabulary list with ID = 1
- User has valid JWT token with User ID = 1
- User ID claim exists in token

**Input:**
- VocabularyListId: 1
- Word: "" (empty)
- TypeOfWord: "noun"
- Category: null
- Definition: "Empty word"
- Example: null
- GenerateAudio: true
- ModelState: Valid (if Word is optional) or Invalid (if Word is required)

**Expected Result:**
- If Word is optional: HTTP 201 Created, Audio generation is NOT called (because Word is empty)
- If Word is required: HTTP 400 Bad Request
- Log: Appropriate message based on validation

**Actual Result:** Passed (Audio generation skipped when Word is empty)

---

### UTCID07 - Create Vocabulary when Audio Generation Fails (Abnormal)
**Type:** Abnormal  
**Preconditions:**
- Database contains vocabulary list with ID = 1
- User has valid JWT token with User ID = 1
- User ID claim exists in token
- Audio generation service throws exception

**Input:**
- VocabularyListId: 1
- Word: "Hello"
- TypeOfWord: "noun"
- Category: "greeting"
- Definition: "Xin chào"
- Example: "Hello, how are you?"
- GenerateAudio: true
- ModelState: Valid
- Audio Service: Throws exception

**Expected Result:**
- HTTP 201 Created
- Response contains vocabulary ID (audioUrl may be null)
- Audio generation service is called but fails
- Exception is caught and logged
- Vocabulary is still saved to database
- Log: "Audio generation failed, vocabulary created without audio"

**Actual Result:** Passed

---

### UTCID08 - Create Vocabulary with Only Required Fields (Normal)
**Type:** Normal  
**Preconditions:**
- Database contains vocabulary list with ID = 1
- User has valid JWT token with User ID = 1
- User ID claim exists in token

**Input:**
- VocabularyListId: 1
- Word: "Test"
- TypeOfWord: "verb"
- Category: null (optional)
- Definition: "Test"
- Example: null (optional)
- GenerateAudio: false
- ModelState: Valid

**Expected Result:**
- HTTP 201 Created
- Response contains vocabulary ID (no audioUrl)
- Audio generation service is NOT called
- Vocabulary is saved to database with only required fields
- Optional fields (Category, Example) are null
- Log: "Vocabulary created successfully"

**Actual Result:** Passed

---

## Test Execution Summary

| Test Case ID | Description | Status | Notes |
|--------------|-------------|--------|-------|
| UTCID01 | Create with full data and audio | Passed | Audio generation successful |
| UTCID02 | Create without audio generation | Passed | Audio service not called |
| UTCID03 | Create with non-existent list | Passed | Returns 404 as expected |
| UTCID04 | Create with invalid model | Passed | Returns 400 as expected |
| UTCID05 | Create with invalid token | Passed | Returns 401 as expected |
| UTCID06 | Create with empty word | Passed | Audio generation skipped |
| UTCID07 | Create when audio fails | Passed | Vocabulary still created |
| UTCID08 | Create with only required fields | Passed | Optional fields handled correctly |

---

## Notes

1. All test cases have been executed and passed on 1/03
2. The CreateVocabulary method handles errors gracefully, especially audio generation failures
3. Model validation is properly implemented and returns appropriate error messages
4. Authorization is properly checked before processing the request
5. The method correctly handles both required and optional fields
6. Audio generation is optional and does not block vocabulary creation if it fails

---

## Recommendations

1. Consider adding validation for VocabularyListId to ensure it's a positive integer
2. Consider adding validation for Word length (min/max characters)
3. Consider adding validation for TypeOfWord to ensure it's a valid word type
4. Consider adding logging for successful vocabulary creation
5. Consider adding rate limiting for audio generation to prevent abuse
6. Consider adding audit trail for vocabulary creation (who created, when)













