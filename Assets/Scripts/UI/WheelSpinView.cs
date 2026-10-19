using System;
using DG.Tweening;
using UnityEngine;

namespace VertigoWheelDemo.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class WheelSpinView : MonoBehaviour
    {
        // The supplied wheel has eight equally spaced reward slots.
        private const int SlotCount = 8;

        [SerializeField, Min(0.1f)] private float spinDuration = 3f;
        [SerializeField, Min(1)] private int fullTurns = 4;
        [SerializeField, Min(0f)] private float idleRotationSpeed = 6f;

        private RectTransform wheelTransform;
        private Tween spinTween;
        private bool idleRotationEnabled;

        public bool IsSpinning => spinTween != null && spinTween.IsActive();
        public event Action<bool> SpinningChanged;
        public event Action<int> SpinCompleted;

        private void Awake()
        {
            wheelTransform = GetComponent<RectTransform>();
        }

        private void OnValidate()
        {
            spinDuration = Mathf.Max(0.1f, spinDuration);
            fullTurns = Mathf.Max(1, fullTurns);
            idleRotationSpeed = Mathf.Max(0f, idleRotationSpeed);
        }

        public void SetIdleRotation(bool enabled)
        {
            idleRotationEnabled = enabled;
        }

        private void Update()
        {
            if (idleRotationEnabled && !IsSpinning)
                wheelTransform.Rotate(0f, 0f, -idleRotationSpeed * Time.deltaTime);
        }

        public void ResetPose()
        {
            if (!IsSpinning)
                wheelTransform.localRotation = Quaternion.identity;
        }

        // The caller selects the result; this view only animates it.
        public void SpinToSlot(int slotIndex)
        {
            if (!isActiveAndEnabled || IsSpinning)
                return;

            if (slotIndex < 0 || slotIndex >= SlotCount)
            {
                Debug.LogError("Wheel slot must be between 0 and 7.", this);
                return;
            }

            float startAngle = wheelTransform.localEulerAngles.z;
            float targetAngle = slotIndex * (360f / SlotCount);

            // Decreasing Z rotates clockwise. Extra full turns add anticipation.
            float travelAngle = fullTurns * 360f
                + Mathf.Repeat(startAngle - targetAngle, 360f);
            float endAngle = startAngle - travelAngle;

            spinTween = DOTween.To(
                    () => startAngle,
                    angle => wheelTransform.localRotation = Quaternion.Euler(0f, 0f, angle),
                    endAngle,
                    spinDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    wheelTransform.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
                    spinTween = null;
                    SpinningChanged?.Invoke(false);
                    SpinCompleted?.Invoke(slotIndex);
                });

            SpinningChanged?.Invoke(true);
        }

        private void OnDisable()
        {
            if (spinTween == null)
                return;

            spinTween.Kill();
            spinTween = null;
            SpinningChanged?.Invoke(false);
        }
    }
}
