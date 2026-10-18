using System;
using UnityEngine;

namespace VertigoWheelDemo.Data
{
    [Serializable]
    public sealed class WheelSlotData
    {
        [SerializeField] private RewardDefinition reward;
        [SerializeField, Min(0)] private int amount = 1;

        public RewardDefinition Reward => reward;
        public int Amount => amount; 

        public WheelSlotData() { }

        public WheelSlotData(RewardDefinition reward, int amount)
        {
            this.reward = reward;
            this.amount = amount;
        }
    }
}
