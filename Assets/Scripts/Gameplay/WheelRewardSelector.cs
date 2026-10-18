using System;
using System.Collections.Generic;
using VertigoWheelDemo.Data;

namespace VertigoWheelDemo.Gameplay
{
    // Select once per zone. A displayed wheel must never change on a button press.
    public sealed class WheelRewardSelector
    {
        private readonly Random random;

        public WheelRewardSelector(Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public WheelSlotData[] Select(WheelDefinition definition)
        {
            if (definition.RewardPool == null || definition.RewardPool.Count == 0)
            {
                var configured = new WheelSlotData[definition.SlotCount];
                for (int i = 0; i < configured.Length; i++)
                    configured[i] = definition.GetSlot(i);
                return configured;
            }

            var candidates = new List<WheelSlotData>();
            foreach (WheelSlotData slot in definition.RewardPool)
            {
                if (slot == null || slot.Reward == null || slot.Amount <= 0 ||
                    slot.Reward.Type == RewardType.Bomb)
                    continue;

                if (!candidates.Exists(entry => entry.Reward == slot.Reward))
                    candidates.Add(slot);
            }

            WheelSlotData bomb = null;
            for (int i = 0; i < definition.SlotCount; i++)
            {
                WheelSlotData slot = definition.GetSlot(i);
                if (slot?.Reward != null && slot.Reward.Type == RewardType.Bomb)
                    bomb = slot;
            }

            int rewardCount = definition.SlotCount - (bomb == null ? 0 : 1);
            if (candidates.Count < rewardCount)
                throw new InvalidOperationException("The reward pool needs enough distinct rewards for this wheel.");

            Shuffle(candidates);
            var selected = new List<WheelSlotData>(definition.SlotCount);

            // Always offer both currencies, then fill the remaining slots with varied items.
            foreach (RewardType type in new[] { RewardType.Gold, RewardType.Cash })
            {
                int index = candidates.FindIndex(slot => slot.Reward.Type == type);
                if (index >= 0 && selected.Count < rewardCount)
                {
                    selected.Add(candidates[index]);
                    candidates.RemoveAt(index);
                }
            }

            for (int i = 0; selected.Count < rewardCount; i++)
                selected.Add(candidates[i]);
            if (bomb != null)
                selected.Add(bomb);

            Shuffle(selected);
            return selected.ToArray();
        }

        private void Shuffle(List<WheelSlotData> slots)
        {
            for (int i = slots.Count - 1; i > 0; i--)
            {
                int other = random.Next(i + 1);
                (slots[i], slots[other]) = (slots[other], slots[i]);
            }
        }
    }
}
