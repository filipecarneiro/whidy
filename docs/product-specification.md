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
## Target Audience

- Scrum developers preparing for daily standups
- Distributed teams with asynchronous communication
- Freelancers working across multiple repositories
- Engineers who want to replace manual standup notes with real activity data

---
## Primary Use Case

Developers run Whidy at any time to see a structured summary of their previous day’s work:

- What they focused on
- What repositories they worked in
- What kind of work they did (coding, review, debugging)
- High-level insights about their activity

---

## Command Interface

All arguments resolve to a datetime interval internally and are processed through the same pipeline.  
If no argument is provided, `yesterday` is assumed.

### yesterday _(default)_

```
whidy yesterday
```

Generates a structured report of the previous calendar day.

---

### today

```
whidy today
```

Generates a report of the current day so far.

---

### week day

```
whidy monday
whidy friday
```

Generates a report for the most recent past occurrence of the specified weekday.

---

### specific date

```
whidy 2026-05-12
```

Generates a report for the given date. Accepts ISO 8601 format (YYYY-MM-DD).

---

### date interval

```
whidy 2026-05-12 2026-05-16
```

Generates a report spanning the specified date range (inclusive).

---

### relative periods

```
whidy last-week
whidy last-month
```

Generates a report for the previous full calendar week or month.

---

## System Architecture

CLI Application

→ Configuration Loader  
→ Event Fetcher (commits, pull requests)  
→ Event Normalizer  
→ Insight Engine  
→ Console Renderer  

---

## First-Run Flow

On first execution, Whidy prompts the user for a repository, project, or PR link from their Git provider:

```
Paste any repository, project or PR link:
> https://dev.azure.com/my-org/project/_git/repo
```

- The organization is extracted automatically from the URL
- The user is then prompted for a Personal Access Token
- Configuration is saved locally and reused on subsequent runs

Supported providers: Azure DevOps, GitHub (and other Git providers).

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
- Improved insight engine
- Better context switching detection
