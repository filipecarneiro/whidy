# Whidy

A CLI that reconstructs your workday from commits, pull requests, code reviews, and builds.

It answers one question every developer knows from Scrum standups:

> What did I do yesterday?

## Why Whidy exists

Daily standups are simple in theory, but in practice they rely on memory, guesswork, or Jira updates that no one fully trusts.

Whidy solves this by rebuilding your day from real engineering activity:

- commits
- pull requests
- code reviews
- builds

No manual logging. No timesheets. No tracking.

Just your actual work, reconstructed.

## What it shows you

Instead of raw logs, Whidy gives you:

- what you worked on
- where your focus was
- how your time was distributed
- a clean standup-style summary

Example output:

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

## Features

- Reconstructs your workday from commits, pull requests, code reviews, and builds
- Groups activity into meaningful work areas
- Generates standup-ready summaries
- Detects focus, context switching, and work patterns
- Runs entirely from the command line
- No background services
- No database
- No manual input after setup

## Installation

Download the latest `whidy.exe` from [GitHub Releases](https://github.com/filipecarneiro/whidy/releases), place it anywhere in your `PATH`, and run it.

No .NET runtime required — it is a self-contained executable.

### Building from source

```bash
dotnet publish -r win-x64 -c Release --self-contained true -p:PublishSingleFile=true
```

The output is a single `whidy.exe` in `bin/Release/net10.0/win-x64/publish/`.

## Usage
### Generate yesterday’s report

```
whidy yesterday
```

## First run

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

Configuration is saved locally and reused on subsequent runs.

## How it works

Whidy is built on a simple idea:

> Your work already exists in commits, pull requests, code reviews, and builds. It just needs to be interpreted.

Pipeline:

```
Commits · Pull Requests · Reviews · Builds
                ↓
        Event normalisation
                ↓
    Episode grouping (by repo + time)
                ↓
          Insight engine
                ↓
    Human-readable standup report
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

See [ROADMAP.md](ROADMAP.md).


## The idea

Whidy exists to remove the gap between:

> what you did
> and
> what you remember you did

## License

MIT
