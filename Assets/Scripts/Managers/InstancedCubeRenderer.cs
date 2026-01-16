using UnityEngine;
using System.Collections.Generic;
using ArrowBlast.Core;

namespace ArrowBlast.Managers
{
    public interface IInstancedObject
    {
        void AddToInstancer(InstancedCubeRenderer renderer);
    }

    /// <summary>
    /// Optimized Universal Renderer for all RoundedCubes.
    /// Manages a persistent registry of blocks and arrows for reliable rendering.
    /// </summary>
    public class InstancedCubeRenderer : MonoBehaviour, IUpdateable
    {
        public static InstancedCubeRenderer Instance { get; private set; }

        private class RenderBatch
        {
            public Mesh mesh;
            public Dictionary<BlockColor, List<Matrix4x4>> colorBatches = new Dictionary<BlockColor, List<Matrix4x4>>();
            public List<Matrix4x4> shadowMatrices = new List<Matrix4x4>();

            public RenderBatch(Mesh m)
            {
                mesh = m;
                foreach (BlockColor color in System.Enum.GetValues(typeof(BlockColor)))
                    colorBatches[color] = new List<Matrix4x4>(128);
                shadowMatrices = new List<Matrix4x4>(256);
            }

            public void Clear()
            {
                foreach (var list in colorBatches.Values) list.Clear();
                shadowMatrices.Clear();
            }
        }

        private List<IInstancedObject> _registry = new List<IInstancedObject>(200);
        private Dictionary<Mesh, RenderBatch> batchesByMesh = new Dictionary<Mesh, RenderBatch>();

        [Header("Assets")]
        [SerializeField] private Material blockMaterial;

        private Matrix4x4[] matrixArray = new Matrix4x4[511];
        private static readonly int BaseColorPropId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                propertyBlock = new MaterialPropertyBlock();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (UpdateManager.Instance != null)
                UpdateManager.Instance.RegisterUpdateable(this);

            // Proactively find objects that might have been enabled before we existed
            var existing = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mono in existing)
            {
                if (mono is IInstancedObject inst)
                    Register(inst);
            }
        }

        private void OnDestroy()
        {
            if (UpdateManager.Instance != null)
                UpdateManager.Instance.UnregisterUpdateable(this);
        }

        public void Register(IInstancedObject obj)
        {
            if (!_registry.Contains(obj)) _registry.Add(obj);
        }

        public void Unregister(IInstancedObject obj)
        {
            _registry.Remove(obj);
        }

        public void AddToBatch(Transform t, BlockColor color, Mesh mesh)
        {
            if (mesh == null) return;

            if (!batchesByMesh.TryGetValue(mesh, out RenderBatch batch))
            {
                batch = new RenderBatch(mesh);
                batchesByMesh[mesh] = batch;
            }

            Matrix4x4 matrix = t.localToWorldMatrix;
            batch.colorBatches[color].Add(matrix);
            batch.shadowMatrices.Add(matrix);

            // Capture material if not already set manually
            if (blockMaterial == null)
            {
                // Try to get material from the renderer (even if disabled)
                var mr = t.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterial != null)
                {
                    blockMaterial = mr.sharedMaterial;
                    blockMaterial.enableInstancing = true; // FORCE ENABLE
                }
            }
        }

        public void ManagedUpdate()
        {
            if (_registry.Count == 0) return;

            // 1. GATHER
            for (int i = _registry.Count - 1; i >= 0; i--)
            {
                var obj = _registry[i];
                if (obj == null || (obj is MonoBehaviour mono && mono == null))
                {
                    _registry.RemoveAt(i);
                    continue;
                }
                obj.AddToInstancer(this);
            }

            // 2. RENDER
            if (blockMaterial != null)
            {
                foreach (var batch in batchesByMesh.Values)
                {
                    if (batch.shadowMatrices.Count == 0) continue;
                    RenderShadowPass(batch);
                    RenderColorPass(batch);
                }
            }

            // 3. CLEAR
            foreach (var batch in batchesByMesh.Values)
            {
                batch.Clear();
            }
        }

        private void RenderShadowPass(RenderBatch batch)
        {
            int count = batch.shadowMatrices.Count;
            int index = 0;
            while (index < count)
            {
                int range = Mathf.Min(511, count - index);
                batch.shadowMatrices.CopyTo(index, matrixArray, 0, range);
                Graphics.DrawMeshInstanced(batch.mesh, 0, blockMaterial, matrixArray, range, null, UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly, false);
                index += range;
            }
        }

        private void RenderColorPass(RenderBatch batch)
        {
            foreach (var pair in batch.colorBatches)
            {
                List<Matrix4x4> matrices = pair.Value;
                int count = matrices.Count;
                if (count == 0) continue;

                Color color = GamePalette.GetColor(pair.Key);
                propertyBlock.SetColor(BaseColorPropId, color);
                propertyBlock.SetColor(ColorPropId, color);

                int index = 0;
                while (index < count)
                {
                    int range = Mathf.Min(511, count - index);
                    matrices.CopyTo(index, matrixArray, 0, range);
                    Graphics.DrawMeshInstanced(batch.mesh, 0, blockMaterial, matrixArray, range, propertyBlock, UnityEngine.Rendering.ShadowCastingMode.Off, true);
                    index += range;
                }
            }
        }
    }
}
