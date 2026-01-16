using UnityEngine;
using TMPro;
using ArrowBlast.Core;
using ArrowBlast.Managers;

namespace ArrowBlast.Game
{
    public class Slot : MonoBehaviour, IUpdateable
    {
        private Quaternion targetRotation = Quaternion.identity;
        private float rotationSpeed = 10f;
        private MaterialPropertyBlock _propBlock;
        private Transform _transform;

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

        public void Initialize()
        {
            _transform = transform;
            ClearSlot();
            targetRotation = _transform.localRotation;
        }

        private void OnEnable()
        {
            if (UpdateManager.Instance != null)
                UpdateManager.Instance.RegisterUpdateable(this);
        }

        private void OnDisable()
        {
            if (UpdateManager.Instance != null)
                UpdateManager.Instance.UnregisterUpdateable(this);
        }

        public void ManagedUpdate()
        {
            if (Quaternion.Angle(_transform.localRotation, targetRotation) > 0.01f)
            {
                _transform.localRotation = Quaternion.Slerp(_transform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
            else
            {
                _transform.localRotation = targetRotation;
            }
        }

        public void RotateToward(Vector3 worldTarget)
        {
            Vector3 direction = worldTarget - _transform.position;
            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            targetRotation = Quaternion.Euler(0, 0, -angle);
        }

        public void ResetRotation()
        {
            targetRotation = Quaternion.identity;
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

            if (bgMesh)
            {
                if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                bgMesh.GetPropertyBlock(_propBlock);
                Color c = GamePalette.SlotEmpty;
                _propBlock.SetColor("_Color", c);
                _propBlock.SetColor("_BaseColor", c);
                bgMesh.SetPropertyBlock(_propBlock);
            }
            if (ammoText) ammoText.text = "";
            ResetRotation();
        }

        private void UpdateVisuals()
        {
            if (bgMesh)
            {
                if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                bgMesh.GetPropertyBlock(_propBlock);

                Color c = GamePalette.GetColor(CurrentColor);

                _propBlock.SetColor("_Color", c);
                _propBlock.SetColor("_BaseColor", c);
                bgMesh.SetPropertyBlock(_propBlock);
            }
            if (ammoText)
            {
                ammoText.text = AmmoCount.ToString();
                ammoText.color = Color.black;
            }
        }
    }
}
