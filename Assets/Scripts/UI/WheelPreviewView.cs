using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VertigoWheelDemo.Data;
using VertigoWheelDemo.Gameplay;

namespace VertigoWheelDemo.UI
{
    public sealed class WheelPreviewView : MonoBehaviour
    {
        [SerializeField] private Image wheelImage;
        [SerializeField] private Image indicatorImage;
        [SerializeField] private Image[] rewardImages;
        [SerializeField] private TMP_Text zoneText;
        [SerializeField] private RectTransform animatedVisual;

        public RectTransform AnimatedVisual => animatedVisual;
        public CanvasGroup Group => GetComponent<CanvasGroup>();
        public TMP_Text ZoneLabel => zoneText;
        public float WheelWidth => wheelImage.rectTransform.sizeDelta.x;

        public void Show(WheelDefinition definition, WheelSlotData[] slots, int zone, float angle = 0)
        {
            gameObject.SetActive(definition != null && slots != null);
            if (definition == null || slots == null)
                return;

            wheelImage.sprite = definition.BaseSprite;
            indicatorImage.sprite = definition.IndicatorSprite;
            wheelImage.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            zoneText.text = $"BÖLGE {zone:00}";
            for (int i = 0; i < rewardImages.Length; i++)
            {
                rewardImages[i].sprite = slots[i].Reward.Icon;
                rewardImages[i].rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
            }
        }
    }
}
