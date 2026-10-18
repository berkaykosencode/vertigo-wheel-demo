using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VertigoWheelDemo.Data;

namespace VertigoWheelDemo.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class WheelContentView : MonoBehaviour
    {
        [SerializeField] private WheelDefinition wheelDefinition;
        [SerializeField] private Image indicatorImage;
        [SerializeField] private Image[] rewardImages = new Image[8];
        [SerializeField] private TMP_Text[] rewardAmountTexts =
            new TMP_Text[8];

        private Image wheelImage;
        private int[] rewardAmounts;
        private WheelSlotData[] displayedSlots;

        private void Awake()
        {
            wheelImage = GetComponent<Image>();
        }

        private void LateUpdate()
        {
            if (rewardImages == null)
                return;

            Quaternion counterRotation =
                Quaternion.Inverse(transform.localRotation);

            for (int i = 0; i < rewardImages.Length; i++)
            {
                if (rewardImages[i] == null)
                    continue;

                rewardImages[i].rectTransform.localRotation =
                    counterRotation;
            }
        }

        public void SetDefinition(
            WheelDefinition definition,
            int[] amounts)
        {
            var slots = new WheelSlotData[definition.SlotCount];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = definition.GetSlot(i);
            SetDefinition(definition, slots, amounts);
        }

        public void SetDefinition(WheelDefinition definition, WheelSlotData[] slots, int[] amounts)
        {
            wheelDefinition = definition;
            displayedSlots = slots;
            rewardAmounts = amounts;
            Refresh();
        }

        public void Refresh()
        {
            if (wheelDefinition == null || indicatorImage == null)
            {
                Debug.LogError("Çark bağlantıları eksik.", this);
                return;
            }

            int slotCount = wheelDefinition.SlotCount;

            if (rewardImages == null ||
                rewardImages.Length != slotCount ||
                rewardAmountTexts == null ||
                rewardAmountTexts.Length != slotCount ||
                rewardAmounts == null ||
                rewardAmounts.Length != slotCount)
            {
                Debug.LogError(
                    "Çarktaki görsel veya miktar sayıları hatalı.",
                    this);
                return;
            }

            wheelImage.sprite = wheelDefinition.BaseSprite;
            indicatorImage.sprite = wheelDefinition.IndicatorSprite;

            for (int i = 0; i < slotCount; i++)
            {
                WheelSlotData slot = displayedSlots[i];

                if (rewardImages[i] == null ||
                    rewardAmountTexts[i] == null ||
                    slot == null ||
                    slot.Reward == null ||
                    slot.Reward.Icon == null)
                {
                    Debug.LogError(
                        $"Slot {i} bağlantıları eksik.",
                        this);
                    continue;
                }

                rewardImages[i].sprite = slot.Reward.Icon;

                bool isBomb = slot.Reward.Type == RewardType.Bomb;

                rewardAmountTexts[i].gameObject.SetActive(!isBomb);
                rewardAmountTexts[i].text =
                    rewardAmounts[i].ToString();
            }
        }
    }
}
