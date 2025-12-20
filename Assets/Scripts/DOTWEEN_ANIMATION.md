# DOTween Arrow Collection Animation

## Installation Required
If DOTween is not installed, follow these steps:

1. **Open Package Manager**: Window > Package Manager
2. **Add Package from git URL**: Click + button > Add package from git URL
3. **Enter**: `https://github.com/Demigiant/dotween.git`
4. **Import**: Or download from Asset Store: https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676

## Animation Sequence

### 1. Arrow Movement (0.5s)
- Arrow moves in its direction (Up/Down/Left/Right)
- Continues until head exits grid bottom
- Uses `DOMove` with `Ease.OutQuad`

### 2. Body Merging (0.3s per segment)
- Head stops at exit position
- Each body part "merges" with head sequentially
- **Ammo incremented** for each merge
- Visual pulse effect on each merge
- Debug log shows progress

### 3. Move to Slot (0.4s)
- Arrow moves from exit position to slot
- Scales down to zero with `Ease.InBack`
- Final ammo count set on slot

### 4. Cleanup
- GameObject destroyed after animation completes
- Grid updated immediately (before animation)

## Ammo Distribution
- Total ammo divided by arrow length
- Each segment (head + body parts) adds its share
- Example: Length 4, 40 ammo total = 10 ammo per segment
- Incremented during merge animation for visual feedback

## Debug Output
```
[COLLECT] Attempting to collect arrow at (3, 4)
[ESCAPE] Arrow can escape!
[ARROW ANIM] Body part 1/3 merging...
[ARROW ANIM] Ammo incremented: +10, Total: 10
[ARROW ANIM] Body part 2/3 merging...
[ARROW ANIM] Ammo incremented: +10, Total: 20
[ARROW ANIM] Body part 3/3 merging...
[ARROW ANIM] Ammo incremented: +10, Total: 30
[ARROW ANIM] Head merged (final ammo)
[ARROW ANIM] Ammo incremented: +10, Total: 40
[ARROW ANIM] Collection complete!
[SUCCESS] Arrow collected! Final ammo: 40
```

## Tweakable Parameters
- `moveDuration`: 0.5s (exit movement)
- `mergeDuration`: 0.3s (per body part)
- `exitDistance`: Grid bottom + 2 units
- Scale pulse: 1.2x with 2 loops
- Final movement: 0.4s
