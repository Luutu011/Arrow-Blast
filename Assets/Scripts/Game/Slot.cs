using UnityEngine;
using TMPro;
using ArrowBlast.Core;

namespace ArrowBlast.Game
{
    /// <summary>
    /// 3D GameObject-based Slot using MeshRenderer + TextMeshPro
    /// All slots shoot simultaneously - no "active shooter" concept
    /// Slots don't shift when empty - they just clear
    /// </summary>
    public class Slot : MonoBehaviour
    {
        public BlockColor CurrentColor { get; private set; }
        public int AmmoCount { get; private set; }
        public bool IsOccupied { get; private set; }
        public bool IsReserved { get; private set; }

        public void SetReserved(bool reserved)
        {
            IsReserved = reserved;
        }

        [SerializeField] private MeshRenderer bgMesh;
        [SerializeField] private TextMeshPro ammoText;
        [SerializeField] private Color[] colorDefinitions;
        [SerializeField] private Color emptyColor = Color.gray;
        [SerializeField] private Color textColor = Color.black;

        public void Initialize()
        {
            ClearSlot();
        }

        public void FillSlot(BlockColor color, int amount)
        {
            IsOccupied = true;
            IsReserved = false;
            CurrentColor = color;
            AmmoCount = amount;
            UpdateVisuals();
        }

        public void InitializeCollection(BlockColor color)
        {
            CurrentColor = color;
            AmmoCount = 0;
            IsReserved = true;
            IsOccupied = false;
            UpdateVisuals();
        }

        public void FinalizeCollection()
        {
            IsOccupied = true;
            IsReserved = false;
            UpdateVisuals();
        }

        public void AddAmmo(int amount)
        {
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
            IsReserved = false;
            AmmoCount = 0;

            if (bgMesh) bgMesh.material.color = emptyColor;
            if (ammoText) ammoText.text = "";
        }

        private void UpdateVisuals()
        {
            if (bgMesh && (int)CurrentColor < colorDefinitions.Length)
            {
                bgMesh.material.color = colorDefinitions[(int)CurrentColor];
            }
            if (ammoText)
            {
                ammoText.text = AmmoCount.ToString();
                ammoText.color = textColor;
            }
        }
    }
}
