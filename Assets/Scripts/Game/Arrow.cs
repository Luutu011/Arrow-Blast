using UnityEngine;
using ArrowBlast.Core;

namespace ArrowBlast.Game
{
    public class Arrow : MonoBehaviour
    {
        public BlockColor Color { get; private set; }
        public Direction ArrowDirection { get; private set; }
        public int Length { get; private set; } // 1, 2, 3, 4
        
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer headRenderer;
        [SerializeField] private Color[] colorDefinitions;

        public void Init(BlockColor color, Direction dir, int length, int x, int y)
        {
            Color = color;
            ArrowDirection = dir;
            Length = length;
            GridX = x;
            GridY = y;

            UpdateVisuals();
        }

        public void UpdateGridPosition(int x, int y)
        {
            GridX = x;
            GridY = y;
            transform.localPosition = new Vector3(x, y, 0);
        }

        public int GetAmmoAmount()
        {
            // Length 1 = 10, 2 = 20, 3 = 20 (Wait, prompt said 3=20?), 4=40
            // Prompt: 1=10, 2=20, 3=20, 4=40. 
            // This might be a typo in prompt (3=30?), but I will follow instructions strictly.
            // "Length 3 = 20 Ammo"
            
            switch (Length)
            {
                case 1: return 10;
                case 2: return 20;
                case 3: return 20; // Following prompt strictly
                case 4: return 40;
                default: return 10;
            }
        }

        public System.Collections.Generic.List<Vector2Int> GetOccupiedCells()
        {
            var cells = new System.Collections.Generic.List<Vector2Int>();
            Vector2Int head = new Vector2Int(GridX, GridY);
            cells.Add(head);

            Vector2Int back = Vector2Int.zero;
            switch(ArrowDirection)
            {
                case Direction.Up: back = new Vector2Int(0, 1); break; // Forward is GridY-1 (Up), Back is +1 (Down)
                case Direction.Down: back = new Vector2Int(0, -1); break;
                case Direction.Left: back = new Vector2Int(1, 0); break;
                case Direction.Right: back = new Vector2Int(-1, 0); break;
            }

            for(int i = 1; i < Length; i++)
            {
                cells.Add(head + back * i);
            }
            return cells;
        }

        private void UpdateVisuals()
        {
            // Rotate based on direction
            float rotZ = 0;
            switch(ArrowDirection)
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
                if(bodyRenderer) bodyRenderer.color = c;
                if(headRenderer) headRenderer.color = c;
            }

            // Length scaling
            // Pivot is at Head? 
            // If sprite is tail-based, we scale differently.
            // Assuming simplified sprite logic for now: Body scales, Head separate.
            // Or just stretch one sprite?
            if(bodyRenderer)
            {
                // Length 1 = 1 unit size. Length 4 = 4 units.
                // Center needs to shift?
                // If Pivot is center of Length 1 sprite.
                // Better: Just scale Y.
                Vector3 scale = bodyRenderer.transform.localScale;
                scale.y = Length; 
                bodyRenderer.transform.localScale = scale;
                
                // If Pivot is at Head (Top of sprite?), then scaling Y extends down?
                // If Pivot is Center, scaling extends both ways.
                // Adjust position locally if needed explicitly.
                // Assuming standard "Pivot at Bottom/Tail" or "Pivot at Top/Head" sprite setup.
                // Ideally Sprite Pivot is Top-Center (Head). 
                // So scaling Y (negative?) or just scaling extends away.
                // Let's assume User has set up Prefab correct for pivot.
            }
        }
    }
}
