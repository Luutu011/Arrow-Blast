using UnityEngine;
using System.Collections.Generic;
using ArrowBlast.Core;

namespace ArrowBlast.Game
{
    /// <summary>
    /// 3D Arrow with MeshRenderer
    /// Moves with 2D-style animation when collected
    /// </summary>
    public class Arrow : MonoBehaviour
    {
        public BlockColor Color { get; private set; }
        public Direction ArrowDirection { get; private set; }
        public int Length { get; private set; }

        public int GridX { get; private set; }
        public int GridY { get; private set; }

        [SerializeField] private MeshRenderer bodyRenderer;
        [SerializeField] private MeshRenderer headRenderer;
        [SerializeField] private Color[] colorDefinitions;

        public void Init(BlockColor color, Direction dir, int length, int x, int y)
        {
            Color = color;
            ArrowDirection = dir;
            Length = length;
            GridX = x;
            GridY = y;

            UpdateVisuals();

            // Ensure collider is present and enabled
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"[ARROW INIT] No collider found on arrow at ({x}, {y})! Adding BoxCollider.");
                col = gameObject.AddComponent<BoxCollider>();
                col.isTrigger = false;
            }

            col.enabled = true;
            Debug.Log($"[ARROW INIT] Arrow initialized at ({x}, {y}), Collider: {col.GetType().Name}, Enabled: {col.enabled}");
        }

        public int GetAmmoAmount()
        {
            switch (Length)
            {
                case 1: return 10;
                case 2: return 20;
                case 3: return 20;
                case 4: return 40;
                default: return 10;
            }
        }

        public List<Vector2Int> GetOccupiedCells()
        {
            var cells = new List<Vector2Int>();
            Vector2Int head = new Vector2Int(GridX, GridY);
            cells.Add(head);

            Vector2Int back = Vector2Int.zero;
            switch (ArrowDirection)
            {
                case Direction.Up: back = new Vector2Int(0, 1); break;
                case Direction.Down: back = new Vector2Int(0, -1); break;
                case Direction.Left: back = new Vector2Int(1, 0); break;
                case Direction.Right: back = new Vector2Int(-1, 0); break;
            }

            for (int i = 1; i < Length; i++)
            {
                cells.Add(head + back * i);
            }
            return cells;
        }

        /// <summary>
        /// Animate arrow moving from bottom grid to middle slot position (2D-style)
        /// </summary>
        public void AnimateToSlot(Vector3 targetPosition, System.Action onComplete = null)
        {
            StartCoroutine(MoveToSlotCoroutine(targetPosition, onComplete));
        }

        private System.Collections.IEnumerator MoveToSlotCoroutine(Vector3 target, System.Action onComplete)
        {
            Vector3 start = transform.position;
            float duration = 0.5f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth ease out
                t = 1f - Mathf.Pow(1f - t, 2f);

                // Move in 2D plane (XY only, Z stays 0)
                Vector3 currentPos = Vector3.Lerp(start, target, t);
                currentPos.z = 0;
                transform.position = currentPos;

                yield return null;
            }

            transform.position = target;
            onComplete?.Invoke();
        }

        private void UpdateVisuals()
        {
            // Rotate based on direction (2D rotation on Z axis)
            float rotZ = 0;
            switch (ArrowDirection)
            {
                case Direction.Up: rotZ = 0; break;
                case Direction.Right: rotZ = -90; break;
                case Direction.Down: rotZ = 180; break;
                case Direction.Left: rotZ = 90; break;
            }
            transform.localRotation = Quaternion.Euler(0, 0, rotZ);

            // Color
            if (colorDefinitions != null && (int)Color < colorDefinitions.Length)
            {
                Color c = colorDefinitions[(int)Color];
                if (bodyRenderer) bodyRenderer.material.color = c;
                if (headRenderer) headRenderer.material.color = c;
            }

            // Length scaling for body
            if (bodyRenderer)
            {
                Vector3 scale = bodyRenderer.transform.localScale;
                scale.y = Length;
                bodyRenderer.transform.localScale = scale;
            }
        }
    }
}
