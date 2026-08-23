# Third-party notices

The HexR Developer Tutorial (Meta OVR) is released under the MIT License (see
[LICENSE](LICENSE)). That license covers **Microtube Technologies' own work only**:

- the tutorial scenes under `Assets/Scenes/`
- the Android manifest helper at `Assets/Plugins/Android/Editor/OculusManifestBTFixer.cs`
- the project configuration under `ProjectSettings/` and `Packages/`
- the documentation in this repository

Everything listed below is redistributed with, or referenced by, this repository but is
**not** covered by that license. Each remains under its own terms, and nothing here
sublicenses any of it.

---

## The HexR package — MIT, with its own carve-outs

`Packages/com.microtube.hexr/`

The package is embedded in this repository rather than fetched from its git URL, so a
clone opens and compiles without further setup. It is MIT-licensed for Microtube
Technologies' own work, but it bundles native Bluetooth binaries that are **not**.

Read its own notices before redistributing anything from it:

- [`Packages/com.microtube.hexr/LICENSE`](Packages/com.microtube.hexr/LICENSE)
- [`Packages/com.microtube.hexr/THIRD-PARTY-NOTICES.md`](Packages/com.microtube.hexr/THIRD-PARTY-NOTICES.md)

In particular, the Android (`classes.jar`) and macOS (`BluetoothUnityAPI.bundle`)
Bluetooth binaries are marked "all rights reserved" and their redistribution status is
unresolved there too.

---

## TextMesh Pro — Unity Companion License

`Assets/TextMesh Pro/`

The TextMesh Pro "Essential Resources" (fonts, shaders, sprite assets and style sheets)
imported into the project. Copyright © Unity Technologies, distributed under the
[Unity Companion License](https://unity3d.com/legal/licenses/Unity_Companion_License).

The bundled emoji sprite sheet carries its own attribution, retained at
`Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt`.

---

## Meta XR SDK — not redistributed

`com.meta.xr.sdk.all` is declared as a dependency in `Packages/manifest.json` and is
downloaded by the Unity Package Manager into `Library/`, which is **not** committed. No
Meta code ships in this repository. It remains under the
[Oculus SDK License](https://developer.oculus.com/licenses/oculussdk/), which you accept
when you add the SDK to your own Unity account.

---

## Tutorial art — REDISTRIBUTION STATUS UNRESOLVED

`Assets/Tutorial/Art/`

The 3D models, textures, materials and audio that dress the demo scenes. These are
third-party assets. Microtube Technologies has **not** established in this repository
that it holds the right to redistribute them.

| Location | What the files themselves record |
| --- | --- |
| `Art/Hospital/Hospital Medical Office Modular/` | Models exported from **Blender 2.83.2** ("Blender (stable FBX IO)", Blender Foundation). `Models/Equipment_MD_01.fbx` still references a source texture at `C:\Users\alexi\Desktop\Medical_Keyboard.psd`, i.e. an outside artist's working directory. |
| `Art/Hospital/Mannequin 1.fbx` | **Mixamo / Adobe.** The file embeds the strings `Mixamo` and `Adobe_Art_Repo`, and its texture paths point at Mixamo's own conversion server (`/home/app/mixamo-mini/tmp/skins_…`, character `Ch36`). |
| `Art/Fountain/` | Exported with the Autodesk FBX SDK 2016. No author or license recorded in the files. |
| `Art/Effects/` | Materials, shaders and textures. No author or license recorded in the files. |

> **Status: unresolved.** These assets are believed to have come from the Unity Asset
> Store and from Mixamo, and no license text was supplied with any of them.
>
> This matters because both sets of terms restrict exactly what this repository does.
> The Unity Asset Store EULA generally permits using an asset **in your own product**,
> but not redistributing it in a form from which it can be extracted and reused — which
> is what publishing raw `.fbx`, `.png` and `.mat` files in a public repository amounts
> to. Adobe's Mixamo terms are similar: the characters and animations may be used within
> a project, but not redistributed as standalone files.
>
> Until this is settled, treat everything under `Assets/Tutorial/Art/` as **not licensed
> for redistribution**. Do not copy it out of this project into your own distributed
> products, and do not assume the MIT License in `LICENSE` extends to it — it explicitly
> does not.
>
> Resolving it means one of: locating the original Asset Store invoices and confirming
> each pack's terms; obtaining written permission from the publishers; or replacing the
> art with assets Microtube owns or that carry a redistribution-friendly license
> (CC0 / CC-BY). Nothing in the tutorial depends on this specific art — the scenes teach
> haptics, not set dressing.

---

## `Assets/Tutorial/Scripts/WindowBle/` — removed

`BLE.cs`, `Impl.cs`, `WindowHaptHandler.cs`

Recorded here because the files were previously distributed in this repository. They were
a managed C# wrapper and its P/Invoke declarations for `BleWinrtDll.dll`, and they were not
Microtube Technologies' code: they match the Unity-side sources of
[adabru/BleWinrtDll](https://github.com/adabru/BleWinrtDll) (MIT), copied in without the
copyright notice the MIT License requires to be retained.

They were also dead code — a search for their script GUIDs across every scene, prefab and
asset returned nothing, and the HexR package already wraps the same native DLL inside
`HaptGlove.dll`. All three have been deleted, which resolves the attribution problem rather
than papering over it. Anyone needing a Windows BLE wrapper should take it from adabru's
repository directly, with its notice intact.

---

## Reporting a problem with these notices

If you own any component listed here and believe it is being redistributed incorrectly,
please open an issue on this repository so it can be corrected or removed.
