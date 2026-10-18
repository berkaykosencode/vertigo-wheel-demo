using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VertigoWheelDemo.Data;
using VertigoWheelDemo.Gameplay;

namespace VertigoWheelDemo.UI
{
    [RequireComponent(typeof(Button), typeof(CanvasGroup))]
    public sealed class RewardRevealView : MonoBehaviour
    {
        [SerializeField] private GameSessionController sessionController;
        [SerializeField] private Button dismissButton;
        [SerializeField] private CanvasGroup overlay;
        [SerializeField] private RectTransform animatedContent;
        [SerializeField] private RectTransform starFlash;
        [SerializeField] private Image glowImage;
        [SerializeField] private Image rewardImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text rewardNameText;
        [SerializeField] private TMP_Text amountText;

        private Sequence entrance;
        private Tween starTween;
        private Tween glowTween;

        private void OnValidate()
        {
            dismissButton = GetComponent<Button>();
            overlay = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            dismissButton = GetComponent<Button>();
            overlay = GetComponent<CanvasGroup>();
            Hide();
        }

        private void OnEnable()
        {
            sessionController.SpinResolved += Show;
            dismissButton.onClick.AddListener(Dismiss);
        }

        private void OnDisable()
        {
            sessionController.SpinResolved -= Show;
            dismissButton.onClick.RemoveListener(Dismiss);
            Hide();
        }

        private void Show(WheelSlotData slot, int amount)
        {
            Hide();
            bool bomb = slot.Reward.Type == RewardType.Bomb;
            titleText.text = bomb ? "BOMBA!" : "ÖDÜLÜNÜ KAZANDIN";
            titleText.color = bomb ? new Color32(255, 113, 105, 255) : new Color32(255, 217, 127, 255);
            rewardImage.sprite = slot.Reward.Icon;
            rewardNameText.text = bomb ? "Bu oturumdaki ödüller kaybedildi" : slot.Reward.DisplayName;
            amountText.text = bomb ? "" : $"× {amount:N0}";
            glowImage.color = bomb ? new Color(1f, 0.14f, 0.08f, 0.65f) : new Color(1f, 0.7f, 0.24f, 0.8f);
            starFlash.GetComponent<Image>().color = bomb ? new Color(1f, 0.22f, 0.12f, 0.3f) : new Color(1f, 0.78f, 0.35f, 0.38f);
            overlay.blocksRaycasts = true;
            overlay.interactable = true;
            animatedContent.localScale = Vector3.one * 0.78f;
            entrance = DOTween.Sequence()
                .Append(overlay.DOFade(1f, 0.18f))
                .Join(animatedContent.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
            starTween = starFlash.DOLocalRotate(new Vector3(0, 0, 360), 24f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart);
            glowTween = glowImage.rectTransform.DOScale(1.12f, 1f).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void Dismiss()
        {
            if (!sessionController.IsRewardPending)
                return;
            Hide();
            sessionController.AcknowledgeReward();
        }

        private void Hide()
        {
            entrance?.Kill();
            starTween?.Kill();
            glowTween?.Kill();
            overlay.alpha = 0;
            overlay.blocksRaycasts = false;
            overlay.interactable = false;
            starFlash.localRotation = Quaternion.identity;
            glowImage.rectTransform.localScale = Vector3.one;
        }
    }
}
