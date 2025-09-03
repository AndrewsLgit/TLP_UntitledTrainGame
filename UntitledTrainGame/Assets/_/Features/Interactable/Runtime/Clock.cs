using System;
using Manager.Runtime;
using SharedData.Runtime;
using TMPro;
using UnityEngine;

namespace Interactable.Runtime
{
    public class Clock : MonoBehaviour
    {
        private TMP_Text _clockText;
        private ClockManager _clockManager;
        
        #region Unity API
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _clockText = GetComponentInChildren<TMP_Text>();
            _clockManager = ClockManager.Instance;
            _clockManager.m_OnTimeUpdated += UpdateTime;
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
            _clockText.text = time.ToString();
        }
    }
}
