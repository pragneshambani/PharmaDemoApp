---
name: pharmacy-medicine-tracker
description: Use this skill whenever building, modifying, or reviewing any part of the ABC Pharmacy Medicine Tracker application — a .NET Core Web API + JavaScript SPA (ASP.NET MVC host) that manages medicine inventory and sale records. Trigger on mentions of "medicine list", "medicine grid", "pharmacy app", "IRepository", "medicine API", "sale records", or any file under /Api, /Web, or /Data related to this project. Encodes the fixed business rules, data contract, architecture pattern, and UI conventions so every agent/command produces consistent, compatible output.
---

# ABC Pharmacy — Medicine Tracker Skill

## Purpose
This skill is the single source of truth for domain rules and architectural conventions
for the ABC Pharmacy application. Any agent or command touching this codebase must read
this file first and never contradict it.

## Data Contract — Medicine
| Attribute  | Type                  | Notes                                      |
|------------|-----------------------|---------------------------------------------|
| Id         | Guid / int            | Server-generated, not user-editable         |
| FullName   | string                | Required, shown in grid                     |
| Notes      | string                | Free text — **excluded from the grid view** |
| ExpiryDate | DateTime (date only)  | Required, shown in grid                     |
| Quantity   | int                   | Required, >= 0, shown in grid                |
| Price      | decimal(18,2)         | Required, >= 0, always render 2 decimals    |
| Brand      | string                | Required, shown in grid                     |

Sale record (separate entity, referencing Medicine by Id):
| Attribute   | Type     | Notes                                  |
|-------------|----------|-----------------------------------------|
| Id          | Guid/int | Server-generated                        |
| MedicineId  | Guid/int | FK to Medicine                          |
| QuantitySold| int      | Required, > 0, must not exceed stock    |
| SaleDate    | DateTime | Defaults to now, server-set             |
| UnitPrice   | decimal  | Snapshot of Medicine.Price at sale time |
| TotalAmount | decimal  | QuantitySold * UnitPrice                |

Every sale **must decrement** the corresponding Medicine.Quantity via the repository —
never mutate stock directly from the controller.

## Fixed Business Rules (never negotiable)
1. **Expiry highlight**: grid row background = RED when `ExpiryDate < Today + 30 days`.
2. **Low stock highlight**: grid row background = YELLOW when `Quantity < 10`.
3. If a row qualifies for both, expiry (RED) takes visual priority; still surface a
   secondary low-stock indicator (icon/badge) so the signal isn't lost.
4. Notes is **never** shown in the grid — only in the add/edit form and a detail view.
5. Price is always formatted with exactly 2 decimal places, both in API responses
   (as decimal, not float) and in the UI.
6. A sale can never take Quantity below 0 — validate server-side (400 Bad Request),
   don't rely on client validation alone.

## Architecture (mandatory)
```
/src
  /Api                      → ASP.NET Core Web API (.NET 8)
    /Controllers            → MedicinesController, SalesController — thin, call services/repos
    /Repositories
      IMedicineRepository.cs   → interface, storage-agnostic
      JsonMedicineRepository.cs→ reads/writes /App_Data/medicines.json
      SqlMedicineRepository.cs → EF Core-backed, added later without touching controllers
      ISaleRepository.cs / JsonSaleRepository.cs / SqlSaleRepository.cs (mirror pattern)
    /Models                 → Medicine, Sale (POCOs / DTOs kept separate from persistence models)
    /Services                (optional) → business logic (stock decrement, validation)
    Program.cs               → DI registration, Swagger, CORS
  /Web                      → ASP.NET MVC shell hosting the SPA
    /wwwroot/js              → SPA (vanilla JS, or lightweight framework — see below)
    /wwwroot/css/site.css    → single centrally-shared stylesheet (see UI Conventions)
    /Views/Shared/_Layout.cshtml → top nav menu, shared search box, includes site.css/site.js
  /App_Data/medicines.json  → JSON file store (created if missing, git-ignored after seed)
```

### Repository pattern — hard requirement
- `IMedicineRepository` and `ISaleRepository` expose storage-agnostic async CRUD +
  query methods (`GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`,
  `SearchAsync(term)`).
- Controllers depend **only** on the interfaces (constructor injection).
- Swap implementation via DI registration in `Program.cs`
  (`builder.Services.AddScoped<IMedicineRepository, JsonMedicineRepository>();`) —
  swapping to `SqlMedicineRepository` must require **zero controller changes**.
- JSON repository reads the whole file, mutates in-memory, writes back atomically
  (write to temp file, then move) to avoid corruption under concurrent writes; guard
  with a `SemaphoreSlim` per repository instance.

## API Conventions
- RESTful routes: `GET/POST /api/medicines`, `GET/PUT/DELETE /api/medicines/{id}`,
  `GET /api/medicines/search?term=&sortBy=&sortDir=&page=&pageSize=`,
  `GET/POST /api/sales`, `GET /api/sales/{id}`.
- Swagger/OpenAPI (Swashbuckle) enabled for **every** controller, with XML doc comments
  surfaced (`GenerateDocumentationFile` in csproj) so parameters/responses are documented.
- Standard response envelope for paged results:
  ```json
  { "items": [...], "totalCount": 0, "page": 1, "pageSize": 20 }
  ```
- Validation errors return 400 with `ProblemDetails`; not-found returns 404.

## UI Conventions (SPA)
- CSS lives in **one** file, `wwwroot/css/site.css`, referenced only from `_Layout.cshtml`
  so every page (Medicine List, Add/Edit, Sales) inherits it — this is what "centrally
  accessible CSS" means; no per-page inline `<style>` blocks.
- Shared top search bar lives in the layout header, searches across all medicine text
  fields (FullName, Brand, and optionally Notes) via the `/search` API endpoint.
- Medicine List page adds its own **field-level** filter row (one input per column)
  in addition to the shared top search.
- **Universal Column Sorting & Real-Time Updates**:
  - **All list views** across all pages (Medicine Inventory grid AND Sales Records history list) must support interactive column sorting (clicking table headers toggles asc/desc via `sortBy` and `isAscending` parameters).
  - Any data modification action (adding a medicine, recording a sale) must automatically update and refresh all affected list views across all pages without requiring a full manual browser reload.
- Grid supports column sort (click header, toggles asc/desc) and server-side paging
  (page + pageSize params); avoid loading the entire dataset client-side once it grows.
- Row coloring is applied via CSS classes (`.row-expiring`, `.row-low-stock`) toggled
  by JS based on the rules above — never inline hex colors scattered in JS.
- Top nav menu (in `_Layout.cshtml`) links: **Medicines**, **Add Medicine**, **Sales**.
- JS framework choice is open (vanilla JS + fetch, or a light framework); keep it
  consistent across the whole SPA — don't mix approaches per page.

## Non-negotiables checklist for any change
- [ ] Does it go through `IMedicineRepository`/`ISaleRepository`, not a concrete class?
- [ ] Does the grid still exclude Notes?
- [ ] Are red/yellow rules still evaluated server-agnostic (computed client-side or
      returned as flags from API — pick one and stay consistent)?
- [ ] Is Price still serialized/displayed with 2 decimals?
- [ ] Is Swagger still generating for the touched controller?
- [ ] Is CSS still only in `site.css`?
