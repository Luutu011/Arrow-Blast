using UnityEngine;

namespace ArrowBlast.Managers
{
    /// <summary>
    /// Auto-scales and positions the three game sections (Wall, Slots, Arrows)
    /// based on screen resolution to ensure they fit and don't overlap
    /// </summary>
    public class AutoScaler : MonoBehaviour
    {
        [Header("Container References")]
        [SerializeField] private Transform wallContainer;
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private Transform arrowContainer;

        [Header("Grid Settings")]
        [SerializeField] private int wallWidth = 6;
        [SerializeField] private int wallHeight = 8;
        [SerializeField] private int arrowWidth = 6;
        [SerializeField] private int arrowHeight = 8;
        [SerializeField] private float cellSize = 0.8f;
        [SerializeField] private float padding = 1f; // Space between sections

        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
            CalculateAndApplyPositions();
        }

        private void OnValidate()
        {
            // Recalculate in editor when values change
            if (Application.isPlaying)
            {
                CalculateAndApplyPositions();
            }
        }

        public void UpdateSettings(int wWidth, int wHeight, int aWidth, int aHeight, float cSize)
        {
            wallWidth = wWidth;
            wallHeight = wHeight;
            arrowWidth = aWidth;
            arrowHeight = aHeight;
            cellSize = cSize;
            CalculateAndApplyPositions();
        }

        public void CalculateAndApplyPositions()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            // Get visible height in world units (for orthographic camera) based on height requirements ONLY
            float visibleHeight = mainCamera.orthographicSize * 2f;

            // Calculate total required height for all sections
            float visibleWallRows = Mathf.Min(8, wallHeight);
            float wallSectionHeight = visibleWallRows * cellSize;
            float slotSectionHeight = 1.5f; // Slots are roughly 1.5 units tall
            float arrowSectionHeight = arrowHeight * cellSize;
            float totalRequiredHeight = wallSectionHeight + slotSectionHeight + arrowSectionHeight + (padding * 2);

            // If total required height exceeds visible height, adjust camera size
            if (totalRequiredHeight > visibleHeight)
            {
                mainCamera.orthographicSize = totalRequiredHeight / 2f + 1f; // +1 for extra margin
                visibleHeight = mainCamera.orthographicSize * 2f;
            }

            // Recalculate aspect/width after potential height adjustment
            float visibleWidth = visibleHeight * mainCamera.aspect;
            float safeWidth = visibleWidth - 1.0f; // Leave 0.5f margin on each side

            // --- Apply Scaling for Width Fit ---

            // 1. Wall Scale
            float requiredWallWidth = wallWidth * cellSize;
            float wallScale = 1.0f;
            if (requiredWallWidth > safeWidth)
            {
                wallScale = safeWidth / requiredWallWidth;
            }

            // 2. Arrow Scale
            float requiredArrowWidth = arrowWidth * cellSize;
            float arrowScale = 1.0f;
            if (requiredArrowWidth > safeWidth)
            {
                arrowScale = safeWidth / requiredArrowWidth;
            }

            // 3. Slot Scale (Match Wall Scale usually, or max of both to fit? 
            // Slots usually align with Wall columns. So use wallScale.)
            float slotScale = wallScale;


            // --- Apply Positions & Scales ---

            // Calculate positions from top to bottom
            float topPadding = visibleHeight * 0.1f;
            float topY = (visibleHeight / 2f) - topPadding - 1f;

            // Wall at top
            // Scale affects visual height, but blocks are children. 
            // Note: wallContainer position is usually the BOTTOM or CENTER of the wall?
            // "wallContainer.position = new Vector3(0, wallY, 0);" and earlier comments: 
            // "container is at bottom of wall section... blocks go from 0 to wallSectionHeight... containerY = topY - wallSectionHeight"
            // If we scale the container, the effective height shrinks too: wallSectionHeight * wallScale.

            float scaledWallHeight = wallSectionHeight * wallScale;
            float wallY = topY - scaledWallHeight;

            float scaledSlotHeight = slotSectionHeight * slotScale;
            float slotsY = wallY - padding - (scaledSlotHeight / 2f); // Center slots in their reserved band

            float scaledArrowHeight = arrowSectionHeight * arrowScale;
            // Place arrows: slotsY is center of slots.
            // Arrow top should be below slots bottom? Or padding?
            // "slotsY - (slotSectionHeight / 2f) - padding - (arrowSectionHeight / 2f)" was old logic (centering arrows in their band).
            // Let's stick to placing top of arrows below slots.
            // Arrow container pivot? Assuming it's centered Y? 
            // The old logic `arrowY = slotsY - ... - (arrowSectionHeight/2f)` implies arrowContainer pivot is center Y.
            // Let's preserve that logic but use scaled heights.
            float arrowY = slotsY - (scaledSlotHeight / 2f) - padding - (scaledArrowHeight / 2f);


            if (wallContainer != null)
            {
                wallContainer.localScale = Vector3.one * wallScale;
                wallContainer.position = new Vector3(0, wallY, 0);
            }

            if (slotsContainer != null)
            {
                slotsContainer.localScale = Vector3.one * slotScale;
                slotsContainer.position = new Vector3(0, slotsY, 0);
            }

            if (arrowContainer != null)
            {
                arrowContainer.localScale = Vector3.one * arrowScale;
                arrowContainer.position = new Vector3(0, arrowY, 0);
            }

            // Debug.Log($"AutoScaler: Wall Scale {wallScale:F2}, Arrow Scale {arrowScale:F2}");
        }

        // Call this when screen orientation or resolution changes
        private void OnRectTransformDimensionsChange()
        {
            CalculateAndApplyPositions();
        }

#if UNITY_EDITOR
        // Draw gizmos to visualize sections in editor
        private void OnDrawGizmos()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            float visibleHeight = mainCamera.orthographicSize * 2f;
            float visibleWidth = visibleHeight * mainCamera.aspect;

            // Draw camera bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(mainCamera.transform.position + new Vector3(0, 0, 10), new Vector3(visibleWidth, visibleHeight, 0.1f));

            // Draw container positions
            if (wallContainer != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(wallContainer.position, new Vector3(5, wallHeight * cellSize, 0.1f));
            }

            if (slotsContainer != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(slotsContainer.position, new Vector3(5, 1.5f, 0.1f));
            }

            if (arrowContainer != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(arrowContainer.position, new Vector3(5, arrowHeight * cellSize, 0.1f));
            }
        }
#endif
    }
}
