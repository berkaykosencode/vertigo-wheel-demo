using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VertigoWheelDemo.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform visual;
        private Button button;
        private Tween tween;
        private void Awake() => button = GetComponent<Button>();
        public void OnPointerDown(PointerEventData data)
        {
            if (button.interactable) Animate(0.94f);
        }
        public void OnPointerUp(PointerEventData data) => Animate(1f);
        public void OnPointerExit(PointerEventData data) => Animate(1f);
        private void Animate(float scale)
        {
            tween?.Kill();
            tween = visual.DOScale(scale, 0.1f).SetEase(Ease.OutQuad);
        }
        private void OnDisable()
        {
            tween?.Kill();
            if (visual != null)
                visual.localScale = Vector3.one;
        }
    }
}
