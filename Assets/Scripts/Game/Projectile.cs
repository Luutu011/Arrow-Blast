using UnityEngine;
using DG.Tweening;
using ArrowBlast.Core;

namespace ArrowBlast.Game
{
    public class Projectile : MonoBehaviour
    {
        private BlockColor color;
        private System.Action onHit;
        private System.Action<Projectile> onRelease;
        [SerializeField] private Color[] colorDefinitions;
        private MeshRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
        }

        public void Initialize(BlockColor color, Material material, System.Action onHitCallback, System.Action<Projectile> onReleaseCallback)
        {
            this.color = color;
            this.onHit = onHitCallback;
            this.onRelease = onReleaseCallback;

            if (_renderer != null)
            {
                if (material != null) _renderer.sharedMaterial = material;

                Color c = (colorDefinitions != null && (int)color < colorDefinitions.Length)
                    ? colorDefinitions[(int)color]
                    : UnityEngine.Color.white;

                _renderer.material.color = c;
            }
        }

        public void Launch(Vector3 targetPosition, float duration)
        {
            transform.DOKill(true); // Kill and complete any existing tweens before launching
            transform.DOMove(targetPosition, duration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    onHit?.Invoke();
                    onRelease?.Invoke(this);
                });

            // Subtle rotation while flying
            transform.DORotate(new Vector3(360, 360, 0), duration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear);
        }
    }
}
