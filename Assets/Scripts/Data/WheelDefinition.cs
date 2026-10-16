using System;
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