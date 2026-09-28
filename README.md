# Project Conductor — Programming Portfolio

This repository is a **portfolio-oriented source-code extract** from the team project **Project Conductor**.

## Project
- Engine: Unity
- Language: C#
- Development period: 2026.03–2026.05
- Team size: 6
- Applicant: CHAE SANGHUN / 蔡 相勳（チェ サンフン）

## What I implemented
My work focused on the game's current/electricity mechanic and related gameplay support systems.

### Core current system
- Wire-path based current movement
- A / B / C current types with different paths, speeds, and colors
- Switch-triggered current spawning
- Gap detection while moving along the wire
- Tracking success/failure of multiple currents

### Overload / co-op mechanic
- Detection of intersections between three wire paths
- Automatic overload-zone placement at matching points
- Overload behavior based on A/B/C currents
- Two-player occupancy tracking with `HashSet<PlayerShockHandler>`
- Debug tooling for overload testing

### Presentation / gameplay support
- Clear-zone camera direction
- Camera bounds clamping
- Off-screen current indicators
- Player shock feedback

## Troubleshooting examples

### 1. Fast current skipped a gap
Point-only collision checks could miss a narrow gap when the current moved a large distance in one frame.
The movement check was changed to inspect the segment between the previous and next positions so the gap could still be detected.

### 2. Two-player state bug
A single bool / single player reference was insufficient when two players entered and left an overload zone independently.
The implementation was changed to:

```csharp
private HashSet<PlayerShockHandler> playersInNode = new HashSet<PlayerShockHandler>();
public bool HasPlayer => playersInNode.Count > 0;
```

This keeps the state correct even when only one of two players leaves the zone.

## Source provenance
The files in `PortfolioSource/` were copied from commits authored by **20211398-wq** in the original team repository. They are provided here for portfolio review. Team assets and unrelated teammate code are intentionally excluded.

Some scripts reference project-side classes that are not included in this extract (for example PlayerController, InGameManager, or UI components). This repository is therefore intended as a **source-code portfolio**, not as a standalone playable Unity build.

## Full project
The full team project remains private. A gameplay video and presentation materials are provided separately in the application portfolio.
