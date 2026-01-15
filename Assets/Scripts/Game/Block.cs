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
        public BlockColor Color { get; protected set; }
        public BlockColor SecondaryColor { get; protected set; }
        public bool IsTwoColor { get; protected set; }
        public bool IsTargeted { get; set; }
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private MeshRenderer innerMeshRenderer;
        [SerializeField] private string colorPropertyName = "_Color";
        protected MaterialPropertyBlock _propBlock;
        protected bool _isInitialized;

        public virtual void Init(BlockColor color, int x, int y, bool isTwoColor = false, BlockColor secondaryColor = BlockColor.Red)
        {
            Color = color;
            SecondaryColor = secondaryColor;
            IsTwoColor = isTwoColor;
            GridX = x;
            GridY = y;
            IsTargeted = false; // Reset for pooling
            transform.localScale = Vector3.one;
            UpdateVisuals();
        }

        public virtual bool TakeHit()
        {
            if (IsTwoColor)
            {
                // We don't update visuals immediately here, GameManager will call AnimateTransition
                return false;
            }
            return true;
        }

        public System.Collections.IEnumerator AnimateDeath()
        {
            float elapsed = 0;
            float duration = 0.1f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
                yield return null;
            }
            transform.localScale = Vector3.zero;
        }

        public System.Collections.IEnumerator AnimateTransition()
        {
            if (!IsTwoColor) yield break;

            float elapsed = 0;
            float duration = 0.2f;

            // Pop effect for outer layer: slight swell then shrink or just disappear?
            // User said "pop", which often means a quick scale up then gone.
            // But inner needs to grow.

            Vector3 innerStartScale = innerMeshRenderer != null ? innerMeshRenderer.transform.localScale : new Vector3(0.7f, 0.7f, 0.7f);
            Vector3 innerTargetScale = Vector3.one;
            Vector3 innerStartPos = innerMeshRenderer != null ? innerMeshRenderer.transform.localPosition : new Vector3(0, 0, -0.2f);
            Vector3 innerTargetPos = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Inner grows and moves back to center
                if (innerMeshRenderer != null)
                {
                    innerMeshRenderer.transform.localScale = Vector3.Lerp(innerStartScale, innerTargetScale, t);
                    innerMeshRenderer.transform.localPosition = Vector3.Lerp(innerStartPos, innerTargetPos, t);
                }

                // Outer pops (grows then disappears)
                float popScale = 1.0f + Mathf.Sin(t * Mathf.PI) * 0.2f;
                meshRenderer.transform.localScale = Vector3.one * popScale;

                // Optional: Fade out outer layer if material supports it, 
                // but since we don't know the shader, let's just scale it down at the very end
                if (t > 0.8f) meshRenderer.transform.localScale = Vector3.zero;

                yield return null;
            }

            // Finalize transition logic
            Color = SecondaryColor;
            IsTwoColor = false;

            // Reset scales for the new state
            meshRenderer.transform.localScale = Vector3.one;
            if (innerMeshRenderer != null)
                innerMeshRenderer.transform.localScale = innerStartScale; // Reset for future use if needed, though it will be hidden

            UpdateVisuals();
        }

        public float AnimationProgress { get; private set; } = 1f;

        public void UpdateGridPosition(int x, int y, Vector3 targetWorldPosition)
        {
            GridX = x;
            GridY = y;

            // 2D-style fall animation - only changes Y position
            StopAllCoroutines(); // Ensure no overlapping fall animations
            StartCoroutine(AnimateToPosition(targetWorldPosition, 0.3f));
        }

        private System.Collections.IEnumerator AnimateToPosition(Vector3 target, float duration)
        {
            Vector3 start = transform.localPosition;
            float elapsed = 0;
            AnimationProgress = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                AnimationProgress = Mathf.Clamp01(elapsed / duration);
                float t = AnimationProgress;

                // Ease out for bounce effect
                t = 1f - Mathf.Pow(1f - t, 3f);

                transform.localPosition = Vector3.Lerp(start, target, t);
                yield return null;
            }

            transform.localPosition = target;
            AnimationProgress = 1f;
        }

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
        }

        protected virtual void UpdateVisuals()
        {
            Color c = GetVisualColor(Color);
            if (meshRenderer != null)
            {
                if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_Color", c);
                _propBlock.SetColor("_BaseColor", c);
                meshRenderer.SetPropertyBlock(_propBlock);
            }

            if (innerMeshRenderer != null)
            {
                innerMeshRenderer.gameObject.SetActive(IsTwoColor);
                if (IsTwoColor)
                {
                    innerMeshRenderer.GetPropertyBlock(_propBlock);
                    Color sc = GetVisualColor(SecondaryColor);
                    _propBlock.SetColor("_Color", sc);
                    _propBlock.SetColor("_BaseColor", sc);
                    innerMeshRenderer.SetPropertyBlock(_propBlock);

                    innerMeshRenderer.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f); // Slightly larger
                    innerMeshRenderer.transform.localPosition = new Vector3(0, 0, -0.2f); // Push forward more noticeably
                }
            }
        }

        private Color GetVisualColor(BlockColor color)
        {
            return GamePalette.GetColor(color);
        }
    }
}
