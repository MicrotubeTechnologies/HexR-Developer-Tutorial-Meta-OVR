# HexR Unity Integration — Meta OVR ℹ️

A Unity 6 tutorial project for the **HexR** haptic glove, built against the **Meta XR SDK
(OVR)**. It ships ready-to-run demo scenes plus the embedded `com.microtube.hexr` package,
so you can feel haptics on a Quest before writing any code of your own.

> [!NOTE]
> For **`OpenXR`** projects, **Pico** headset compatibility, and the **`Python`** library,
> see the official [HexR documentation](https://microtube.tech/hexr-docs).

---

## 📑 Contents

| Section | What's inside |
|---|---|
| [🚀 Getting Started](#-getting-started) | Prerequisites, clone & open, Bluetooth permissions |
| [🧤 HaptGlove Plugin Reference](#-haptglove-plugin-reference) | The low-level API — `HaptGloveHandler`, Bluetooth and haptics calls |
| [🧩 HexR Code Structure](#-hexr-code-structure) | The components you actually wire up in a scene |
| [🎬 Demo Scenes](#-demo-scenes) | Walkthroughs of the tutorial scenes |
| [📄 License](#-license) | MIT, plus third-party notices |

---

## 🚀 Getting Started

### ✅ Prerequisites

| Requirement | Details |
|---|---|
| **Unity 6** (6000.0 or newer) | Required by the Meta XR SDK this project pins — `com.meta.xr.sdk.all` 205.0.0 declares `"unity": "6000.0"`. The `com.microtube.hexr` package itself supports **Unity 2022.3 and newer**; it is the Meta SDK, not HexR, that sets the floor here. |
| **Hand tracking backend** | Either **Meta Interaction SDK** (used by this tutorial) or **OpenXR**. The package requires neither up front — install one via **HexR → HexR Tools → Project Setup**. |
| **Build target** | **Android**, for a Meta Quest headset. |

### 📥 Setup

1. **Clone this repository:**

   ```bash
   git clone https://github.com/MicrotubeTechnologies/HexR-Developer-Tutorial-Meta-OVR.git
   ```

2. **Open the project in Unity 6.**

3. **Switch to the Android platform** — *File → Build Settings → Android → Switch Platform*.

4. **Open `Assets/Scenes/`** to explore the tutorial scenes — start with *1.Basic Tutorial*.

### 📶 Bluetooth Permissions

The glove is discovered over Bluetooth, which on Quest sits behind location permissions.
Grant these **in the headset**, not in Unity:

1. Go to **Meta Quest → Settings → Privacy and Security → App Permission**.
2. Set both of the following:

   | Permission | Value |
   |---|---|
   | **Location** | Precise |
   | **Nearby Device** | On |

> [!WARNING]
> Without both permissions the glove will not be discoverable by the headset, and the
> connection will fail.

<details>
  <summary>📦 Adding HexR to another project (outside this tutorial)</summary>

<br>

HexR ships as a Unity package — **`com.microtube.hexr`**. Don't copy folders out of this
project; install the package instead, so you get updates and the correct assembly setup.

> [!IMPORTANT]
> The package is **not on a registry**, so **Add package by name** fails with *"Unable to
> find package"*. `com.microtube.hexr` names the package; it is not somewhere Unity can
> fetch it from. Use the git URL below.

1. **Window → Package Manager → + → Add package from git URL…**, and paste:

   ```
   https://github.com/MicrotubeTechnologies/com.microtube.hexr.git
   ```

   Or add it straight to your `Packages/manifest.json`:

   ```json
   "com.microtube.hexr": "https://github.com/MicrotubeTechnologies/com.microtube.hexr.git"
   ```

   `main` is kept releasable, so this URL always resolves the latest release. Append a tag
   (`#v0.4.0`) if you need a build to stay reproducible. UPM caches a git dependency by the
   ref it resolved, so an unpinned URL updates only when you ask Package Manager to update
   the package.

   > [!IMPORTANT]
   > The **`HaptGlove`** plugin and its Bluetooth transports come bundled — there is nothing
   > to copy in by hand. If your project already has its own `HaptGlove.dll` or
   > `ArduinoBluetoothApiLocal.dll` under `Assets/Plugins/`, **delete them first**: two
   > copies of the same assembly is a hard compile error.

2. **HexR → HexR Tools → Project Setup** — choose **Meta OVR** or **Open XR** and install
   whatever it reports missing. The package depends on neither backend, so nothing pulls
   them in for you.

3. **HexR → Create HexR Rig →** your backend, then **HexR → Auto Setup Scene** and
   **HexR → Validate Scene Setup**.

> [!NOTE]
> On **Open XR** you must also add a `ProximityCheck` (with a trigger collider) to each
> object the hand should feel — it is the only thing that sets "hand is near" on that
> backend, and without one haptics never fire. Meta OVR gets this from its grab/poke
> interactors instead. *Validate Scene Setup* flags it.

📖 Full package documentation: [`Packages/com.microtube.hexr/README.md`](Packages/com.microtube.hexr/README.md)

</details>

---

## 🧤 HaptGlove Plugin Reference

The **`HaptGlove`** plugin holds the core logic for talking to the glove — applying
haptics and vibrations, and handling Bluetooth. The **`HexR`** system is built on top of
it; the breakdown below is for when you want to build on those foundations directly.

<details>
  <summary>🔍 <code>HaptGloveHandler</code> class — Bluetooth &amp; haptics API</summary>

<br>

`HaptGloveHandler` is the **primary controller** for interfacing with the HexR glove.

📦 **Namespace:** `using HaptGlove;`

> [!WARNING]
> You need **two instances** of this class, attached to two separate GameObjects in your
> scene — one per hand. Each instance manages its own haptics and Bluetooth connection.

#### 📡 Bluetooth functions

| Function | Description |
|---|---|
| `BTConnection()` | Initiates the Bluetooth connection with the glove. |
| `GetAirPressure()` | Returns an `int[]` of air pressure data for each finger (0–5). |
| `GetBatteryLevel()` | Returns a `float` for the glove's current battery level. |
| `BTSend(byte[] data)` | Sends raw `byte[]` data to the device — used to trigger and stop haptics. |

#### 🎛 Haptics functions

| Function | Description | Input parameters |
|---|---|---|
| `HEXRPressure()` | Triggers haptic pressure; supports multiple fingers via array input. | `finger` — `haptics.Finger` enum (e.g. `Thumb`, `Index`)<br>`state` — `true`/`false`<br>`intensity` — 0.1–1.0<br>`speed` — 0.1–1.0 |
| `HEXRVibration()` | Triggers vibration effects; supports multiple fingers via array input. | `finger` — `haptics.Finger` enum<br>`state` — `true`/`false`<br>`frequency` — 0.1–40.0<br>`intensity` — 0.1–1.0<br>`peakRatio` — 0.2–0.8<br>`speed` — 0.1–1.0 |

</details>

---

## 🧩 HexR Code Structure

The **`HexR`** system wraps the `HaptGlove` plugin to simplify and improve the experience
of using the glove. These are the components you'll wire up in a scene — expand whichever
one you're working on.

<details>
  <summary>1️⃣ Hand Tracking — <code>PhysicsHandTracking</code></summary>

<br>

The HexR hand supports both the **OpenXR** and **Meta OVR** hand skeleton structures:

- **OpenXR** hand skeleton
- **Meta OVR** hand skeleton

`PhysicsHandTracking` mimics the position and rotation of either skeleton. It is attached
to the Left/Right hand physics component under **HexR Main**.

![Hand Skeleton](https://github.com/user-attachments/assets/2585a044-ae44-4814-88e5-abe61c876f8e)

> [!NOTE]
> If you use a custom hand structure, you will have to recreate `PhysicsHandTracking` to
> track each joint yourself.

</details>

<details>
  <summary>2️⃣ Bluetooth Connection Manager — <code>HexRManager</code></summary>

<br>

`HexRManager` handles the Bluetooth connection through the HexR plugins.

| Member | Purpose |
|---|---|
| `ConnectRightBT()` / `ConnectLeftBT()` | Initiate the right or left HexR connection. |
| `OnConnected`, `OnConnectionFail`, `OnDisconnected` | Connection events — edit them to suit your program's needs. |
| `HexRManager.Instance` | Static handle to the manager from any script. |

#### 🛠 Inspector setup

1. Set **XR Framework** to **Meta OVR** (this is the Meta OVR tutorial).
2. Click **Auto Set Up HexR**.
3. Confirm there are no missing links on **HexR Main**, **Left Hand Physics** and
   **Right Hand Physics**.
4. Check the debug log to confirm the setup succeeded.

![Setup Image](https://github.com/user-attachments/assets/f09f713f-fa81-484e-8646-bbe830ecce35)

#### ⚙️ HexRManager settings

| Setting | Notes |
|---|---|
| **XR Framework** | Select **Meta OVR** — this project's scenes are built against it, and choosing OpenXR leaves assets unresolved. For OpenXR projects, see the [HexR documentation](https://microtube.tech/hexr-docs). |
| **HexR Panel Component** | The floating HexR Panel that controls the connection to the glove. |

</details>

<details>
  <summary>3️⃣ Haptics Controller — <code>PressureTrackerMain</code></summary>

<br>

`PressureTrackerMain` contains all the functions that trigger haptics. The glove has
**6 channels** — one per finger, plus the palm.

- Functions are categorised as **single-channel** or **multi-channel** triggers.
- Haptic intensity ranges from `0.1` (no haptics) to `1` (max haptics).
- Refer to the demo scenes for working examples.

#### `IsHandNear()`

Checks whether the user's left or right hand is grabbing or near the target object, so
haptics fire at the right time and on the right hand.

#### `CustomSingleHaptics(Haptics.Finger finger, bool states, float intensity, float speed, bool ByPassHandCheck)`

| Parameter | Range / Type | Meaning |
|---|---|---|
| `finger` | `Haptics.Finger` | Which channel to trigger: index, middle, ring, pinky, thumb, palm. |
| `states` | `bool` | `true` = haptics in, `false` = haptics out. |
| `intensity` | 0.1 – 1 | Min → max haptics. |
| `speed` | 0.1 – 1 | Slowly increase haptics vs. fast increase. |
| `ByPassHandCheck` | `bool` | `true` triggers haptics without checking `IsHandNear()`. |

#### `CustomSingleVibrations(Haptics.Finger finger, bool states, float intensity, float frequency, bool ByPassHandCheck)`

| Parameter | Range / Type | Meaning |
|---|---|---|
| `finger` | `Haptics.Finger` | Which channel to trigger: index, middle, ring, pinky, thumb, palm. |
| `states` | `bool` | `true` = haptics in, `false` = haptics out. |
| `intensity` | 0.1 – 1 | Min → max haptics. |
| `frequency` | 0.1 – 40 | Vibration frequency. |
| `ByPassHandCheck` | `bool` | `true` triggers haptics without checking `IsHandNear()`. |

</details>

<details>
  <summary>4️⃣ Grab and Pinch — <code>HexRGrabbable</code></summary>

<br>

`HexRGrabbable` lets objects be picked up by the HexR hands.

> [!TIP]
> This is optional — you can also use the grab/pinch provided by **Meta OVR**, though the
> haptic triggering and grab physics differ. Try both and see which suits you.

#### 🛠 Setup

1. Ensure the object has a **Collider (Trigger)** and a **Rigidbody** on the same GameObject.
2. Because the interaction is physics-based, adjust the collider size to improve grab and
   pinch behaviour.
3. Optionally attach an additional collider if the object should interact with other
   GameObjects.

![Grabbable Example](https://github.com/user-attachments/assets/3fadad3e-80d7-4f57-9186-a63d4ebc125f)

#### ⚙️ HexRGrabbable settings

| Setting | Behaviour |
|---|---|
| **Type of Grab → Palm Grab** | Requires the palm and at least one finger to touch the object (thumb not required). |
| **Type of Grab → Pinch Grab** | Requires the thumb and at least one finger to touch the object (palm not required). |
| **Gravity Bool** | If enabled, gravity affects the object once released. |
| **Haptic Slider** | Strength of the haptic feedback during grab or pinch — `0` = no haptics, `60` = maximum. |
| **On Grab Event** | Fires when the object is grabbed or pinched. |
| **On Release Event** | Fires when the object is released. |

</details>

<details>
  <summary>5️⃣ Haptic Zones — <code>SpecialHaptics</code></summary>

<br>

`SpecialHaptics` makes an object trigger a custom haptic effect when touched.

![image](https://github.com/user-attachments/assets/15bc96c7-db42-452c-adeb-68b657984802)

#### 🛠 Setup

1. Ensure the object has a **Collider (Trigger)** on the same GameObject.
2. Because the interaction is physics-based, adjust the collider size to shape the haptic zone.
3. Select the type of haptics in the inspector.

#### ⚙️ SpecialHaptics effects

| Effect | Behaviour | Parameters |
|---|---|---|
| **Custom Vibrations** | Creates vibration effects. | *Frequency Speed* — frequency of the vibrations<br>*Haptic Strength* — strength of the vibrations |
| **Custom Haptics** | Triggers a constant haptic on touch. | *Haptic Pressure* — `10` = weakest, `60` = strongest |
| **Fountain Effect** | Simulates running water. | — |
| **Raindrop Effect** | Simulates raindrops, with randomised haptic triggers. | — |
| **Heart Beat Effect** | Simulates a beating heart — affects the fingers only, not the palm. | — |
| **Hand Squeeze Effect** | Lets the player trigger an event by squeezing their hand. | `0.1` = fully closed hand<br>`1` = fully open hand |

</details>

<details>
  <summary>6️⃣ Hand Proximity — <code>ProximityCheck</code> <em>(optional on Meta OVR)</em></summary>

<br>

`ProximityCheck` checks whether the left or right hand is near the target object. Haptics
only fire when a hand is near.

#### 🛠 Setup

1. Place the `ProximityCheck` prefab as a **child of the target object**.
2. Click **Auto Set Up**.
3. Adjust the trigger collider size so the zone is well fitted to the object.

> [!NOTE]
> **Optional on Meta OVR.** With Meta OVR you can use the native `HandGrabInteractor` /
> `HandPokeInteractor` to determine which hand is grabbing or hovering, instead of
> `ProximityCheck`. On **OpenXR** it is required — see the note in *Adding HexR to another
> project* above.

</details>

---

## 🎬 Demo Scenes

<details>
  <summary>🧪 Demo Scene: Basic Tutorial</summary>

<br>

The **Basic Tutorial** scene demonstrates grabbing and pinching objects with HexR.

<img width="1255" height="973" alt="Basic Tutorial scene" src="https://github.com/user-attachments/assets/fcddd60f-3875-41c8-b4c6-3ef09bd3daec" />

| Object | Interactor used | How haptics are triggered |
|---|---|---|
| **Foam** ☁️ | Meta `HandGrabInteractor` (All) | A Haptic Zone with `SpecialHaptics` on a child GameObject fires on touch. Driven by physics colliders, so it may be less precise — match the collider to the object's shape to improve it. |
| **Key** 🔑 | Meta `HandGrabInteractor` (Pinch) | Meta's *Pointable Unity Event Wrapper* fires the pinch haptics the instant the key is picked up — by event, not by collider. |
| **Torch** 🔥 | Meta `HandGrabInteractor` (Palm) | *Pointable Unity Event Wrapper* fires on pickup (event-driven). `SpecialHaptics` on the child haptic zone adds vibrations when touching the fire. |
| **Button** 🎮 | Meta `PokeInteractor` | *Pointable Unity Event Wrapper* fires the haptics the instant the button is pressed. |

![Key object setup](https://github.com/user-attachments/assets/21f6616d-c075-4b49-a4a3-1a2ef8ad2982)

> [!TIP]
> Using OpenXR instead? See the [HexR documentation](https://microtube.tech/hexr-docs) for
> how to implement its hand interactions.

</details>

<details>
  <summary>⛲ Demo Scene: Rain and Fountain Tutorial</summary>

<br>

The **Rain and Fountain Tutorial** scene shows how triggers and colliders drive haptics.
There is a haptic zone in both the fountain and the rain clouds.

#### 🛠 Creating a haptic zone

1. Attach the `SpecialHaptics` script and a **Collider (Trigger)** to a GameObject.
2. In the inspector, click **Find Hand Physics** to set up the class.
3. The colliders on the fingers and palm then trigger `SpecialHaptics`, which fires the haptics.

<img width="1255" height="736" alt="Rain and Fountain scene" src="https://github.com/user-attachments/assets/4b830af2-1cf7-4f70-81a5-43334b3ddcfb" />

| Object | Setup |
|---|---|
| **Rain** ☁️ | A Haptic Zone with `SpecialHaptics` is added as a child of the Rain object. Several effect types are available in the inspector — the **Raindrop Effect** was selected here. |

<img width="647" height="234" alt="Raindrop effect inspector" src="https://github.com/user-attachments/assets/37bb7935-e3b9-4ad9-a722-119ce696cff7" />

</details>

---

## 📄 License

MIT — see [LICENSE](LICENSE).

That covers Microtube Technologies' own work here: the tutorial scenes, the helper scripts
under `Assets/Tutorial/Scripts/`, the project configuration and this documentation.

It does **not** cover the embedded HexR package (which carries its own LICENSE and
third-party notices), Unity's TextMesh Pro resources, or the demo art under
`Assets/Tutorial/Art/`. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for the full
list and their terms — in particular the demo art, whose redistribution status is
unresolved.
