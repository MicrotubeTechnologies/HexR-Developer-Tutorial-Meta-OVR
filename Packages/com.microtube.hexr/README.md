# com.microtube.hexr

HexR haptic glove integration for Unity: hand-tracking-driven finger/palm haptics, grab
detection, and preset effects.

Runs on **either OpenXR or the Meta Interaction SDK** — neither is a hard dependency, so
this installs into any Unity 2023.2+ project and you pick the backend afterwards.

## Installing

### Package Manager (recommended)

**Window → Package Manager → + → Add package from git URL…**, then paste:

```
https://github.com/MicrotubeTechnologies/com.microtube.hexr.git#v0.3.0
```

Or add it to `Packages/manifest.json` directly:

```json
{
  "dependencies": {
    "com.microtube.hexr": "https://github.com/MicrotubeTechnologies/com.microtube.hexr.git#v0.3.0"
  }
}
```

Pin a tag (`#v0.3.0`) rather than tracking the default branch — UPM caches a git dependency
by the ref it resolved, so an unpinned URL updates at unpredictable moments, usually the
moment someone else clones the project.

Requires **Unity 2023.2 or newer** and git available on your `PATH` (Unity shells out to it).

### Local checkout, for working on the package itself

Clone it next to your project and point the manifest at the folder:

```json
"com.microtube.hexr": "file:../../com.microtube.hexr"
```

The path is relative to your project's `Packages/` folder. Edits recompile immediately, and
the package stays a normal git checkout you can branch and commit in — which the git-URL
form does not give you (UPM installs those read-only under `Library/PackageCache/`).

### After installing

1. **HexR → HexR Tools → Project Setup** — pick OpenXR or Meta OVR and install whatever it
   reports missing. Nothing pulls either backend in automatically; that is the deliberate
   cost of the package not hard-depending on either.
2. Set up a hand-tracking camera rig in your scene (Meta Building Blocks' "Hand Tracking"
   block, or an OpenXR rig with `com.unity.xr.hands`). This package assumes one exists — it
   does not create it.
3. **HexR → Create HexR Rig →** your backend.
4. **HexR → Auto Setup Scene**, then **HexR → Validate Scene Setup**.

Full walkthrough in [Getting started in a new project](#getting-started-in-a-new-project)
below, including the OpenXR-only `ProximityCheck` requirement.

> **Upgrading from 0.2.x?** `package.json` no longer depends on
> `com.meta.xr.sdk.interaction`, so a Meta project that relied on HexR to pull the SDK in
> must now declare it itself. Nothing else changes — no scene or prefab migration.

### Conflicts to clear first

If the project already carries its own copy of `HaptGlove.dll` or the
`ArduinoBluetoothAPI*` binaries under `Assets/Plugins/`, delete them before installing. Two
copies of the same assembly is a hard compile error, not a warning.

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

1. Install the package — see [Installing](#installing) above. The `HaptGlove` runtime and
   its Bluetooth transport come bundled, so there is nothing to import by hand.
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
