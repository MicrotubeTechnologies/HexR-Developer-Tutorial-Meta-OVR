# Third-party notices

`com.microtube.hexr` is released under the MIT License (see [LICENSE](LICENSE)). That
license covers **Microtube Technologies' own work only**:

- everything under `Runtime/HexR/`, `Runtime/MetaOVR/` and `Editor/`
- the prefabs, UI and textures under `Runtime/Prefabs/` and `Runtime/UI/`
- `Runtime/Plugins/HaptGlove.dll`
- `Runtime/Plugins/ArduinoBluetoothAPILocal.dll`

The components listed below are redistributed with the package but are **not** covered by
that license. They remain under their own terms, and nothing in this package sublicenses
them.

---

## BleWinrtDll.dll — MIT

`Runtime/Plugins/BleWinrtDll.dll`

Native WinRT Bluetooth Low Energy access, P/Invoked directly on the Windows and Unity
Editor code path.

- Upstream: https://github.com/adabru/BleWinrtDll
- Copyright (c) adabru
- License: MIT

```
Permission is hereby granted, free of charge, to any person obtaining a copy of this
software and associated documentation files (the "Software"), to deal in the Software
without restriction, including without limitation the rights to use, copy, modify, merge,
publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
```

---

## BluetoothUnityAPI — Tony Abou Zaidan, all rights reserved

`Runtime/Plugins/Android/classes.jar` (`com.tony.bluetoothunityapi`)
`Runtime/Plugins/BluetoothUnityAPI.bundle` (`com.tony.BluetoothUnityAPI`)

The native Android (Java) and macOS Bluetooth implementations that
`ArduinoBluetoothAPILocal` binds to at runtime.

- Copyright © 2019 Tony Abou Zaidan. All rights reserved.
  (as declared in `BluetoothUnityAPI.bundle/Contents/Info.plist`)
- License: **proprietary — no license is granted by this package**

> **Status: unresolved.** These binaries carry an "all rights reserved" notice and ship
> with no license text. They appear to originate from a commercial Unity Asset Store
> plugin, whose EULA generally does not permit redistributing an asset in a form that
> allows extraction and reuse. Microtube Technologies has not established in this
> repository that it holds redistribution rights for them.
>
> Until that is resolved, treat these two files as **not licensed for redistribution**. Do
> not copy them out of this package into your own distributed products, and do not assume
> the MIT License in `LICENSE` extends to them — it explicitly does not.
>
> If you need Bluetooth support on Android or macOS and are distributing your own build,
> obtain your own license for the underlying plugin from its author.

### Swift runtime libraries

`BluetoothUnityAPI.bundle/Contents/Frameworks/libswift*.dylib`

The bundle embeds Apple's Swift runtime redistributables (`libswiftCore`,
`libswiftFoundation`, `libswiftDarwin` and others). These are part of the Swift project,
licensed under the **Apache License 2.0 with the Runtime Library Exception**.

- Upstream: https://github.com/apple/swift
- License: Apache-2.0 WITH Swift-exception

---

## Reporting a problem with these notices

If you own any component listed here and believe it is being redistributed incorrectly,
please open an issue on this repository so it can be corrected or removed.
