using UnityEngine;
using ArrowBlast.Core;
using ArrowBlast.Managers;

namespace ArrowBlast.Game
{
    public class Projectile : MonoBehaviour, IFixedUpdateable, IInstancedObject
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
        private Transform _transform;

        private void Awake()
        {
            _transform = transform;
            _renderer = GetComponent<MeshRenderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (UpdateManager.Instance != null)
                UpdateManager.Instance.RegisterFixedUpdateable(this);

            if (InstancedCubeRenderer.Instance != null)
                InstancedCubeRenderer.Instance.Register(this);
        }

        private void OnDisable()
        {
            if (UpdateManager.Instance != null)
                UpdateManager.Instance.UnregisterFixedUpdateable(this);

            if (InstancedCubeRenderer.Instance != null)
                InstancedCubeRenderer.Instance.Unregister(this);
        }

        public void AddToInstancer(InstancedCubeRenderer renderer)
        {
            renderer.AddToBatch(transform, TargetBlock.Color, GetComponent<MeshFilter>().sharedMesh);
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
                _renderer.enabled = false; // Disable native renderer
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
            _startPos = _transform.position;
            _targetPos = targetPosition;
            _duration = (duration > 0) ? duration : 0.01f;
            _elapsed = 0;
            _isMoving = true;
        }

        public void ManagedFixedUpdate()
        {
            if (!_isMoving) return;

            _elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            // Replicate InQuad: fixed math t*t
            float easedT = t * t;
            _transform.position = Vector3.Lerp(_startPos, _targetPos, easedT);

            // Subtle rotation while flying
            _transform.Rotate(new Vector3(1, 1, 0) * (360f * Time.fixedDeltaTime));

            if (t >= 1f)
            {
                _isMoving = false;
                _onHit?.Invoke(this);
                _onRelease?.Invoke(this);
            }
        }
    }
}
