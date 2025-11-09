# Revit + AI Agent MVP — Summary and Action Plan

## TL;DR
Build a **pyRevit-based in-app copilot** that converts natural-language requests into **approved Revit actions**. Use a **local AI service** to parse intent, **preview changes**, then **commit via Transactions** after user confirmation. Add a **cloud batch lane** later (APS Design Automation) and a **viewer/data lane** if needed (APS/Speckle). **Revit LT is out.**

---

## Problem Statement
Architects waste time on repetitive edits, standards checks, view/sheet management, and data handoffs. We want an **AI assistant inside Revit** that accelerates routine work while keeping humans in control.

## MVP Goals
- Speed up **repetitive edits** and **batch operations**.
- Keep users in control with **previews and confirm-before-commit**.
- Start **in-process** (desktop) with minimal UI.
- Respect **Revit API constraints**: single-threaded access and Transactions.

## Target Users
- Small–mid architecture offices using **full Revit**.
- Power users and BIM managers who can curate “safe tools”.

---

## Architecture (MVP)
1. **Execution Layer**: pyRevit toolset exposing **safe, testable functions** (e.g., create views/sheets, rename/renumber, place tags, set parameters).
2. **Agent Layer**: Local service (HTTP/gRPC). Transforms NL → **intent JSON** → queues into Revit via **ExternalEvent**.
3. **Revit Bridge**: Modeless pyRevit panel that:
   - Collects user prompt.
   - Shows **intent preview** (diff list, element highlights).
   - Applies changes in a **Transaction** after confirmation.
4. **Context/Memory**: Project-scoped store (JSON/SQLite) for:
   - Recent prompts, decisions, naming conventions, templates.
   - Allowed tools and **permission policy** per project.
5. **Telemetry & Undo**:
   - Log intents, results, and failures. 
   - Every change is transactional → **native Undo** works.

> YAGNI: No multi-agent choreography. No cloud dependency on day‑1. Keep the service local. One ribbon panel, one prompt box, one preview list.

---

## User Experience
- **Panel** in Revit with: Prompt input, “Preview,” “Apply,” and “Undo last AI change.”
- AI returns a **plain-language plan** and a **machine diff**:
  - Example: “Create 12 dependent views with template *A-Plan-1*, place on sheets S101–S106, update title block date.”
- User clicks **Apply** → one Transaction commits. Failures are surfaced with trace.

---

## Permissions and Safety
- **Allowlist of tools** only. Each tool has:
  - Signature, expected preconditions, side effects.
  - Max scope guardrails (e.g., “never edit > N elements” without explicit user consent).
- **Escalation prompts** for sensitive ops (delete, purge, renumber).
- **Dry-run mode** for preview diff without writes.

---

## Configuration
- Add-in settings page:
  - **AI provider + API key**.
  - Project policy: allowed tools, element scope limits, default templates.
  - Path to **project context file** (JSON/SQLite).

---

## Roadmap
### Phase 0 — Spike (1–2 days)
- pyRevit button + “Hello, Transaction.”
- ExternalEvent queue + minimal intent schema.
- One safe tool: **Batch rename views** with preview and confirm.

### Phase 1 — MVP (1–2 weeks)
- **Core tools (v0.1)**:
  - Create/duplicate views, apply view templates.
  - Batch sheet creation + view placement.
  - Parameter set/propagate on filtered elements.
- **UX**: Prompt → Preview list → Apply.
- **Context**: Project JSON with naming rules and recent actions.
- **Policy**: Allowlist + scope guardrails.
- **Logging**: Per‑project log file with intent, diff, duration, result.

### Phase 2 — Expansion (2–4 weeks)
- **Dynamo bridge**: run prebuilt graphs with parameters.
- **Model selection helpers**: element highlights, saved filters.
- **Basic analytics**: success rate, time saved estimations.
- **Data lane (optional)**: Speckle or APS Model Derivative for web review.

### Phase 3 — Batch/Cloud (optional)
- **APS Design Automation for Revit** workitems for heavy jobs.
- Webhook callback → attach outputs back to model or publish to Docs.

---

## Candidate “Safe Tools” (v0.1)
- View ops: duplicate with detailing, apply template, rename by rule.
- Sheet ops: create from template, place views by rule, renumber.
- Annotation ops: auto-tag in view by category with filters.
- Parameters: batch set on selection/filter; never across entire model by default.
- QA checks: find missing parameters, view template drift, sheet title mismatches.

---

## Risks and Mitigations
- **Overreach edits** → Guard with scope limits and previews.
- **API threading pitfalls** → All API calls via ExternalEvent on Revit thread.
- **Ambiguous prompts** → Force clarification questions or dry-run preview.
- **User trust** → Clear diffs, fast Undo, detailed logs.

---

## Acceptance Criteria (MVP)
- Can **rename 50+ views** with preview and single-click apply in < 10s on a mid model.
- **Undo** reverts entire AI change set.
- **No crashes** when prompts are empty, ambiguous, or partially failing.
- All write ops occur within a **single Transaction** per action.

---

## Next Steps (Actionable)
- Pick provider and create **local intent service** with 3 intents: `rename_views`, `create_sheets`, `set_param`.
- Ship **pyRevit panel** with Prompt, Preview, Apply, Undo.
- Implement **allowlist + scope limits** and **dry-run**.
- Add **project.json** context and minimal logs.
- Pilot with one architect for 3 days. Collect prompts. Expand toolset based on top-10 asks.

---

*“No job is too big, no pup is too small.” — Ryder*
