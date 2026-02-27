# HexR Unity Integration (Uses Meta OVR) ℹ️

## 🚀 Getting Started

### QuickLinks:
- For projects using **`Open XR`**, refer to the official [HexR-developer-tutorial-XR](https://github.com/MicrotubeTechnologies/HexR-developer-tutorial-XR).
- For Pico headset compatibility, refer to the pico branch in the official [HexR-developer-tutorial-XR](https://github.com/MicrotubeTechnologies/HexR-developer-tutorial-XR).
- For plugin in **`Python`**, refer to the official [HaptGlovePython](https://github.com/MicrotubeTechnologies/HexR-developer-tutorial-XR](https://github.com/MicrotubeTechnologies/HaptGlovePython/tree/main)).

### Prerequisites:
- ✅ Minimum Unity version **Unity 2021.3.26f1**.
- Uses the **`HaptGlove`**  and **`ArduinoBluetoothApiLocal`** plugin.
  
### Steps to Get Started:
1. **Clone this repository:**
   [HexR Developer Tutorial Repository](https://github.com/MicrotubeTechnologies/HexR-Developer-Tutorial.git)

2. **Open the HexR Developer Tutorial project in Unity.**
   
4. **Switch to the Android platform in build settings.**
   
3. **Navigate to the Scene folder to explore the different tutorial scenes.**

### Adding HexR to your projects:
1. **Copy the Plugins folder from this project to your new project.**

2. **Copy the HexRAssets folder from this project to your new project.**\

### Bluetooth Permissions:
1. **For HexR to be connected and discoverable by the Meta Quest device, **`Location and Nearby Device`** permissions need to be given in the headset**

2. **In Meta Quest > Settings > Privacy and Security > App Permission >  Location = Precise + Nearby Device = turn on**
---

<details>
  <summary>🔍 HaptGlove Plugin Structure</summary>

## 🧤 **`HaptGlove` Plugin Overview**

The **`HaptGlove`** plugin contains the core logic for interacting with the glove, including applying haptics/vibrations and handling various Bluetooth-related functions.

The **`HexR`** system is built on top of the `HaptGlove` plugin. The breakdown below highlights its core components, designed to help you integrate or develop your own projects using our foundational methods.

---

## **`HaptGloveHandler` Class**

This is the **primary controller** for calling functions to interface with the HexR glove.

⚠️ **Note:** You’ll need to create **two instances** of this class by attaching it to two separate GameObjects in your Unity scene — one for each hand. Each instance manages the haptics and Bluetooth connection independently.

📦 **Namespace:** `using HaptGlove;`

---

### 📡 Bluetooth-Related Functions

| Function           | Description                                                                 |
|--------------------|-----------------------------------------------------------------------------|
| `BTConnection()`   | Initiates Bluetooth connection with the glove device.                      |
| `GetAirPressure()` | Returns `int[]` representing air pressure data for each finger (0–5).      |
| `GetBatteryLevel()`| Returns a `float` representing the glove's current battery level.          |
| `BTSend(byte[] data)` | Sends raw `byte[]` data to the device (used to trigger/stop haptics).     |

---

### 🎛 Haptics-Related Functions

| Function           | Description                                                                 | Input Parameters                                                                                   |
|--------------------|-----------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------|
| `HEXRPressure()`    | Triggers haptic pressure; supports multiple fingers via array input.        | - `finger`: Use `haptics.Finger` enum (e.g., `Thumb`, `Index`)<br>- `state`: `true`/`false`<br>- `intensity`: 0.1–1.0<br>- `speed`: 0.1–1.0 |
| `HEXRVibration()`   | Triggers vibration effects; supports multiple fingers via array input.      | - `finger`: Use `haptics.Finger` enum<br>- `state`: `true`/`false`<br>- `frequency`: 0.1–40.0<br>- `intensity`: 0.1–1.0<br>- `peakRatio`: 0.2–0.8<br>- `speed`: 0.1–1.0 |

</details>

<details>
  <summary>🧩 HexR code structure</summary>

### The **`HexR`** system is built on top of the `HaptGlove` plugin, to simplify and improve the experience of using the HexR glove.
### Learn more about the HexR code structure and architecture 💡

<details>
  <summary>1. Hand Tracking (PhysicsHandTracking)</summary>

#### The HexR hand supports both the **OpenXR** and **Meta OVR** hand skeleton structure.  
Here’s a summary of the differences in hand structure:
- **OpenXR Hand Skeleton**
- **Meta OVR Hand Skeleton**  
The `PhysicsHandTracking` script mimics the position/rotation of either the OpenXR or Meta OVR hands, the script is attached to the Left/Right hand physics component under HexR Main.

![Hand Skeleton](https://github.com/user-attachments/assets/2585a044-ae44-4814-88e5-abe61c876f8e)

If a custom hand structure is used, you will have to recreate the `PhysicsHandTracking` to track each joint.

</details>

<details>
    <summary>2. HexR Bluetooth Connection Manager (HaptGloveManager + HaptGloveUI)</summary>

#### The `HaptGloveManager` and HaptGloveUI handle the Bluetooth connection using the HexR plugins.  
- Call the function ConnectRightBT() or ConnectLeftBT() to intitiate right or left HexR connection.
- The OnConnected, OnConnectionFail, and OnDisconnected events can be found in HaptGloveManager and can be edited to suit your programme needs.

#### Unity inspector set up.  
- In the inspector, ensure the XR framework is set to OpenXR and click the **"Auto Set Up HexR"** button.
- If the setup is successful, there should be no missing links in the inspector for HexR main, Left Hand Physics, and Right Hand Physics.
- Check the debug log to ensure the setup is successful. 

![Setup Image](https://github.com/user-attachments/assets/f09f713f-fa81-484e-8646-bbe830ecce35)

#### HaptGloveManager Settings:
- **XR Framework:**  
  - Do select only the meta OVR Framework as there will be missing assets if OpenXR is selected, for projects using OpenXR refer to the OpenXR developer tutorial in the link above.

- **HexR Panel Component:**  
  - The floating HexR Panel controls the connection to the HexR glove.  
  
</details>

<details>
  <summary>3. Haptics Controller (PressureTrackerMain)</summary>

#### The `PressureTrackerMain` script contains all of the functions to trigger haptics.
#### There is 6 Channels in the HexR glove allowing haptics to be triggered for each finger and the palm

- Overview
  - Functions are categorized by **single-channel** or **multi-channel** triggers.
  - Haptics intensity range from 0.1 (no haptics) to 1 (Max haptics).
  - Refer to the demo scene to see examples of how these functions are used.

- Function : IsHandNear()
  - This is use to check if the user left or right hand is grabbing or near the target object, so that haptics is correctly triggered at the right timme and by the right hand.
    
- Function : CustomSingleHaptics ( Haptics.Finger finger, bool states, float intensity, float speed, bool ByPassHandCheck )
  - Haptics.Finger = which finger is to be triggered: index,middle,ring,pinky,thumb,palm
  - states : true = haptics in , false = haptics out
  - intensity : 0.1 - 1 , min haptics - max haptics
  - speed : 0.1 - 1 , slowly increase haptics vs fast increase haptics
  - ByPassHandCheck : true = will trigger haptics without checking IsHandNear()

- Function : CustomSingleVibrations(Haptics.Finger finger, bool states, float intensity, float frequency, bool ByPassHandCheck)
  - Haptics.Finger = which finger is to be triggered: index,middle,ring,pinky,thumb,palm
  - states : true = haptics in , false = haptics out
  - frequency : 0.1 - 40 
  - intensity : 0.1 - 1 , min haptics - max haptics
  - ByPassHandCheck : true = will trigger haptics without checking IsHandNear()
</details>

<details>
  <summary>4. HexR Grab and Pinch (HexRGrabbable)</summary>

#### The `HexRGrabbable` script enables objects to be picked up by the HexR hands.
#### This is optional as you can also use the grab/pinch provided by **Meta OVR**, however the haptics trigger and physics of grab will be different. Give both a try to see which is more suitable for you.
To set up `HexRGrabbable`:
1. Ensure the object has a **Collider (Trigger)** and **Rigidbody** attached to the same GameObject.
2. Since the interaction is physics-based, adjust the size of the collider to improve grab/pinch behavior.
3. Optionally, attach an additional collider if you want the object to interact with other GameObjects.

![Grabbable Example](https://github.com/user-attachments/assets/3fadad3e-80d7-4f57-9186-a63d4ebc125f)

#### HexRGrabbable Settings:
- **Type of Grab:**  
  - **Palm Grab:** Requires the palm and at least one finger to touch the object (thumb not required).
  - **Pinch Grab:** Requires the thumb and at least one finger to touch the object (palm not required).

- **Gravity Bool:**  
  If enabled, gravity will affect the object when released.

- **Haptic Slider:**  
  Controls the strength of the haptic feedback during grab or pinch.  
  - `0`: No haptics  
  - `60`: Maximum haptics strength

- **On Grab Event:**  
  Trigger an event when the object is grabbed or pinched.

- **On Release Event:**  
  Trigger an event when the object is released.

</details>

<details>
  <summary>5. Creating Haptic Zones (SpecialHaptics)</summary>

#### The `SpecialHaptics` script enables objects to trigger a custom haptic effect when touch.

![image](https://github.com/user-attachments/assets/15bc96c7-db42-452c-adeb-68b657984802)

To set up `SpecialHaptics`:
1. Ensure the object has a **Collider (Trigger)** attached to the same GameObject.
2. Since the interaction is physics-based, adjust the size of the collider for the haptic zone.
3. Select the type of Haptics in the inspector.

#### SpecialHaptics Settings:
- **Custom Vibrations:**  
  - When activated will create the vibration effects.
  - *Frequency Speed:* the frequency of the vibrations.
  - *Haptic Strength:* the strength of the vibrations.
- **Custom Haptics:**
  - When activated/touch will trigger a constant haptic.
  - *Haptic Pressure:* slider to adjust strength of haptic. 10 = weakest, 60 = strongest.
- **Fountain Effect:**  
  - When activated will simulate running water.
 
- **Raindrop Effect:**  
  - When activated will simulate raindrops with random haptics trigger.
    
- **Heart Beat Effect:**  
  - When activated will simulate beating heart, but only affects fingers and not palm.
    
- **Hand Squeeze Effect:**  
  - When activated will allows the player to trigger an event by squeezing the hand
  - `0.1`: Fully closed hand  
  - `1`: Fully open hand
</details> 

<details>
  <summary>6. Determine if hand is near (ProximityCheck)</summary>

### (Optional for Meta OVR)
#### If you are using Meta OVR, you can use the native handgrabinteractor/handpokeinteractor to check which hand is grabbing/hovering instead of this ProximityCheck.

#### The `ProximityCheck` script checks if the left or right hand is near the target object.
#### Haptics is only trigger when the hand is near the object.
#### Place the `ProximityCheck` prefab as a child of the target object and click the auto set up.
#### You should adjust the size of your trigger collider to ensure that it is optimise.


</details> 
</details>

&nbsp;


<details>
<summary> Demo Scene : Basic Tutorial </summary>
 
## **Demo Scene : Basic Tutorial **

#### The **Basic Tutorial ** demo scene contains the implementation to grab and pinch object using HexR grabbing and pinching.

<img width="1255" height="973" alt="image" src="https://github.com/user-attachments/assets/fcddd60f-3875-41c8-b4c6-3ef09bd3daec" />


- Foam Object ☁️
  - The Meta handgrabinteractor(All) is used for the interaction.
  - A Haptic Zone with the special haptics script is added in the child of the foam gameobject to trigger haptics when touch.
  - The Haptic is triggered based on physics colliders and may not be as precise, try to match the object shape with your collider(t%rigger) to increase pricision.

- Key Object 🔑
  - The Meta handgrabinteractor(pinch) is used for the interaction.
  - Meta's Pointable Unity Event Wrapper script is use to trigger the pinch haptics the instance the key is picked up.
  - Haptics is triggered by event and not physical colliders.
    
![image](https://github.com/user-attachments/assets/21f6616d-c075-4b49-a4a3-1a2ef8ad2982)

- Torch Object 🔥
  - The Meta handgrabinteractor(palm) is used for the interaction.
  - Meta's Pointable Unity Event Wrapper script is use to trigger the haptics the instance the torch is picked up.
  - Haptics is triggered by event and not physical colliders.
  - The SpecialHaptics is attach to the haptic zone(child gameobject) to allow vibrations to be triggered when touching the fire.

- Button Object 🎮
  - The Meta pokeinteractor is used for the interaction.
  - Meta's Pointable Unity Event Wrapper script is use to trigger the haptics the instance the button is pressed.

Take a look at Open XR documentation to understand how to implement their hands interactions.
</details>

<details>
<summary> Demo Scene : Rain and Fountain Tutorial </summary>
 
## **  Demo Scene : Rain and Fountain Tutorial ⛲ **

#### The **Rain and Fountain Tutorial** demo scene contains the haptics implementations for using triggers and colliders to trigger haptics. 
#### There is a haptic zone in the fountain and rain clouds.
#### To create a haptic zone simply attach the `SpecialHaptics` Script and a collider(trigger) to a gameobject.

![image](https://github.com/user-attachments/assets/961d80fa-59ed-4431-a33e-46df43450ca8)
