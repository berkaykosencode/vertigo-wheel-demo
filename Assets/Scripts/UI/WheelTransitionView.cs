using DG.Tweening;
using TMPro;
using UnityEngine;
using VertigoWheelDemo.Gameplay;

namespace VertigoWheelDemo.UI
{
    public sealed class WheelTransitionView : MonoBehaviour
    {
        [SerializeField] private GameSessionController sessionController;
        [SerializeField] private RectTransform currentVisual;
        [SerializeField] private CanvasGroup currentGroup;
        [SerializeField] private CanvasGroup spinButtonGroup;
        [SerializeField] private TMP_Text[] amountTexts;
        [SerializeField] private WheelPreviewView previousWheel;
        [SerializeField] private WheelPreviewView nextWheel;
        [SerializeField, Min(0.1f)] private float slideDuration = 0.58f;
        [SerializeField, Min(0.05f)] private float settleDuration = 0.16f;

        private Sequence transition;
        private Vector2 currentPosition;
        private Vector2 previousPosition;
        private Vector2 nextPosition;
        private float previousAlpha;
        private float nextAlpha;

        private void Awake()
        {
            currentPosition = currentVisual.anchoredPosition;
            previousPosition = previousWheel.AnimatedVisual.anchoredPosition;
            nextPosition = nextWheel.AnimatedVisual.anchoredPosition;
            previousAlpha = previousWheel.Group.alpha;
            nextAlpha = nextWheel.Group.alpha;
        }

        private void OnEnable()
        {
            sessionController.ZoneTransitionRequested += Play;
            sessionController.StateChanged += StopIfInterrupted;
        }

        private void OnDisable()
        {
            sessionController.ZoneTransitionRequested -= Play;
            sessionController.StateChanged -= StopIfInterrupted;
            transition?.Kill();
            transition = null;
            Restore();
            sessionController.CompleteZoneTransition();
        }

        private void StopIfInterrupted()
        {
            if (transition == null || sessionController.IsTransitioning)
                return;
            transition.Kill();
            transition = null;
            Restore();
        }

        private void Play()
        {
            transition?.Kill();
            Restore();

            RectTransform previous = previousWheel.AnimatedVisual;
            RectTransform next = nextWheel.AnimatedVisual;
            Vector2 outgoingTarget = currentVisual.parent.InverseTransformPoint(previous.position);
            Vector2 incomingTarget = next.parent.InverseTransformPoint(currentVisual.position);
            float heroWidth = currentVisual.sizeDelta.x;
            float outgoingScale = previousWheel.WheelWidth / heroWidth;
            float incomingScale = heroWidth / nextWheel.WheelWidth;

            // Move only the visual children; layout anchors and the center button stay fixed.
            transition = DOTween.Sequence().SetUpdate(true)
                .Append(currentVisual.DOAnchorPos(outgoingTarget, slideDuration).SetEase(Ease.InOutCubic))
                .Join(currentVisual.DOScale(outgoingScale, slideDuration).SetEase(Ease.InOutCubic))
                .Join(currentGroup.DOFade(previousAlpha, slideDuration))
                .Join(next.DOAnchorPos(incomingTarget, slideDuration).SetEase(Ease.InOutCubic))
                .Join(next.DOScale(incomingScale, slideDuration).SetEase(Ease.InOutCubic))
                .Join(nextWheel.Group.DOFade(1, slideDuration))
                .Join(previous.DOAnchorPos(previousPosition + new Vector2(-180, -30), slideDuration).SetEase(Ease.InOutCubic))
                .Join(previous.DOScale(0.78f, slideDuration).SetEase(Ease.InOutCubic))
                .Join(previousWheel.Group.DOFade(0, slideDuration * 0.5f))
                .Join(nextWheel.ZoneLabel.DOFade(0, slideDuration * 0.3f))
                .Join(spinButtonGroup.DOFade(0, slideDuration * 0.25f));

            foreach (TMP_Text text in amountTexts)
                transition.Join(text.DOFade(0, slideDuration * 0.35f));

            transition.AppendCallback(() =>
            {
                // At the same pose and size, exchange the preview for the playable wheel.
                nextWheel.Group.alpha = 0;
                currentGroup.alpha = 0;
                RestorePositions();
                sessionController.CommitZoneTransition();
                currentGroup.alpha = 1;
                previousWheel.Group.alpha = previousAlpha;
                nextWheel.ZoneLabel.alpha = 1;
            });

            transition.Append(nextWheel.Group.DOFade(nextAlpha, settleDuration))
                .Join(spinButtonGroup.DOFade(1, settleDuration));
            foreach (TMP_Text text in amountTexts)
                transition.Join(text.DOFade(1, settleDuration));

            transition.OnComplete(() =>
            {
                transition = null;
                Restore();
                sessionController.CompleteZoneTransition();
            });
        }

        private void RestorePositions()
        {
            currentVisual.anchoredPosition = currentPosition;
            currentVisual.localScale = Vector3.one;
            previousWheel.AnimatedVisual.anchoredPosition = previousPosition;
            previousWheel.AnimatedVisual.localScale = Vector3.one;
            nextWheel.AnimatedVisual.anchoredPosition = nextPosition;
            nextWheel.AnimatedVisual.localScale = Vector3.one;
        }

        private void Restore()
        {
            RestorePositions();
            currentGroup.alpha = 1;
            spinButtonGroup.alpha = 1;
            previousWheel.Group.alpha = previousAlpha;
            nextWheel.Group.alpha = nextAlpha;
            previousWheel.ZoneLabel.alpha = 1;
            nextWheel.ZoneLabel.alpha = 1;
            foreach (TMP_Text text in amountTexts)
                text.alpha = 1;
        }
    }
}
