using UnityEngine;

namespace VertigoWheelDemo.Data
{
    [CreateAssetMenu(
        fileName = "reward_new",
        menuName = "Vertigo/Reward Definition")]
    
    public sealed class RewardDefinition : ScriptableObject
    {
        [SerializeField] private string rewardId;
        [SerializeField] private string displayName;
        [SerializeField] private RewardType rewardType;
        [SerializeField] private Sprite icon;

        public string RewardId => rewardId;
        public string DisplayName => displayName;
        public RewardType Type => rewardType;
        public Sprite Icon => icon;

    }
}
