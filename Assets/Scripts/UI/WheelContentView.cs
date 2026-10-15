using UnityEngine;
using UnityEngine.UI;
using VertigoWheelDemo.Data;

namespace VertigoWheelDemo.UI
{
    [DisallowMultipleComponent]
    public sealed class WheelContentView : MonoBehaviour
    {
        [SerializeField] private WheelDefinition wheelDefinition;
        [SerializeField] private Image[] rewardImages = new Image[8];

        private void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            if(wheelDefinition == null)
            {
                Debug.LogError("Lack of wheel definition connection.", this);
                return;
            }

            if(rewardImages == null || rewardImages.Length != wheelDefinition.SlotCount)
            {
                Debug.LogError("Number of reward images does not match with number of slots.", this);
                return;
            }

            for(int i = 0; i < wheelDefinition.SlotCount; i++)
            {
                WheelSlotData slot = wheelDefinition.GetSlot(i);

                if(rewardImages[i] == null || slot == null || slot.Reward == null || slot.Reward.Icon == null)
                {
                    Debug.LogError($"No connection for slot {i}", this);
                    continue;
                }

                rewardImages[i].sprite = slot.Reward.Icon;
            }
        }
     }
}