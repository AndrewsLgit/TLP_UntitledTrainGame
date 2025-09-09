using System;
using UnityEngine;

namespace Tools.Runtime
{
    public class ChoiceSelectionController
    {
        #region Variables

        #region Private
        // --- Start of Private Variables ---

        private readonly float _navRepeatCooldown;
        private float _navRepeatTimer;

        // --- End of Private Variables --- 

        #endregion

        #region Public

        // --- Start of Public Variables ---

        public event Action<int> OnSelectionChanged;
        public event Action<int> OnSubmit;
        public event Action OnCancel;
        
        public bool IsOpen { get; private set; }
        public int SelectedIndex { get; private set; }
        public int OptionCount { get; private set; } // max index is OptionCount - 1

        // --- End of Public Variables --- 

        #endregion

        public ChoiceSelectionController(float navRepeatCooldown = 0.2f)
        {
            _navRepeatCooldown = Mathf.Max(0f, navRepeatCooldown);
        }

        #endregion

        #region Main Methods

        public void Open(int optionCount, int initialIndex = 0)
        {
            OptionCount = Mathf.Max(0, optionCount);
            SelectedIndex = Mathf.Clamp(initialIndex, 0, Mathf.Max(OptionCount - 1));
            IsOpen = true;
            _navRepeatTimer = 0f;
            OnSelectionChanged?.Invoke(SelectedIndex);
        }

        public void Close()
        {
            IsOpen = false;
            OptionCount = 0;
            _navRepeatTimer = 0f;
        }

        public void HandleNavigate(Vector2 dir, float axisDeadzone = 0.05f, bool verticalOnly = true)
        {
            if(!IsOpen || OptionCount <= 0) return;
            if(verticalOnly && Mathf.Abs(dir.y) < axisDeadzone) return;
            
            if(_navRepeatTimer > 0f) return;
            _navRepeatTimer = _navRepeatCooldown;

            // if (OptionCount == 2)
            // {
            //     // Simple toggle for 2 options
            //     SelectedIndex = (SelectedIndex == 0) ? 1 : 0;
            //     OnSelectionChanged?.Invoke(SelectedIndex);
            //     return;
            // }

            // For 2+ options, respect direction and wrap:
            // Up (y > 0): previous (wraps from 0 -> last)
            // Down (y < 0): next (wraps from last -> 0)
            switch (dir.y)
            {
                case > 0f:
                    MovePrev();
                    break;
                case < 0f:
                    MoveNext();
                    break;
            }
        }

        public void SetIndex(int index)
        {
            if(!IsOpen || OptionCount <= 0) return;
            var clamped = Mathf.Clamp(index, 0, OptionCount - 1);
            if (clamped == SelectedIndex) return;
            SelectedIndex = clamped;
            OnSelectionChanged?.Invoke(SelectedIndex);
        }

        public void Submit()
        {
            if(!IsOpen || OptionCount <= 0) return;
            OnSubmit?.Invoke(SelectedIndex);
        }

        public void Cancel()
        {
            if (!IsOpen) return;
            OnCancel?.Invoke();
        }

        #endregion

        #region Helpers/Utils

        private void MovePrev()
        {
            if(!IsOpen || OptionCount <= 0) return;
            SelectedIndex = (SelectedIndex - 1 + OptionCount) % OptionCount;
            OnSelectionChanged?.Invoke(SelectedIndex);
        }

        private void MoveNext()
        {
            if(!IsOpen || OptionCount <= 0) return;
            SelectedIndex = (SelectedIndex + 1) % OptionCount;
            OnSelectionChanged?.Invoke(SelectedIndex);
        }
        
        // Call every frame with deltaTime for repeat cooldown
        public void Tick(float deltaTime)
        {
            if (_navRepeatTimer > 0f)
                _navRepeatTimer -= deltaTime;
        }

        #endregion
    }
}