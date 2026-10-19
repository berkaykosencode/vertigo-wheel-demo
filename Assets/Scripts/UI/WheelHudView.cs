using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VertigoWheelDemo.Gameplay;

namespace VertigoWheelDemo.UI
{
    public sealed class WheelHudView : MonoBehaviour
    {
        [SerializeField] private GameSessionController sessionController;
        [SerializeField] private WheelSpinView currentWheel;
        [SerializeField] private WheelPreviewView previousWheel;
        [SerializeField] private WheelPreviewView nextWheel;
        [SerializeField] private TMP_Text zoneText;
        [SerializeField] private TMP_Text zoneKindText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private TMP_Text bankCountText;
        [SerializeField] private Image cashOutImage;
        [SerializeField] private TMP_Text cashOutText;
        [SerializeField] private Sprite availableButton;
        [SerializeField] private Sprite unavailableButton;
        [SerializeField] private GameObject restartButton;
        [SerializeField] private Image wheelGlow;

        private void OnEnable()
        {
            sessionController.StateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            sessionController.StateChanged -= Refresh;
            currentWheel.SetIdleRotation(false);
            previousWheel.SetIdleRotation(false);
            nextWheel.SetIdleRotation(false);
        }

        private void Refresh()
        {
            int zone = sessionController.CurrentZone;
            bool super = zone % 30 == 0;
            bool safe = zone % 5 == 0;
            Color accent = super ? new Color32(255, 206, 83, 255) :
                safe ? new Color32(183, 225, 244, 255) : new Color32(229, 165, 94, 255);

            zoneText.text = $"BÖLGE {zone:00}";
            zoneKindText.text = super ? "SÜPER ÇARK" : safe ? "GÜVENLİ ÇARK" : "NORMAL ÇARK";
            zoneKindText.color = accent;
            wheelGlow.color = new Color(accent.r, accent.g, accent.b, 0.16f);
            bankCountText.text = $"{sessionController.Rewards.Count} ÇEŞİT ÖDÜL";
            previousWheel.Show(sessionController.PreviousDefinition, sessionController.PreviousSlots, zone - 1, sessionController.PreviousWheelAngle);
            nextWheel.Show(sessionController.NextDefinition, sessionController.NextSlots, zone + 1);

            bool idle = sessionController.CanSpin;
            currentWheel.SetIdleRotation(idle);
            previousWheel.SetIdleRotation(idle);
            nextWheel.SetIdleRotation(idle);

            bool ended = sessionController.IsGameOver;
            restartButton.SetActive(ended && !sessionController.IsRewardPending);
            cashOutImage.gameObject.SetActive(!ended);
            cashOutImage.sprite = sessionController.CanCashOut ? availableButton : unavailableButton;
            cashOutText.color = sessionController.CanCashOut ? new Color32(37, 25, 9, 255) : new Color32(183, 190, 203, 255);

            if (sessionController.HasCashedOut)
                hintText.text = "ÖDÜLLERİN ALINDI  ·  Bankadan tüm kazançlarını görebilirsin.";
            else if (ended)
                hintText.text = "BOMBA ÇIKTI  ·  Bu oturumdaki ödüllerin kaybedildi.";
            else if (sessionController.IsRewardPending)
                hintText.text = "";
            else if (sessionController.IsTransitioning)
                hintText.text = "";
            else if (sessionController.IsSpinning)
                hintText.text = "Şansın dönüyor...";
            else if (safe)
                hintText.text = "BOMBA YOK  ·  Çevir veya ödüllerini alıp ayrıl.";
            else
                hintText.text = "Bomba tüm kazançlarını siler. Her 5. bölgede güvenle ayrılabilirsin.";
        }
    }
}
