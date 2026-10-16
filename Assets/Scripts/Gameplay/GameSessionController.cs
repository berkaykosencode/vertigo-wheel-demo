using System;
using UnityEngine;
using VertigoWheelDemo.Data;
using VertigoWheelDemo.UI;

namespace VertigoWheelDemo.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameSessionController : MonoBehaviour
    {
        [SerializeField] private WheelDefinition bronzeWheel;
        [SerializeField] private WheelDefinition silverWheel;
        [SerializeField] private WheelDefinition goldenWheel;

        [SerializeField] private WheelSpinView wheelSpinView;
        [SerializeField] private WheelContentView wheelContentView;

        private readonly GameSession session = new GameSession();

        private WheelDefinition wheelDefinition;
        private WheelSpinView subscribedWheel;

        public event Action StateChanged;
        public event Action<WheelSlotData, int> SpinResolved;

        public bool IsGameOver => session.IsGameOver;

        public bool HasCashedOut => session.HasCashedOut;

        public int CurrentZone => session.CurrentZone;

        public bool CanSpin =>
            isActiveAndEnabled &&
            wheelDefinition != null &&
            subscribedWheel != null &&
            subscribedWheel.isActiveAndEnabled &&
            !subscribedWheel.IsSpinning &&
            !session.IsGameOver;

        public bool CanCashOut => CanSpin && session.CanCashOut;

        private void OnEnable()
        {
            if (bronzeWheel == null ||
                silverWheel == null ||
                goldenWheel == null ||
                wheelSpinView == null ||
                wheelContentView == null)
            {
                Debug.LogError(
                    "Oyun yöneticisinin bağlantıları eksik.", this);
                return;
            }

            wheelDefinition = GetWheelForZone(CurrentZone);

            subscribedWheel = wheelSpinView;
            subscribedWheel.SpinningChanged += HandleSpinningChanged;
            subscribedWheel.SpinCompleted += HandleSpinCompleted;

            StateChanged?.Invoke();
        }

        private void Start()
        {
            if (subscribedWheel == null)
                return;

            RefreshWheel();
            StateChanged?.Invoke();
        }

        private void OnDisable()
        {
            if (subscribedWheel != null)
            {
                subscribedWheel.SpinningChanged -= HandleSpinningChanged;
                subscribedWheel.SpinCompleted -= HandleSpinCompleted;
                subscribedWheel = null;
            }

            StateChanged?.Invoke();
        }

        private WheelDefinition GetWheelForZone(int zone)
        {
            if (zone % 30 == 0)
                return goldenWheel;

            if (zone % 5 == 0)
                return silverWheel;

            return bronzeWheel;
        }

        private void RefreshWheel()
        {
            wheelDefinition = GetWheelForZone(CurrentZone);
            int[] amounts = new int[wheelDefinition.SlotCount];

            for(int i = 0; i < wheelDefinition.SlotCount; i++)
            {
                WheelSlotData slot = wheelDefinition.GetSlot(i);

                if(slot == null || slot.Reward == null)
                {
                    Debug.LogError($"Slot {i} bağlantısı eksik.", this);
                    return;
                }

                amounts[i] = session.CalculateRewardAmount(slot);
            }
            
            wheelContentView.SetDefinition(wheelDefinition, amounts);
        }

        public void RequestSpin()
        {
            if (!CanSpin)
                return;

            session.PrepareSpin();
            RefreshWheel();

            int slotIndex = UnityEngine.Random.Range(
                0, wheelDefinition.SlotCount);

            subscribedWheel.SpinToSlot(slotIndex);
        }

        public void RequestCashOut()
        {
            if(!CanCashOut)
                return;
            
            if(!session.TryCashOut())
                return;
            
            StateChanged?.Invoke();
        }

        private void HandleSpinningChanged(bool isSpinning)
        {
            StateChanged?.Invoke();
        }

        private void HandleSpinCompleted(int slotIndex)
        {
            if (session.IsGameOver)
                return;

            WheelSlotData slot = wheelDefinition.GetSlot(slotIndex);
            int earnedAmount = session.ApplySpin(slot);

            StateChanged?.Invoke();
            SpinResolved?.Invoke(slot, earnedAmount);

            Debug.Log(
                $"Bölge: {CurrentZone} | " +
                $"Altın: {GetAmount(RewardType.Gold)} | " +
                $"Para: {GetAmount(RewardType.Cash)} | " +
                $"Oyun bitti: {IsGameOver}",
                this);
        }

        public int GetAmount(RewardType type)
        {
            return session.GetAmount(type);
        }

        [ContextMenu("Reset Session")]
        public void ResetSession()
        {
            if (!Application.isPlaying || subscribedWheel == null)
                return;

            if (subscribedWheel.IsSpinning)
                return;

            session.Reset();
            RefreshWheel();

            StateChanged?.Invoke();
            Debug.Log("Yeni oturum başladı.", this);
        }
    }
}