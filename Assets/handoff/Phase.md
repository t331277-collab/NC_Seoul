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


## 2026-06-27 - Money Shortage Toast Popup

- Role: Code Builder
- Status: Completed
- Implemented:
  - Added `ToastPopupManager` on `UI` to control `UI/ToastPopUp`.
  - `ToastPopUp/TXT` shows `돈이 모자랍니다!`.
  - Toast starts above its saved position, drops down to the saved position, remains visible for 2 seconds, then exits upward quickly and disables.
  - Build, support, repair, and destroy insufficient-money branches now call the toast.
  - Repair now checks/spends `RepairCost` before closing with dummy behavior so repair can also show money shortage.
- Verified with Unity-MCP:
  - `UI` has `ToastPopupManager`.
  - `UI/ToastPopUp` exists, starts inactive, and is saved at `(0, 457)`.
  - `UI/ToastPopUp/TXT` is `돈이 모자랍니다!`.
  - Unity console error filter showed no script compilation errors.
- User Play-Mode verification needed:
  - Trigger insufficient money for build/support/repair/destroy and confirm the toast animation timing and visibility.


## 2026-06-27 - Investment Handoff Step 1-2 Implementation

- Role: Code Builder
- Status: Completed
- Scope: `BuildingInvestmentUpgradeBalance_Handoff.md` 1~2단계
- Implemented:
  - Added `Assets/Scripts/Core/StructureInvestmentState.cs`.
  - Added `Assets/Scripts/Core/StructureInvestmentStateBootstrap.cs`.
  - Attached `StructureInvestmentState` to all current investment target buildings in `InGameScene`.
  - Added `StructureInvestmentStateBootstrap` to `UI` so missing states are configured on runtime start.
- Verified with Unity-MCP:
  - Active scene: `Assets/Scenes/InGameScene.unity`.
  - Investment targets: 200.
  - Added states: 200.
  - Missing states: 0.
  - Duplicate state objects: 0.
  - Houses: 99 with max success 15.
  - Common facilities: 63 with max success 10.
  - Unique structures: 38 with max success 3.
  - `Stru_CommonSense` containers: 25, with state components: 0.
  - `UI` has `StructureInvestmentStateBootstrap` and no `StructureInvestmentState`.
  - Unity console error filter showed no script compilation errors.
- User Play-Mode verification needed:
  - Scene: `InGameScene`
  - Open any district Summary.
  - Confirm region click and `CurStruc` list still work normally.
  - Confirm row buttons still appear and are clickable.


## 2026-06-27 - Investment Handoff Step 3-4 Implementation

- Role: Code Builder
- Status: Completed
- Scope: `BuildingInvestmentUpgradeBalance_Handoff.md` 3~4단계
- Implemented:
  - `StructureActionManager.OpenInvestPanel(...)` now displays calculated investment cost instead of raw CSV `지원 비용` for investment targets.
  - Added house investment cost table: `80, 100, 125, 155, 195, 250, 320, 410, 520, 660, 850, 1100, 1400, 1800, 2300`.
  - Added success chance formulas for house, common facility, and unique structure investments using `StructStageManager.Science`.
  - Unique structure chance now scales down by total provided resources and requires more science as resource total increases.
  - `ConfirmInvest()` no longer creates the old immediate random-duration 1.5x boost.
  - `ConfirmInvest()` now spends calculated cost, blocks duplicate pending investments, blocks max-success targets, and writes pending data into `StructureInvestmentState`:
    - `hasPendingInvestment = true`
    - `pendingResolveYear = CurrentYear + 1`
    - `pendingCost = calculated cost`
    - `pendingSuccessChance = calculated chance`
- Verified with Unity-MCP:
  - Unity console error filter showed no script compilation errors after enum reference fix.
  - Reflection check: house costs at success counts `0/5/10` are `80/250/850`.
  - Reflection check: house chance at science `555`, success `0` is `0.455`.
  - Reflection check: house chance at science `2035`, success `5` is `0.813`.
  - Reflection check: common facility chance at science `555`, success `0`, resource total `8` is `0.303`.
  - Reflection check: common facility chance at science `2035`, success `5`, resource total `8` is `0.631`.
  - Reflection check: `Stru_SeoulNationalCemetery` unique chance is `0.124` at science `555` and `0.296` at science `2035`.
  - Pending registration check: temporary `House1` investment with money `10000`, science `555`, year `1945` wrote `pending=True`, `resolveYear=1946`, `cost=80`, `chance=0.455`, and reduced test money to `9920`; test mutations were restored after verification.
- User Play-Mode verification needed:
  - Scene: `InGameScene`
  - Open a district Summary and press a building `InvestBtn`.
  - Confirm the invest panel shows the calculated cost for the selected building.
  - Confirm confirming investment immediately deducts money but does not immediately upgrade or apply a visible success/failure result yet.
  - Confirm pressing invest again on the same pending building is blocked until the next-year resolution feature is implemented.
  - Step 5 remains responsible for resolving pending success/failure on the next year.


## 2026-06-27 - Investment Handoff Step 5 Implementation

- Role: Code Builder
- Status: Completed
- Scope: `BuildingInvestmentUpgradeBalance_Handoff.md` 5단계
- Implemented:
  - `StructureActionManager.HandleBeforeYearProduction(...)` now calls `ResolvePendingInvestments(currentYear)` before construction completion checks.
  - `ResolvePendingInvestments(...)` finds `StructureInvestmentState` objects with `hasPendingInvestment == true` and `pendingResolveYear <= currentYear`.
  - Resolves investment by `Random.value <= pendingSuccessChance`.
  - Success increments `successfulInvestmentCount`.
  - Failure increments `failedInvestmentCount`.
  - Both results increment `totalInvestmentAttemptCount`, write `lastInvestmentSucceeded` / `lastResolvedYear`, clear pending fields, and log the result.
- Verified with Unity-MCP:
  - Unity console error filter showed no script compilation errors.
  - Saved scene scan before verification: `stateCount=200`, `pending=0`.
  - Forced 100% success temporary `House1`: pending cleared, attempts `1`, success `1`, fail `0`, last result `true`, resolved year `1946`.
  - Forced 0% success temporary `House2`: pending cleared, attempts `1`, success `0`, fail `1`, last result `false`, resolved year `1946`.
  - Temporary verification objects were destroyed after test.
  - Saved scene scan after verification: `stateCount=200`, `pendingCount=0`.
- User Play-Mode verification needed:
  - Scene: `InGameScene`
  - Invest in `House1` or another building.
  - Press `NextYearBtn` once.
  - Confirm the console logs `Investment succeeded ...` or `Investment failed ...`.
  - Confirm the same building can be invested again after the next-year result resolves.
  - Model replacement and stat multiplier changes are still later steps.


## 2026-06-27 - Investment Handoff Step 6 Implementation

- Role: Code Builder
- Status: Completed
- Scope: `BuildingInvestmentUpgradeBalance_Handoff.md` 6단계
- Implemented:
  - Added `StructureInvestmentState.RefreshCurrentStatMultiplier()`.
  - Added `StructureInvestmentState.CalculateStatMultiplier(...)`.
  - Added permanent milestone tracking through `permanentMilestoneStage`.
  - House/common facility multiplier now follows the handoff table: `0=1.0`, `1=1.1`, `5=2.0`, `6=2.2`, `10=4.0`, `15=6.0`.
  - Unique structure multiplier now increases by `0.1` per success up to 3 successes.
  - `StructureActionManager.TryGetProductionMultiplier(...)` now returns `StructureInvestmentState.currentStatMultiplier` for production calculation.
  - Pending investment resolution now refreshes `currentStatMultiplier` after success/failure is resolved.
- Verified with Unity-MCP:
  - Unity console error filter showed no C# script compilation errors.
  - Formula check: house `[0,1,5,6,10,15] = 1.0,1.1,2.0,2.2,4.0,6.0`.
  - Formula check: common facility success `10 = 4.0`.
  - Formula check: unique structure success `3 = 1.3`.
  - `TryGetProductionMultiplier(...)` returned `2.2` for temporary `House1` with success count `6`.
  - `TryGetProductionMultiplier(...)` returned `1.3` for temporary `Stru_Test1` with success count `3`.
  - `StructStageManager.CalculateCurrentStructValues()` production path check: temporary `House1` success `5` converted base `1/3/1` money/people/convenience into delta `2/6/2` with multiplier `2.0`.
  - Temporary verification objects were destroyed after test.
  - Scene scan after verification: `stateCount=200`, `pendingCount=0`, `rootHouse1UnderSeoul=0`.
- User Play-Mode verification needed:
  - Scene: `InGameScene`
  - Invest in a building until a success result occurs.
  - Compare `PlusMinus` before and after success; successful investment should increase production.
  - Failure should not increase production.
  - 5 successful investments should make production use the 2x milestone basis.
  - Model replacement is still a later step.


## 2026-06-27 - Investment Handoff Step 7 And Debug Success Toggle

- Role: Code Builder
- Status: Completed
- Scope: `BuildingInvestmentUpgradeBalance_Handoff.md` 7단계 plus debug support requested by user.
- Implemented:
  - `StructureActionManager` keeps root building GameObject and replaces only visual model under `VisualRoot`.
  - `House1~4` success `5+` uses `Assets/Prefab/NewHouse.prefab`.
  - `House1~4` success `10+` uses `Assets/Prefab/ApartMent.prefab`.
  - `School` success `5+` uses `Assets/Prefab/NewSchool.prefab`.
  - `DistrictOffice` success `5+` uses `Assets/Prefab/NewDistrict.prefab`.
  - `University` success `5+` uses `Assets/Prefab/NewUniversity.prefab`.
  - Model replacement happens only after next-year investment success resolution.
  - Existing original visual child is moved under `VisualRoot` and deactivated when replacement happens.
  - Added `Assets/Scripts/Core/Debug.cs` as a separate debug script under namespace `NCSeoulDebug` to avoid `UnityEngine.Debug` name conflicts.
  - `UI/DebugBtn` is connected through the new debug component on `UI`.
  - Clicking `UI/DebugBtn` enables 100% investment success chance for all future investments and updates already pending investments to `pendingSuccessChance = 1f`.
- Verified with Unity-MCP:
  - Required prefabs exist and are assigned on `StructureActionManager`: `NewHouse`, `ApartMent`, `NewSchool`, `NewDistrict`, `NewUniversity`.
  - `UI/DebugBtn` exists and has a `Button` component.
  - `UI` has `NCSeoulDebug.Debug` component.
  - Forced visual test: temporary `House1` success `5` created `VisualRoot`, disabled original `default`, set `modelStage=1`, and spawned `NewHouse`.
  - Forced visual test: same temporary `House1` success `10` set `modelStage=2` and spawned `ApartMent`.
  - Forced visual test: temporary `School` success `5` set `modelStage=1` and spawned `NewSchool`.
  - Debug test: invoking debug enable changed an existing pending test state from `0.25` to `1.00`.
  - Debug test: with force enabled, `GetInvestmentSuccessChance(...)` returned `1.00`.
  - Debug force flag was reset to `False` after verification so Play starts normally until `DebugBtn` is clicked.
  - Scene scan after verification: `stateCount=200`, `pending=0`, `visualRootCount=0`.
  - Unity console error filter showed no C# script compilation errors; only Unity-MCP client handler logs remained.
- User Play-Mode verification needed:
  - Scene: `InGameScene`.
  - Click `UI/DebugBtn` once.
  - Invest in a house and press `NextYearBtn`; success should be guaranteed.
  - At 5 successful house investments, the model should switch to `NewHouse`.
  - At 10 successful house investments, the model should switch to `ApartMent`.
  - For `School`, `DistrictOffice`, and `University`, 5 successes should switch to their matching new prefab.
  - Confirm model replacement does not break clicking, destruction, or production calculation.


## 2026-06-27 - Investment Handoff Step 8 Implementation

- Role: Code Builder
- Status: Completed
- Scope: `BuildingInvestmentUpgradeBalance_Handoff.md` 8단계
- Implemented:
  - `StructureActionManager.GetInvestmentStatusText(...)` returns row UI status text for investment state.
  - `StructureActionManager.CanInvestInStructure(...)` returns whether `InvestBtn` should remain interactable.
  - `DistrictStructurePanelManager.CreateItem(...)` appends investment state to current-structure row `StruName` text.
  - `DistrictStructurePanelManager.BindStructureActionButton(...)` disables `InvestBtn` while pending or after max enhancement.
  - Row status includes success count, max success count, pending state, next investment cost, and success chance.
- Verified with Unity-MCP:
  - Unity console error filter showed no C# script compilation errors.
  - House normal status: `강화 0/15 | 다음 80K | 성공 30%`, can invest `True`.
  - House pending status: `강화 0/15 | 투자 진행 중`, can invest `False`.
  - House max status: `강화 15/15 | 최대 강화`, can invest `False`, generated `InvestBtn.interactable=False`.
  - School status at success 5: `강화 5/10 | 다음 250K | 성공 8%`.
  - Unique structure status at success 3: `강화 3/3 | 최대 강화`, can invest `False`.
  - Scene scan after verification: `stateCount=200`, `pending=0`.
- User Play-Mode verification needed:
  - Scene: `InGameScene`.
  - Open a district Summary.
  - Confirm current structure rows show investment state under the building name.
  - Confirm pending rows show `투자 진행 중` and their `InvestBtn` cannot be clicked.
  - Confirm max-enhanced rows show `최대 강화` and their `InvestBtn` cannot be clicked.
  - Confirm normal rows show next cost and success chance.


## 2026-06-27 - Investment Step 8 Display Location Adjustment

- Role: Designer default with direct fix implementation
- Status: Completed
- Scope: Move investment enhancement status display from current structure row `StruName` to `InvestPanel/Explain`.
- Implemented:
  - `DistrictStructurePanelManager.CreateItem(...)` now keeps row `StruName` as pure building display name only.
  - Removed the row-level enhancement status append logic.
  - `StructureActionManager` now binds `CurStruc/StruContainer/InvestPanel/Explain` as `investExplainText`.
  - `OpenInvestPanel(...)` now writes enhancement status to `Explain` after opening the panel.
  - Existing `Desc` cost text remains unchanged.
  - Existing `InvestBtn` pending/max enhancement lock remains unchanged.
- Verified with Unity-MCP:
  - `InvestPanel` contains text objects `StruName`, `Desc`, `Explain`, and `InvestBtnTXT`.
  - Test `House1` success count 2 opened `InvestPanel/Explain` as `강화 상태: 강화 2/15 | 다음 125K | 성공 30%`.
  - `DistrictStructurePanelManager.cs` now sets row `StruName` to `displayName` only.
  - `DistrictStructurePanelManager.cs` no longer contains `rowDisplayName`.
  - Unity console error filter showed no C# script compilation errors.
- User Play-Mode verification needed:
  - Open district Summary and confirm row building names no longer include enhancement status.
  - Click a building `InvestBtn` and confirm `InvestPanel/Explain` shows enhancement status.


## 2026-06-27 - Investment Handoff Step 9 Implementation

- Role: Code Builder
- Status: Completed
- Scope: `BuildingInvestmentUpgradeBalance_Handoff.md` 9단계
- Implemented:
  - Added serialized `structingPrefab` reference to `StructureActionManager`.
  - `BindInvestmentVisualPrefabs()` now loads `Assets/Prefab/Structing.prefab` in editor if missing.
  - `ConfirmBuild()` now creates a construction work visual immediately when a build job starts.
  - Added `CreateConstructionWorkVisual(...)` to instantiate `Structing.prefab` under the target building parent using the target building local position, rotation, and scale.
  - Added `ConstructionJob.WorkVisualObject` to retain the spawned construction visual reference.
  - Construction completion now destroys `WorkVisualObject` before activating the real target building.
  - Assigned `Structing.prefab` reference on scene `StructureActionManager` and saved the active scene.
- Verified with Unity-MCP:
  - `Assets/Prefab/Structing.prefab` exists.
  - Sample inactive build target exists: `Seoul/GangNamGu/Stru_Coex`.
  - Forced build test created one construction job with `WorkVisualObject=Structing`.
  - Work visual parent matched the target building parent.
  - Target building stayed inactive during construction.
  - Forced completion destroyed the work visual, activated the target building, and removed the construction job.
  - Test restored target active state and money after verification.
  - Cleanup check: `sceneStructingCount=0`, `constructionJobs=0`.
  - Unity console error filter showed no C# script compilation errors.
- User Play-Mode verification needed:
  - Scene: `InGameScene`.
  - Open a district Build panel and start constructing an inactive building.
  - Confirm `Structing.prefab` appears immediately at the building location.
  - Confirm the real building remains inactive and does not contribute production until construction completes.
  - Confirm after construction time passes, `Structing.prefab` disappears and the real building appears.


## 2026-06-27 - Investment Handoff Step 10 Implementation

- Role: Code Builder
- Status: Completed
- Scope: `BuildingInvestmentUpgradeBalance_Handoff.md` 10단계
- Implemented:
  - Added `demolitionJobs` list to `StructureActionManager`.
  - Added `DemolitionJob` with `TargetObject`, `WorkVisualObject`, and `RemainingYears`.
  - `ConfirmDestroy()` now starts a 1-year demolition job instead of treating demolition as only immediate disable.
  - Demolition start creates `Structing.prefab` at the target building position through existing `CreateConstructionWorkVisual(...)`.
  - Demolition start immediately disables the real target building.
  - Added `IsDemolitionPending(...)`.
  - `HandleBeforeYearProduction(...)` now resolves demolition jobs.
  - Demolition completion destroys `WorkVisualObject`, keeps target building inactive, and removes the job.
  - `DistrictStructurePanelManager` now excludes demolition-pending targets from buildable structure rows.
- Verified with Unity-MCP:
  - Unity console error filter showed no C# script compilation errors.
  - Forced demolition test with `Seoul/DongJakGu/Stru_CommonSense/House1` created one demolition job.
  - Forced demolition test stored `WorkVisualObject=Structing`.
  - Work visual parent matched the target parent.
  - Target building became inactive immediately on demolition start.
  - `RemainingYears=1` and `IsDemolitionPending=True` after demolition start.
  - Forced next-year resolution destroyed the work visual, kept target inactive, removed the demolition job, and `IsDemolitionPending=False`.
  - Cleanup check: `sceneStructingCount=0`, `demolitionJobs=0`, `constructionJobs=0`.
- User Play-Mode verification needed:
  - Scene: `InGameScene`.
  - Open a current building row and start demolition.
  - Confirm real building disappears immediately and `Structing.prefab` appears at the same location.
  - Confirm demolished building is excluded from production while demolition is pending.
  - Press `NextYearBtn` once and confirm `Structing.prefab` disappears while the target building remains inactive.


## 2026-06-27 - Investment Handoff Step 11 Implementation

- Role: Code Builder
- Status: Completed
- Scope: `BuildingInvestmentUpgradeBalance_Handoff.md` 11단계
- Implemented:
  - Added `Assets/Scripts/Core/InvestmentBalanceSimulator.cs`.
  - Attached `InvestmentBalanceSimulator` to `UI` and saved the active scene.
  - Added inspector ContextMenu: `Run Investment Balance Simulation`.
  - Simulation loads `StructDefinition.csv`, calculates current active-scene baseline annual production from `Seoul`, and runs repeated investment attempts from 1945 to 2050.
  - Simulation reports average goal years for focused investment targets:
    - `House1` 5/10 successes
    - `School` 5/10 successes
    - `DistrictOffice` 5/10 successes
    - `University` 5/10 successes
    - Low-resource unique structure 3 successes
    - High-resource unique structure 3 successes
  - Simulator mirrors current investment cost/chance formulas and successful-investment production multiplier rules.
- Verified with Unity-MCP:
  - Unity console error filter showed no C# script compilation errors.
  - `InvestmentBalanceSimulator` is attached to `UI`.
  - Simulation ran 1000 repeated runs over 1945~2050.
  - Baseline annual production from active scene: money `69`, science `37`, people `327`, convenience `192`, love `71`.
  - `House1`: 5 successes avg `1967.0`, reached `1000/1000`; 10 successes avg `2011.1`, reached `999/1000`.
  - `School`: 5 successes avg `1973.6`; 10 successes avg `2021.4`.
  - `DistrictOffice`: 5 successes avg `1982.8`; 10 successes avg `2029.4`.
  - `University`: 5 successes avg `1976.2`; 10 successes avg `2023.3`.
  - Low-resource unique `Stru_JosunChongdokBu`: 3 successes avg `1954.7`, reached `1000/1000`.
  - High-resource unique `Stru_LotteTower`: 3 successes avg `2033.5`, reached `8/1000`.
- Balance decision:
  - House 5-success timing is within the intended 1960s range.
  - House 10-success timing is near the intended 2000s range, slightly later but close enough for this pass.
  - Common facilities are slower than houses, matching the intended pacing.
  - Unique structures show strong resource-total difficulty separation.
  - No automatic cost/chance adjustment was made in this step.
- User Play-Mode verification needed:
  - Normal play can still feel different from the simulator because player choices vary.
  - Use DebugBtn or repeated `NextYearBtn` testing if manual validation is needed.
  - Report if house 10-success feels too late or high-resource unique structures feel impossible in actual play.
