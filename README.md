# HexR Unity Integration (Uses Meta OVR)

## Installation

#### Make sure you're using Unity 2021.3.26f1 or newer.
#### For projects using MRTK or OpenXR refer to : https://github.com/MicrotubeTechnologies/HaptGlove_Example
#### Clone this repo 
https://github.com/MicrotubeTechnologies/HexR-Developer-Tutorial.git
#### Then, open the HexR Developer Tutorial project in Unity.

## **Demo Scene : Basic Tutorial**

#### The **Basic Tutorial** demo scene contains the basic haptics implementations to your unity projects. 
#### There is four objects in the demo to demonstrate the different ways to incorporate haptics.

### [  In the Hierarchy  ]
#### OVRCameraRig contains the components for VR: Passthrough, Hands, Interactions etc.
#### HexR Main contains the components for the implementation of haptic glove.
#### Game contains the components of the game: table, UI panels etc.
![BasicTutorialSS](https://github.com/user-attachments/assets/e2594913-a6b9-4181-9989-22fc88832ea0)

<details>
<summary> [  Setting up HexR Main for new projects  ] </summary>

#### HexR related assets is located in Plugin folder and HexRAssets folder, copy both folder to new project.  
#### The HexR Main prefab can be found in Assets/HexRAssets/MainPrefab but requires setting up after dragging to hierarchy.
#### Left/Right Hand Physics contains the main script for mapping the HexR hand to Meta Hand and Pressure Tracker Main which contains the functions for triggering Haptics.
#### Drag the left HandGrabInteractor and HandPokeInteractor to the left Pressure Tracker Main and repeat for the right side.
#### Drag the Meta Hand Root to Physics Hand Tracking.
![RightHandSS](https://github.com/user-attachments/assets/c3157e73-38a3-4ba5-9f67-3eff2c1a5bdd)

</details>
  
### [  Type of Haptics Triggers  ]
#### There is 6 programmer zones for the haptics. Thumb, Index, Middle, Ring, Pinky, Palm.
#### 2 type of Haptics: Vibrations and Constant Pressure.
#### Haptics can be triggered either by physics collision or calling the functions.
<details>

<summary> [  Collision Based Haptics Overview  ]</summary>  

#### To allow a gameobject to trigger haptics from collision, add a collider to the gameobject and set it to "is trigger" and attach the script "Meta Hap Material" to the same gameobject.
#### To allow a gameobject to trigger vibration from collision, add a collider to the gameobject and set it to "is trigger" and attach the script "Meta Hap Vibrations" to the same gameobject.
![SS](https://github.com/user-attachments/assets/98a0f4d2-7499-46e5-b70d-af43f67c0834)


</details>

<details>
  
<summary> [  Functions Overview  ]</summary>  

#### Haptics Function can be found in the script "Pressure Tracker Main" in both the left and right hand.
### **1. To trigger single haptics :**
#### TriggerSingleHapticsIncrease(byte[] FingerTypeByte, int TargetPressure, bool ByPassHandInteractionCheck)
##### byte[] FingerTypeByte : new byte[] { a, b } | a = which zone, 0 = thumb, 1 = index, 2 = middle, 3 = ring, 4 = pinky, 5 = palm | b = air in or air out, 0 = in, 2 = out.
##### int TargetPressure : 0 to 60
##### ByPassHandInteractionCheck : True if not triggering from meta grab or poke interaction, and it will bypass the checks for which hand is interacting with the objects.

### **2. To trigger all haptics :**
#### TriggerAllHapticsIncrease(int TargetPressure)
##### int TargetPressure : 0 to 60

### **3. To trigger custom multiple haptics :**
#### TriggerCustomHapticsIncrease(byte[][] FingerTypeByte, int TargetPressure)
##### byte[][] FingerTypeByte : new byte[][] { new byte[] { a, b }, new byte[] { a, b },... }; ( any combination of the above single zone haptics )
##### a = which zone, 0 = thumb, 1 = index, 2 = middle, 3 = ring, 4 = pinky, 5 = palm | b = air in or air out, 0 = in, 2 = out.
##### int TargetPressure : 0 to 60

### **4. To remove all haptics :**
#### RemoveAllHaptics()

#### Example : The torch using events to trigger the all haptics increase.
![SS](https://github.com/user-attachments/assets/c637044b-0ad2-4521-93cf-f5722378bab8)

</details>
