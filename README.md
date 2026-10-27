# מערכת לניהול פניות (Requests Management System)

מערכת Full Stack לניהול פניות שירות: `.NET 8 Web API` עם `EF Core` ו-`SQL Server`, ו-`Angular` בצד הלקוח.

> המסמכים המפורטים בתיקייה [`docs/`](docs/) כתובים באנגלית. קובץ זה הוא סיכום בעברית עם קישורים אליהם.

## תוכן עניינים

מיפוי מהיר של כל דרישה לקוד ולהוכחה: [מיפוי דרישות המבחן](#מיפוי-דרישות-המבחן)

1. [סקירה כללית](#1-סקירה-כללית)
2. [טכנולוגיות וגרסאות](#2-טכנולוגיות-וגרסאות)
3. [הוראות הרצה](#3-הוראות-הרצה)
4. [מבנה הפתרון](#4-מבנה-הפתרון)
5. [API](#5-api)
6. [בסיס נתונים ואינדקסים](#6-בסיס-נתונים-ואינדקסים)
7. [Concurrency](#7-concurrency)
8. [Bulk Update](#8-bulk-update)
9. [Cache ו-Invalidation](#9-cache-ו-invalidation)
10. [בדיקות אוטומטיות](#10-בדיקות-אוטומטיות)
11. [החלטות טכנולוגיות](#11-החלטות-טכנולוגיות)
12. [מגבלה ידועה ושיפור אפשרי](#12-מגבלה-ידועה-ושיפור-אפשרי)
13. [תכנון עבודה](#13-תכנון-עבודה)
14. [שימוש בכלי AI](#14-שימוש-בכלי-ai)

---

## 1. סקירה כללית

המערכת מנהלת פניות שירות של ארגונים. לכל פנייה יש כותרת, ארגון, סטטוס, עדיפות, גורם מטפל ותאריכים.
המשתמש יכול לחפש, לסנן, למיין ולדפדף ב-100,000 פניות. כל הפעולות האלה מתבצעות בשרת, בתוך SQL Server.
אפשר לשנות סטטוס של פנייה בודדת או של עד 100 פניות בבת אחת. המערכת מונעת דריסה של שינויים מקבילים (`409 Conflict`), ושומרת היסטוריה של כל שינוי סטטוס.
בראש המסך מוצג פאנל סיכום (כמות פניות לפי סטטוס ולפי עדיפות), והוא נשמר ב-Cache קצר.

---

## מיפוי דרישות המבחן

נתיבי הקבצים יחסיים ל-`backend/` (שרת) ול-`frontend/requests-app/src/app/` (Angular). הבדיקות נמצאות ב-`backend/RequestsApi.Tests/`.

| דרישה | איפה ממומש | הוכחה |
|---|---|---|
| **1. שליפה:** דפדוף, פילטרים, חיפוש ומיון בשרת | `RequestsRepository.GetPagedAsync`, `ApplyFilters`, `ApplySorting` (ב-SQL, `Skip`/`Take`) | `GetRequests_CombinedFiltersAndPaging_ReturnsExactPageAndTotalCount`; ה-SQL שנוצר ב-[סעיף 6](#6-בסיס-נתונים-ואינדקסים) |
| הגבלת Page Size ל-100 ו-Validation | `RequestQueryValidator` (`MaxPageSize = 100`) | `GetRequests_InvalidQueryParameter_Returns400WithFieldError` (`pageSize=500`, `status=Done`) |
| Aggregations | `RequestsRepository.GetCountsAsync` (`GROUP BY`), `RequestsService.GetStatsAsync` | `GetStats_AfterSuccessfulStatusUpdate_ReturnsUpdatedCountsImmediately`; מדידה 2 ב-[`performance.md`](docs/performance.md) |
| `CancellationToken` | כל 8 המתודות ב-`IRequestsRepository` ו-6 ב-`IRequestsService`, מה-Controller ועד EF Core | קוד |
| DTOs בלבד | `RequestsBL/Dtos`, `RequestsBL/Mapping/RequestMappings.cs` | ה-API לא יכול להשתמש ב-Entities: `DisableTransitiveProjectReferences` ([סעיף 4](#4-מבנה-הפתרון)) |
| **2. עדכון סטטוס:** `404` | `RequestsService.UpdateStatusAsync` | `UpdateStatus_UnknownId_Returns404` |
| מעבר לא חוקי נדחה בשרת | `Rules/StatusTransitions.cs` (`GetError`), `RequestsService.ApplyStatusChangeAsync` | `UpdateStatus_InvalidTransition_Returns400AndChangesNothing`; `GetError_Transition_ReturnsNullOnlyWhenAllowed` (14 מקרים); `InvalidTransition` בבדיקת ה-Bulk |
| Optimistic Concurrency שנאכף בבסיס הנתונים | `RequestsRepository.UpdateStatusAsync` (`RowVersion` כ-`OriginalValue` → `WHERE [RowVersion] = @p`) | `UpdateStatus_TwoConcurrentUpdatesWithSameRowVersion_OneSucceedsAndOneConflicts`; [סעיף 7](#7-concurrency) |
| עדכון `UpdatedAt` ו-`RowVersion` | `RequestsRepository.UpdateStatusAsync` | נבדק ב-`PATCH` אמיתי (ראו תוצאות אימות) |
| **3. היסטוריה:** שורה באותה טרנזקציה, בלי שורה כשהעדכון נכשל (גם ב-Bulk) | שורת ההיסטוריה והעדכון באותו `SaveChanges` | בדיקות ה-Concurrency, ה-Bulk המעורב, המעבר הלא חוקי וה-Bulk הלא תקין בודקות את מספר שורות ההיסטוריה (1 למצליח, 0 לנכשל) |
| Endpoint להיסטוריה | `GET /api/requests/{id}/history`, `RequestsRepository.GetHistoryAsync` | `GetHistory_AfterSuccessfulUpdate_ReturnsEntryWithAllFields`, `GetHistory_UnknownId_Returns404` |
| **4. Bulk:** עד 100, Partial Success | `RequestsService.BulkUpdateStatusAsync`, `StatusUpdateValidator` (`MaxBulkItems = 100`) | `BulkUpdateStatus_MixedItems_ReturnsOutcomePerItemAndChangesOnlyValidItems`; `BulkUpdateStatus_InvalidRequest_Returns400AndChangesNothing` (101 פריטים, מזהים כפולים); [סעיף 8](#8-bulk-update) |
| **5. Angular:** `debounceTime` על החיפוש בלבד | `requests-page.ts` (`search$`) | קוד; [סעיף 4](#4-מבנה-הפתרון) |
| `switchMap` שמבטל בקשה קודמת | `requests-page.ts` (`loads$`) | קוד |
| Loading / Empty / Error עם Retry | `requests-page.html` (`list.loading`, `totalCount === 0`, `list.error` עם `retry()`) | קוד |
| טיפול ב-`409` | `requests-page.ts` (`onStatusChange`): הודעה, ו-`GET /api/requests/{id}` לשורה בלבד | נבדק ב-Chrome מול ה-API: שינוי מקביל ← הודעה ← השורה נטענה מחדש ([`ai-usage.md`](docs/ai-usage.md)) |
| Aggregations ב-UI | `requests-stats` (נטען מחדש אחרי עדכון מוצלח) | נבדק ב-Chrome |
| היסטוריה ב-UI | `request-history-dialog` (Loading / Empty / Error) | נבדק ב-Chrome (הדיאלוג מציג את השינויים); מצבי Empty ו-Error בקוד |
| הפרדה בין Components ל-Services | רק `core/services/requests-api.service.ts` משתמש ב-`HttpClient`; רכיבי התצוגה עובדים עם `input()`/`output()` בלבד | חיפוש בקוד (ראו דגשים) |
| **6. ביצועים:** Execution Plans, אינדקסים, צוואר בקבוק ושיפור | `RequestsDAL/Data/Configurations`, 2 Migrations, Rebuild ב-`02_seed.sql` | [`performance.md`](docs/performance.md) ו-17 קובצי `.sqlplan`; השוואה לפני ואחרי אינדקס |
| **7. Cache:** Expiration, Invalidation, Scale-out | `RequestsService` (`StatsCacheDuration` של 60 שניות, `_cache.Remove` אחרי כל עדכון מוצלח) | `GetStats_AfterSuccessfulStatusUpdate_ReturnsUpdatedCountsImmediately`; [`cache.md`](docs/cache.md) |
| **8. בדיקות** | `backend/RequestsApi.Tests`: 30 בדיקות (12 Integration מול LocalDB, 18 Unit) | [`tests.md`](docs/tests.md) |
| **9. תכנון עבודה** | – | [`work-plan.md`](docs/work-plan.md) |
| **10. איכות קוד** | 3 שכבות שנאכפות בקומפילציה, `ProblemDetails` אחיד (`GlobalExceptionHandler`) | `dotnet build`: 0 אזהרות |
| **11. תיעוד** | README זה, [`docs/`](docs/) | [`ai-usage.md`](docs/ai-usage.md), [`known-limitations.md`](docs/known-limitations.md) |

### תוצאות אימות

נבדק ב-27.10.2026 על `git clone` נקי של commit `ff39d85`, לפי [סעיף 3](#3-הוראות-הרצה) (דרך א' ליצירת ה-DB):

- `dotnet test`: **25 בדיקות, כולן עברו** (`Failed: 0`). אחרי שנוספו 5 בדיקות (מעבר לא חוקי, היסטוריה ו-Validation של Bulk), הורץ שוב: **30 בדיקות, כולן עברו**.
- `ng build --configuration production`: הסתיים **בלי אזהרות ובלי שגיאות**.
- `02_seed.sql`: בדיוק **100,000** שורות ב-`Requests` (כ-6 שניות, ועוד כשנייה ל-Rebuild).
- הוראות ההרצה עבדו מקצה לקצה: `01_schema.sql`, `02_seed.sql`, `npm install`, ה-API (Swagger מחזיר `200`) והאפליקציה (`200`, הרשימה וה-Stats מוצגים ב-Chrome).
- `PATCH` אמיתי: `200`, ו-`RowVersion` ו-`UpdatedAt` חדשים. שליחה חוזרת עם ה-`RowVersion` הישן: `409`. מזהה שלא קיים: `404`. בהיסטוריה נכתבה שורה אחת.

### דגשים להערכה

- **אין סינון, מיון או דפדוף בצד הלקוח:** כל השאילתה נשלחת כ-Query Parameters (`requests-api.service.ts`). חיפוש בכל קוד ה-Angular לא מוצא `.filter(`, `.sort(` או `.slice(` על רשימת הפניות. ([סעיף 4](#4-מבנה-הפתרון))
- **ה-Concurrency נאכף בבסיס הנתונים, לא רק ב-UI:** `UPDATE ... WHERE [RowVersion] = @p`. בקשות מקבילות אמיתיות: בכל ההרצות התקבלו `200` אחד ו-`409` אחד. ([סעיף 7](#7-concurrency))
- **ל-Cache יש אסטרטגיית Invalidation:** מחיקה אחרי כל עדכון מוצלח, ו-Expiration של 60 שניות כגיבוי. ([סעיף 9](#9-cache-ו-invalidation), [`cache.md`](docs/cache.md))
- **תוכנית העבודה משקפת דרישות, תלויות ומאמץ:** 35 משימות ב-9 Epics, עם תלויות, הערכת זמן (כ-16 שעות) וטבלה שמקשרת כל סעיף במבחן למשימות. ([`work-plan.md`](docs/work-plan.md))
- **בלי Over-engineering:**
  - **Redis לא מומש בכוונה:** יש שרת API אחד, ולכן `IMemoryCache` מספיק. מה צריך לשנות כשיש כמה שרתים מתועד ב-[`cache.md`](docs/cache.md).
  - **Full-Text Search לא מומש בכוונה:** הוא לא מותקן ב-LocalDB שעליו המערכת רצה (`FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 0`). בנוסף, הוא מחפש מילים ולא כל תת-מחרוזת. העלות של `LIKE` נמדדה ומתועדת כשיפור אפשרי. ([סעיף 12](#12-מגבלה-ידועה-ושיפור-אפשרי))

---

## 2. טכנולוגיות וגרסאות

| רכיב | גרסה | מקור |
|---|---|---|
| .NET SDK | `8.0.415` (עם `rollForward: latestFeature`) | `backend/global.json` |
| ASP.NET Core / Target Framework | `net8.0` | קובצי `.csproj` |
| EF Core (`Microsoft.EntityFrameworkCore.SqlServer`) | `8.0.11` | `backend/RequestsDAL/RequestsDAL.csproj` |
| `dotnet-ef` (כלי מקומי) | `8.0.11` | `backend/.config/dotnet-tools.json` |
| Swashbuckle (Swagger) | `6.6.2` | `backend/RequestsApi/RequestsApi.csproj` |
| xUnit | `2.5.3` | `backend/RequestsApi.Tests/RequestsApi.Tests.csproj` |
| `Microsoft.AspNetCore.Mvc.Testing` | `8.0.11` | `backend/RequestsApi.Tests/RequestsApi.Tests.csproj` |
| Angular | `22.2.3` (`package.json`: `^22.2.0`) | `frontend/requests-app/package.json` |
| Angular CLI | `22.2.4` | `frontend/requests-app/package.json` |
| Angular Material / CDK | `22.2.4` | `frontend/requests-app/package.json` |
| RxJS | `7.8.2` (`package.json`: `~7.8.0`) | `frontend/requests-app/package.json` |
| TypeScript | `6.0.3` | `frontend/requests-app/package.json` |
| Node.js | נדרש `^22.22.3 \|\| ^24.15.0 \|\| >=26.0.0`; פותח ונבדק עם `22.23.3` | `engines` של `@angular/core` |
| SQL Server | `SQL Server LocalDB` (נבדק עם `SQL Server 2025 LocalDB 17.0.4025.3`) | `backend/RequestsApi/appsettings.json` |

הגרסאות המדויקות של חבילות ה-npm נעולות ב-`frontend/requests-app/package-lock.json`.

---

## 3. הוראות הרצה

ההוראות נבדקו מ-`git clone` נקי, על Windows. הפקודות כתובות ל-PowerShell או ל-`cmd`, ויש להריץ אותן מתיקיית השורש של ה-Repository, אלא אם כתוב אחרת.

### 3.1 דרישות מוקדמות

- **.NET SDK 8** בגרסה `8.0.415` ומעלה, מאותו Feature Band (`8.0.4xx`). בדיקה: `dotnet --version`.
- **Node.js** בגרסה `22.22.3` ומעלה (או `24.15.0` ומעלה), ו-npm. בדיקה: `node -v`.
- **SQL Server LocalDB**: מגיע עם Visual Studio, או עם התקנת SQL Server Express (רכיב `LocalDB`). בדיקה: `sqllocaldb info`.
- **`sqlcmd`**: מגיע עם כלי SQL Server. בדיקה: `sqlcmd -?`.

**שימוש ב-SQL Server אחר (לא LocalDB):** ה-API קורא את ה-Connection String בשם `RequestsDb` מתוך `backend/RequestsApi/appsettings.json`:

```json
"ConnectionStrings": {
  "RequestsDb": "Server=(localdb)\\MSSQLLocalDB;Database=RequestsManagementDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

אפשר לשנות אותו בקובץ, או לדרוס אותו במשתנה סביבה בלי לשנות קבצים:

```powershell
$env:ConnectionStrings__RequestsDb = "Server=myserver;Database=RequestsManagementDb;User Id=sa;Password=...;TrustServerCertificate=True"
```

בפקודות `sqlcmd` שבהמשך, יש להחליף את `-S "(localdb)\MSSQLLocalDB"` בשם השרת, ולהוסיף `-U` ו-`-P` אם לא משתמשים ב-Windows Authentication.

### 3.2 שכפול ה-Repository

```powershell
git clone <repository-url> requests-management
cd requests-management
```

### 3.3 יצירת בסיס הנתונים

יש שתי דרכים שקולות. שתיהן יוצרות את בסיס הנתונים `RequestsManagementDb` עם אותן טבלאות ואותם אינדקסים.

**דרך א': סקריפט SQL**

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "CREATE DATABASE RequestsManagementDb"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d RequestsManagementDb -i database\01_schema.sql
```

הקובץ `01_schema.sql` נוצר מה-Migrations של EF Core, והוא Idempotent: אפשר להריץ אותו שוב בלי נזק.

**דרך ב': EF Core Migrations**

```powershell
cd backend
dotnet tool restore
dotnet ef database update --project RequestsDAL --startup-project RequestsApi
cd ..
```

`dotnet tool restore` מתקין את `dotnet-ef 8.0.11` המוגדר ב-`backend/.config/dotnet-tools.json`. הפקודה `database update` יוצרת את בסיס הנתונים אם הוא לא קיים.

### 3.4 טעינת 100,000 פניות

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d RequestsManagementDb -i database\02_seed.sql
```

הסקריפט מוחק את כל הנתונים הקיימים (כולל ההיסטוריה), מכניס 100,000 פניות בפקודת `INSERT ... SELECT` אחת, ובונה מחדש את האינדקסים. הנתונים דטרמיניסטיים, כך שכל הרצה מייצרת בדיוק אותן שורות. זמן ריצה: 5 עד 7 שניות.

### 3.5 הפעלת ה-API

בחלון טרמינל ראשון:

```powershell
cd backend
dotnet run --project RequestsApi
```

- API: `http://localhost:5069`
- Swagger: `http://localhost:5069/swagger`

הפרופיל `http` ב-`launchSettings.json` מפעיל את סביבת `Development`. בסביבה זו פעילים Swagger ו-CORS עבור `http://localhost:4200`.

### 3.6 הפעלת אפליקציית Angular

בחלון טרמינל שני:

```powershell
cd frontend\requests-app
npm install
npm start
```

- האפליקציה: `http://localhost:4200`

כתובת ה-API מוגדרת ב-`frontend/requests-app/src/environments/environment.development.ts` (`apiUrl`). אם ה-API רץ בכתובת אחרת, יש לעדכן אותה שם.

כדי לשנות סטטוס, יש להקליד שם בשדה `Your name` בראש המסך.

### 3.7 הרצת הבדיקות

**Backend** (xUnit, כולל Integration Tests מול LocalDB):

```powershell
cd backend
dotnet test
```

הבדיקות יוצרות בסיס נתונים נפרד בשם `RequestsDb_Tests`, ולא נוגעות ב-`RequestsManagementDb`. ה-Connection String של הבדיקות מוגדר ב-`backend/RequestsApi.Tests/Infrastructure/ApiFixture.cs`.

אם ה-API רץ באותו זמן מאותה תיקייה, ייתכן שה-Build ייכשל בגלל קבצי DLL נעולים. במקרה כזה יש לעצור את ה-API לפני `dotnet test`.

**Frontend** (Vitest):

```powershell
cd frontend\requests-app
npm test -- --watch=false
```

---

## 4. מבנה הפתרון

```text
.
├── README.md
├── backend/
│   ├── RequestsManagement.sln
│   ├── global.json                 גרסת ה-SDK
│   ├── .config/dotnet-tools.json   dotnet-ef 8.0.11
│   ├── RequestsApi/                שכבת API: Controllers, Middleware, Program.cs
│   ├── RequestsBL/                 שכבה עסקית: Services, Dtos, Validation, Rules, Mapping, Exceptions
│   ├── RequestsDAL/                גישה לנתונים: Data (DbContext, Configurations, Migrations), Entities, Repositories
│   └── RequestsApi.Tests/          בדיקות: Infrastructure, Integration, Unit
├── database/
│   ├── 01_schema.sql               יצירת הטבלאות והאינדקסים (Idempotent)
│   └── 02_seed.sql                 100,000 פניות
├── frontend/requests-app/          אפליקציית Angular
└── docs/                           תיעוד מפורט (באנגלית)
```

### שלוש השכבות

| שכבה | פרויקט | אחריות |
|---|---|---|
| API | `RequestsApi` | HTTP בלבד: ניתוב, קלט, קודי סטטוס, טיפול גלובלי בשגיאות (`GlobalExceptionHandler` שמחזיר `ProblemDetails`). ה-Controllers דקים וקוראים רק ל-`IRequestsService`. |
| Business Logic | `RequestsBL` | Validation של הקלט, כללי מעבר הסטטוס (`Rules/StatusTransitions.cs`), Concurrency, Bulk, Cache, והמרה ל-DTOs. מחזירה DTOs בלבד, אף פעם לא Entities. |
| Data Access | `RequestsDAL` | `DbContext`, Entities, קונפיגורציה, Migrations ו-`IRequestsRepository`. כל הסינון, המיון והדפדוף מתבצעים כאן, ב-SQL. |

**הפרדת השכבות נאכפת על ידי הקומפיילר.** `RequestsApi` מפנה רק ל-`RequestsBL`, וב-`RequestsApi.csproj` מוגדר:

```xml
<DisableTransitiveProjectReferences>true</DisableTransitiveProjectReferences>
```

בלי ההגדרה הזו, ה-API היה "רואה" גם את `RequestsDAL` דרך `RequestsBL`. איתה, כל ניסיון להשתמש ב-`DbContext` או ב-Entity מתוך ה-API נכשל בקומפילציה. ה-DLL של ה-DAL עדיין מועתק לתיקיית ההרצה, כך שבזמן ריצה הכול עובד.

ה-DI מוגדר בכל שכבה במתודת הרחבה משלה: `Program.cs` קורא רק ל-`AddBusinessLayer(connectionString)`, והיא קוראת ל-`AddDataAccessLayer(connectionString)`. כל ה-Services, ה-Repositories וה-`DbContext` רשומים כ-`Scoped`.

### מבנה Angular

```text
src/app/
├── core/
│   ├── models/          ממשקי TypeScript שתואמים ל-DTOs של ה-API
│   ├── services/        requests-api.service.ts (קריאות HTTP בלבד), current-user.service.ts
│   └── utils/           error-message.ts (הודעת שגיאה מתוך ProblemDetails)
└── features/requests/
    ├── requests-page/   רכיב העמוד: מחזיק את ה-State ואת זרם ה-RxJS
    └── components/      requests-filters, requests-table, requests-stats, status-editor,
                         request-history-dialog
```

- **`RequestsApiService`** הוא המקום היחיד שקורא ל-`HttpClient`.
- **`RequestsPage`** מחזיק את ה-State: חיפוש, פילטרים, מיון ודפדוף נכנסים ל-`combineLatest`. רק טקסט החיפוש עובר `debounceTime(300)` ו-`distinctUntilChanged`, ו-`switchMap` מבטל בקשה ישנה כשמגיעה שאילתה חדשה.
- **רכיבי תצוגה** (`requests-filters`, `requests-table`, `requests-stats`, `status-editor`) מקבלים נתונים ב-`input()` ומדווחים על שינויים ב-`output()`. הם לא קוראים ל-API.
- **`request-history-dialog`** הוא דיאלוג Material שטוען בעצמו את ההיסטוריה של פנייה אחת.
- **Subscriptions** מנוהלים עם `async pipe` או `takeUntilDestroyed`.

---

## 5. API

כל השגיאות מוחזרות בפורמט `ProblemDetails` (RFC 7807). שגיאה לא צפויה מחזירה `500` עם הודעה כללית, בלי Stack Trace.

| Method | Route | מטרה | קודי סטטוס |
|---|---|---|---|
| `GET` | `/api/requests` | רשימת פניות עם חיפוש, פילטרים, מיון ודפדוף | `200`, `400` |
| `GET` | `/api/requests/stats` | כמות פניות לפי סטטוס ולפי עדיפות (כל הפניות, ללא פילטרים) | `200` |
| `GET` | `/api/requests/{id}` | פנייה אחת | `200`, `404` |
| `PATCH` | `/api/requests/{id}/status` | שינוי סטטוס, עם `rowVersion` ו-`changedBy` | `200`, `400`, `404`, `409` |
| `GET` | `/api/requests/{id}/history` | היסטוריית שינויי הסטטוס, מהחדש לישן | `200`, `404` |
| `POST` | `/api/requests/bulk-status` | שינוי סטטוס של 1 עד 100 פניות, Partial Success | `200`, `400` |

**פרמטרים של `GET /api/requests`:**

| פרמטר | ערכים |
|---|---|
| `page` | ברירת מחדל `1` |
| `pageSize` | ברירת מחדל `20`, מקסימום `100` |
| `status` | `New`, `InProgress`, `Waiting`, `Completed` |
| `priority` | `Low`, `Medium`, `High` |
| `organizationName`, `assignedTo` | התאמה מדויקת |
| `createdFrom`, `createdTo` | תאריך. אם `createdTo` הוא תאריך בלי שעה, כל היום נכלל. |
| `search` | חיפוש טקסט ב-`Title` וב-`OrganizationName`, עד 100 תווים |
| `sortBy` | `createdAt` (ברירת מחדל), `updatedAt`, `priority`, `status`, `title`, `organizationName` |
| `sortDirection` | `asc` או `desc` (ברירת מחדל) |

ערך לא תקין מחזיר `400` עם שגיאה לכל שדה, למשל:

```json
{ "title": "Validation failed", "status": 400, "errors": { "pageSize": ["Page size must be between 1 and 100."] } }
```

**כללי מעבר סטטוס:** `New → InProgress`, `InProgress → Waiting / Completed`, `Waiting → InProgress`. `Completed` הוא סטטוס סופי. כל `RequestDto` כולל את השדה `allowedNextStatuses`, שמחושב בשרת מאותם כללים. כך ה-UI מציג רק מעברים חוקיים, והכללים לא משוכפלים ב-Angular.

---

## 6. בסיס נתונים ואינדקסים

**למה SQL Server:** זה חלק מהאפיון. בנוסף, יש בו עמודת `rowversion` מובנית, שמתאימה בדיוק ל-Optimistic Concurrency. LocalDB מאפשר הרצה מקומית בלי התקנת שרת מלא, ו-Execution Plans מאפשרים למדוד את השאילתות.

### טבלאות

| טבלה | עמודות עיקריות |
|---|---|
| `Requests` | `Id`, `Title` (`nvarchar(200)`), `OrganizationName` (`nvarchar(200)`), `Status` (`tinyint`), `Priority` (`tinyint`), `AssignedTo` (`nvarchar(100)`, יכול להיות `NULL`), `CreatedAt`, `UpdatedAt` (`datetime2`, ב-UTC), `RowVersion` (`rowversion`) |
| `RequestStatusHistory` | `Id`, `RequestId` (FK עם `ON DELETE CASCADE`), `PreviousStatus`, `NewStatus`, `ChangedAt`, `ChangedBy` (`nvarchar(100)`) |

`Status` ו-`Priority` נשמרים כ-`tinyint` (Enums), וב-API הם מוצגים כמחרוזות.

### אינדקסים

| אינדקס | עמודות | התרחיש שהוא משרת |
|---|---|---|
| `PK_Requests` (Clustered) | `Id` | שליפה לפי `id`, ו-Key Lookup |
| `IX_Requests_Status_CreatedAt` | `Status, CreatedAt` | סינון לפי סטטוס, ממוין לפי תאריך יצירה (הרשימה הנפוצה ביותר), כולל ה-`COUNT` |
| `IX_Requests_Priority_CreatedAt` | `Priority, CreatedAt` | סינון לפי עדיפות, ממוין לפי תאריך יצירה |
| `IX_Requests_CreatedAt` | `CreatedAt` | מיון ברירת המחדל (`createdAt desc`) וטווח תאריכים |
| `IX_Requests_OrganizationName` | `OrganizationName` | סינון לפי ארגון |
| `IX_Requests_AssignedTo` | `AssignedTo` | סינון לפי גורם מטפל |
| `IX_RequestStatusHistory_RequestId_ChangedAt` | `RequestId, ChangedAt` | היסטוריה של פנייה אחת, מהחדש לישן |

אחרי המדידות, האינדקס `IX_Requests_Priority` (עמודה אחת) הוחלף ב-`(Priority, CreatedAt)`: דף ראשון של `priority=High` ירד מ-232 ל-63 Logical Reads. הפרטים ב-[`docs/performance.md`](docs/performance.md).

### איך נמנעת טעינה של כל השורות לזיכרון

ה-Repository בונה `IQueryable` (עם `AsNoTracking`), ומוסיף לו `Where`, `OrderBy`, `Skip` ו-`Take`. EF Core מתרגם את כל זה לשאילתת SQL אחת, כך שמה-DB חוזרים רק 20 השורות של הדף, ועוד `COUNT` אחד. מיון תמיד מסתיים ב-`Id`, כדי שהסדר יהיה יציב בין דפים. לדוגמה, `status=Waiting`, דף 1:

```sql
SELECT COUNT(*) FROM [Requests] AS [r] WHERE [r].[Status] = @__status_0

SELECT [r].[Id], [r].[AssignedTo], [r].[CreatedAt], ... , [r].[UpdatedAt]
FROM [Requests] AS [r]
WHERE [r].[Status] = @__status_0
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @__p_1 ROWS FETCH NEXT @__p_2 ROWS ONLY
```

גם ה-Aggregations מחושבים ב-SQL, ב-`GROUP BY [Status], [Priority]`. מה-DB חוזרות 12 שורות לכל היותר.

מדידות, Execution Plans (קובצי `.sqlplan`), השוואה לפני ואחרי אינדקס, וצווארי בקבוק: [`docs/performance.md`](docs/performance.md).

---

## 7. Concurrency

**הבעיה:** שני משתמשים קוראים את אותה פנייה ומשנים אותה במקביל. בלי הגנה, השינוי השני דורס את הראשון ("Lost Update").

**הפתרון: Optimistic Concurrency עם `rowversion`.**

1. לכל שורה ב-`Requests` יש עמודת `RowVersion` מסוג `rowversion`. SQL Server משנה אותה אוטומטית בכל עדכון של השורה.
2. ה-API מחזיר את הערך (Base64) בכל `RequestDto`, והלקוח שולח אותו בחזרה ב-`PATCH`.
3. ה-Repository מגדיר את הערך שהלקוח שלח כ-`OriginalValue`, ולכן EF Core מייצר:

   ```sql
   UPDATE [Requests] SET [Status] = @p5, [UpdatedAt] = @p6
   OUTPUT INSERTED.[RowVersion]
   WHERE [Id] = @p7 AND [RowVersion] = @p8;
   ```

4. SQL Server נועל את השורה בזמן העדכון, כך ששני עדכונים מקבילים רצים אחד אחרי השני. הראשון מצליח ומשנה את `RowVersion`. אצל השני ה-`WHERE` כבר לא מתאים, ולכן 0 שורות מתעדכנות.
5. EF Core מזהה 0 שורות וזורק `DbUpdateConcurrencyException`. ה-Repository מחזיר `Conflict`, וה-API מחזיר `409`. כל הטרנזקציה מתבטלת, כולל שורת ההיסטוריה.

שורת ההיסטוריה נכתבת **באותו `SaveChanges`** כמו העדכון, כלומר באותה טרנזקציה: או ששניהם נשמרים, או ששניהם לא.

ההחלטה מתקבלת בבסיס הנתונים, ולכן היא נכונה גם כשיש כמה שרתי API. בבדיקה אמיתית עם בקשות מקבילות, כל סבב הסתיים בתשובה `200` אחת ובתשובה `409` אחת, ובשורת היסטוריה אחת.

**ה-409 ב-UI:** כשמתקבל `409`, האפליקציה מציגה את ההודעה *"This request was changed by someone else. The latest version was loaded. Please try again."*. אחר כך היא טוענת מחדש רק את השורה הזו (`GET /api/requests/{id}`) ומחליפה אותה בטבלה. עכשיו יש למשתמש את הסטטוס העדכני ואת ה-`RowVersion` החדש, והוא יכול לנסות שוב. בזמן השמירה, עורך הסטטוס של השורה מושבת, כדי למנוע שליחה כפולה.

**לבדיקה ידנית בדפדפן (שתי לשוניות):** לפתוח `http://localhost:4200` בשתי לשוניות, להקליד שם בכל אחת, ולבחור באותה פנייה (בסטטוס `New` או `InProgress`). לשנות את הסטטוס בלשונית הראשונה. בלשונית השנייה, בלי לרענן, לבחור סטטוס לאותה שורה: תופיע הודעת ה-Conflict, והשורה תתעדכן לגרסה של הלשונית הראשונה.

---

## 8. Bulk Update

`POST /api/requests/bulk-status`:

```json
{
  "status": "InProgress",
  "changedBy": "Dana",
  "items": [ { "id": 1, "rowVersion": "AAAAAAAAB9E=" }, { "id": 2, "rowVersion": "AAAAAAAAB9I=" } ]
}
```

**Validation של הבקשה כולה** (`400`, ואף פריט לא מעובד): בין 1 ל-100 פריטים, בלי מזהים כפולים, סטטוס תקין, `changedBy` חובה (עד 100 תווים), וכל `rowVersion` תקין.

**עיבוד:**
- כל השורות נטענות בשאילתה אחת (`WHERE Id IN (...)`).
- כל פריט מעובד בנפרד, ב-`SaveChanges` משלו, כלומר בטרנזקציה משלו, עם בדיקת `rowVersion` משלו ושורת היסטוריה משלו.
- הבדיקה וההשמירה משתמשות באותו קוד כמו העדכון הבודד, בלי שכפול.
- אחרי Conflict, רק הפריט שנכשל מנותק מה-`DbContext`, כדי שלא יישמר שוב עם הפריט הבא.

**התשובה** היא תמיד `200` כשהבקשה עצמה תקינה, עם תוצאה לכל פריט, לפי סדר השליחה:

```json
{
  "requested": 3, "succeeded": 1, "failed": 2,
  "results": [
    { "id": 1, "outcome": "Updated", "rowVersion": "AAAAAAAJkf0=" },
    { "id": 999999, "outcome": "NotFound", "error": "Request 999999 was not found." },
    { "id": 10, "outcome": "InvalidTransition", "error": "Cannot change status from Completed to InProgress." }
  ]
}
```

התוצאות האפשריות: `Updated` (עם ה-`rowVersion` החדש), `NotFound`, `Conflict`, `InvalidTransition` (עם הסיבה). בלוג נכתבת שורת סיכום אחת לכל בקשה, ולא שורה לכל פריט.

**למה Partial Success:** ראו [סעיף 11](#11-החלטות-טכנולוגיות).

---

## 9. Cache ו-Invalidation

- **מה נשמר:** רק התוצאה של `GET /api/requests/stats`, ב-`IMemoryCache`, תחת מפתח אחד.
- **למה דווקא זה:** זו השאילתה היקרה ביותר (`GROUP BY` על כל הטבלה). היא גלובלית (בלי פילטרים), כך שמפתח אחד משרת את כל המשתמשים, והיא נקראת הרבה יותר משהנתונים משתנים. רשימות ופניות בודדות לא נשמרות ב-Cache, כי הן תלויות בפילטרים, וכי הלקוח חייב לקבל את ה-`rowVersion` העדכני.
- **Expiration:** מוחלט, 60 שניות.
- **Invalidation:** אחרי כל שינוי סטטוס **מוצלח**, בעדכון בודד ובכל פריט מוצלח ב-Bulk. Bulk שאף פריט בו לא הצליח לא מוחק את ה-Cache.
- **כמה שרתים:** `IMemoryCache` נמצא בזיכרון של תהליך אחד, ולכן Invalidation בשרת אחד לא מנקה את השרתים האחרים. פתרונות אפשריים: Cache משותף (Redis עם `IDistributedCache`), הודעת Invalidation לכל השרתים (למשל Redis Pub/Sub), או להסתמך רק על Expiration קצר.

הפרטים, כולל הסיכון הקטן שנשאר: [`docs/cache.md`](docs/cache.md).

---

## 10. בדיקות אוטומטיות

30 בדיקות ב-`backend/RequestsApi.Tests`: 12 Integration Tests עם `WebApplicationFactory<Program>` מול SQL Server LocalDB, ו-18 Unit Tests לכללי המעבר.

| נבדק | סוג |
|---|---|
| סינון משולב ודפדוף מחזירים בדיוק את השורות ואת ה-`totalCount` הצפויים | Integration |
| `pageSize=500` ו-`status=Done` מחזירים `400` עם שגיאה לשדה הנכון | Integration |
| שני `PATCH` מקבילים עם אותו `rowVersion`: `200` אחד, `409` אחד ושורת היסטוריה אחת | Integration |
| `PATCH` למזהה שלא קיים מחזיר `404` | Integration |
| מעבר לא חוקי (`Completed → New`) מחזיר `400`, והסטטוס, ה-`RowVersion` וה-`UpdatedAt` לא משתנים ב-DB, ואין שורת היסטוריה | Integration |
| ההיסטוריה אחרי עדכון מוצלח: רשומה אחת עם כל השדות; מזהה שלא קיים מחזיר `404` | Integration |
| Bulk מעורב: תוצאה נכונה לכל פריט, ורק הפריטים התקינים השתנו ב-DB | Integration |
| Bulk לא תקין (101 פריטים, מזהים כפולים) מחזיר `400` ולא משנה אף פנייה ולא מוסיף היסטוריה | Integration |
| ה-Stats מתעדכנים מיד אחרי `PATCH` מוצלח (Cache Invalidation) | Integration |
| כללי המעבר ו-`allowedNextStatuses` (`[Theory]`) | Unit |

**למה בסיס נתונים אמיתי ולא `EF Core InMemory`:** ה-InMemory Provider לא תומך ב-`rowversion` ובטרנזקציות, ולא מריץ SQL אמיתי. בדיקות ה-Concurrency, ה-Audit בטרנזקציה וה-SQL שנוצר (סינון, מיון, `GROUP BY`) לא היו בודקות את מה שבאמת רץ בייצור. הבדיקות יוצרות בסיס נתונים נפרד (`RequestsDb_Tests`) מה-Migrations, ומאפסות לפני כל בדיקה סט קטן וידוע של 12 פניות.

הפרטים: [`docs/tests.md`](docs/tests.md).

---

## 11. החלטות טכנולוגיות

### החלטה 1: Optimistic Concurrency עם `rowversion` של SQL Server

| חלופה | למה לא נבחרה |
|---|---|
| **עמודת גרסה ידנית** (`int Version` שהקוד מעלה בכל עדכון) | עובד, אבל מחייב לזכור להעלות את הגרסה בכל מקום שמעדכן את השורה, כולל סקריפטים ועדכונים ישירים ב-SQL. `rowversion` מתעדכן אוטומטית על ידי SQL Server בכל שינוי, כך שאי אפשר לשכוח אותו. EF Core תומך בו ישירות (`IsRowVersion()`). |
| **Pessimistic Locking** (נעילת השורה מרגע הקריאה ועד השמירה) | משתמש יכול לפתוח פנייה ולא לשמור אותה דקות ארוכות. כל הזמן הזה השורה נעולה, והמשתמשים האחרים ממתינים. ב-Web אין חיבור פתוח לאורך כל העריכה, ולכן צריך מנגנון נעילה וזמני תפוגה נוספים. התנגשויות אצלנו נדירות, ולכן עדיף לא לנעול, ולזהות התנגשות רק כשהיא קורית. |

**נבחר:** `rowversion` עם `WHERE RowVersion = @rv`. הבדיקה מתבצעת בבסיס הנתונים, באופן אטומי. לא נדרשת נעילה מתמשכת, וזה עובד גם עם כמה שרתי API.

### החלטה 2: Bulk Update עם Partial Success

| חלופה | למה לא נבחרה |
|---|---|
| **All-or-Nothing** (כל הפריטים בטרנזקציה אחת; אם פריט אחד נכשל, הכול מתבטל) | הפניות בבקשה לא תלויות זו בזו. פנייה אחת שמישהו אחר שינה לפני שנייה (`409`) הייתה חוסמת את 99 האחרות. המשתמש היה צריך למצוא את הפריט הבעייתי, להוציא אותו ולשלוח שוב. כשהרבה משתמשים עובדים במקביל, בקשה גדולה כמעט לא הייתה מצליחה. בנוסף, טרנזקציה אחת ארוכה על 100 שורות מחזיקה נעילות לאורך כל הזמן. |

**נבחר:** כל פריט בטרנזקציה משלו, עם תוצאה מפורשת לכל פריט. כל פנייה מקבלת בדיוק את אותו טיפול כמו עדכון בודד. הלקוח יודע בדיוק מה קרה: `Updated` קיבל `rowVersion` חדש, ב-`Conflict` צריך לטעון מחדש ולנסות שוב, ו-`NotFound` או `InvalidTransition` לא יצליחו גם בניסיון חוזר. המחיר: הבקשה עלולה להצליח חלקית, ולכן התשובה מפרטת כל פריט.

---

## 12. מגבלה ידועה ושיפור אפשרי

**מגבלה: אין Authentication.** השם שמוקלד בשדה `Your name` נשלח כ-`changedBy`, והשרת לא יכול לאמת אותו. בהיסטוריה מופיע השם שהמשתמש הצהיר עליו. עם Authentication, השרת היה לוקח את המשתמש מה-Token ולא מגוף הבקשה. הפרטים: [`docs/known-limitations.md`](docs/known-limitations.md).

**שיפורים אפשריים, מתוך ניתוח הביצועים** (לא מומשו):

| צוואר בקבוק | מדידה | שיפור מוצע |
|---|---|---|
| חיפוש `LIKE '%VPN%'` לא יכול להשתמש באינדקס וסורק את כל הטבלה | ה-`COUNT` של החיפוש: כ-470 ms CPU | **Full-Text Search** (`CONTAINS`) על `Title` ו-`OrganizationName` |
| `OFFSET` בדפים עמוקים מייצר וממיין את כל השורות הקודמות | דף 4000: מיון של 80,000 שורות, כ-200 ms | **Keyset Pagination** (`WHERE CreatedAt < @lastCreatedAt OR (CreatedAt = @lastCreatedAt AND Id < @lastId)`), שעולה בערך אותו דבר בכל דף |
| `COUNT` בכל בקשת דף | בחיפוש: הדף 3 ms, ה-`COUNT` 472 ms | ספירה רק בדף הראשון, או `hasMore` במקום סכום מדויק |

הניתוח המלא, כולל Parameter Sniffing בין דף 1 לדפים עמוקים: [`docs/performance.md`](docs/performance.md).

---

## 13. תכנון עבודה

חלוקת העבודה ל-Epics ולמשימות, עם תלויות והערכות זמן, נמצאת ב-[`docs/work-plan.md`](docs/work-plan.md).

---

## 14. שימוש בכלי AI

הקוד נכתב בעזרת Claude Code, משימה אחרי משימה, לפי הנחיות מפורטות שכתבתי לכל שלב. הנחיות קבועות לפרויקט נמצאות ב-`CLAUDE.md`, למשל: בלי CQRS או MediatR, סינון ומיון רק ב-SQL, ו-DTOs בלבד.

**מה נוצר בעזרת AI:** רוב הקוד (שלוש השכבות, ה-Migrations, סקריפטי ה-SQL, הבדיקות ואפליקציית Angular), סקריפטי המדידה, וטיוטות של מסמכי התיעוד.

**מה בדקתי, החלטתי ושיניתי בעצמי:**
- **ההחלטות:** אכיפת השכבות על ידי הקומפיילר, ושם נפרד לבסיס הנתונים כדי לא לפגוע בבסיס נתונים קיים. שינויי סכמה נעשו רק באישורי: שני השינויים שבאו מהמדידות (Rebuild לאינדקסים בסוף ה-Seed, והחלפת אינדקס ה-Priority) אושרו על ידי לפני הביצוע.
- **בדיקה בכל שלב:**
  - הרצה אמיתית ובדיקה של ה-SQL שנוצר.
  - בקשות מקבילות אמיתיות לבדיקת `409`.
  - מעבר על כל קודי הסטטוס.
  - בדיקה שבסיס הנתונים של הפיתוח לא משתנה בהרצת הבדיקות.
  - שבירה מכוונת של ה-Cache Invalidation, כדי לוודא שהבדיקה נכשלת.
  - בדיקה של ה-UI בדפדפן אמיתי.
- **בעיות שנמצאו בבדיקות ותוקנו:**
  - גבול `createdTo` שלא כלל את היום האחרון.
  - `ChangeTracker.Clear()` אחרי Conflict, שהיה מנתק גם את שאר הפריטים ב-Bulk.
  - אינדקסים שנשארו מפוצלים (Fragmented) אחרי ה-Seed.
- **בעיה שנמצאה ותועדה בלי תיקון:** Parameter Sniffing בין דף 1 לדפים עמוקים (פרטים ב-`docs/performance.md`).

הפירוט לפי משימה: [`docs/ai-usage.md`](docs/ai-usage.md).
