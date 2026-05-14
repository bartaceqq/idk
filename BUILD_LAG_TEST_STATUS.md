# Build Lag Test Status

Current default preset: `WalkingOnly`.

Codex validation done:

| Check | Result |
| --- | --- |
| Non-GUI C# compile check | Passed |
| `git diff --check` | Passed |
| Unity Play Mode test | Not run |
| Built player test | Not run |

Runtime preset test log:

| Preset | Tested On PC | Result | Notes |
| --- | --- | --- | --- |
| `WalkingOnly` | Not tested | Unknown | Baseline: only looking/walking should run. |
| `BuildController` | Not tested | Unknown | Adds build preview/snapping/controller. |
| `InventoryCrafting` | Not tested | Unknown | Adds inventory and crafting. |
| `Interactions` | Not tested | Unknown | Adds ray interaction, tools, weapons. |
| `WorldRuntime` | Not tested | Unknown | Adds culling optimizer, terrain/tree/stone/chest runtime scripts. |
| `EnemiesAnimals` | Not tested | Unknown | Adds AI, animals, NPC/dialogue. |
| `Everything` | Not tested | Unknown | Full game runtime. |

When a test is done, fill `Result` with one of:

- `Smooth`
- `Hitches`
- `Cannot test`

If a preset hitches, stop there and split that preset into smaller script groups.

