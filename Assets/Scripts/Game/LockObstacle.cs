using UnityEngine;
using System.Collections.Generic;
using ArrowBlast.Core;

namespace ArrowBlast.Game
{
    /// <summary>
    /// Lock Obstacle that appears in the bottom grid (wall)
    /// Blocks arrows from exiting in its area until unlocked by a matching KeyBlock
    /// Can span multiple cells based on its size
    /// </summary>
    public class LockObstacle : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public int SizeX { get; private set; }
        public int SizeY { get; private set; }
        public int LockId { get; private set; } // ID to match with KeyBlock
        public bool IsLocked { get; private set; }

        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f); // Dark gray
        [SerializeField] private Color unlockingColor = new Color(0.8f, 0.8f, 0.2f); // Yellow glow

        private List<Vector2Int> occupiedCells = new List<Vector2Int>();
        private MaterialPropertyBlock _propBlock;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
        }

        public void Init(int x, int y, int sizeX, int sizeY, int lockId, float cellSize)
        {
            GridX = x;
            GridY = y;
            SizeX = Mathf.Max(1, sizeX);
            SizeY = Mathf.Max(1, sizeY);
            LockId = lockId;
            IsLocked = true;

            // Calculate all occupied cells
            occupiedCells.Clear();
            for (int ox = 0; ox < SizeX; ox++)
            {
                for (int oy = 0; oy < SizeY; oy++)
                {
                    occupiedCells.Add(new Vector2Int(GridX + ox, GridY + oy));
                }
            }

            // Scale to match the multiple cells it covers
            transform.localScale = new Vector3(SizeX * cellSize, SizeY * cellSize, cellSize * 0.5f);
            UpdateVisuals();
        }

        public void Unlock()
        {
            if (!IsLocked) return;
            IsLocked = false;
            StartCoroutine(AnimateUnlock());
        }

        private System.Collections.IEnumerator AnimateUnlock()
        {
            float elapsed = 0;
            float duration = 0.5f;
            Vector3 startScale = transform.localScale;
            Color startColor = lockedColor;

            // Flash and change color
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                if (meshRenderer != null)
                {
                    meshRenderer.GetPropertyBlock(_propBlock);
                    Color c = Color.Lerp(startColor, unlockingColor, t);
                    _propBlock.SetColor("_Color", c);
                    _propBlock.SetColor("_BaseColor", c);
                    meshRenderer.SetPropertyBlock(_propBlock);
                }
                // Slight pulse
                transform.localScale = Vector3.Lerp(startScale, startScale * 1.1f, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            // Fade out and shrink
            elapsed = 0;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                if (meshRenderer != null)
                {
                    meshRenderer.GetPropertyBlock(_propBlock);
                    Color col = unlockingColor;
                    col.a = 1f - t;
                    _propBlock.SetColor("_Color", col);
                    _propBlock.SetColor("_BaseColor", col);
                    meshRenderer.SetPropertyBlock(_propBlock);
                }
                yield return null;
            }

            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
        }

        private void UpdateVisuals()
        {
            if (meshRenderer != null)
            {
                if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(_propBlock);
                Color c = IsLocked ? lockedColor : unlockingColor;
                _propBlock.SetColor("_Color", c);
                _propBlock.SetColor("_BaseColor", c);
                meshRenderer.SetPropertyBlock(_propBlock);
            }

            // Ensure collider is present
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }
            col.enabled = IsLocked; // Only block when locked
        }

        /// <summary>
        /// Check if this lock blocks a specific exit column
        /// Used to prevent arrows from escaping through locked areas
        /// </summary>
        public bool BlocksColumn(int column)
        {
            if (!IsLocked) return false;
            return column >= GridX && column < GridX + SizeX;
        }

        /// <summary>
        /// Check if this lock blocks a specific row
        /// </summary>
        public bool BlocksRow(int row)
        {
            if (!IsLocked) return false;
            return row >= GridY && row < GridY + SizeY;
        }

        /// <summary>
        /// Get all cells occupied by this lock
        /// </summary>
        public List<Vector2Int> GetOccupiedCells()
        {
            return new List<Vector2Int>(occupiedCells);
        }

        /// <summary>
        /// Check if a specific cell is blocked by this lock
        /// </summary>
        public bool BlocksCell(int x, int y)
        {
            if (!IsLocked) return false;
            return occupiedCells.Contains(new Vector2Int(x, y));
        }
    }
}
