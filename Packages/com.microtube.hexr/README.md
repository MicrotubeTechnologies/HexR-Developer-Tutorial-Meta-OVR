# com.microtube.hexr

HexR haptic glove integration for Unity: hand-tracking-driven finger/palm haptics, grab
detection, and preset effects.

Runs on **either OpenXR or the Meta Interaction SDK** — neither is a hard dependency, so
this installs into any Unity 2022.3+ project and you pick the backend afterwards.

## Installing

### Package Manager (recommended)

> [!IMPORTANT]
> `com.microtube.hexr` is **not published to a registry**. **Add package by name** will fail
> with *"Unable to find package"* — the name identifies the package, it is not somewhere to
> fetch it from. Add it by **git URL**:

**Window → Package Manager → + → Add package from git URL…**, then paste:

```
https://github.com/MicrotubeTechnologies/com.microtube.hexr.git
```

Or add it to `Packages/manifest.json` directly:

```json
{
  "dependencies": {
    "com.microtube.hexr": "https://github.com/MicrotubeTechnologies/com.microtube.hexr.git"
  }
}
```

Pinning is optional: `main` is kept releasable, so the unpinned URL above always resolves
the latest release. Append a tag (`#v0.4.0`) when a build has to stay reproducible.

Either way, UPM caches a git dependency by the ref it resolved, so an unpinned URL does not
re-check on its own. To pull a newer `main`, use **Window → Package Manager →
com.microtube.hexr → Update**, or remove the entry from `Packages/packages-lock.json`.

Requires **Unity 2022.3 or newer** (verified through Unity 6.x) and git available on your `PATH` (Unity shells out to it).

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

Or run **HexR → Create Demo Scene** to get all of the above plus a grabbable cube, a haptic
zone and a wired `ProximityCheck` in one command. It picks the backend from whichever SDK is
actually installed, so nothing cross-backend is serialised.

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
  Rig, Create Demo Scene, Add HexR Panel, Auto Setup Scene, Validate Scene Setup, and one-off
  Migration commands) and the `HexRToolsWindow` window: Haptics Tester, HexR Setup (scene checklist)
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
| `HaptGlove.dll` | The runtime `HexR.Runtime` compiles against. References only mscorlib/System/UnityEngine/`ArduinoBluetoothAPILocal` — no `UnityEditor`, so it is player-build safe. **The shipped binary is a `Debug` build** — see [Known gaps](#known-gaps-found-while-assembling-this-package--not-yet-resolved). |
| `ArduinoBluetoothAPILocal.dll` | `HaptGlove.dll`'s only non-BCL dependency, and Microtube's own code — source is in the firmware repo (`ArduinoBluetoothAPILocal.sln`, netstandard2.1). It declares its types in the `ArduinoBluetoothAPI` namespace, which is why `HaptGlove` compiles against it without the third-party assembly. **Also a `Debug` build.** |
| `Android/classes.jar` | Third-party (`com.tony.bluetoothunityapi`). The Java BLE implementation `ArduinoBluetoothAPILocal` binds to by name via `AndroidJavaObject` — this *is* Android Bluetooth. |
| `BleWinrtDll.dll` | Native WinRT BLE, P/Invoked directly for the Windows/Editor path. Built from adabru/BleWinrtDll (MIT) — the embedded PDB path still reads `C:\Users\ABrun\Documents\BleWinrtDll`. Needs attribution, nothing more. |
| `BluetoothUnityAPI.bundle` | Native macOS BLE. Carries the build machine's Xcode `DerivedData` paths (`/Users/tony/…`), and `Contents/_CodeSignature/CodeResources` contains the author's personal email. Both are upstream artifacts — stripping them would invalidate the bundle's code signature, so they stay. |
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
   the tutorial props (torch, squeezable heart, lightbulb, ball, connect/grab audio). The
   sample is props only — there is no scene in it, because a saved scene would have to
   reference one backend's camera rig and would arrive broken on the other. **HexR > Create
   Demo Scene** generates one instead, against whichever backend is installed.

## Updating `HaptGlove.dll`

It is a build artifact of the `HexR Plugin ( Source Code )/HaptGlove/HaptGlove` C#
project (`AssemblyName` `HaptGlove`, .NET Framework 4.8) in the HexR Plugin firmware
repo. To refresh it, build that project in **Release** and drop the output over
`Runtime/Plugins/HaptGlove.dll`, leaving the `.meta` in place so the GUID — and every
scene/prefab reference to the types inside — survives.

Build it with the symbol path suppressed, so the output does not carry the build
machine's directory layout into everyone's project:

```
dotnet build -c Release -p:DebugType=none -p:Deterministic=true
```

`DebugType=none` emits no PDB and no PDB path. If symbols are wanted, keep them but strip
the absolute path instead:

```
dotnet build -c Release -p:DebugType=portable -p:PathMap=$(MSBuildProjectDirectory)=/src
```

The same applies to `ArduinoBluetoothAPILocal.dll`, built from `ArduinoBluetoothAPILocal.sln`
in the same repo.

Two things to check before swapping a build in: that it still references no `UnityEditor`
(otherwise Quest builds break), and that its only non-BCL dependency is still
`ArduinoBluetoothAPILocal` (otherwise the new dependency has to be bundled here too).
The firmware project's `.csproj` points at Unity 2021.3 install paths and a
`Library/ScriptAssemblies` folder on the original author's machine, so it will not build
unmodified on another machine — expect to fix those `HintPath`s locally. That is the
blocker on rebuilding either DLL, so it has to be cleared first.

## Known gaps (found while assembling this package — not yet resolved)

- **`HaptGlove.dll` and `ArduinoBluetoothAPILocal.dll` are `Debug` builds.** Both were built
  in the Debug configuration and shipped as-is, so the package's entire BLE and haptics
  runtime is unoptimised code in a real-time path on Quest. Their embedded PDB paths are
  what give it away:

  ```
  HaptGlove.dll                 C:\Users\Microtube Tech\Documents\GitHub\HexR Plugin\…\obj\Debug\HaptGlove.pdb
  ArduinoBluetoothAPILocal.dll  C:\Users\Microtube Tech\Desktop\HexR Plugin\…\obj\Debug\netstandard2.1\…pdb
  ```

  Those strings also put a build machine's `Desktop`/`Documents` layout into every consuming
  project. Rebuilding both in Release with `-p:DebugType=none` fixes the performance problem
  and the path leak together — see [Updating `HaptGlove.dll`](#updating-haptglovedll). Blocked
  on the firmware `.csproj` `HintPath`s described there.
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
- **`WindowBle/BLE.cs`/`Impl.cs`/`WindowHaptHandler.cs`** — *resolved: dead code.* These were
  left loose in the consuming project (now `Assets/Tutorial/Scripts/WindowBle/` in the
  tutorial repo, after the `Assets/` reorganisation). Searching for their script GUIDs across
  every scene, prefab and asset returned nothing — they were attached to no GameObject
  anywhere — so `WindowHaptHandler.cs` has been deleted. `BLE.cs` and `Impl.cs` were only
  ever consumed by it and are now orphaned in turn; they are a second managed wrapper over
  `BleWinrtDll.dll`, which this package already wraps inside `HaptGlove.dll`, so they are
  redundant as well as unused and should be removed next.
- **Redistribution rights for the Android/macOS Bluetooth binaries are unconfirmed.**
  `Runtime/Plugins/Android/classes.jar` and `Runtime/Plugins/BluetoothUnityAPI.bundle` are
  Tony Abou Zaidan's, marked "all rights reserved", and appear to come from a commercial
  Asset Store plugin. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) — this needs
  settling, and until it is, those two files should not be treated as redistributable.

## License

MIT — see [LICENSE](LICENSE).

That covers Microtube Technologies' own work: everything under `Runtime/HexR/`,
`Runtime/MetaOVR/` and `Editor/`, the prefabs and UI, `HaptGlove.dll` and
`ArduinoBluetoothAPILocal.dll`.

It does **not** cover the third-party binaries bundled under `Runtime/Plugins/`, which stay
under their own terms and are not sublicensed here. See
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for the list — in particular the Android
and macOS Bluetooth binaries, whose redistribution status is unresolved.
