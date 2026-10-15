using System;
using System.Collections.Generic;
using VertigoWheelDemo.Data;

namespace VertigoWheelDemo.Gameplay
{
    public sealed class GameSession
    {
        private readonly Dictionary<RewardDefinition, int> rewards = new Dictionary<RewardDefinition, int>();

        public bool IsGameOver
        {
            get;
            private set;
        }

        public IReadOnlyDictionary<RewardDefinition, int> Rewards => rewards;

        public void ApplySpin(WheelSlotData slot)
        {
            if (IsGameOver)
                return;
            
            if(slot == null || slot.Reward == null)
            {
                throw new ArgumentException("Missing connection between slot and the reward.", nameof(slot));
            }

            if(slot.Reward.Type == RewardType.Bomb)
            {
                rewards.Clear();
                IsGameOver = true;
                return;
            }

            if(slot.Amount <= 0)
            {
                throw new ArgumentException("Amount of reward must be positive.", nameof(slot));
            }

            rewards.TryGetValue(slot.Reward, out int previousAmount);
            rewards[slot.Reward] = previousAmount + slot.Amount;
        }

        public int GetAmount(RewardType type)
        {
            int total = 0;

            foreach(var entry in rewards)
            {
                if(entry.Key.Type == type)
                {
                    total += entry.Value;
                }
            }

            return total;
        }
        

        public void Reset()
        {
            rewards.Clear();
            IsGameOver = false;
        }
    }
}