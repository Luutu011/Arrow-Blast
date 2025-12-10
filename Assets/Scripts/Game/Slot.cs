using UnityEngine;
using UnityEngine.UI; // Assuming UI based slots or World space
using TMPro; // Assuming TextMeshPro
using ArrowBlast.Core;

namespace ArrowBlast.Game
{
    public class Slot : MonoBehaviour
    {
        public BlockColor CurrentColor { get; private set; }
        public int AmmoCount { get; private set; }
        public bool IsOccupied { get; private set; }

        [SerializeField] private Image bgImage;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private Color[] colorDefinitions;

        public void Initialize()
        {
            ClearSlot();
        }

        public void FillSlot(BlockColor color, int amount)
        {
            IsOccupied = true;
            CurrentColor = color;
            AmmoCount = amount;
            UpdateVisuals();
        }

        public void AddAmmo(int amount)
        {
            if (!IsOccupied) return;
            AmmoCount += amount;
            UpdateVisuals();
        }

        public bool UseAmmo(int amount = 1)
        {
            if (!IsOccupied || AmmoCount <= 0) return false;
            
            AmmoCount -= amount;
            if (AmmoCount <= 0)
            {
                AmmoCount = 0;
                ClearSlot();
            }
            else
            {
                UpdateVisuals();
            }
            return true;
        }

        public void ClearSlot()
        {
            IsOccupied = false;
            AmmoCount = 0;
            // Set visual to empty state
            if(bgImage) bgImage.color = Color.gray; 
            if(ammoText) ammoText.text = "";
        }

        private void UpdateVisuals()
        {
             if(bgImage && (int)CurrentColor < colorDefinitions.Length) 
             {
                 bgImage.color = colorDefinitions[(int)CurrentColor];
             }
             if(ammoText)
             {
                 ammoText.text = AmmoCount.ToString();
             }
        }
    }
}
