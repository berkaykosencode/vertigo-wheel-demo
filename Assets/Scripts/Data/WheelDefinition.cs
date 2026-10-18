using System;
using System.Collections.Generic;
using UnityEngine;

namespace VertigoWheelDemo.Data
{
    [CreateAssetMenu(
        fileName = "wheel_new",
        menuName = "Vertigo/Wheel Definition")]
    
    public sealed class WheelDefinition : ScriptableObject
    {
        private const int RequiredSlotCount = 8;

        [SerializeField] private Sprite baseSprite;
        [SerializeField] private Sprite indicatorSprite;

        public Sprite BaseSprite => baseSprite;
        public Sprite IndicatorSprite => indicatorSprite;


        [SerializeField]
        private WheelSlotData[] slots = new WheelSlotData[RequiredSlotCount];

        // Leave the pool empty to use the eight explicitly configured slots.
        [Tooltip("Optional randomized reward pool. Leave empty to play the eight configured Slots exactly. The Slots still determine whether this wheel includes a bomb.")]
        [SerializeField] private WheelSlotData[] rewardPool = Array.Empty<WheelSlotData>();

        public IReadOnlyList<WheelSlotData> RewardPool => rewardPool;

        public int SlotCount => slots.Length;

        public WheelSlotData GetSlot(int index){
            return slots[index];
        }

        private void OnValidate()
        {
            if (slots == null || slots.Length != RequiredSlotCount)
            {
                Array.Resize(ref slots, RequiredSlotCount);
            }
            
            for(int i = 0; i < slots.Length; i++)
            {
                slots[i] ??= new WheelSlotData();
            }
        }
    }
}
