using ServiceInterfaces.Runtime;
using Services.Runtime;
using SharedData.Runtime;
using TMPro;
using UnityEngine;

namespace Interactable.Runtime
{
    public class Clock : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] _clockText;
        private IClockService _clockManager;
        
        #region Unity API
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _clockText = GetComponentsInChildren<TMP_Text>();
            _clockManager = ServiceRegistry.Resolve<IClockService>();
            _clockManager.OnTimeUpdated += UpdateTime;
            
            UpdateTime(_clockManager.CurrentTime);
        }

        // private void OnEnable()
        // {
        //     _clockManager.OnTimeUpdated += UpdateTime;
        // }
        private void OnDestroy()
        {
            _clockManager.OnTimeUpdated -= UpdateTime;
        }
        
        #endregion
        
        private void UpdateTime(GameTime time)
        {
            foreach (var clockText in _clockText)
                clockText.text = time.ToString();
        }
    }
}
