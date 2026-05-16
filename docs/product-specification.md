# Whidy — Product Specification (MVP)

## Overview

Whidy is a lightweight CLI tool that reconstructs a developer’s workday from commits and pull requests.  
It answers the question: **What did I do yesterday?** in a human-readable, standup-style format.

The goal is not tracking, but understanding work through automatic interpretation of development activity.

---

## Core Principles

- No database
- No background service
- Stateless execution
- Minimal configuration
- Works from commits and pull requests
- Insight over raw data
- Human-readable output

---

## Primary Use Case

Developers run Whidy at any time to see a structured summary of their previous day’s work:

- What they focused on
- What repositories they worked in
- What kind of work they did (coding, review, debugging)
- High-level insights about their activity

---

## Command Interface

### yesterday

```
whidy yesterday
```

Generates a structured report of the previous calendar day.

---

## System Architecture

CLI Application

→ Configuration Loader  
→ Event Fetcher (commits, pull requests)  
→ Event Normalizer  
→ Insight Engine  
→ Console Renderer  

---

## Configuration

Stored locally in a JSON file:

```json
{
  "organization": "optional-or-inferred-context",
  "pat": "personal-access-token"
}
```

No expiration tracking. No authentication pre-validation.

---

## Authentication Model

- PAT is used directly in API calls
- If request fails:
  - 401 → prompt for new PAT
  - 403 → prompt for PAT with correct permissions
- Config is updated immediately after successful retry

---

## Data Model (in-memory only)

### Event

- timestamp
- type (commit, pull request)
- repository
- title/message

---

### Episode

A grouped set of related events representing a coherent unit of work.

- repo
- events
- type (coding, review, debugging)
- label (human-readable summary)

---

### Insight

High-level interpretation of episodes.

- title
- summary
- bullet points

---

## Insight Engine

Transforms raw events into meaningful work summaries.

### Pipeline

Events  
→ Group into episodes  
→ Classify episode type  
→ Generate labels  
→ Extract insights  
→ Render report  

---

### Episode Grouping Rules

- Same repository
- Time gap less than 60–90 minutes

---

### Classification Heuristics

- Commit-heavy → coding
- Pull request-heavy → review
- Failure signals → debugging

---

### Insight Rules

- Primary repository → focus detection
- Number of episodes → context switching indicator
- Event density → intensity indicator
- Failure signals → debugging-heavy day

---

## Output Format

### Yesterday Report

- Repository-based grouping of work
- Short bullet points per activity
- Human-readable summaries

### Example

YESTERDAY

Authentication Service  
You spent most of your time here.

- Fixed login token refresh issue  
- Reviewed authentication flow improvements  

Web App  
- UI adjustments in dashboard layout  

Insights  
- Focused primarily on authentication work  
- Mostly stabilization and bug fixing  
- Low context switching

---

## Wow Moment Requirement

On first successful run, Whidy must:

- Immediately generate a meaningful workday summary
- Avoid raw commit dumps
- Highlight focus areas and patterns
- Make the user recognize their own day instantly

---

## Non-Goals

Whidy does NOT:

- Store historical data
- Track time
- Require continuous background execution
- Use machine learning models in MVP
- Depend on complex configuration

---

## Future Extensions

- GitHub integration expansion
- Jira integration
- Episode persistence layer
- Windows UI wrapper
- Advanced behavioral insights
