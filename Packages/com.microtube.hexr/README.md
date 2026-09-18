# com.microtube.hexr

HexR haptic glove integration for Unity: hand-tracking-driven finger/palm haptics, grab
detection, and preset effects.

Runs on **either OpenXR or the Meta Interaction SDK** — neither is a hard dependency, so
this installs into any Unity 2022.3+ project and you pick the backend afterwards. The OpenXR
path is device-agnostic: Quest, PICO, Vive and anything else exposing Unity XR Hands all use
the same rig and the same settings.

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
the latest release. Append a tag (`#v0.5.1`) when a build has to stay reproducible.

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

1. **HexR → HexR Tools → Project Setup** — pick OpenXR, Meta OVR or PICO and install whatever it
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
copies of the same types is a hard compile error, not a warning — and this now includes
`HaptGlove.dll` specifically, because the package ships that runtime as source. A project
that keeps its own `HaptGlove.dll` will collide with `HaptGlove.Runtime`.

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
- `Runtime/HaptGlove/` (assembly `HaptGlove.Runtime`) — the HaptGlove runtime:
  `HaptGloveHandler`, `Haptics`, `Grasping`, the BLE transport and the encode/decode layer.
- `Runtime/Plugins/` — the native Bluetooth transports the runtime binds to. See below.
- `Editor/` (assembly `HexR.Editor`) — the `HexR` toolbar menu (`HexRMenu.cs`: Create HexR
  Rig, Create Demo Scene, Add HexR Panel, Auto Setup Scene, Validate Scene Setup, and one-off
  Migration commands) and the `HexRToolsWindow` window: Haptics Tester, HexR Setup (scene checklist)
  and Project Setup (backend detection + package installer + docs links).
- `Samples~/Tutorial Content` — optional tutorial props, imported from Package Manager.

## The HaptGlove runtime (`Runtime/HaptGlove/`)

The `HaptGlove` runtime (`HaptGloveHandler`, `Haptics`, `Grasping`, BLE transport,
encode/decode) ships as **source**, in the `HaptGlove.Runtime` assembly. It used to be a
precompiled `HaptGlove.dll` built from the separate HexR Plugin repo; that binary was a
`Debug` build, lagged the source it was built from, and could not be fixed from here, so
the source was brought in instead. `HexR.Runtime` and `HexR.Editor` both reference
`HaptGlove.Runtime`.

The native transports it binds to are still binaries, and are bundled so a fresh install
compiles and connects without the consuming project having to source any of it:

| File | What it is |
| --- | --- |
| `ArduinoBluetoothAPILocal.dll` | The runtime's only non-BCL dependency, and Microtube's own code — source is in the firmware repo (`ArduinoBluetoothAPILocal.sln`, netstandard2.1). It declares its types in the `ArduinoBluetoothAPI` namespace, which is why `HaptGlove` compiles against it without the third-party assembly. **Also a `Debug` build.** |
| `Android/classes.jar` | Third-party (`com.tony.bluetoothunityapi`). The Java BLE implementation `ArduinoBluetoothAPILocal` binds to by name via `AndroidJavaObject` — this *is* Android Bluetooth. |
| `BleWinrtDll.dll` | Native WinRT BLE, P/Invoked directly for the Windows/Editor path. Built from adabru/BleWinrtDll (MIT) — the embedded PDB path still reads `C:\Users\ABrun\Documents\BleWinrtDll`. Needs attribution, nothing more. |
| `BluetoothUnityAPI.bundle` | Native macOS BLE. Carries the build machine's Xcode `DerivedData` paths (`/Users/tony/…`), and `Contents/_CodeSignature/CodeResources` contains the author's personal email. Both are upstream artifacts — stripping them would invalidate the bundle's code signature, so they stay. |
| `Android/hexrbluetooth.androidlib/` | Library-project manifest carrying the Bluetooth/location permissions and the BLE helper activity, merged into the consuming app's manifest at build time. |

The native plugins above are auto-referenced (`isExplicitlyReferenced: 0`), so no asmdef has
to list them. `HaptGlove.Runtime` is different — it is an asmdef, so `HexR.Runtime` and
`HexR.Editor` both name it in their `references`.

### How connecting behaves

- **Android runtime permissions are requested before the first scan.** On Android 12 (API 31)
  and later, `BLUETOOTH_SCAN` and `BLUETOOTH_CONNECT` are runtime permissions. Declaring them
  in the manifest is not enough: without a granted permission `startScan` returns no results
  and `connectGatt` throws, and neither surfaces as an error — it looks exactly like "no glove
  nearby". `HexRManager` now requests them and reports a denial explicitly instead of guessing.
- **A connected glove is remembered.** On a successful connect the device address is stored in
  `PlayerPrefs` (`HexR_<hand>_DeviceId`). A later connect asks the plugin whether it can still
  resolve that device and, if so, connects straight away — skipping the fixed ~8 s scan window
  the plugin otherwise runs to completion before it even attempts a connection. If the cached
  device turns out to be gone, it falls back to a normal scan.
- **Drops are detected and retried.** The Android BLE layer has no disconnect callback at all,
  so a glove that goes out of range or runs flat used to leave the app believing it was still
  connected. A supervisor polls the link once a second while connected, raises
  `onBluetoothDisconnected` when it goes down, and reconnects with backoff — 1 s, 2 s, 4 s,
  8 s — giving up after about a minute and re-showing the panel. Retries are only ever
  scheduled from a definitive outcome, never from a timer guessing that an attempt has
  finished; that distinction is why an earlier attempt at auto-reconnect had to be reverted.
- **Only one hand scans at a time.** The plugin keeps its scanning flag and its discovered-
  device list in static fields shared by both hands, so two concurrent scans clear each
  other's results. A second connect request is queued and started when the first finishes.

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

`HexR.Runtime`'s asmdef references `Unity.TextMeshPro` and `HaptGlove.Runtime` — the latter
being the in-package HaptGlove source. `HexR.Editor` references both as well. The native
transports under `Runtime/Plugins/` stay auto-referenced plugins, so nothing has to name
them.

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

### PICO

**PICO is not a third backend.** It is OpenXR plus a vendor OpenXR plugin, so it uses the
OpenXR rig, the `HexR Main (Open XR)` prefab and `XRFramework = OpenXR` exactly like any
other OpenXR runtime. There is no PICO setting anywhere in HexR, and there shouldn't be.

Use **HexR → Create HexR Rig → Open XR (Quest, PICO, SteamVR)** and leave `XR Framework` on
`OpenXR`.

**Why it just works.** `PhysicsHandTracking.OpenXRStart` resolves joints by the Unity XR
Hands names — `L_ThumbMetacarpal`, `L_IndexTip`, … `L_Palm` — and `AutoSetup` finds the hand
roots as `Left`/`Right Hand Interaction Visual` → `L_Wrist`/`R_Wrist`. PICO's hand-tracking
subsystem produces exactly those names, because they come from `com.unity.xr.hands` rather
than from the vendor. Nothing in this package names a headset.

**Getting the plugin.** `com.unity.xr.openxr.picoxr` is a zip from
[PICO's developer downloads](https://developer.picoxr.com/resources/), unpacked so that the
folder holding its `package.json` sits directly under `Packages/`. It is on no registry, so
Project Setup can detect it and link to it but cannot install it. Verified against v1.4.1,
whose assemblies are `Unity.XR.OpenXR.Features.PICOSupport`, `Unity.XR.OpenXRPico`,
`PICO.Platform` and `PICO.TobSupport`.

> [!IMPORTANT]
> This is **not** the legacy PICO Unity Integration SDK (the PXR one, assembly
> `Unity.XR.PICO`). That brings its own XR loader instead of `OpenXRLoader`, and HexR's
> OpenXR path does not support it.

**Project settings.** OpenXR as the plug-in provider under XR Plug-in Management → Android;
"PICO OpenXR Features" and the hand-tracking feature ticked under OpenXR → Android; ARM64
and IL2CPP in Player Settings.

**`Quest BLE Buffering` (`isQuest`) is untested on PICO.** It ships **on**, which is the
default on both rigs, and nothing is known to be wrong with it there — but it selects a
Bluetooth write-buffering strategy inside the closed-source `HaptGlove.dll` and has only
ever been exercised on Quest. *If a PICO build pairs with the glove but no haptics arrive,
turn it off first.*

**The `ProximityCheck` requirement applies in full** — PICO sits in the OpenXR column of the
table below, so a scene without one never fires haptics at all.

## Hand-near gating differs by backend

Haptics only fire when `PressureTrackerMain.IsHandNear()` is true (unless the call passes
`ByPassHandCheck`). It ORs three flags, and the backends do **not** feed it equally:

| Flag | Set by | Meta OVR | OpenXR (incl. PICO) |
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

## Updating the HaptGlove runtime

It is source now, under `Runtime/HaptGlove/` — edit it in place and Unity recompiles it like
any other package code. There is no build step and no binary to swap.

Upstream, the same code lives in the `HexR Plugin ( Source Code )/HaptGlove/HaptGlove` C#
project in the HexR Plugin repo (that repo's working tree was later stripped to firmware
only, in `ab7d087`; the sources are still in its history). If changes are made there, port
them here as source rather than reintroducing a built `HaptGlove.dll` — a second copy of
these types collides with `HaptGlove.Runtime`.

`ArduinoBluetoothAPILocal.dll` is still a binary, built from `ArduinoBluetoothAPILocal.sln`
in the same repo. Build it with the symbol path suppressed, so the output does not carry the
build machine's directory layout into everyone's project:

```
dotnet build -c Release -p:DebugType=none -p:Deterministic=true
```

`DebugType=none` emits no PDB and no PDB path. If symbols are wanted, keep them but strip
the absolute path instead:

```
dotnet build -c Release -p:DebugType=portable -p:PathMap=$(MSBuildProjectDirectory)=/src
```

Two things to check before swapping a build in: that it still references no `UnityEditor`
(otherwise Quest builds break), and that it introduces no new non-BCL dependency (otherwise
that dependency has to be bundled here too).

## Known gaps (found while assembling this package — not yet resolved)

- **`isQuest` has never been tested on PICO.** It ships on. Now that the runtime is source,
  what it does is no longer a mystery: `HaptGloveHandler.FixedUpdate` accumulates every
  `BTSend` into `questBleBuffer` and flushes it as one write at most every
  `bleDelayThreshold` (100 ms) instead of writing immediately. Nothing about that is
  Quest-specific, so PICO should behave the same — but it trades latency for fewer writes,
  so it is still the first thing to toggle if a PICO build pairs but feels laggy or stays
  silent. See [PICO](#pico).

- **`HaptGloveHandler.BuildPlatform` — *resolved*.** The enum is
  `{ Android = 0, Window = 1 }`, and the field is a plain serialised value that nothing ever
  reassigns: it is **not** derived from `Application.platform` at runtime. Android builds work
  simply because `Android` is `0` and every shipped prefab serialises `0`. A Windows player
  therefore needs it set to `Window` explicitly — which is exactly what the Tools window does
  in Play mode for Editor testing.

- **`targetDeviceName` is cosmetic in a prefab — *resolved*.** `HaptGloveHandler.Start`
  re-derives it from `whichHand`, and `Update` keeps re-deriving it every frame, unless
  `UseCustomDeviceName` is ticked. So a wrong value in a prefab is masked. The OVR rig's left
  hand had carried `HaptGloveAR Right` since before v0.5.0 (the OpenXR rig was corrected in
  `7c2d6c4`, the OVR rig was missed); it has now been fixed too, so both rigs agree.

- **`ArduinoBluetoothAPILocal.dll.meta` still carries `Android: CPU: ARMv7`.** Harmless — the
  CPU setting is ignored for a managed assembly, and `Any: enabled: 1` governs — so it was
  left alone rather than risk dropping the assembly from a build for no gain. The same
  setting *was* removed from `hexrbluetooth.androidlib`, where it was not harmless.

- **`ArduinoBluetoothAPILocal.dll` is a `Debug` build.** It was built in the Debug
  configuration and shipped as-is, and its embedded PDB path is what gives it away:

  ```
  ArduinoBluetoothAPILocal.dll  C:\Users\Microtube Tech\Desktop\HexR Plugin\…\obj\Debug\netstandard2.1\…pdb
  ```

  That string also puts a build machine's `Desktop` layout into every consuming project.
  Rebuilding in Release with `-p:DebugType=none` fixes both together — see
  [Updating the HaptGlove runtime](#updating-the-haptglove-runtime). `HaptGlove.dll` had the
  same problem and no longer applies: it ships as source now, so Unity compiles it with
  everything else.
- **`HaptGloveUI.cs`** moved into this package because the rig prefab has it on its root,
  but it is redundant: `HexRManager` has equivalent `ConnectLeftBT`/`ConnectRightBT`.
  Deleting it needs those buttons' `OnClick` targets re-wired by hand first.
- **`HexRDebugLogPanel` is attached to nothing.** It was written to read logs on-headset
  without adb, and no prefab or scene references it. (`HexRPanelConnectButtons`, previously
  listed here too, *is* wired — `Runtime/UI/HexR Panel.prefab` and
  `Runtime/Prefabs/HexR Main (OVR).prefab` both carry it — so the panel's connect toggles no
  longer depend on per-scene overrides.)
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
  redundant as well as unused and should be removed next. (The wrapper this package uses is
  now `Runtime/HaptGlove/BLE.cs` and `Impl.cs`, in source.)
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
