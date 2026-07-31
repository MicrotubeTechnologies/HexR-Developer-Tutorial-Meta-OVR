# com.microtube.hexr

HexR haptic glove integration for Unity: hand-tracking-driven finger/palm haptics, grab
detection, and preset effects.

## Layout

- `Runtime/HexR/` (assembly `HexR.Runtime`) — `HexRManager` (scene wiring, Auto Setup,
  Bluetooth connect flow), `PhysicsHandTracking` (raw-hand joint resolution + optional
  ghost-rig mirroring), `HapticFingerTrigger`, `HexRGrabbable`, `HexRUsable`,
  `FingerUseTracking`, `PressureTrackerMain`, `SpecialHaptics`, `HaptGloveCollidersVisualizer`,
  `HexRDebugLogPanel`, `HexRPanelConnectButtons`.
- `Runtime/UI/` — the `HexR Panel` prefab and its textures/material.
- `Editor/` (assembly `HexR.Editor`) — the `HexR` toolbar menu (`HexRMenu.cs`: Create HexR
  Rig, Add HexR Panel, Auto Setup Scene, Validate Scene Setup, and one-off Migration
  commands) and the `HexRToolsWindow` setup/status window.

**Not in this package** — unlike some other HexR package builds, the `HaptGlove` runtime
(`HaptGloveHandler`, `Haptics`, BLE transport, encode/decode) is **not** source code here.
It ships as a precompiled `Assets/Plugins/HaptGlove.dll` in the consuming project. This
package's `HexR.Runtime` assembly references the `HaptGlove` namespace it exposes, but
has no visibility into (and makes no claims about) its internal behavior, licensing, or
which Bluetooth plugins it depends on internally.

`Assets/HexRAssets/Main Prefab/*` (the `HexR Main (OVR)`/`HexR Main (Open XR)` rig
prefabs, OVR camera rig, hand anchors) also live **outside** this package, in the
consuming project's `Assets/`. They assume the project already has Meta's XR SDK and a
hand-tracking camera rig set up.

## Package dependencies

`package.json` declares `com.meta.xr.sdk.interaction` and `com.unity.textmeshpro`.
`HexR.Runtime`'s asmdef references `Oculus.Interaction` and `Unity.TextMeshPro` directly;
it does **not** reference a `HaptGlove.Runtime` assembly, since `HaptGlove.dll` is a
precompiled plugin (auto-referenced by every assembly by default) rather than an asmdef
in this project.

## Getting started in a new project

1. Install this package (embed the `Packages/com.microtube.hexr` folder, or add it via a
   git URL once this repo/branch is reachable from wherever you're installing from).
   `com.meta.xr.sdk.interaction`/`com.unity.textmeshpro` resolve automatically.
2. Import `HaptGlove.dll` into the consuming project's `Assets/Plugins/` — this package
   does not bundle it.
3. Make sure Meta's XR SDK and a hand-tracking camera rig (Building Blocks' "Hand
   Tracking" block, or an equivalent OVR rig) are already set up in the target scene —
   this package assumes that exists, it doesn't create it.
4. Bring in (or build) a rig with `HexRManager` + `PhysicsHandTracking` on a "Left/Right
   Hand Physics" object per hand, referencing your hand-tracking rig. `HexR Main
   (OVR).prefab`/`HexR Main (Open XR).prefab` in this project's `Assets/HexRAssets/Main
   Prefab/` are working examples to copy from.
5. Run **HexR > Auto Setup Scene** (or the Inspector's "Auto Set Up HexR" button), then
   **HexR > Validate Scene Setup** to confirm hand roots, the Pressure Controllers, and
   the fingertip/palm colliders are all wired correctly.

## Known gaps (found while assembling this package — not yet resolved)

- **`HaptGloveUI.cs`** (`Assets/HexRAssets/Main Script/`) is still live and referenced by
  several scenes/prefabs for the Bluetooth connect buttons. `HexRManager` now has its own
  equivalent `ConnectLeftBT`/`ConnectRightBT`, so the two are currently redundant —
  `HaptGloveUI` was deliberately left in place rather than deleted, since removing it
  would need those buttons' `OnClick` targets re-wired by hand in the Editor first.
- **`SpecialHaptics.cs`** has an unguarded `using UnityEditor;` and its custom inspector
  class isn't wrapped in `#if UNITY_EDITOR` — this will fail to compile in an actual
  Standalone/Quest build, not just in-Editor. Pre-existing, not introduced by this
  package restructure.
- **`Assets/HexRAssets/WindowBle/BLE.cs`/`Impl.cs`** — unclear whether these loose,
  non-`HaptGlove`-namespaced scripts are still used, or dead code left over from before
  `HaptGlove.dll` was introduced. Not investigated as part of this package restructure.
