---
description: Scaffold or extend the ABC Pharmacy Medicine Tracker (.NET Core Web API + ASP.NET MVC SPA host) end-to-end — API with swappable repositories, JSON storage, Swagger, and the medicine/sales SPA UI.
argument-hint: [feature-or-area] (optional — e.g. "sales module", "search paging", "swagger")
---

# /scaffold-pharmacy-app

Build or extend the ABC Pharmacy Medicine Tracker application.

Target area for this run: $ARGUMENTS (if empty, run the full end-to-end scaffold).

## Steps

1. **Load context**
   - Read `.claude/skills/pharmacy-medicine-tracker/SKILL.md` in full before writing
     any code. Treat its data contract, business rules, and architecture as fixed
     constraints — do not deviate without flagging it explicitly to the user.
   - Check for an existing `/src` tree; if present, treat this as an incremental
     change, not a rewrite — read existing files before editing.

2. **Scaffold the solution (first run only)**
   - `dotnet new webapi -o src/Api` for the API project.
   - `dotnet new mvc -o src/Web` for the MVC host that serves the SPA static assets
     (or a single combined `webapi` project serving `wwwroot` if the user prefers
     one project — confirm with the user if ambiguous, otherwise default to two
     projects for a clean API/UI separation).
   - `dotnet new sln` at the root and add both projects.
   - Add Swashbuckle.AspNetCore to the API project for Swagger.

3. **Delegate work to subagents** (see `.claude/agents/`):
   - Use the **api-repository-agent** for anything under `/src/Api` — models,
     `IMedicineRepository`/`ISaleRepository`, JSON-backed implementations, controllers,
     Swagger setup, and validation.
   - Use the **frontend-spa-agent** for anything under `/src/Web/wwwroot` and
     `_Layout.cshtml` — grid, search/sort/paging, add/update/delete forms, nav menu,
     shared `site.css`.
   - Use the **qa-swagger-agent** after API or UI changes to verify: Swagger renders
     for every controller, the repository swap works with zero controller changes,
     and the red/yellow highlight + paging/sort/search rules behave as specified.

4. **Seed data**
   - On first run, create `src/Api/App_Data/medicines.json` with 15–20 realistic
     seed medicines, including at least a few that intentionally trigger the RED
     (expiry < 30 days) and YELLOW (quantity < 10) rules, so the UI is verifiable
     immediately without manual data entry.

5. **Verify before finishing**
   - Run `dotnet build` across the solution; fix any errors before handing back.
   - Confirm `dotnet run --project src/Api` serves Swagger at `/swagger`.
   - Confirm the MVC/SPA host loads the medicine grid and the nav menu links to
     Medicines / Add Medicine / Sales.
   - Report to the user: what was scaffolded/changed, how to run it (`dotnet run`
     commands per project), and any deviations from the skill file with rationale.

6. **Stop and ask** if the request would contradict a fixed rule in the skill file
   (e.g., "put Notes in the grid", "hardcode SQL only") — surface the conflict rather
   than silently overriding it.
