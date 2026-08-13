# com.microtube.hexr

HexR haptic glove integration for Unity: hand-tracking-driven finger/palm haptics, grab
detection, and preset effects.

## Layout

- `Runtime/HexR/` (assembly `HexR.Runtime`) — backend-agnostic; compiles with neither XR
  backend installed. `HexRManager` (scene wiring, Auto Setup, Bluetooth connect flow),
  `PhysicsHandTracking` (raw-hand joint resolution + optional ghost-rig mirroring),
  `PhysicsHandTrackingOpenXR`, `HapticFingerTrigger`, `HexRGrabbable`, `HexRUsable`,
  `FingerUseTracking`, `PressureTrackerMain`, `ProximityCheck`, `SpecialHaptics`,
  `HaptGloveCollidersVisualizer`, `HexRDebugLogPanel`, `HexRPanelConnectButtons`.
- `Runtime/MetaOVR/` (assembly `HexR.Runtime.MetaOVR`) — the only code that touches
  `Oculus.Interaction`. Excluded from the build entirely when the Meta SDK isn't installed.
  See "How the Meta OVR support stays optional" below.
- `Runtime/UI/` — the `HexR Panel` prefab and its textures/material.
- `Runtime/Prefabs/` — the `HexR Main` rig prefabs, `Pressure Controller`, hand menu and
  grab audio. See below.
- `Runtime/Plugins/` — the precompiled `HaptGlove` runtime and the Bluetooth transport it
  needs. See below.
- `Editor/` (assembly `HexR.Editor`) — the `HexR` toolbar menu (`HexRMenu.cs`: Create HexR
  Rig, Add HexR Panel, Auto Setup Scene, Validate Scene Setup, and one-off Migration
  commands) and the `HexRToolsWindow` window: Haptics Tester, HexR Setup (scene checklist)
  and Project Setup (backend detection + package installer + docs links).
- `Samples~/Tutorial Content` — optional tutorial props, imported from Package Manager.

## The HaptGlove runtime (`Runtime/Plugins/`)

The `HaptGlove` runtime (`HaptGloveHandler`, `Haptics`, `Grasping`, BLE transport,
encode/decode) is **not** source code here — it ships as a precompiled `HaptGlove.dll`,
built from the separate HexR Plugin firmware repo. This package bundles it, along with
everything it loads at runtime, so a fresh install compiles and connects without the
consuming project having to source any of it:

| File | What it is |
| --- | --- |
| `HaptGlove.dll` | The runtime `HexR.Runtime` compiles against. References only mscorlib/System/UnityEngine/`ArduinoBluetoothAPILocal` — no `UnityEditor`, so it is player-build safe. |
| `ArduinoBluetoothAPILocal.dll` | `HaptGlove.dll`'s only non-BCL dependency, and Microtube's own code — source is in the firmware repo (`ArduinoBluetoothAPILocal.sln`, netstandard2.1). It declares its types in the `ArduinoBluetoothAPI` namespace, which is why `HaptGlove` compiles against it without the third-party assembly. |
| `Android/classes.jar` | Third-party (`com.tony.bluetoothunityapi`). The Java BLE implementation `ArduinoBluetoothAPILocal` binds to by name via `AndroidJavaObject` — this *is* Android Bluetooth. |
| `BleWinrtDll.dll` | Native WinRT BLE, P/Invoked directly for the Windows/Editor path. Built from adabru/BleWinrtDll (MIT) — the embedded PDB path still reads `C:\Users\ABrun\Documents\BleWinrtDll`. Needs attribution, nothing more. |
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

## The rig (`Runtime/Prefabs/`)

`HexR Main (OVR)` / `HexR Main (Open XR)` (the rig **HexR > Create HexR Rig** instantiates),
`Pressure Controller`, `Hand Menu With Button Activation` and `Grab Audio` ship here, along
with `New Material.mat` which they reference. They used to live in the consuming project's
`Assets/HexRAssets/`, which meant a fresh install got the scripts and nothing to instantiate
-- and `Pressure Controller` is not optional: every `HapticFingerTrigger` looks one up per
scene and produces no haptics without it.

`HexRMenu` prefers a copy under `Assets/` when one exists, so a project that customised the
rig in place keeps getting its own version rather than the packaged one.

These still assume the project already has Meta's XR SDK and a hand-tracking camera rig in
the scene -- the rig mirrors and instruments that, it does not create it.

**Known caveat:** the panel's text uses `Electronic Highway Sign SDF` from TextMesh Pro's
*Examples & Extras*, which is an optional TMP import many projects skip. Without it the panel
falls back to TMP's default font -- cosmetic only, nothing functional depends on it.

Note that a package installed from a git URL or registry is **immutable**, so the
`HexR > Migration` commands (which rewrite the rig prefab) only work where the package is
embedded, as it is in this repo. They are one-off migrations for legacy projects; a fresh
install has no need of them.

## Package dependencies

`package.json` declares only `com.unity.textmeshpro`. **Neither XR backend is a hard
dependency** — the package installs and compiles in a project with no Meta SDK, and in one
with no OpenXR packages.

`HexR.Runtime`'s asmdef references `Unity.TextMeshPro` only. It does **not** reference a
`HaptGlove.Runtime` assembly, since `HaptGlove.dll` is a precompiled plugin
(auto-referenced by every assembly by default) rather than an asmdef in this project.

### How the Meta OVR support stays optional

Everything that needs `Oculus.Interaction` lives in a second assembly,
`Runtime/MetaOVR/` (`HexR.Runtime.MetaOVR`):

- its asmdef carries `"defineConstraints": ["HEXR_META_OVR"]`, and a `versionDefines` entry
  that defines `HEXR_META_OVR` only when `com.meta.xr.sdk.interaction` is present. With no
  Meta SDK, the constraint fails, Unity excludes the assembly outright, and its
  `Oculus.Interaction` reference is never resolved — so there is no missing-reference
  warning either.
- `HexR.Runtime` can't reference it (that would put the dependency straight back), so the
  dependency runs the other way: `MetaOVRBackendSetup` registers itself into the
  `HexRManager.PressureTrackerBackendSetup` hook, which `AutoSetup` invokes if anything is
  listening and warns about if nothing is.
- `PressureTrackerMain.handGrabInteractor` / `.pokeInteractor` deliberately stayed on the
  core component, widened from `HandGrabInteractor`/`PokeInteractor` to `MonoBehaviour`.
  They hold live scene references (32 across this repo's own tutorial scenes), and Unity
  only preserves a serialized `objectReference` across a type change when the new type
  still accepts it — widening does, relocating the field to another component does not.
  `MetaOVRHandNearSource` casts them and polls `HasInteractable`.

## Hand-near gating differs by backend

Haptics only fire when `PressureTrackerMain.IsHandNear()` is true (unless the call passes
`ByPassHandCheck`). It ORs three flags, and the backends do **not** feed it equally:

| Flag | Set by | Meta OVR | OpenXR |
| --- | --- | --- | --- |
| `HandGrabbing` | `MetaOVRHandNearSource` ← `HandGrabInteractor.HasInteractable` | ✅ | ❌ |
| `PokeHovering` | `MetaOVRHandNearSource` ← `PokeInteractor.HasInteractable` | ✅ | ❌ |
| `CollisionNearHand` | `ProximityCheck` trigger volume | ✅ | ✅ (only source) |

So on OpenXR, **a scene with no `ProximityCheck` never fires haptics at all** — which looks
exactly like broken hardware. `ValidateSetup` fails on this for OpenXR rigs specifically.
Put a `ProximityCheck` with a trigger collider on each object the hand should be able to
feel, and hit its "Auto Set Up" to wire both Pressure Controllers.

## Getting started in a new project

1. Install this package (embed the `Packages/com.microtube.hexr` folder, or add it via a
   git URL once this repo/branch is reachable from wherever you're installing from). The
   `HaptGlove` runtime and its Bluetooth transport come with the package — nothing to
   import by hand. If the project already has its own copy of `HaptGlove.dll` (or of the
   `ArduinoBluetoothAPI*` binaries) under `Assets/Plugins/`, delete it: two copies of the
   same assembly is a hard compile error, not a warning.
2. Open **HexR > HexR Tools > Project Setup**, pick the backend this project targets, and
   install whatever it reports missing. Nothing pulls these in automatically any more —
   that's the price of the package not hard-depending on either.
3. Set up a hand-tracking camera rig in the target scene (Meta Building Blocks' "Hand
   Tracking" block, or an OpenXR rig with `com.unity.xr.hands`). This package assumes that
   exists, it doesn't create it.
4. Run **HexR > Create HexR Rig > Meta OVR** (or **Open XR**). The rig prefab ships with
   the package, so there is nothing to source or copy in first.
5. Run **HexR > Auto Setup Scene** (or the Inspector's "Auto Set Up HexR" button), then
   **HexR > Validate Scene Setup** to confirm hand roots, the Pressure Controllers, the
   fingertip/palm colliders and (on OpenXR) a `ProximityCheck` are all wired correctly.
6. Optional: **Window > Package Manager > HexR > Samples > Tutorial Content > Import** for
   the tutorial props (torch, squeezable heart, lightbulb, ball, connect/grab audio).

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

- **`HaptGloveUI.cs`** moved into this package because the rig prefab has it on its root,
  but it is redundant: `HexRManager` has equivalent `ConnectLeftBT`/`ConnectRightBT`.
  Deleting it needs those buttons' `OnClick` targets re-wired by hand first.
- **`HexRPanelConnectButtons` and `HexRDebugLogPanel` are attached to nothing.** Both were
  written to remove the need for per-scene UnityEvent wiring (and, for the log panel, to
  read logs on-headset without adb), and neither is referenced by any prefab or scene. The
  panel's connect buttons and collider toggle still rely on per-scene overrides as a result.
- **`SpecialHaptics.cs` and `FingerUseTracking.cs` have an unguarded `using UnityEditor;`.**
  Untidy, but *not* a build blocker as previously recorded here: their editor classes are
  correctly `#if UNITY_EDITOR`'d, nothing outside those blocks uses the namespace, and the
  built `Library/Bee/PlayerScriptAssemblies/HexR.Runtime.dll` carries no `UnityEditor`
  reference. Quest builds succeed.
- **`Assets/HexRAssets/WindowBle/BLE.cs`/`Impl.cs`/`WindowHaptHandler.cs`** — left in the
  consuming project. Unclear whether these loose, non-`HaptGlove`-namespaced scripts are
  still used or are dead code from before `HaptGlove.dll` existed. Note
  `WindowHaptHandler.targetDeviceName` defaults to `"HaptGloveAR Right"` for both hands.
- **`package.json` declares `com.meta.xr.sdk.interaction: 71.0.0`** while the code targets
  the v201 OpenXR hand skeleton (`XRHand_*`). A project that already has 201 is unaffected,
  but a clean install resolves 71 and the hands will not bind. `"unity": "2021.3"` is
  likewise below the 2023.2 this is developed against.
