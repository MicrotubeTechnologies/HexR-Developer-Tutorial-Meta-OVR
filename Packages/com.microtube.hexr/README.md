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
- `Runtime/Plugins/` — the precompiled `HaptGlove` runtime and the Bluetooth transport it
  needs. See below.
- `Editor/` (assembly `HexR.Editor`) — the `HexR` toolbar menu (`HexRMenu.cs`: Create HexR
  Rig, Add HexR Panel, Auto Setup Scene, Validate Scene Setup, and one-off Migration
  commands) and the `HexRToolsWindow` setup/status window.

## The HaptGlove runtime (`Runtime/Plugins/`)

The `HaptGlove` runtime (`HaptGloveHandler`, `Haptics`, `Grasping`, BLE transport,
encode/decode) is **not** source code here — it ships as a precompiled `HaptGlove.dll`,
built from the separate HexR Plugin firmware repo. This package bundles it, along with
everything it loads at runtime, so a fresh install compiles and connects without the
consuming project having to source any of it:

| File | What it is |
| --- | --- |
| `HaptGlove.dll` | The runtime `HexR.Runtime` compiles against. References only mscorlib/System/UnityEngine/`ArduinoBluetoothAPILocal` — no `UnityEditor`, so it is player-build safe. |
| `ArduinoBluetoothAPILocal.dll` | `HaptGlove.dll`'s only non-BCL dependency. |
| `ArduinoBluetoothAPI.dll`, `Android/ArduinoBluetoothAPI.dll`, `WSA/ArduinoBluetoothAPI.dll` | Per-platform managed BLE transport. |
| `Android/classes.jar` | The Android-side BLE implementation the above binds to. |
| `BleWinrtDll.dll` | Native WinRT BLE, used by the Windows/Editor path. |
| `BluetoothUnityAPI.bundle` | Native macOS BLE. |
| `Android/hexrbluetooth.androidlib/` | Library-project manifest carrying the Bluetooth/location permissions and the BLE helper activity, merged into the consuming app's manifest at build time. |

All of these are auto-referenced plugins (`isExplicitlyReferenced: 0`), which is why
`HexR.Runtime`'s asmdef does not — and must not — list `HaptGlove` explicitly.

The permissions ship as an `.androidlib` rather than a plain `Plugins/Android/AndroidManifest.xml`
because Unity only honours the latter at `Assets/Plugins/Android/`, which a package cannot
provide. A library-project manifest *is* merged from a package, so that is the only shape
that works here.

**Still outside this package**: `Assets/Plugins/iOS/` (the iOS BLE framework and its Xcode
post-process script). Left in the consuming project deliberately — that script imports
`UnityEditor.iOS.Xcode`, so folding it into `HexR.Editor` would break the whole editor
assembly for anyone without the iOS Build Support module installed. HexR targets Quest and
the Windows Editor; iOS support is opt-in and project-side.

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
   `com.meta.xr.sdk.interaction`/`com.unity.textmeshpro` resolve automatically. The
   `HaptGlove` runtime and its Bluetooth transport come with the package — nothing to
   import by hand. If the project already has its own copy of `HaptGlove.dll` (or of the
   `ArduinoBluetoothAPI*` binaries) under `Assets/Plugins/`, delete it: two copies of the
   same assembly is a hard compile error, not a warning.
2. Make sure Meta's XR SDK and a hand-tracking camera rig (Building Blocks' "Hand
   Tracking" block, or an equivalent OVR rig) are already set up in the target scene —
   this package assumes that exists, it doesn't create it.
3. Bring in (or build) a rig with `HexRManager` + `PhysicsHandTracking` on a "Left/Right
   Hand Physics" object per hand, referencing your hand-tracking rig. `HexR Main
   (OVR).prefab`/`HexR Main (Open XR).prefab` in this project's `Assets/HexRAssets/Main
   Prefab/` are working examples to copy from.
4. Run **HexR > Auto Setup Scene** (or the Inspector's "Auto Set Up HexR" button), then
   **HexR > Validate Scene Setup** to confirm hand roots, the Pressure Controllers, and
   the fingertip/palm colliders are all wired correctly.

## Updating `HaptGlove.dll`

It is a build artifact of the `HexR Plugin ( Source Code )/HaptGlove/HaptGlove` C#
project (`AssemblyName` `HaptGlove`, .NET Framework 4.8) in the HexR Plugin firmware
repo. To refresh it, build that project in **Release** and drop the output over
`Runtime/Plugins/HaptGlove.dll`, leaving the `.meta` in place so the GUID — and every
scene/prefab reference to the types inside — survives.

Two things to check before swapping a build in: that it still references no `UnityEditor`
(otherwise Quest builds break), and that its only non-BCL dependency is still
`ArduinoBluetoothAPILocal` (otherwise the new dependency has to be bundled here too).
The firmware project's `.csproj` points at Unity 2021.3 install paths and a
`Library/ScriptAssemblies` folder on the original author's machine, so it will not build
unmodified on another machine — expect to fix those `HintPath`s locally.

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
