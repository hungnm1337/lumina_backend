# Unit Test Case Document - CreateArticleAsync

## Header Information

| Field | Value |
|-------|-------|
| **Code Module** | `ArticleService` |
| **Method** | `CreateArticleAsync` |
| **Created By** | [Your Name] |
| **Executed By** | [Your Name] |
| **Test requirement** | Verify CreateArticleAsync method successfully creates article with valid inputs, handles validation errors, and manages transaction rollback on exceptions |

## Summary of Test Results

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

## Test Cases Detail

### Conditions / Preconditions

| Condition | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 | UTCID08 |
|-----------|:-------:|:-------:|:-------:|:-------:|:-------:|:-------:|:-------:|:-------:|
| Database contains valid category with CategoryId = 1 | O | O | O | O | - | - | O | O |
| Database contains valid user with UserId = 1 | O | O | O | O | O | - | O | O |
| Transaction can be started successfully | O | O | O | O | O | O | O | O |
| **Request.Title** | | | | | | | | |
| "Test Article" | O | O | O | - | O | O | O | O |
| **Request.Summary** | | | | | | | | |
| "Test Summary" | O | O | O | - | O | O | O | O |
| **Request.CategoryId** | | | | | | | | |
| 1 (valid) | O | O | O | - | O | O | O | O |
| 999 (not found) | - | - | - | O | - | - | - | - |
| 0 (invalid) | - | - | - | - | - | - | - | O |
| **Request.PublishNow** | | | | | | | | |
| false | O | O | - | - | O | O | O | O |
| true | - | - | O | - | - | - | - | - |
| **Request.Sections** | | | | | | | | |
| Contains 1 section with valid data | O | - | - | - | - | - | - | - |
| Empty list [] | - | O | - | - | - | - | - | - |
| null | - | - | O | - | O | O | O | O |
| **creatorUserId** | | | | | | | | |
| 1 (valid) | O | O | O | O | O | - | O | O |
| 999 (not found) | - | - | - | - | - | O | - | - |
| **Category Repository** | | | | | | | | |
| FindByIdAsync returns valid category | O | O | O | - | O | O | O | O |
| FindByIdAsync returns null | - | - | - | O | - | - | - | - |
| **User Repository** | | | | | | | | |
| GetUserByIdAsync returns valid user | O | O | O | O | O | - | O | O |
| GetUserByIdAsync returns null | - | - | - | - | - | O | - | - |
| **Article Repository** | | | | | | | | |
| AddAsync succeeds | O | O | O | O | - | O | O | O |
| AddAsync throws Exception | - | - | - | - | O | - | - | - |

### Confirm / Return

| Expected Result | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 | UTCID08 |
|-----------------|:-------:|:-------:|:-------:|:-------:|:-------:|:-------:|:-------:|:-------:|
| Returns ArticleResponseDTO | O | O | O | - | - | - | O | - |
| ArticleResponseDTO.ArticleId > 0 | O | O | O | - | - | - | O | - |
| ArticleResponseDTO.Title = "Test Article" | O | O | O | - | - | - | O | - |
| ArticleResponseDTO.Status = "Draft" | O | O | - | - | - | - | O | - |
| ArticleResponseDTO.Status = "Published" | - | - | O | - | - | - | - | - |
| ArticleResponseDTO.IsPublished = false | O | O | - | - | - | - | O | - |
| ArticleResponseDTO.IsPublished = true | - | - | O | - | - | - | - | - |
| ArticleResponseDTO.Sections.Count = 1 | O | - | - | - | - | - | - | - |
| ArticleResponseDTO.Sections.Count = 0 | - | O | O | - | - | - | O | - |
| ArticleResponseDTO.AuthorName = "Test User" | O | O | O | - | - | - | O | - |
| ArticleResponseDTO.CategoryName = "Test" | O | O | O | - | - | - | O | - |
| Throws KeyNotFoundException("Category not found.") | - | - | - | O | - | - | - | - |
| Throws KeyNotFoundException("Creator user not found.") | - | - | - | - | - | O | - | - |
| Throws Exception | - | - | - | - | O | - | - | - |
| Transaction.RollbackAsync is called | - | - | - | - | O | - | - | - |
| Transaction.CommitAsync is called | O | O | O | - | - | - | O | - |
| AddSectionsRangeAsync is NOT called | - | O | O | - | - | - | O | - |
| AddSectionsRangeAsync is called | O | - | - | - | - | - | - | - |

### Exception / Log message

| Log/Exception Message | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 | UTCID08 |
|----------------------|:-------:|:-------:|:-------:|:-------:|:-------:|:-------:|:-------:|:-------:|
| "Successfully created article with ID {ArticleId} by User {UserId}" | O | O | O | - | - | - | O | - |
| "Failed to create article for user {UserId}" | - | - | - | - | O | - | - | - |
| KeyNotFoundException: "Category not found." | - | - | - | O | - | - | - | - |
| KeyNotFoundException: "Creator user not found." | - | - | - | - | - | O | - | - |
| Exception: "Database error" | - | - | - | - | O | - | - | - |

### Result

| Field | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 | UTCID08 |
|-------|:-------:|:-------:|:-------:|:-------:|:-------:|:-------:|:-------:|:-------:|
| **Type** | N | N | N | A | A | A | A | B |
| **Passed/Failed** | P | P | P | P | P | P | P | P |
| **Executed Date** | [Date] | [Date] | [Date] | [Date] | [Date] | [Date] | [Date] | [Date] |
| **Defect ID** | - | - | - | - | - | - | - | - |

---

## Test Case Descriptions

### UTCID01 - Create Article Successfully with Draft Status and Sections (Normal)
**Type:** Normal  
**Description:** Verify that CreateArticleAsync successfully creates an article with Draft status when PublishNow is false, and correctly adds sections to the article.

**Preconditions:**
- Database contains valid category with CategoryId = 1
- Database contains valid user with UserId = 1
- Transaction can be started successfully

**Input:**
- Request.Title = "Test Article"
- Request.Summary = "Test Summary"
- Request.CategoryId = 1
- Request.PublishNow = false
- Request.Sections = [1 section with valid data]
- creatorUserId = 1

**Expected Result:**
- Returns ArticleResponseDTO
- Status = "Draft"
- IsPublished = false
- Sections.Count = 1
- Transaction.CommitAsync is called
- AddSectionsRangeAsync is called
- Log: "Successfully created article with ID {ArticleId} by User {UserId}"

---

### UTCID02 - Create Article Successfully with Draft Status and Empty Sections (Normal)
**Type:** Normal  
**Description:** Verify that CreateArticleAsync successfully creates an article with Draft status when Sections is an empty list, and does not call AddSectionsRangeAsync.

**Preconditions:**
- Database contains valid category with CategoryId = 1
- Database contains valid user with UserId = 1
- Transaction can be started successfully

**Input:**
- Request.Title = "Test Article"
- Request.Summary = "Test Summary"
- Request.CategoryId = 1
- Request.PublishNow = false
- Request.Sections = [] (empty list)
- creatorUserId = 1

**Expected Result:**
- Returns ArticleResponseDTO
- Status = "Draft"
- IsPublished = false
- Sections.Count = 0
- Transaction.CommitAsync is called
- AddSectionsRangeAsync is NOT called
- Log: "Successfully created article with ID {ArticleId} by User {UserId}"

---

### UTCID03 - Create Article Successfully with Published Status (Normal)
**Type:** Normal  
**Description:** Verify that CreateArticleAsync successfully creates an article with Published status when PublishNow is true.

**Preconditions:**
- Database contains valid category with CategoryId = 1
- Database contains valid user with UserId = 1
- Transaction can be started successfully

**Input:**
- Request.Title = "Test Article"
- Request.Summary = "Test Summary"
- Request.CategoryId = 1
- Request.PublishNow = true
- Request.Sections = null
- creatorUserId = 1

**Expected Result:**
- Returns ArticleResponseDTO
- Status = "Published"
- IsPublished = true
- Sections.Count = 0
- Transaction.CommitAsync is called
- Log: "Successfully created article with ID {ArticleId} by User {UserId}"

---

### UTCID04 - Create Article with Category Not Found (Abnormal)
**Type:** Abnormal  
**Description:** Verify that CreateArticleAsync throws KeyNotFoundException when the specified category does not exist in the database.

**Preconditions:**
- Database does NOT contain category with CategoryId = 999
- Transaction can be started successfully

**Input:**
- Request.Title = "Test Article"
- Request.Summary = "Test Summary"
- Request.CategoryId = 999 (not found)
- creatorUserId = 1

**Expected Result:**
- Throws KeyNotFoundException
- Exception message = "Category not found."
- Transaction.RollbackAsync is NOT called (exception occurs before transaction)
- Log error is NOT called

---

### UTCID05 - Create Article with Database Exception (Abnormal)
**Type:** Abnormal  
**Description:** Verify that CreateArticleAsync properly handles database exceptions by rolling back the transaction and rethrowing the exception.

**Preconditions:**
- Database contains valid category with CategoryId = 1
- Database contains valid user with UserId = 1
- Transaction can be started successfully
- ArticleRepository.AddAsync throws Exception

**Input:**
- Request.Title = "Test Article"
- Request.Summary = "Test Summary"
- Request.CategoryId = 1
- Request.PublishNow = false
- Request.Sections = null
- creatorUserId = 1

**Expected Result:**
- Throws Exception
- Exception message = "Database error"
- Transaction.RollbackAsync is called
- Log: "Failed to create article for user {UserId}"

---

### UTCID06 - Create Article with Creator User Not Found (Abnormal)
**Type:** Abnormal  
**Description:** Verify that CreateArticleAsync throws KeyNotFoundException when the specified creator user does not exist in the database.

**Preconditions:**
- Database contains valid category with CategoryId = 1
- Database does NOT contain user with UserId = 999
- Transaction can be started successfully

**Input:**
- Request.Title = "Test Article"
- Request.Summary = "Test Summary"
- Request.CategoryId = 1
- Request.PublishNow = false
- Request.Sections = null
- creatorUserId = 999 (not found)

**Expected Result:**
- Throws KeyNotFoundException
- Exception message = "Creator user not found."
- Transaction.RollbackAsync is NOT called (exception occurs before transaction)
- Log error is NOT called

---

### UTCID07 - Create Article Successfully with Null Sections (Normal)
**Type:** Normal  
**Description:** Verify that CreateArticleAsync successfully creates an article when Sections is null, and does not call AddSectionsRangeAsync.

**Preconditions:**
- Database contains valid category with CategoryId = 1
- Database contains valid user with UserId = 1
- Transaction can be started successfully

**Input:**
- Request.Title = "Test Article"
- Request.Summary = "Test Summary"
- Request.CategoryId = 1
- Request.PublishNow = false
- Request.Sections = null
- creatorUserId = 1

**Expected Result:**
- Returns ArticleResponseDTO
- Status = "Draft"
- IsPublished = false
- Sections.Count = 0
- Transaction.CommitAsync is called
- AddSectionsRangeAsync is NOT called
- Log: "Successfully created article with ID {ArticleId} by User {UserId}"

---

### UTCID08 - Create Article with Invalid CategoryId (Boundary)
**Type:** Boundary  
**Description:** Verify that CreateArticleAsync throws KeyNotFoundException when CategoryId is 0 (boundary value for invalid ID).

**Preconditions:**
- Database does NOT contain category with CategoryId = 0
- Transaction can be started successfully

**Input:**
- Request.Title = "Test Article"
- Request.Summary = "Test Summary"
- Request.CategoryId = 0 (invalid/boundary)
- Request.PublishNow = false
- Request.Sections = null
- creatorUserId = 1

**Expected Result:**
- Throws KeyNotFoundException
- Exception message = "Category not found."
- Transaction.RollbackAsync is NOT called (exception occurs before transaction)
- Log error is NOT called

---

## Notes

- All test cases use Moq framework for mocking dependencies
- Transaction handling is verified to ensure data consistency
- Log messages are verified to ensure proper logging for debugging
- Section handling is verified to ensure sections are only added when provided
- Status and IsPublished flags are verified to ensure correct article state










