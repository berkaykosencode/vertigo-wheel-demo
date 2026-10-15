using UnityEngine;
using UnityEngine.UI;
using VertigoWheelDemo.Gameplay;

namespace VertigoWheelDemo.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class RestartButtonView : MonoBehaviour
    {
        [SerializeField] private Button restartButton;
        [SerializeField] private GameSessionController sessionController;

        private GameSessionController subscribedController;

        private void OnValidate()
        {
            restartButton = GetComponent<Button>();
        }

        private void Awake()
        {
            restartButton = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if(sessionController == null)
            {
                restartButton.interactable = false;
                Debug.LogError("Session controler bağlantısı eksik", this);
                return;
            }

            subscribedController = sessionController;
            restartButton.onClick.AddListener(HandleRestartClicked);
            subscribedController.StateChanged += RefreshButton;
            RefreshButton();
        }

        private void OnDisable()
        {
            if(restartButton != null)
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            
            if(subscribedController != null)
            {
                subscribedController.StateChanged -= RefreshButton;
                subscribedController = null;
            }
        }

        private void HandleRestartClicked()
        {
            subscribedController.ResetSession();
        }

        private void RefreshButton()
        {
            restartButton.interactable =
                subscribedController != null &&
                subscribedController.isActiveAndEnabled &&
                subscribedController.IsGameOver;
        }   
    }
}