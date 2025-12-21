using UnityEngine;
using DG.Tweening;
using ArrowBlast.Core;

namespace ArrowBlast.Game
{
    public class Projectile : MonoBehaviour
    {
        private BlockColor color;
        private System.Action onHit;

        public void Initialize(BlockColor color, Material material, Color visualColor, System.Action onHitCallback)
        {
            this.color = color;
            this.onHit = onHitCallback;

            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = new Material(material);
                mr.material.color = visualColor;
            }
        }

        public void Launch(Vector3 targetPosition, float duration)
        {
            transform.DOMove(targetPosition, duration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    onHit?.Invoke();
                    Destroy(gameObject);
                });

            // Subtle rotation while flying
            transform.DORotate(new Vector3(360, 360, 0), duration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear);
        }
    }
}
