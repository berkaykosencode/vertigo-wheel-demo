using UnityEngine;
using UnityEngine.UI;
using VertigoWheelDemo.Gameplay;

namespace VertigoWheelDemo.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class CashOutButtonView : MonoBehaviour
    {
        [SerializeField] private Button cashOutButton;
        [SerializeField] private GameSessionController sessionController;

        private GameSessionController subscribedController;

        private void OnValidate()
        {
            cashOutButton = GetComponent<Button>();
        }

        private void Awake()
        {
            cashOutButton = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (sessionController == null)
            {
                cashOutButton.interactable = false;
                Debug.LogError(
                    "Ödülleri al butonunun Controller bağlantısı eksik.",
                    this);
                return;
            }

            subscribedController = sessionController;

            cashOutButton.onClick.AddListener(HandleCashOutClicked);
            subscribedController.StateChanged += RefreshButton;

            RefreshButton();
        }

        private void OnDisable()
        {
            if (cashOutButton != null)
            {
                cashOutButton.onClick.RemoveListener(
                    HandleCashOutClicked);
            }

            if (subscribedController != null)
            {
                subscribedController.StateChanged -= RefreshButton;
                subscribedController = null;
            }
        }

        private void HandleCashOutClicked()
        {
            if (subscribedController != null)
                subscribedController.RequestCashOut();
        }

        private void RefreshButton()
        {
            cashOutButton.interactable =
                subscribedController != null &&
                subscribedController.CanCashOut;
        }
    }
}