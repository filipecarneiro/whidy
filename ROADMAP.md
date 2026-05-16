# Whidy — Roadmap

Planned work beyond the MVP. For the full MVP scope, see [docs/mvp-product-spec.md](docs/mvp-product-spec.md).

---

## Provider Integrations

- **GitHub** — fetch commits, pull requests, and review events from GitHub repositories
- **Jira** — include work item activity alongside code events
- **Calendar** — integrate a free/busy feed to surface meeting load and explain gaps in coding activity

## Insight Improvements

- **Improved insight engine** — richer pattern detection and more nuanced narrative generation
- **Advanced behavioral insights** — longer-term patterns across multiple days
- **Better context switching detection** — more accurate episode boundary detection

## Data and Persistence

- **Episode persistence layer** — optional local cache to enable week and month trend analysis without re-fetching all data

## Delivery

- **Nightly email report** — a scheduled service that sends the previous day's Whidy report by email each night, so the standup summary arrives in your inbox before the morning meeting

## Platform

- **Windows UI wrapper** — companion app for non-terminal users, including Windows toast notifications with the daily summary
