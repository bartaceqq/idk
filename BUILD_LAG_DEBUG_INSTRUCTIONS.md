# Build Lag Debug Instructions

Goal: find which gameplay system causes the short movement/building hitches.

The project now starts with `GameplayDebugFeatureGate` enabled. By default it uses `WalkingOnly`, which keeps only the walking/look controller and input enabled. Everything else is disabled at runtime so the first test is a clean baseline.

## Preset Order

Test in this order. Restart Play Mode or the build after changing the preset.

1. `WalkingOnly`
   - Enabled: player input, `FPSControllerTest`, camera look/walk.
   - Disabled: building, inventory, crafting, interactions, enemies, animals, world runtime optimizers, particles, non-player animators.

2. `BuildController`
   - Adds: `LookingController`, `RayCastScriptTest`, snap point scripts, runtime build markers.
   - Use this to test build preview and snapping without inventory/crafting/interactions.

3. `InventoryCrafting`
   - Adds: inventory, slots, crafting station/menu/scripts.
   - Use this to test selecting/crafting/building items.

4. `Interactions`
   - Adds: ray interactions, item switching, action/weapon scripts, hit testing.
   - Use this to test chopping/mining/attacking related systems.

5. `WorldRuntime`
   - Adds: runtime optimizer/culling, terrain tree conversion/proxies, tree/stone/chest runtime scripts.
   - If hitches return here, inspect `ResHandler` and terrain/tree runtime scripts first.

6. `EnemiesAnimals`
   - Adds: enemies, animals, NPC/dialogue scripts.
   - If hitches return here, inspect AI/animal Update loops first.

7. `Everything`
   - Disables the debug gate filtering and lets the game run normally.

## How To Change Preset

In Unity Editor:

1. Open the project.
2. Use menu `IDK > Debug Feature Preset > ...`.
3. Enter Play Mode.
4. If switching presets while already in Play Mode acts weird, stop and start Play Mode again.

In a built player:

```powershell
idk.exe -idkFeaturePreset WalkingOnly
idk.exe -idkFeaturePreset BuildController
idk.exe -idkFeaturePreset InventoryCrafting
idk.exe -idkFeaturePreset Interactions
idk.exe -idkFeaturePreset WorldRuntime
idk.exe -idkFeaturePreset EnemiesAnimals
idk.exe -idkFeaturePreset Everything
```

You can also set an environment variable before launching:

```powershell
$env:IDK_DEBUG_FEATURE_PRESET='WalkingOnly'
```

## Test Method

For every preset:

1. Start the same scene from a fresh launch.
2. Walk and look around for at least 60 seconds.
3. Use the same route/camera movement every time.
4. Record whether the short freeze/stutter appears.
5. If the hitch appears after enabling a preset, the issue is probably in the group added by that preset.

Do not test multiple new groups at once. The point is to find the first preset where the hitch appears.

## If A Preset Fails

If `BuildController` fails:
- Split `RayCastScriptTest` snapping, preview material handling, and placement raycast.
- Temporarily disable snapping first.

If `InventoryCrafting` fails:
- Split inventory UI, crafting UI, slot refresh, and item lookup.

If `Interactions` fails:
- Split `RayScript`, `ActionScript`, `ItemSwitchScript`, and `TestHitting`.

If `WorldRuntime` fails:
- Split `ResHandler`, terrain tree conversion/proxy scripts, tree scripts, stone scripts, and chest scripts.

If `EnemiesAnimals` fails:
- Split zombie, skeleton, animal, and NPC/dialogue scripts.

