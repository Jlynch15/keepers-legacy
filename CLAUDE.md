# Keeper's Legacy

Mobile-first creature collection and care game. Single codebase: Godot 4.6 (.NET) / C#.
Ship targets: Steam (Windows / Mac / Linux), iOS, iPadOS. Landscape on all platforms.

## Worktree Convention

This project uses **project-local worktrees** at `.worktrees/<branch-name>/`.
The `.worktrees/` directory is gitignored.

**Use this directory for ALL parallel/feature work** — don't use global locations
(`~/.config/superpowers/worktrees/...`) or sibling directories. Multiple Claude
sessions running in parallel on this project must each operate in their own
worktree under `.worktrees/` to avoid cross-session interference.

## Mobile-First Constraints

- Single-tap input only — no right-click, no hover-only states
- 44x44px minimum touch target
- Long-press only for destructive actions (e.g. release creature)
- No creature inventory — every owned creature is housed in a habitat at all times

## Code Conventions

- C# runtime code; tests use NUnit in `KeeperLegacyGodot/Tests/`
- Managers are Godot autoload singletons (`HabitatManager`, `ProgressionManager`, etc.)
- UI components are code-built `Control` nodes (no per-component `.tscn` files)
- Per-screen color palettes live in dedicated palette files (e.g. `HabitatPalette.cs`)
  to make the planned palette rework a single-file edit
- Visual elements (positions, sizes, decorations, animations) ship with in-game
  drag/key tuning controls from day one — bake values via paste-ready C# print output

## Active Work

See `docs/superpowers/specs/` and `docs/superpowers/plans/` for current designs and
implementation plans.
