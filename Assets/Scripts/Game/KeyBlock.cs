using UnityEngine;
using ArrowBlast.Core;

namespace ArrowBlast.Game
{
    /// <summary>
    /// Key Block is a special block that unlocks a LockObstacle when destroyed.
    /// It behaves like a normal block but holds a LockId.
    /// </summary>
    public class KeyBlock : Block
    {
        public int LockId { get; private set; }

        public void Init(BlockColor color, int x, int y, int lockId, bool isTwoColor = false, BlockColor secondaryColor = BlockColor.Red)
        {
            LockId = lockId;
            base.Init(color, x, y, isTwoColor, secondaryColor);
        }

        protected override void UpdateVisuals()
        {
            base.UpdateVisuals();
        }

        public System.Collections.IEnumerator AnimateFlyToTarget(Vector3 targetPos, float duration)
        {
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Ease In Cubic for "taking off" feel or just smooth lerp
                float smoothT = t * t * (3f - 2f * t);

                transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
                transform.localScale = Vector3.Lerp(startScale, startScale * 1.2f, Mathf.Sin(t * Mathf.PI));

                yield return null;
            }

            transform.position = targetPos;
        }
    }
}
