using UnityEngine;
using System.Collections.Generic;
using ArrowBlast.Core;

namespace ArrowBlast.Game
{
    /// <summary>
    /// Key Block that appears in the top grid (arrow grid)
    /// Falls down when blocks beneath it are removed
    /// Unlocks associated LockObstacle when it reaches the bottom grid
    /// </summary>
    public class KeyBlock : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public int LockId { get; private set; } // ID to match with LockObstacle

        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Color keyColor = new Color(1f, 0.84f, 0f); // Gold color

        private bool isFalling = false;
        private bool hasUnlocked = false;

        public void Init(int x, int y, int lockId)
        {
            GridX = x;
            GridY = y;
            LockId = lockId;
            transform.localScale = Vector3.one;
            UpdateVisuals();
        }

        public void UpdateGridPosition(int x, int y, Vector3 targetWorldPosition)
        {
            GridX = x;
            GridY = y;

            // Animate fall to new position
            if (!isFalling)
            {
                StartCoroutine(AnimateToPosition(targetWorldPosition, 0.3f));
            }
        }

        private System.Collections.IEnumerator AnimateToPosition(Vector3 target, float duration)
        {
            isFalling = true;
            Vector3 start = transform.localPosition;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Ease out for bounce effect
                t = 1f - Mathf.Pow(1f - t, 3f);

                transform.localPosition = Vector3.Lerp(start, target, t);
                yield return null;
            }

            transform.localPosition = target;
            isFalling = false;
        }

        public System.Collections.IEnumerator AnimateUnlock()
        {
            if (hasUnlocked) yield break;
            hasUnlocked = true;

            float elapsed = 0;
            float duration = 0.4f;
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = Vector3.one * 1.5f;

            // Pulse effect
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            // Shrink and disappear
            elapsed = 0;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, t);
                yield return null;
            }

            transform.localScale = Vector3.zero;
        }

        private void UpdateVisuals()
        {
            if (meshRenderer != null)
            {
                meshRenderer.material.color = keyColor;
            }

            // Ensure collider is present for clicking/interaction if needed
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }
            col.enabled = true;
        }

        public bool CanFall(int arrowRows, System.Func<int, int, bool> isArrowGridCellOccupied)
        {
            // Key can fall if the cell below it in the arrow grid is empty
            // or if it's at the bottom of the arrow grid
            if (GridY <= 0) return false; // Already at bottom

            int cellBelow = GridY - 1;
            return !isArrowGridCellOccupied(GridX, cellBelow);
        }

        public bool HasReachedBottom(int bottomY)
        {
            return GridY <= bottomY;
        }
    }
}
