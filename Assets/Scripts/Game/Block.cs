using UnityEngine;
using ArrowBlast.Core;
using DG.Tweening; // Logic requires DOTween package

namespace ArrowBlast.Game
{
    public class Block : MonoBehaviour
    {
        public BlockColor Color { get; private set; }
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private MeshRenderer meshRenderer; // For 3D support

        [SerializeField] private Color[] colorDefinitions; 

        public void Init(BlockColor color, int x, int y)
        {
            Color = color;
            GridX = x;
            GridY = y;
            UpdateVisuals();
        }

        public void UpdateGridPosition(int x, int y)
        {
            GridX = x;
            GridY = y;
            // DOTween gravity simulation
            transform.DOLocalMove(new Vector3(x, y, 0), 0.4f).SetEase(Ease.OutBounce);
        }

        private void UpdateVisuals()
        {
            Color c = (colorDefinitions != null && (int)Color < colorDefinitions.Length) ? colorDefinitions[(int)Color] : UnityEngine.Color.white;
            
            if (spriteRenderer != null) spriteRenderer.color = c;
            if (meshRenderer != null) meshRenderer.material.color = c;
        }
    }
}
