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
        [SerializeField] private int wallHeight = 8;
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

        public void CalculateAndApplyPositions()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            // Get visible height in world units (for orthographic camera)
            float visibleHeight = mainCamera.orthographicSize * 2f;

            // Calculate total required height for all sections
            float wallSectionHeight = wallHeight * cellSize;
            float slotSectionHeight = 1.5f; // Slots are roughly 1.5 units tall
            float arrowSectionHeight = arrowHeight * cellSize;
            float totalRequiredHeight = wallSectionHeight + slotSectionHeight + arrowSectionHeight + (padding * 2);

            // If total required height exceeds visible height, adjust camera size
            if (totalRequiredHeight > visibleHeight)
            {
                mainCamera.orthographicSize = totalRequiredHeight / 2f + 1f; // +1 for extra margin
                visibleHeight = mainCamera.orthographicSize * 2f;
                Debug.Log($"Camera size adjusted to {mainCamera.orthographicSize} to fit all sections");
            }

            // Calculate positions from top to bottom
            // Add 10% top padding to avoid front camera/notch
            float topPadding = visibleHeight * 0.1f;
            float topY = (visibleHeight / 2f) - topPadding - 1f; // Start from top with padding and margin

            // Wall at top
            float wallY = topY - (wallSectionHeight / 2f);

            // Slots in middle
            float slotsY = wallY - (wallSectionHeight / 2f) - padding - (slotSectionHeight / 2f);

            // Arrows at bottom
            float arrowY = slotsY - (slotSectionHeight / 2f) - padding - (arrowSectionHeight / 2f);

            // Apply positions
            if (wallContainer != null)
            {
                wallContainer.position = new Vector3(0, wallY, 0);
                Debug.Log($"Wall positioned at Y: {wallY}");
            }

            if (slotsContainer != null)
            {
                slotsContainer.position = new Vector3(0, slotsY, 0);
                Debug.Log($"Slots positioned at Y: {slotsY}");
            }

            if (arrowContainer != null)
            {
                arrowContainer.position = new Vector3(0, arrowY, 0);
                Debug.Log($"Arrows positioned at Y: {arrowY}");
            }

            Debug.Log($"AutoScaler: Screen {Screen.width}x{Screen.height}, Visible height: {visibleHeight}, Required: {totalRequiredHeight}, Top padding: {topPadding}");
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
