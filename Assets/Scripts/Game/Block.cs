using UnityEngine;
using ArrowBlast.Core;

namespace ArrowBlast.Game
{
    /// <summary>
    /// 3D Block with MeshRenderer for wall
    /// Falls down with 2D-style animation (only Y axis)
    /// </summary>
    public class Block : MonoBehaviour
    {
        public BlockColor Color { get; private set; }
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Color[] colorDefinitions;

        public void Init(BlockColor color, int x, int y)
        {
            Color = color;
            GridX = x;
            GridY = y;
            UpdateVisuals();
        }

        public void UpdateGridPosition(int x, int y, Vector3 targetWorldPosition)
        {
            GridX = x;
            GridY = y;

            // 2D-style fall animation - only changes Y position
            StartCoroutine(AnimateToPosition(targetWorldPosition, 0.3f));
        }

        private System.Collections.IEnumerator AnimateToPosition(Vector3 target, float duration)
        {
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
        }

        private void UpdateVisuals()
        {
            Color c = (colorDefinitions != null && (int)Color < colorDefinitions.Length)
                ? colorDefinitions[(int)Color]
                : UnityEngine.Color.white;

            if (meshRenderer != null)
            {
                meshRenderer.material.color = c;
            }
        }
    }
}
