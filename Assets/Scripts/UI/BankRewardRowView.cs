using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VertigoWheelDemo.Data;

namespace VertigoWheelDemo.UI
{
    public sealed class BankRewardRowView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text amountText;

        public void Show(RewardDefinition reward, int amount)
        {
            iconImage.sprite = reward.Icon;
            nameText.text = reward.DisplayName;
            amountText.text = $"× {amount:N0}";
        }
    }
}
