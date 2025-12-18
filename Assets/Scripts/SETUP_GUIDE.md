# Arrow Blast - Setup Guide (Portrait Mobile Layout)

## 🚀 Quick Start - Auto Setup

The fastest way to get started:

1. Open Unity
2. Go to **Arrow Blast > Auto Setup Scene**
3. Click **"Create Complete Setup"**
4. Press **Play**!

The tool will automatically create:
- GameManager with LevelGenerator
- All three containers (Wall, Slots, Arrows)
- Prefabs (Block, Arrow, Slot)
- Portrait camera configuration

---

## 📱 Portrait Layout (Top to Bottom)

```
┌─────────────────┐
│                 │
│   WALL (y=6)    │  ← Colored blocks to destroy
│   🟥🟦🟩🟨      │
│                 │
├─────────────────┤
│  SLOTS (y=0)    │  ← 5 shooter slots (ALL shoot)
│  [💚][🟡][  ]  │
│                 │
├─────────────────┤
│ ARROWS (y=-6)   │  ← Puzzle grid to extract arrows
│   ↑ ↓ → ←      │
│                 │
└─────────────────┘
```

---

## 🎮 Gameplay Summary

### Core Loop
1. **Click arrows** in the bottom grid to extract them
2. Arrows convert to **ammo** and fill the **5 shooter slots** in the middle
3. **ALL occupied slots shoot simultaneously** at matching colored blocks
4. Destroy all blocks to **win**!

### Key Mechanics
- **Arrow Escape Rule**: Can only extract an arrow if its path (direction it points) is clear
- **All Slots Shoot**: Every occupied slot fires at once (no "active shooter")
- **No Shifting**: When a slot empties, it stays empty (doesn't shift)
- **Portrait Mobile**: Designed for 9:16 aspect ratio

---

## 🛠️ Manual Setup (If Not Using Auto Tool)

### 1. Scene Hierarchy

Create these GameObjects:

```
Scene
├── Main Camera (Orthographic, Size: 7)
├── GameManager
│   └── Components: GameManager.cs, LevelGenerator.cs
├── WallContainer (Position: 0, 6, 0)
├── SlotsContainer (Position: 0, 0, 0)
└── ArrowContainer (Position: 0, -6, 0)
```

### 2. Prefab Creation

#### A. Block Prefab
1. Create **Quad** primitive
2. Add `Block.cs` script
3. Scale to `0.75, 0.75, 1`
4. **Assign** `spriteRenderer` field
5. **Set Color Definitions** (6 colors): Red, Blue, Green, Yellow, Purple, Orange
6. Save as `Assets/Prefabs/Block.prefab`

#### B. Arrow Prefab  
1. Create **Empty GameObject** named "Arrow"
2. Add `Arrow.cs` script
3. Add child **Quad** named "Body" (local scale: 0.5, 0.8, 1)
4. Add child **Cube** named "Head" (local scale: 0.4, 0.4, 0.4)
5. Add **BoxCollider** to root (size: 0.8, 1.2, 0.2)
6. **Assign** `bodyRenderer` and `headRenderer`
7. **Set Color Definitions** (same 6 colors)
8. Save as `Assets/Prefabs/Arrow.prefab`

#### C. Slot Prefab
1. Create **Quad** primitive
2. Add `Slot.cs` script
3. Add child GameObject with **TextMeshPro** component
4. **Assign** `bgSprite` (Quad's SpriteRenderer) and `ammoText` (TextMeshPro)
5. **Set Color Definitions** (same 6 colors)
6. Save as `Assets/Prefabs/Slot.prefab`

### 3. Wire Up GameManager

Select GameManager in hierarchy:
- **Level Generator**: Drag GameManager itself
- **Wall Container**: Drag WallContainer
- **Slots Container**: Drag SlotsContainer  
- **Arrow Container**: Drag ArrowContainer
- **Prefabs**: Drag Block, Arrow, Slot prefabs
- **Settings**: Cell Size = 0.8, Fire Rate = 0.2

### 4. Camera Setup

- **Projection**: Orthographic
- **Size**: 7
- **Position**: (0, 0, -10)
- **Background**: Dark color (0.1, 0.1, 0.15)

---

## 🎨 Level Editor

Create and edit levels via **Arrow Blast > Level Editor**:

### Features
- **Wall Editor**: Add colored blocks
- **Arrow Editor**: Place arrows with direction and length
- **Generate Random**: Quick random level generation
- **Save/Load**: JSON-based level persistence

### Workflow
1. Open Level Editor
2. Set dimensions (portrait recommended: width=6, height=8-10)
3. Click "Generate Random Level" or manually add blocks/arrows
4. Click "Save Level"
5. Set GameManager to Story Mode and enter your level name

---

## 🎯 Tips

### Portrait Dimensions
- **Wall**: 6 wide × 8-10 tall
- **Arrow Grid**: 6 wide × 8-10 tall
- **Camera Size**: 7-8 orthographic units

### All Slots Shoot
- Every occupied slot fires simultaneously
- Match colors strategically to clear multiple blocks per round
- Slots don't shift when empty - plan your shot order

### Arrow Extraction
- Think ahead! Arrows block each other
- Extract in the right order to unlock all arrows
- Some levels require specific extraction sequences

---

## ✨ Next Steps

1. **Test**: Press Play and click arrows!
2. **Create Levels**: Use the Level Editor
3. **Customize**: Adjust fire rate, colors, dimensions
4. **Build**: Export for mobile (portrait orientation)

---

## 🐛 Troubleshooting

**Input not working?**
- Ensure you're using the new Input System package
- Check Player Settings > Active Input Handling

**Prefabs not spawning?**
- Verify all prefabs are assigned in GameManager inspector
- Check console for errors

**Camera doesn't show everything?**
- Increase orthographic size (7-8 recommended)
- Adjust container Y positions if needed

---

**Ready to play! 🎮**
