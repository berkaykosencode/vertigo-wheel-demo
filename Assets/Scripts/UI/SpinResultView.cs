using TMPro;
using UnityEngine;
using VertigoWheelDemo.Data;
using VertigoWheelDemo.Gameplay;

namespace VertigoWheelDemo.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class SpinResultView : MonoBehaviour
    {
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text totalsText;
        [SerializeField] private GameSessionController sessionController;

        private GameSessionController subscribedController;

        private void OnValidate()
        {
            resultText = GetComponent<TMP_Text>();
        }

        private void Awake()
        {
            resultText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (sessionController == null || totalsText == null)
            {
                Debug.LogError("Sonuç ekranının bağlantıları eksik.", this);
                return;
            }

            subscribedController = sessionController;
            subscribedController.StateChanged += Refresh;
            subscribedController.SpinResolved += HandleSpinResolved;
            Refresh();
        }

        private void OnDisable()
        {
            if (subscribedController != null)
            {
                subscribedController.StateChanged -= Refresh;
                subscribedController.SpinResolved -= HandleSpinResolved;
                subscribedController = null;
            }
        }

        private void Refresh()
        {
            totalsText.text =
                $"Altın: {subscribedController.GetAmount(RewardType.Gold)}" +
                $"    Para: {subscribedController.GetAmount(RewardType.Cash)}";

            if (subscribedController.IsGameOver)
            {
                resultText.text = "Bomba çıktı! Ödüller kaybedildi.";
            }
            else if (subscribedController.CanSpin)
            {
                resultText.text = "Çarkı çevir";
            }
            else
            {
                resultText.text = "Çark dönüyor...";
            }
        }

        private void HandleSpinResolved(WheelSlotData slot)
        {
            if (subscribedController.IsGameOver)
                return;

            resultText.text =
                $"{slot.Reward.DisplayName} × {slot.Amount}";
        }
    }
}