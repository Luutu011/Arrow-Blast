using UnityEngine;
using UnityEngine.UI;

namespace ArrowBlast.Managers
{
    public class BoosterUIManager : MonoBehaviour
    {
        [Header("Booster Buttons")]
        [SerializeField] private Button instantExitButton;
        [SerializeField] private Button extraSlotButton;

        private GameManager gameManager;

        public void Initialize(GameManager manager)
        {
            gameManager = manager;

            if (instantExitButton != null)
            {
                instantExitButton.onClick.RemoveAllListeners();
                instantExitButton.onClick.AddListener(() => gameManager.ToggleInstantExitBooster());
                UpdateInstantExitVisual(false); // Reset to normal
            }

            if (extraSlotButton != null)
            {
                extraSlotButton.onClick.RemoveAllListeners();
                extraSlotButton.onClick.AddListener(() => gameManager.UseExtraSlotBooster());
            }
        }

        public void UpdateInstantExitVisual(bool isActive)
        {
            if (instantExitButton == null) return;

            ColorBlock cb = instantExitButton.colors;
            Color targetColor = isActive ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.white;

            cb.normalColor = targetColor;
            cb.selectedColor = targetColor;
            instantExitButton.colors = cb;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
