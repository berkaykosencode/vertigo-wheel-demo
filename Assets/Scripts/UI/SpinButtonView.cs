using UnityEngine;
using UnityEngine.UI;
using VertigoWheelDemo.Gameplay;

namespace VertigoWheelDemo.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class SpinButtonView : MonoBehaviour
    {
        [SerializeField] private Button spinButton;
        [SerializeField] private GameSessionController sessionController;

        private GameSessionController subscribedController;

        private void OnValidate()
        {
            spinButton = GetComponent<Button>();
        }

        private void Awake()
        {
            spinButton = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if(sessionController == null)
            {
                spinButton.interactable = false;
                Debug.LogError("Lack of connection between session and controler.", this);
                return;
            }

            subscribedController = sessionController;
            spinButton.onClick.AddListener(HandleSpinClicked);
            subscribedController.StateChanged += RefreshButton;
            RefreshButton();
        }

        private void OnDisable()
        {
            if(spinButton != null)
                spinButton.onClick.RemoveListener(HandleSpinClicked);
            
            if(subscribedController != null)
            {
                subscribedController.StateChanged -= RefreshButton;
                subscribedController = null;
            }
        }

        private void HandleSpinClicked()
        {
            if(subscribedController != null)
                subscribedController.RequestSpin();
        }

        private void RefreshButton()
        {
            spinButton.interactable = subscribedController != null && subscribedController.CanSpin;
        }
    }
}
