using UnityEngine;
using ArrowBlast.Core;

namespace ArrowBlast.Game
{
    public class Projectile : MonoBehaviour
    {
        // Movement Data
        private float _elapsed;
        private float _duration;
        private Vector3 _startPos;
        private Vector3 _targetPos;
        private bool _isMoving;

        // Callback Data (Stored directly on the object to avoid Closures)
        public Block TargetBlock { get; private set; }
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public bool WillDestroy { get; private set; }

        private System.Action<Projectile> _onHit;
        private System.Action<Projectile> _onRelease;

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _propBlock;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        public void Initialize(BlockColor color, Block target, int x, int y, bool destroy,
                             System.Action<Projectile> onHitCallback, System.Action<Projectile> onReleaseCallback)
        {
            TargetBlock = target;
            GridX = x;
            GridY = y;
            WillDestroy = destroy;
            _onHit = onHitCallback;
            _onRelease = onReleaseCallback;

            if (_renderer != null)
            {
                Color c = GamePalette.GetColor(color);

                if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_Color", c);
                _propBlock.SetColor("_BaseColor", c);
                _renderer.SetPropertyBlock(_propBlock);
            }
        }

        public void Launch(Vector3 targetPosition, float duration)
        {
            _startPos = transform.position;
            _targetPos = targetPosition;
            _duration = (duration > 0) ? duration : 0.01f;
            _elapsed = 0;
            _isMoving = true;
        }

        private void FixedUpdate()
        {
            if (!_isMoving) return;

            _elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            // Replicate InQuad: fixed math t*t
            float easedT = t * t;
            transform.position = Vector3.Lerp(_startPos, _targetPos, easedT);

            // Subtle rotation while flying
            transform.Rotate(new Vector3(1, 1, 0) * (360f * Time.fixedDeltaTime));

            if (t >= 1f)
            {
                _isMoving = false;
                _onHit?.Invoke(this);
                _onRelease?.Invoke(this);
            }
        }
    }
}
