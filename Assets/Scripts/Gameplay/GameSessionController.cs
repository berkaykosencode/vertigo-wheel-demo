using System;
using UnityEngine;
using VertigoWheelDemo.Data;
using VertigoWheelDemo.UI;

namespace VertigoWheelDemo.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameSessionController : MonoBehaviour
    {
        [SerializeField] private WheelDefinition wheelDefinition;
        [SerializeField] private  WheelSpinView wheelSpinView;

        private readonly GameSession session = new GameSession();
        private WheelSpinView subscribedWheel;

        public event Action StateChanged;
        public event Action<WheelSlotData> SpinResolved;

        public bool IsGameOver => session.IsGameOver;

        public bool CanSpin => 
            isActiveAndEnabled &&
            wheelDefinition != null &&
            subscribedWheel != null &&
            subscribedWheel.isActiveAndEnabled &&
            !subscribedWheel.IsSpinning &&
            !session.IsGameOver;

        private void OnEnable()
        {
            if(wheelDefinition == null || wheelSpinView == null)
            {
                Debug.LogError("Oyun yöneticisinin bağlantıları eksik.", this);
                return;
            }

            subscribedWheel = wheelSpinView;
            subscribedWheel.SpinningChanged += HandleSpinningChanged;
            subscribedWheel.SpinCompleted += HandleSpinCompleted;

            StateChanged?.Invoke();
        }

        private void Start()
        {
            StateChanged?.Invoke();
        }

        private void OnDisable()
        {
            if(subscribedWheel != null)
            {
                subscribedWheel.SpinningChanged -= HandleSpinningChanged;
                subscribedWheel.SpinCompleted -= HandleSpinCompleted;
                subscribedWheel = null;
            }

            StateChanged?.Invoke();
        }

        public void RequestSpin()
        {
            if(!CanSpin)
                return;
            
            int slotIndex = UnityEngine.Random.Range(0, wheelDefinition.SlotCount);

            subscribedWheel.SpinToSlot(slotIndex);
        }

        private void HandleSpinningChanged(bool IsSpinning)
        {
            StateChanged?.Invoke();
        }
        
        private void HandleSpinCompleted(int slotIndex)
        {
            if(session.IsGameOver)
                return;
            
            WheelSlotData slot = wheelDefinition.GetSlot(slotIndex);
            session.ApplySpin(slot);

            StateChanged?.Invoke();
            SpinResolved?.Invoke(slot);

            Debug.Log(
                $"Altın: {GetAmount(RewardType.Gold)}" +
                $"Para: {GetAmount(RewardType.Cash)}" +
                $"Oyun Bitti!: {IsGameOver}"
                , this);
        }

        public int GetAmount(RewardType type)
        {
            return session.GetAmount(type);
        }

        [ContextMenu("Reset Session")]
        public void ResetSession()
        {
            if(!Application.isPlaying)
                return;
            
            if(subscribedWheel != null && subscribedWheel.IsSpinning)
                return;

            session.Reset();
            StateChanged?.Invoke();
            Debug.Log("New session started.", this);
        }
    }
}