# Arrow Block Buster - Unity Setup Guide

Follow these steps to configure your scene and prefabs to get the game running.

## 1. Scene Setup
1.  **Create a New Scene** (e.g., `MainGame`).
2.  **Create an Empty GameObject** named `GameManager`.
    *   Add the `GameManager` script to it.
    *   Add the `LevelGenerator` script to it.
3.  **Create Container Objects** (Empty GameObjects) to hold generated items:
    *   Create `WallContainer` (Position it at approx `0, 2, 0`).
    *   Create `PuzzleContainer` (Position it at approx `0, -3, 0`).
    *   Create `SlotsContainer` (Position it at `0, -8, 0` or set up as UI).
        *   *Note*: If you want the Slots to be UI (Canvas), create a **Canvas** and create a Panel/HorizontalLayoutGroup for `SlotsContainer`. If keeping it simple World Space, just an empty object is fine.
4.  **Assign References**:
    *   Select `GameManager`.
    *   Drag `WallContainer`, `PuzzleContainer`, and `SlotsContainer` into their respective fields in the Inspector.
    *   Drag the `GameManager` object itself into the `Level Generator` field (since both scripts are on it).

## 2. Prefab Creation
Create the following prefabs and assign them to the `GameManager`.

### A. Block Prefab (The Wall)
1.  Create a **3D Cube** or **2D Sprite (Square)** based on your preference.
2.  Name it `Block`.
3.  Add the `Block.cs` script.
4.  **Inspector Setup**:
    *   **Color Definitions**: Expand the array and add 6 colors (Red, Blue, Green, Yellow, Purple, Orange).
    *   **Visuals**: Drag your `MeshRenderer` (if Cube) or `SpriteRenderer` (if 2D) into the script field.
5.  **Important**: If using 3D Cube, ensure it's size is roughly 1x1x1. Set the Scale to `0.9, 0.9, 0.9` for a little gap between blocks.
6.  Drag it into your `Prefabs` folder and delete from scene.

### B. Arrow Prefab (The Puzzle)
1.  Create an Empty GameObject named `Arrow`.
2.  Add the `Arrow.cs` script.
3.  **Visuals**:
    *   Create a child object named `Body` (Sprite/Cube). This will be the long part.
    *   Create a child object named `Head` (Sprite/Triangle). This indicates direction.
    *   **Pivot Logic**: The `Arrow` object is the "Head" position.
        *   Position the `Head` child at `(0, 0, 0)`.
        *   Position the `Body` child so it extends roughly downwards (locally).
        *   *Alternative*: Just use one Sprite facing **UP**. The code will rotate it.
        *   **Crucial**: The code scales `Body.localScale.y` equal to `Length`. Ensure your Body sprite pivot is at the **Bottom** so it grows upwards, OR at **Top** so it grows downwards?
        *   *Code check*: `GetOccupiedCells` assumes "Back" is opposite to direction. If Arrow Points UP, Body goes DOWN.
        *   So, setup your visual so "Forward/Up" is positive Y.
4.  **Inspector Setup**:
    *   Assign `Head Renderer` and `Body Renderer`.
    *   **Color Definitions**: Copy the same 6 colors from Block.
    *   **Collider**: Add a `BoxCollider` or `BoxCollider2D` to the `Arrow` root object. Size it carefully so Raycasts can hit it.
5.  Save as Prefab.

### C. Slot Prefab (The Turret Queue)
1.  **UI Method (Recommended)**:
    *   In your Canvas (SlotsContainer), create an **Image**.
    *   Add `Slot.cs` script.
    *   Add a child **Text (TMP)** for the Ammo Count.
    *   **Inspector**: Assign the `Image` to `Bg Image` and Text to `Ammo Text`.
    *   **Color Definitions**: Copy the 6 colors.
2.  **World Method**:
    *   Create a Cube/Sprite `Slot`.
    *   Add `Slot.cs`.
    *   Add a `3D Text` or World Canvas for ammo.
3.  Save as Prefab.

## 3. Final Configuration
1.  Select `GameManager`.
2.  **Assign Prefabs**: Drag your new `Block`, `Arrow`, and `Slot` prefabs into the inspector fields.
3.  **Settings**:
    *   `Cell Size`: `1` (if your blocks/arrows are 1 unit big).
    *   `Fire Rate`: `0.2` (Adjust speed of wall destruction).
4.  **Camera**:
    *   If 2D: Set orthographic size to approx `10`.
    *   If 3D: Move Camera to `Pos (3, 5, -15)` looking at `(3, 2, 0)`. Adjust until you see both containers.

## 4. Playing
*   Press **Play**.
*   The `GameManager` should generate a random level.
*   Click arrows to collect them.
*   Watch blocks get destroyed!

## dotTween Note
If you haven't installed DOTween, the gravity animation will not work.
1.  Open **Window > Package Manager**.
2.  Add package (or search Asset Store for DOTween).
3.  Or, remove `using DG.Tweening;` from Block.cs if you don't want to install it.
