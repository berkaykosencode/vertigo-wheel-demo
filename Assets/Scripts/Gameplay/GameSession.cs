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

        public int CurrentZone{ get; private set; } = 1;

        public bool HasCompletedSpin{ get; private set; }

        public bool HasCashedOut{ get; private set; }

        public bool CanCashOut =>
                !IsGameOver &&
                CurrentZone % 5 == 0;

        public IReadOnlyDictionary<RewardDefinition, int> Rewards => rewards;

        public int ApplySpin(WheelSlotData slot)
        {
            if (IsGameOver)
                return 0;
            
            if(slot == null || slot.Reward == null)
            {
                throw new ArgumentException("Missing connection between slot and the reward.", nameof(slot));
            }

            if(slot.Reward.Type == RewardType.Bomb)
            {
                rewards.Clear();
                IsGameOver = true;
                return 0;
            }

            if(slot.Amount <= 0)
            {
                throw new ArgumentException("Amount of reward must be positive.", nameof(slot));
            }

            int earnedAmount = CalculateRewardAmount(slot);

            rewards.TryGetValue(slot.Reward, out int previousAmount);
            rewards[slot.Reward] = previousAmount + earnedAmount;

            HasCompletedSpin = true;

            return earnedAmount;
        }

        public int CalculateRewardAmount(WheelSlotData slot)
        {
            return CalculateRewardAmount(slot, CurrentZone);
        }

        public static int CalculateRewardAmount(WheelSlotData slot, int zone)
        {
            RewardType type = slot.Reward.Type;

            if (type == RewardType.Gold || type == RewardType.Cash)
            {
                int bonus = slot.Amount * (zone - 1) / 10;
                return slot.Amount + bonus;
            }

            return slot.Amount;
        }

        public void PrepareSpin()
        {
            if(IsGameOver)
                return;

            if (HasCompletedSpin)
            {
                CurrentZone++;
                HasCompletedSpin = false;
            }
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
        
        public bool TryCashOut()
        {
            if(!CanCashOut)
                return false;

            HasCashedOut = true;
            IsGameOver = true;

            return true;
        }

        public void Reset()
        {
            rewards.Clear();
            IsGameOver = false;
            HasCashedOut = false;
            CurrentZone = 1;
            HasCompletedSpin = false;
        }
    }
}
