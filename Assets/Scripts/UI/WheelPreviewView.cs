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
        [SerializeField, Min(0f)] private float idleRotationSpeed = 6f;

        private bool idleRotationEnabled;
        private WheelDefinition displayedDefinition;
        private WheelSlotData[] displayedSlots;
        private int displayedZone;

        public RectTransform AnimatedVisual => animatedVisual;
        public RectTransform WheelTransform => wheelImage.rectTransform;
        public CanvasGroup Group => GetComponent<CanvasGroup>();
        public TMP_Text ZoneLabel => zoneText;
        public float WheelWidth => wheelImage.rectTransform.sizeDelta.x;

        public void SetIdleRotation(bool enabled)
        {
            idleRotationEnabled = enabled;
        }

        private void Update()
        {
            if (idleRotationEnabled)
                WheelTransform.Rotate(0f, 0f, -idleRotationSpeed * Time.deltaTime);
        }

        private void LateUpdate()
        {
            Quaternion counterRotation = Quaternion.Inverse(WheelTransform.localRotation);
            foreach (Image image in rewardImages)
                image.rectTransform.localRotation = counterRotation;
        }

        public void Show(WheelDefinition definition, WheelSlotData[] slots, int zone, float angle = 0)
        {
            gameObject.SetActive(definition != null && slots != null);
            if (definition == null || slots == null)
                return;

            wheelImage.sprite = definition.BaseSprite;
            indicatorImage.sprite = definition.IndicatorSprite;
            // HUD refreshes must not rewind an unchanged preview's idle rotation.
            if (displayedDefinition != definition || displayedSlots != slots || displayedZone != zone)
                WheelTransform.localRotation = Quaternion.Euler(0, 0, angle);
            displayedDefinition = definition;
            displayedSlots = slots;
            displayedZone = zone;
            zoneText.text = $"BÖLGE {zone:00}";
            for (int i = 0; i < rewardImages.Length; i++)
            {
                rewardImages[i].sprite = slots[i].Reward.Icon;
                rewardImages[i].rectTransform.localRotation = Quaternion.Inverse(WheelTransform.localRotation);
            }
        }
    }
}
