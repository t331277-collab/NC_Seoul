# Phase

## 2026-06-27 - CurStruc Action Buttons

- Role: Code Builder
- Status: Completed
- Scope: `CurStruc` generated row action buttons and structure action panels.
- Implemented:
  - `DistrictStructurePanelManager` binds generated row `InvestBtn`, `RepairBtn`, and `DestructBtn` to `StructureActionManager`.
  - `StructureActionManager` opens `InvestPanel`, `RepairPanel`, and `DestPanel`, fills `StruName` and `Desc` with selected building display name and CSV cost in `K` format.
  - ESC priority path through `StructureActionManager.TryCloseOpenBuildPanel()` now also closes open structure action panels.
  - Support spends `지원 비용`, applies a random 1-5 year 1.5x production boost, and expires after yearly production.
  - Repair remains dummy behavior and closes/logs only.
  - Destroy spends `철거 비용`, disables the selected building GameObject, refreshes lists/resources, and removes any support boost on that object.
  - `StructStageManager` checks `StructureActionManager.TryGetProductionMultiplier(...)` while calculating active building production.
- Verified with Unity-MCP:
  - `InvestBtn` opened `InvestPanel` with selected name/cost.
  - `RepairBtn` opened `RepairPanel` with selected name/cost.
  - `DestructBtn` opened `DestPanel` with selected name/cost.
  - No generated `StrTemplate_*` verification rows remain in the saved scene.
  - `CurStruc`, `CanBuildStruc`, `InvestPanel`, `RepairPanel`, `DestPanel`, and `BuildPanel` are saved inactive.
  - Unity console error filter showed no script compilation errors.
- User Play-Mode verification needed:
  - Confirm the three row buttons open correctly in actual play.
  - Confirm ESC closes an open action panel before camera reset behavior.
  - Confirm support boost affects next-year PlusMinus/production for the random duration.
  - Confirm destroy disables the selected building after cost payment.


## 2026-06-27 - CurStruc Action Panel Mismatch Fix

- Role: Code Builder
- Status: Completed
- Fixed issue: action panels could show stale/wrong building text such as `주택가3` when clicking another row like `용산구청`.
- Cause: action panel text that had already been replaced once was later read as a template without `{StruName}` / `{InvestAmont}` tokens.
- Implemented:
  - Added `StructureActionButtonBinding` so each generated row button stores its own target object, definition, display name, and action kind.
  - Updated `StructureActionManager.ReadTemplate(...)` to reject stale tokenless panel text and fall back to default token templates.
  - Reset saved `InvestPanel`, `RepairPanel`, and `DestPanel` text values back to token templates.
- Verified with Unity-MCP:
  - `용산구청` `InvestBtn` opens `용산구청 지원 공문 / 20K ...`.
  - `용산구청` `RepairBtn` opens `용산구청 보수 명령서 / 20K ...`.
  - `용산구청` `DestructBtn` opens `용산구청 철거 명령서 / 10K ...`.
  - No generated verification rows remain in the saved scene.
  - Unity console error filter showed no script compilation errors.
