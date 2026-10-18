using System;
using System.Collections.Generic;
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
        private readonly WheelRewardSelector rewardSelector = new WheelRewardSelector(new System.Random());
        private WheelSlotData[] currentSlots;
        private WheelSlotData[] nextSlots;
        private WheelSlotData[] previousSlots;
        private WheelDefinition previousDefinition;
        private bool transitionCommitted;

        public bool IsRewardPending { get; private set; }
        public bool IsBankOpen { get; private set; }
        public bool IsTransitioning { get; private set; }
        public float PreviousWheelAngle { get; private set; }
        public bool IsSpinning => subscribedWheel != null && subscribedWheel.IsSpinning;
        public IReadOnlyDictionary<RewardDefinition, int> Rewards => session.Rewards;
        public WheelDefinition CurrentDefinition => wheelDefinition;
        public WheelDefinition PreviousDefinition => previousDefinition;
        public WheelDefinition NextDefinition => GetWheelForZone(CurrentZone + 1);
        public WheelSlotData[] CurrentSlots => currentSlots;
        public WheelSlotData[] PreviousSlots => previousSlots;
        public WheelSlotData[] NextSlots => nextSlots;

        public event Action StateChanged;
        public event Action<WheelSlotData, int> SpinResolved;
        public event Action ZoneTransitionRequested;

        public bool IsGameOver => session.IsGameOver;

        public bool HasCashedOut => session.HasCashedOut;

        public int CurrentZone => session.CurrentZone;

        public bool CanSpin =>
            isActiveAndEnabled &&
            wheelDefinition != null &&
            subscribedWheel != null &&
            subscribedWheel.isActiveAndEnabled &&
            !subscribedWheel.IsSpinning &&
            !session.IsGameOver &&
            !IsRewardPending && !IsBankOpen && !IsTransitioning;

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
            if (currentSlots == null)
                PrepareLayouts();

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
            CompleteZoneTransition();
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

        private void PrepareLayouts()
        {
            wheelDefinition = GetWheelForZone(CurrentZone);
            currentSlots = rewardSelector.Select(wheelDefinition);
            nextSlots = rewardSelector.Select(NextDefinition);
        }

        private void RefreshWheel()
        {
            int[] amounts = new int[wheelDefinition.SlotCount];

            for(int i = 0; i < wheelDefinition.SlotCount; i++)
            {
                WheelSlotData slot = currentSlots[i];

                if(slot == null || slot.Reward == null)
                {
                    Debug.LogError($"Slot {i} bağlantısı eksik.", this);
                    return;
                }

                amounts[i] = session.CalculateRewardAmount(slot);
            }
            
            wheelContentView.SetDefinition(wheelDefinition, currentSlots, amounts);
        }

        public void RequestSpin()
        {
            if (!CanSpin)
                return;

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

            int completedZone = CurrentZone;

            WheelSlotData slot = currentSlots[slotIndex];
            int earnedAmount = session.ApplySpin(slot);

            IsRewardPending = true;
            StateChanged?.Invoke();
            SpinResolved?.Invoke(slot, earnedAmount);

            Debug.Log(
                $"Sonuçlanan bölge: {completedZone} | " +
                $"Aktif Bölge: {CurrentZone} | " +
                $"Altın: {GetAmount(RewardType.Gold)} | " +
                $"Para: {GetAmount(RewardType.Cash)} | " +
                $"Oyun bitti: {IsGameOver}",
                this);
        }

        // The result stays on its winning slot until the player dismisses the reveal.
        public void AcknowledgeReward()
        {
            if (!IsRewardPending)
                return;

            IsRewardPending = false;
            if (!session.IsGameOver)
            {
                if (ZoneTransitionRequested != null)
                {
                    IsTransitioning = true;
                    transitionCommitted = false;
                    StateChanged?.Invoke();
                    ZoneTransitionRequested.Invoke();
                    return;
                }
                AdvanceZone();
            }
            StateChanged?.Invoke();
        }

        // Swap the content when the moving preview reaches the center.
        public void CommitZoneTransition()
        {
            if (!IsTransitioning || transitionCommitted)
                return;
            transitionCommitted = true;
            AdvanceZone();
            StateChanged?.Invoke();
        }

        // Also used on disable, so an interrupted animation cannot leave input locked.
        public void CompleteZoneTransition()
        {
            if (!IsTransitioning)
                return;
            CommitZoneTransition();
            IsTransitioning = false;
            transitionCommitted = false;
            StateChanged?.Invoke();
        }

        private void AdvanceZone()
        {
            PreviousWheelAngle = wheelSpinView.transform.localEulerAngles.z;
            previousDefinition = wheelDefinition;
            previousSlots = currentSlots;
            session.PrepareSpin();
            wheelDefinition = GetWheelForZone(CurrentZone);
            currentSlots = nextSlots;
            nextSlots = rewardSelector.Select(NextDefinition);
            subscribedWheel?.ResetPose();
            RefreshWheel();
        }

        public void SetBankOpen(bool open)
        {
            if (open && (IsSpinning || IsRewardPending || IsTransitioning))
                return;
            IsBankOpen = open;
            StateChanged?.Invoke();
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

            if (subscribedWheel.IsSpinning || IsRewardPending || IsBankOpen || IsTransitioning)
                return;

            session.Reset();
            previousDefinition = null;
            previousSlots = null;
            PreviousWheelAngle = 0;
            PrepareLayouts();
            subscribedWheel.ResetPose();
            RefreshWheel();

            StateChanged?.Invoke();
            Debug.Log("Yeni oturum başladı.", this);
        }
    }
}
