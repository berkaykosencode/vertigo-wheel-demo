using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VertigoWheelDemo.Data;
using VertigoWheelDemo.Gameplay;

namespace VertigoWheelDemo.UI
{
    public sealed class BankView : MonoBehaviour
    {
        [SerializeField] private GameSessionController sessionController;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private CanvasGroup overlay;
        [SerializeField] private RectTransform animatedPanel;
        [SerializeField] private RectTransform content;
        [SerializeField] private BankRewardRowView rowTemplate;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private ScrollRect scrollRect;

        private readonly List<BankRewardRowView> rows = new List<BankRewardRowView>();
        private Sequence entrance;

        private void Awake()
        {
            overlay.alpha = 0;
            overlay.blocksRaycasts = false;
            overlay.interactable = false;
        }

        private void OnEnable()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            sessionController.StateChanged += RefreshButton;
            RefreshButton();
        }

        private void OnDisable()
        {
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            sessionController.StateChanged -= RefreshButton;
            entrance?.Kill();
            sessionController.SetBankOpen(false);
        }

        private void RefreshButton()
        {
            openButton.interactable = !sessionController.IsSpinning && !sessionController.IsRewardPending && !sessionController.IsTransitioning;
        }

        private void Open()
        {
            if (sessionController.IsSpinning || sessionController.IsRewardPending || sessionController.IsTransitioning)
                return;

            sessionController.SetBankOpen(true);
            var entries = new List<KeyValuePair<RewardDefinition, int>>(sessionController.Rewards);
            entries.Sort((a, b) =>
            {
                int type = a.Key.Type.CompareTo(b.Key.Type);
                return type != 0 ? type : string.CompareOrdinal(a.Key.RewardId, b.Key.RewardId);
            });

            for (int i = 0; i < entries.Count; i++)
            {
                if (i == rows.Count)
                    rows.Add(Instantiate(rowTemplate, content));
                rows[i].gameObject.SetActive(true);
                rows[i].Show(entries[i].Key, entries[i].Value);
            }
            for (int i = entries.Count; i < rows.Count; i++)
                rows[i].gameObject.SetActive(false);

            emptyText.gameObject.SetActive(entries.Count == 0);
            summaryText.text = sessionController.HasCashedOut ? "Ödüllerin alındı. İşte bu oturumdaki kazançların." :
                "Oturum bankası · Güvenli veya süper bölgede ödüllerini alabilirsin.";
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scrollRect.verticalNormalizedPosition = 1;
            overlay.blocksRaycasts = true;
            overlay.interactable = true;
            animatedPanel.localScale = Vector3.one * 0.94f;
            entrance?.Kill();
            entrance = DOTween.Sequence().Append(overlay.DOFade(1f, 0.15f))
                .Join(animatedPanel.DOScale(1f, 0.22f).SetEase(Ease.OutCubic));
        }

        private void Close()
        {
            entrance?.Kill();
            overlay.alpha = 0;
            overlay.blocksRaycasts = false;
            overlay.interactable = false;
            sessionController.SetBankOpen(false);
        }
    }
}
