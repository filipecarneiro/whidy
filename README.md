# Whidy

A CLI that reconstructs your workday from commits and pull requests.

It answers one question every developer knows from Scrum standups:

> What did I do yesterday?

## Why Whidy exists

Daily standups are simple in theory, but in practice they rely on memory, guesswork, or Jira updates that no one fully trusts.

Whidy solves this by rebuilding your day from real engineering activity:

- commits
- pull requests

No manual logging. No timesheets. No tracking.

Just your actual work, reconstructed.

## What it shows you

Instead of raw logs, Whidy gives you:

- what you worked on
- where your focus was
- how your time was distributed
- a clean standup style summary

Example output:

```
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
```

## Features

- Reconstructs your workday from commits and pull requests
- Groups activity into meaningful work areas
- Generates standup ready summaries
- Detects focus, context switching, and work patterns
- Runs entirely from the command line
- No background services
- No database
- No manual input after setup

## Installation

``` bash
dotnet build
```

or future release:

```
whidy install
```

## Usage
### Generate yesterday’s report

```
whidy yesterday
```

## First run

On first execution, Whidy will:

1. Ask for an Azure DevOps or Git provider link
1. Extract your organization automatically
1. Request a Personal Access Token
1. Generate your first workday summary immediately

Example:

```
Paste any repository, project or PR link:
> https://dev.azure.com/my-org/project/_git/repo
```

## How it works

Whidy is built on a simple idea:

> Your work already exists in commits and pull requests. It just needs to be interpreted.

Pipeline:

```
Commits + Pull Requests
        ↓
Event grouping
        ↓
Episode detection
        ↓
Insight engine
        ↓
Human readable standup
```

## Design philosophy

- No databases
- No time tracking
- No manual categorization
- No configuration overhead
- Stateless execution

Whidy is not a tracker.

It is a reflection tool.

## Perfect for

- Scrum developers
- Distributed teams
- Freelancers working across multiple repos
- Engineers tired of manual standups
- Anyone who asks themselves: “What did I actually do yesterday?”

## Roadmap

- GitHub integration
- Jira integration
- Improved insight engine
- Windows UI companion
- “Today so far” live mode
- Better context switching detection


## The idea

Whidy exists to remove the gap between:

> what you did
> and
> what you remember you did

## License

MIT