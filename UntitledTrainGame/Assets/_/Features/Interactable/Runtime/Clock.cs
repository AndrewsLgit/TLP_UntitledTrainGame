using System;
using Manager.Runtime;
using SharedData.Runtime;
using TMPro;
using UnityEngine;

namespace Interactable.Runtime
{
    public class Clock : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] _clockText;
        private ClockManager _clockManager;
        
        #region Unity API
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _clockText = GetComponentsInChildren<TMP_Text>();
            _clockManager = ClockManager.Instance;
            _clockManager.m_OnTimeUpdated += UpdateTime;
            
            UpdateTime(_clockManager.m_CurrentTime);
        }

        // private void OnEnable()
        // {
        //     _clockManager.m_OnTimeUpdated += UpdateTime;
        // }
        private void OnDestroy()
        {
            _clockManager.m_OnTimeUpdated -= UpdateTime;
        }
        
        #endregion
        
        private void UpdateTime(GameTime time)
        {
            foreach (var clockText in _clockText)
                clockText.text = time.ToString();
        }
    }
}
