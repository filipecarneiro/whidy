# Whidy — Product Specification (MVP)

For a project overview, see [README.md](../README.md).

## Overview

A CLI tool that reconstructs a developer's workday from development activity and produces a human-readable, standup-style summary.

## Core Principles

- No database
- No background service
- Stateless execution
- Minimal configuration
- Works from commits, pull requests, reviews, and builds
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
All date calculations use the local machine clock and timezone.

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

### --help

```
whidy --help
```

Displays available commands and arguments.

---

### --version

```
whidy --version
```

Displays the current version.

---

## System Architecture

CLI Application

→ Configuration Loader  
→ Identity Resolver  
→ Event Fetcher (commits, pull requests, reviews, builds) [parallel]  
→ Event Normalizer  
→ Insight Engine  
→ Console Renderer  

**Identity Resolver**: resolves the caller's identity (name, email, user descriptor) from the Azure DevOps `/profile` API using the provided PAT. The resolved identity is used to filter all subsequent API queries to the authenticated user's activity only.

**Event Fetcher**: commits, pull requests, review threads, and build results are fetched in parallel from their respective Azure DevOps API endpoints. All paginated responses are fully consumed before processing continues.

**Scope**: all repositories in the organization accessible with the provided PAT are searched.

---

## First-Run Flow

On first execution, Whidy guides the user through a short setup:

```
Welcome to Whidy!

To get started, paste any Azure DevOps link — a repository, project, or pull request URL:
> https://dev.azure.com/my-org/project/_git/repo

Got it. Now you'll need a Personal Access Token (PAT).

Open Azure DevOps → User Settings → Personal Access Tokens → New Token
Required permissions: Code (read), Pull Request Threads (read), Build (read)

Paste your token:
> ************

All set. Fetching your activity...
```

- The organization URL is extracted from the link and stored in configuration
- The PAT is validated immediately; if it fails, Whidy prompts again with a plain-language explanation
- Configuration is saved locally and reused on subsequent runs

Supported provider (MVP): Azure DevOps.

---

## Configuration

### User Configuration

Stored at `~/.whidy/config.json`, created with owner-only read permissions (mode 600 on Unix; restricted ACL on Windows).

```json
{
  "azureDevOps": {
    "url": "https://dev.azure.com/my-org/",
    "pat": "personal-access-token"
  }
}
```

- `azureDevOps.url`: full Azure DevOps organization URL; used to scope all API calls at runtime
- `azureDevOps.pat`: Personal Access Token; stored in plain text — file permissions are the only protection

The `azureDevOps` group is provider-scoped to allow future integrations (GitHub, Jira, etc.) to add their own sibling groups without breaking existing configuration.

No expiration tracking. No authentication pre-validation.

### Application Settings

Default values are compiled into the application binary. An optional `appsettings.json` placed alongside the executable at runtime overrides these defaults. Not intended for end-user modification.

```json
{
  "episodeWindowMinutes": 90,
  "insights": {
    "focusDetectionThreshold": 0.6,
    "contextSwitchingEpisodeThreshold": 4,
    "workTypeBalanceThreshold": 0.6,
    "intensitySpikeEventCount": 5,
    "intensitySpikeWindowMinutes": 30,
    "failureHeavyDayThreshold": 0.5,
    "debuggingBuildRetriggerThreshold": 2
  }
}
```

- `episodeWindowMinutes`: time gap threshold for episode grouping (default: 90); see Episode Grouping Rules
- `insights.focusDetectionThreshold`: minimum share of events in one repository to trigger focus detection (default: 0.6)
- `insights.contextSwitchingEpisodeThreshold`: minimum number of distinct episodes across different repositories to trigger context switching (default: 4)
- `insights.workTypeBalanceThreshold`: minimum share of episodes of one type to trigger work type balance (default: 0.6)
- `insights.intensitySpikeEventCount`: minimum number of events within the spike window to trigger intensity spike (default: 5)
- `insights.intensitySpikeWindowMinutes`: duration of the spike detection window in minutes (default: 30)
- `insights.failureHeavyDayThreshold`: minimum share of failed or partially succeeded builds to trigger failure-heavy day (default: 0.5)
- `insights.debuggingBuildRetriggerThreshold`: minimum number of build events for the same pipeline to classify an episode as debugging (default: 2)

---

## Authentication Model

- PAT is used directly in API calls
- If any API request fails:
  - 401 → _"I couldn't connect to Azure DevOps with this token. Please provide a new PAT."_
  - 403 → _"This token doesn't have the required permissions. Please create a new PAT with: Code (read), Pull Request Threads (read), Build (read)."_
- If any event source fails during a run, the entire run fails; no partial output is shown
- Configuration is updated immediately after the user provides a valid replacement token

---

## Empty Results and Error Handling

### Message Style

All user-facing messages — errors, prompts, and empty-state notices — must be:

- **Human-readable**: plain language, no HTTP status codes or technical identifiers
- **Actionable**: tell the user exactly what to do next
- **Non-technical**: no stack traces, exception names, or internal system details

Example:
> _"I couldn't access Azure DevOps with this token. Please provide a new PAT with read permissions."_

Not:
> _"HTTP 401 Unauthorized"_

---

### No activity found — `yesterday` lookback

If no events are found for the previous calendar day, Whidy automatically looks back up to 7 calendar days to find the most recent day with activity. The report header reflects the actual day returned.

This covers:
- Weekends (Monday run reports last Friday)
- Public holidays
- Days off or sick days

If no activity is found within the 7-day window:
> _"No activity found in the last 7 days. Have you been on a break?"_

### No activity found — all other commands

For explicit date arguments (specific date, date interval, weekday, relative period), no lookback is performed:
> _"No activity found for [period]. Try a different date, or check that you have commits or pull requests for that time."_

### Network and API errors

- Network failure → _"Couldn't reach Azure DevOps. Check your internet connection and try again."_ — exit with no partial output
- API rate limit (429) → _"Azure DevOps is temporarily limiting requests. Please wait a moment and try again."_ — exit with no retry

---

## Data Model (in-memory only)

### Event

- timestamp
- author (resolved from caller identity; used to filter events to the authenticated user)
- type (commit, pull request, pr comment, pr approval, build)
- repository
- title/message
- outcome (for builds: `succeeded`, `failed`, `partiallySucceeded`, `canceled`; in-progress builds are excluded)

---

### Episode

A grouped set of related events representing a coherent unit of work.

- repo
- events
- type (coding, review, debugging)
- label (human-readable summary)

---

### Insight

High-level interpretation of episodes. Each insight renders as a single human-readable sentence in the narrator voice.

- sentence (the rendered output line)
- rule (which rule produced it, for internal tracing)

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

Events are sorted by timestamp and grouped sequentially:

```
Sort events by timestamp
For each event:
  if same repo AND time gap from last event in current episode < episodeWindowMinutes
    continue current episode
  else
    start new episode
```

- `episodeWindowMinutes` is configured in application settings (default: 90)
- Build and PR events attach to an existing episode for the same repository if one exists within the window; otherwise they start their own episode

---

### Classification Heuristics

Episode type is determined in priority order:

1. **Debugging** — any build in the episode has outcome `failed` or `partiallySucceeded`, or build events for the same pipeline exceed `insights.debuggingBuildRetriggerThreshold` (default: 2)
2. **Review** — majority of events are PR-related (PR created, pr comment, pr approval)
3. **Coding** — majority of events are commits

Priority: debugging > review > coding. When no type is clearly dominant, coding is the default.

**Build outcome mapping**: `failed` and `partiallySucceeded` are treated as failure signals; `canceled` is neutral and excluded from failure signals; in-progress builds are excluded entirely.

---

### Label Generation

Labels are derived from event content after stripping ticket prefixes (e.g. `[PROJ-123]`, `AB#456`). All labels are truncated to 60 characters.

**Coding**  
Use the most recent commit message as the label basis.  
_"Implemented {most recent commit message}"_

**Debugging**  
Use the repository name and a hint from the most recent commit or build message.  
_"Fixed issues in {repo} ({hint from most recent message})"_

**Review**  
Use the PR title if available; fall back to the repository name.  
_"Reviewed: {PR title}"_ or _"Reviewed changes in {repo}"_

**Fallback**  
_"Work in {repo}"_

Labels appear as episode headers in the activity report and feed into insight sentence generation.

---

### Insight Rules

Each rule evaluates the full episode set and produces one narrative sentence. Rules are not mutually exclusive — multiple insights can fire in the same report.

**Focus detection**  
Trigger: one repository accounts for ≥`insights.focusDetectionThreshold` (default: 60%) of all events  
Output: _"You spent most of your time in Authentication Service"_

**Context switching**  
Trigger: distinct episodes across different repositories exceed `insights.contextSwitchingEpisodeThreshold` (default: 4)  
Output: _"You switched contexts several times during the day"_

**Work type balance**  
Trigger: one episode type accounts for ≥`insights.workTypeBalanceThreshold` (default: 60%) of all episodes  
Output examples:
- _"You spent most of your time fixing issues rather than adding features"_ (debugging-heavy)
- _"You mostly reviewed code (more code read than written)"_ (review-heavy)
- _"You focused on new work (high commit activity, low failure rate)"_ (coding-heavy)

**Intensity spike**  
Trigger: `insights.intensitySpikeEventCount` (default: 5) or more events within `insights.intensitySpikeWindowMinutes` (default: 30) minutes  
Output: _"You hit a flow state, a concentrated burst of focused activity"_

**Failure-heavy day**  
Trigger: share of builds with outcome `failed` or `partiallySucceeded` exceeds `insights.failureHeavyDayThreshold` (default: 50%)  
Output: _"You spent a lot of time fighting builds and fixing regressions"_

---

## Output Format

### Activity Report

- Repository-based grouping of work
- Short bullet points per activity
- Human-readable summaries

### Report Header

The header is derived from the **actual date range returned**, not the argument used. Whidy resolves the most natural human label for the period.

| Actual period | Header |
|---|---|
| Today | `TODAY` |
| Previous calendar day | `YESTERDAY` |
| 2–6 days ago | weekday name: `MONDAY`, `FRIDAY`, etc. |
| Previous full calendar week | `LAST WEEK` |
| Previous full calendar month | `LAST MONTH` |
| Specific date older than 7 days | `MAY 12, 2026` |
| Multi-day range | `MAY 12–16, 2026` |

Arguments that resolve to the same period produce the same header. For example, on 2026-05-16 both `whidy yesterday` and `whidy 2026-05-15` produce `YESTERDAY`.

When the `yesterday` lookback fires and returns a day within the last 6 days, the header shows the weekday name (e.g., `FRIDAY`). If the lookback returns a day older than 6 days, the header shows the explicit date.

All keyword labels (`TODAY`, `YESTERDAY`, weekday names, `LAST WEEK`, `LAST MONTH`) are fixed English strings. Explicit dates (rows 6 and 7 in the table) are formatted using the user's system locale settings.

### Example

```
YESTERDAY

Authentication Service
You spent most of your time here.

• Fixed login token refresh issue
• Reviewed authentication flow improvements
• Added retry logic to token validation

Web App
• UI adjustments in dashboard layout

📊 Insights
• You spent most of your time in Authentication Service
• You spent most of your time fixing issues rather than adding features
• You hit a flow state, a concentrated burst of focused activity
```

---

### Language Style

**Activity bullets:**
- Past tense, action-first verb: _"Fixed"_, _"Reviewed"_, _"Added"_, _"Deployed"_, _"Investigated"_
- Strip commit ticket prefixes before rendering (e.g. `[PROJ-123]`, `AB#456`)
- Trim to a readable length; never show raw commit hashes or branch names
- Avoid passive constructions: not _"A fix was applied to..."_ but _"Fixed..."_

**Insight sentences:**
- Always start with _"You"_ as the subject
- Use present tense for patterns: _"You focused on..."_, _"You spent..."_
- Keep under 80 characters
- Use parentheticals to add context without extra sentences: _"Low context switching (focused session)"_
- Prefer qualitative language over raw numbers: _"several"_, _"mostly"_, _"primarily"_

**Repo headers:**
- Display the repository short name, not the full Azure DevOps path
- When focus detection fires, always append _"You spent most of your time here."_ below the repo name

**General:**
- No jargon, no pipeline identifiers, no branch names in output
- The reader should recognise their own day without needing to decode anything
- The application is English only; no localization or translation

Whidy is not summarising events.  
It is reconstructing attention — surfacing where the developer's focus actually was, independent of raw commit counts or PR volume.

### First Successful Run

On first successful run, Whidy must:

- Immediately generate a meaningful workday summary
- Avoid raw commit dumps
- Highlight focus areas and patterns
- Make the user recognize their own day instantly
- Speak in a narrator voice, not a log viewer voice

---

## Non-Goals

Whidy does NOT:

- Track time
- Require continuous background execution
- Depend on complex configuration
- Use machine learning or LLM models (all insight generation is rule-based)

---

## Out of Scope (MVP)

The following are explicitly out of scope for the MVP and tracked in the [roadmap](../ROADMAP.md).

- Additional provider integrations (GitHub, Jira, Calendar)
- Activity persistence and historical trend analysis
- Non-console output formats (web UI, desktop app, email, etc.)
- Machine learning or LLM-based analysis. All insight generation is rule-based
- Internationalization (i18n) and localization
- Multi-platform support. The MVP is built and tested on Windows only
- MSI or packaged installer. Distribution is a self-contained single-file executable via GitHub Releases
