# Revit Ecosystem Research Specification

**Date:** 2025-11-19
**Purpose:** Research existing Revit automation solutions to inform RevitAI architecture decisions
**Context:** Building MCP server tools for LLM-driven Revit automation - need to understand what primitives already exist and how testing is handled

---

## Research Tasks Overview

| Task | Focus Area | Priority | Estimated Time |
|------|-----------|----------|----------------|
| 1 | Revit MCP Server Landscape | HIGH | 2-3 hours |
| 2 | PyRevit + LLM Integration | MEDIUM | 1-2 hours |
| 3 | Revit API Testing Strategies | HIGH | 2-3 hours |

---

## Task 1: Revit MCP Server Landscape Analysis

### Research Prompt

```
Search for "Revit MCP server" and "Model Context Protocol Revit" on GitHub, GitLab, and other code repositories.

For each project you find, collect the following information:

1. **Project Identity:**
   - Repository URL
   - Project name
   - Author/organization
   - License type
   - Last commit date
   - Star/fork count

2. **Functionality:**
   - What operations/tools does it expose? (list all function names with brief descriptions)
   - What use cases does it target? (dimension automation, model querying, family management, etc.)
   - Does it provide read-only access or can it modify the model?
   - Any AI/LLM-specific features mentioned?

3. **Status Assessment:**
   - Is it functional or abandoned?
   - Check recent issues and pull requests
   - Is there active community engagement?
   - Any known bugs or limitations documented?

4. **Technical Architecture:**
   - Technology stack (Python, C#, JavaScript, TypeScript, etc.)
   - How does it interface with Revit?
     - REST API
     - IPC (Inter-Process Communication)
     - Direct .NET integration
     - pyRevit bridge
     - Other method?
   - Does it use Revit API directly or through abstraction layer?
   - Any dependencies on specific Revit versions?

5. **Design Decisions:**
   - Any documentation explaining their architectural choices?
   - How do they handle Revit's threading requirements?
   - Do they provide async operations?
   - Error handling strategies mentioned?

Also search for:
- "Autodesk Revit API MCP"
- "Revit Model Context Protocol"
- "MCP Revit integration"
- Check the official MCP server registry if it exists
```

### Required Output Format

**Summary Section:**
```markdown
## Executive Summary
- Total MCP servers found: [N]
- Functional/Active: [N]
- Abandoned/Incomplete: [N]
- Most common tech stack: [Language/Framework]
- Most common integration method: [Method]
- Key patterns observed: [List]
```

**Detailed Findings Table:**
```markdown
| Project Name | URL | Status | Last Update | Stars | Key Operations | Tech Stack | Integration Method | Notes |
|--------------|-----|--------|-------------|-------|----------------|------------|-------------------|-------|
| Example MCP | github.com/... | Active | 2025-11 | 45 | GetElements, CreateWall, QueryRooms | Python | pyRevit bridge | Well documented |
```

**Operations Inventory:**
Create a consolidated list of ALL unique operations found across projects:
```markdown
### Operation Categories Found

**Model Querying:**
- GetElements(category, filters)
- QueryRoomsByLevel(levelId)
- [etc.]

**Creation Operations:**
- CreateWall(points, height, type)
- CreateDimension(references, offset)
- [etc.]

**Modification Operations:**
- UpdateParameter(elementId, paramName, value)
- [etc.]
```

**Key Findings Section:**
```markdown
## Key Architectural Patterns

1. **Pattern Name:** [e.g., "Event-driven commands"]
   - **Description:** How it works
   - **Pros:** Benefits observed
   - **Cons:** Limitations noted
   - **Used by:** Which projects use this

2. [Repeat for each pattern]

## Gaps Identified
- Operations NOT found but potentially useful: [List]
- Common limitations: [List]
- Unsolved challenges mentioned: [List]
```

---

## Task 2: PyRevit + LLM Integration Analysis

### Research Prompt

```
Search for evidence of PyRevit being integrated with Large Language Models (Claude, GPT, Gemini, etc.).

Search these specific queries:
1. GitHub: "pyRevit" AND ("Claude" OR "GPT" OR "LLM" OR "AI" OR "ChatGPT" OR "OpenAI")
2. GitHub: "Revit" AND "LLM" AND "automation"
3. Reddit r/Revit: Posts about AI or ChatGPT
4. Autodesk forums: Discussions about AI-assisted workflows
5. YouTube: "Revit AI automation" or "Revit ChatGPT"
6. Product Hunt / similar: Commercial products claiming AI + Revit
7. Academic papers: "Building Information Modeling" AND "Large Language Models"

For each finding, document:

**If Integration Attempts Found:**
- What was attempted? (describe the goal)
- What approach was used? (architecture/method)
- Did it work? What were the results?
- What challenges or limitations were reported?
- Is the code available? (link)
- Is it still maintained?

**If Commercial Products Found:**
- Product name and vendor
- Marketing claims vs actual capabilities
- Pricing/availability
- Technology approach (if disclosed)
- User reviews or feedback available?

**If NO Evidence Found:**
- Explicitly state: "Searched [X sources] with [Y queries], found no evidence of PyRevit+LLM integration"
- Note any RELATED findings (e.g., AI for BIM in general, but not Revit-specific)
```

### Required Output Format

```markdown
## Search Results Summary

**Total Findings:** [N]
- GitHub repositories: [N]
- Forum discussions: [N]
- Commercial products: [N]
- Academic papers: [N]
- Other: [N]

**Verdict:** [Successful integrations exist / Only attempts found / No evidence found]

## Detailed Findings

### 1. [Finding Name/Title]
- **Source:** [URL]
- **Type:** [GitHub repo / Forum post / Product / Paper]
- **Date:** [When created/posted]
- **Status:** [Active / Abandoned / Commercial]

**Description:**
[What they tried to accomplish]

**Approach:**
[Technical method used]

**Results:**
- ✅ What worked:
- ❌ What didn't work:
- ⚠️ Limitations:

**Relevance to RevitAI:**
[How this finding informs our approach]

---

[Repeat for each finding]

## Gap Analysis

**What Nobody Has Built Yet:**
1. [Specific capability or approach]
2. [etc.]

**Why These Gaps Might Exist:**
- Technical barriers: [List]
- Market reasons: [List]
- Timing (too early): [List]

## Recommendations

Based on findings:
1. [Actionable recommendation]
2. [etc.]
```

---

## Task 3: Revit API Testing Strategies Research

### Research Prompt

```
Research how developers test Revit add-ins and extensions without requiring full Revit launches for every test.

Search for:
1. "Revit API testing" + "unit test"
2. "Revit API mock" OR "Revit API stub"
3. "pyRevit testing" OR "pyRevit unit tests"
4. "Revit API CI/CD" OR "Revit API continuous integration"
5. "Revit plugin development best practices"
6. Check StackOverflow tags: [revit-api] + [testing]
7. Autodesk Developer Network: Testing documentation
8. GitHub: Search for test files in popular Revit API projects

For each approach found, document:

**Testing Strategy Details:**
- Strategy name (e.g., "Mock API", "Integration tests only", etc.)
- What's being tested (unit level, integration, end-to-end)
- How it works (technical approach)
- Tools/frameworks used
- Setup complexity (easy, moderate, hard)
- Pros and cons
- Who uses it (which projects/developers)

**Mock/Stub Libraries:**
- Library name and URL
- What Revit API surfaces it mocks
- Language/framework (C#, Python, etc.)
- Maturity level (active, stable, abandoned)
- Coverage (what % of Revit API is mocked?)
- Usage examples available?

**CI/CD Approaches:**
- How do teams automate testing?
- Can tests run without Revit installed?
- How long do test suites take to run?
- Any cloud-based solutions?

**Industry Practices:**
- What do successful Revit plugin companies do?
- Any public blog posts or talks about their testing approach?
- Common pain points mentioned?
```

### Required Output Format

```markdown
## Testing Landscape Overview

**Key Finding:** [One-sentence summary of what's possible/not possible]

**Approaches Found:** [N]
- Viable for our use case: [N]
- Not applicable: [N]

## Testing Strategies Comparison

| Strategy | Description | Pros | Cons | Revit Required? | Speed | Recommended For |
|----------|-------------|------|------|-----------------|-------|-----------------|
| Pure Unit (Mocked) | Mock all Revit API | Fast, no Revit | May not catch real bugs | No | Instant | Algorithm logic |
| Integration | Real Revit API | Realistic | Slow, complex | Yes | Minutes | Critical paths |
| [etc.] | ... | ... | ... | ... | ... | ... |

## Mock Libraries Found

### Library Name: [e.g., RevitTestFramework]
- **URL:** [link]
- **Language:** C# / Python
- **Status:** Active / Maintained / Abandoned
- **Last Update:** [date]
- **Coverage:** [What it mocks]
- **API Completeness:** [Partial / Comprehensive]
- **Setup Difficulty:** ⭐⭐⭐☆☆ (3/5)
- **Usage Example:**
  ```python
  # Code example if available
  ```
- **Pros:**
  - [List]
- **Cons:**
  - [List]

---

[Repeat for each library]

## CI/CD Approaches

### Approach 1: [Name]
**Used by:** [Projects/companies]
**Method:** [Description]
**GitHub Actions / Azure DevOps / Jenkins:** [Which CI platforms]
**Test Execution Time:** [X minutes for Y tests]
**Limitations:** [List]

## Real-World Examples

Find 3-5 mature Revit API projects on GitHub and analyze their test strategy:

### Project: [Name]
- **URL:** [link]
- **Stars:** [N]
- **Test Files:** [Link to test directory]
- **Testing Approach:** [What they do]
- **Test Coverage:** [If measurable]
- **Key Takeaways:** [What we can learn]

## Recommendations for RevitAI

Based on research:

**Recommended Approach:**
[Which testing strategy suits our "building MCP tools" use case]

**Reason:**
[Why this approach fits our constraints]

**Implementation Steps:**
1. [Actionable step]
2. [etc.]

**What to Avoid:**
- [Anti-patterns identified]
- [Overly complex approaches]

## Open Questions

After research, what questions remain:
1. [Question that couldn't be answered]
2. [etc.]
```

---

## Consolidated Report Template

After completing all three tasks, create a synthesis document:

```markdown
# Revit Ecosystem Research - Consolidated Findings

**Date:** [Date]
**Researchers:** [Names]
**Review Status:** [Draft / Final]

## Executive Summary

[2-3 paragraphs synthesizing ALL findings across the three tasks]

**Key Insight 1:** [Most important finding]
**Key Insight 2:** [Second most important]
**Key Insight 3:** [Third most important]

## Impact on RevitAI Architecture

### What We Should Do
1. [Specific architectural decision based on findings]
   - **Evidence:** [Which research supports this]
   - **Impact:** [How it affects our roadmap]

2. [etc.]

### What We Should Avoid
1. [Specific anti-pattern or approach to skip]
   - **Reason:** [Why, based on research]

### Gaps We Can Fill
[Opportunities where existing solutions are lacking]

## Next Steps

**Immediate (This Week):**
1. [Action item]
2. [Action item]

**Short-term (This Month):**
1. [Action item]
2. [Action item]

**Questions for Perry (Our Architect User):**
1. [Question based on findings]
2. [etc.]

## Appendices

- Appendix A: Full MCP Server Inventory
- Appendix B: Complete Testing Strategies Matrix
- Appendix C: All PyRevit+LLM Search Queries Used
```

---

## Execution Instructions

1. **Researchers:** Use Gemini, Perplexity, or Claude with extended context
2. **Time Estimate:** 5-8 hours total across all tasks
3. **Deliverables:**
   - Three task-specific reports (using formats above)
   - One consolidated synthesis report
4. **Review:** Technical review by development team before making architectural decisions

## Quality Checklist

- [ ] All tables are complete (no TBD or empty cells without explanation)
- [ ] URLs are verified and accessible
- [ ] "No findings" is explicitly stated where applicable (not left ambiguous)
- [ ] Recommendations are specific and actionable
- [ ] Sources are cited for all claims
- [ ] Technical terms are defined on first use
- [ ] Findings are dated (research can become stale)

---

**Notes for Researchers:**

- Be thorough but honest - "I found nothing" is a valid result
- Prioritize recent (2023+) findings over old ones
- When in doubt, include more detail rather than less
- If a source is behind a paywall, note that but describe what's visible
- Cross-reference findings between tasks when relevant
