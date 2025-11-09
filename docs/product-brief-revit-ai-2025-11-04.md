# Product Brief: revit-ai

**Date:** 2025-11-04
**Author:** Doc
**Context:** Technical Validation PoC

---

## Executive Summary

Revit-ai is an AI co-pilot that automates repetitive documentation tasks in Revit (dimensioning, tagging, schedules) using prompt-based LLM intelligence. This PoC validates technical feasibility with minimal effort by getting one feature working on real project data to assess whether a full-scale product is viable with current technology.

---

## Problem Statement

Architectural firms accumulate "technical debt" of tedious documentation tasks during DD/CD (Design Development / Construction Documentation) phases - work that consumes days to weeks per project. Tasks like dimensioning, tagging, and schedule generation are time-consuming when done manually, create quality inconsistencies as team members change, and pull architects away from creative work.

The deeper issue: architects don't believe prompt-based AI automation can handle these tasks professionally and accurately. They're resigned to manual processes because they haven't seen intelligent (vs. rule-based) automation work in practice.

**Impact:**
- **Time**: 1 day (small projects) to 1 week (large projects) on repetitive tasks
- **Quality**: Variable results depending on who does the work
- **Opportunity**: AI could handle abstract, contextual decisions beyond simple scripts

---

## Solution

A **prompt-based AI co-pilot** integrated into Revit that uses modern LLMs to understand context, interpret standards, and make intelligent decisions - not just follow rigid rules.

**Three PoC Features (easiest first):**
1. **Smart Schedule Generation** - Read model data, apply LLM intelligence to structure/format schedules
2. **Context-Aware Tagging** - Position tags understanding spatial context and standards
3. **Intelligent Dimensioning** - Place dimensions with geometric analysis and "floating" dimension detection

**Key Differentiator:** Intelligence over rules - AI makes contextual decisions like an experienced architect would, creating the "aha moment" when architects see what's actually possible.

**Technical Approach:** Leverage existing tools (Revit API/SDK, LLM APIs, MCPs) with simplest integration path. Standards/customization not a concern for PoC - use whatever's easiest.

---

## Target Users

**Small architectural firms (10-50 people)** with active DD/CD projects. Initial validation with a 10-person firm.

**Key personas:**
- Production architects doing the grunt work
- BIM managers evaluating efficiency tools
- Project architects managing milestones

**Profile:** Users seeking customized Revit features through prompts rather than low-level coding - an alternative to writing custom plugins or scripts.

---

## MVP Scope

**PoC Goal:** Technical validation to understand domain complexity with minimal effort. Create real (non-mock) implementations to reveal actual challenges.

**Guiding Principles:**
- Simplest implementation that actually works
- Real Revit models only - no fake data
- Prioritize learning over polish
- Easiest feature first

**Success Criteria:**
One or more features works on 1 real project, demonstrating:
- LLM intelligence adds value beyond rule-based scripts
- We identify real technical challenges
- We can assess full-scale feasibility

**Failure = cannot get any feature working on real data**

---

## Technical Approach

**Stack:**
- Revit API/SDK (C# .NET)
- LLM APIs (Claude or GPT-4)
- Simplest architecture: script → Revit API + LLM endpoints

**Key Questions:**
1. What data can we extract/modify via Revit API?
2. How much domain knowledge must be encoded vs. LLM inference?
3. Can we achieve professional-quality accuracy?

**Risks:**
- Revit API limitations
- LLM accuracy without domain training
- Integration complexity
- Firm standards too nuanced to capture

**Mitigation:** Start with schedule generation (simplest) to validate before attempting harder features.

---

## Success Metrics & EBD Integration

**Technical Validation Metrics:**
- API capability (can we extract/modify needed data?)
- LLM effectiveness (adds value vs. simple scripts?)
- Accuracy on real data (meets professional standards?)
- Development velocity (< 2 weeks for first feature)

**Evaluation-Based Development:**
- Daily progress evaluation
- Real data testing required
- Concrete findings each iteration
- Decision point after first feature: continue, refine, or pivot

---

## Architect Success Criteria

**[TO BE DEFINED WITH CLIENT ARCHITECT]**

This section captures quality standards in the architect's language to ensure delivered features meet professional expectations.

### Smart Schedule Generation
**What makes a schedule "professionally done"?**
- [ ] *To be defined: completeness criteria*
- [ ] *To be defined: formatting standards*
- [ ] *To be defined: accuracy requirements*
- [ ] *To be defined: what would make this "good enough to use"?*

### Context-Aware Tagging
**What makes tag placement "correct"?**
- [ ] *To be defined: positioning rules (spacing, alignment, readability)*
- [ ] *To be defined: what makes it "wrong" or "unprofessional"?*
- [ ] *To be defined: office-specific conventions that must be followed*
- [ ] *To be defined: acceptable vs. needs-rework threshold*

### Intelligent Dimensioning
**What makes dimensioning "professional quality"?**
- [ ] *To be defined: spacing and alignment standards*
- [ ] *To be defined: completeness (what counts as "floating"/missing)*
- [ ] *To be defined: readability and clarity requirements*
- [ ] *To be defined: when is it "good enough" vs. needs manual fix?*

**Quality Gates:**
- [ ] *To be defined: "This is production-ready" criteria*
- [ ] *To be defined: "This needs work but shows promise" criteria*
- [ ] *To be defined: "This doesn't meet minimum standards" criteria*

**Client Validation Process:**
During the client meeting, populate these criteria WITH the architect using their professional judgment and office standards as the reference.

---

## Timeline

**2-4 weeks:**
- Week 1: Revit API exploration, evaluation checkpoint
- Week 2: LLM integration, first prototype, evaluation checkpoint
- Week 3-4: Real model testing, findings documentation

**Decision Point:** After first feature, evaluate whether to continue based on technical feasibility.

---

_Next: PRD workflow will create detailed requirements and implementation plan._
