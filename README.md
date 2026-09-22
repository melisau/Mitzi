# Mitzi (Paw Path)

Mitzi is a cozy, side-scrolling 2D cat game built with **Unity 2022.3.50f1** and the Built-in Render Pipeline. Guide a cat across gaps and obstacles, collect rewards, and care for your growing group of cats at home. The project is playable but is still under development and has not been validated on physical devices.

## Current gameplay

- Choose **Draw to Play** to draw physical paths with normal, bounce, hazard, or ice brushes, or **Play with Buttons** for direct left/right, jump, and crouch controls.
- Play in street, forest, or city themes. Levels contain gaps, mounds, a finish gate, collectible birds, and running dogs. A climbable tree appears in some levels; climbing starts only when the cat is nearby and the player presses Up, W, or Jump.
- The first five levels use introductory layouts. Later levels vary gap and mound placement deterministically by level number, so replaying the same level keeps its layout.
- Reach the finish gate to advance. Falling or touching a hazard opens a failure screen with a retry option. A new cat can be rescued every five completed levels. The default roster is Mitzi, Pamuk, Kömür, Tarçın, Ada, and Moka.
- At home, select a cat and tap the floor to send it there, or tap an interactive item such as a food bowl, litter box, or cat tree. Buy, place, move, and flip furniture in the shop and home editor.
- Music and sound effects have separate mute controls. Progress is saved locally through Unity `PlayerPrefs` as JSON, including level progress, Love Points, selected and unlocked cats, owned/placed items, and per-cat care values.

## Controls

| Mode | Input |
| --- | --- |
| Drawing | Draw on the screen with a finger or mouse; choose a brush or eraser from the toolbar. |
| Direct control | Use the on-screen Left, Right, Jump, and Crouch buttons. Keyboard controls are also available in the Unity Editor. |
| Home | Tap a cat to select it, tap the floor to move it, or tap an item to interact. Use Edit Home to reposition furniture. |

## Open the project

1. In Unity Hub, add this repository as an **existing project** and open it with Unity **2022.3.50f1**.
2. Open `Assets/Scenes/PawPath.unity` and press **Play**.
3. If rebuilding a starter scene for a new setup, use **Paw Path > Build Starter Scene**. This regenerates the starter scene and catalog; do not use it casually on a customized project.

`Assets/Scenes/HubTest.unity` is a separate development scene. **Paw Path > Reset Save Data** deletes local progress for testing.

## Project structure

| Area | Responsibility |
| --- | --- |
| `Assets/_Game/Scripts/Core` | Single-scene game flow, runtime setup, camera, and local saves. |
| `Assets/_Game/Scripts/Levels` | Level generation, goals, birds, dogs, trees, and cat unlocks. |
| `Assets/_Game/Scripts/Drawing` and `Gameplay` | Drawable colliders, surfaces, and direct/mobile controls. |
| `Assets/_Game/Scripts/Hub` and `UI` | Home interactions, furniture editing, shop, and screens. |
| `Assets/_Game/Scripts/Data` and `Content` | `ScriptableObject` definitions and catalog data. |
| `Assets/_Game/Scripts/Audio` | Theme music, effects, and separate mute settings. |

## Verification and current limitations

- Use **Mitzi > Validate Course Geometry** in the Unity Editor to check road, gap, and mound bounds for levels 1–100. This is a geometry validation command, **not** a full automated gameplay test suite.
- Follow the [QA checklist](QA_CHECKLIST.md) for falling, reaching the goal, cat/item interactions, save loading, screen ratios, and real-device checks. Android and iOS device results have not yet been recorded.
- Hunger, water, and affection are stored separately for each cat, but **they do not currently decrease over time**. Starting a level checks the shared Love Points balance (at least 20), not the selected cat's needs. Care therefore mainly provides daily rewards at present.
- Some non-Mitzi cats do not yet have complete animation sets. Visual alignment and memory use of runtime road-corner masks still need device profiling.

The current artwork, audio, and catalog references live under `Assets/_Game`. Keep gameplay geometry and colliders separate from decorative sprites when adding new environment art.
