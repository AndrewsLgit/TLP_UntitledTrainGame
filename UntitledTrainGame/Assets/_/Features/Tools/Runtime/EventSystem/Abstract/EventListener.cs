using UnityEngine;
using UnityEngine.Events;

namespace Tools.Runtime
{
    public abstract class EventListener<T> : MonoBehaviour
    {
        #region Private Variables
        
        [SerializeField] private EventChannel<T> _eventChannel;
        [SerializeField] private UnityEvent<T> _unityEvent;
        
        #endregion
        
        #region Main Methods

        private void Awake()
        {
            var value = _eventChannel.SubscribeToEvent(this);
            _unityEvent?.Invoke(value);
        }
        private void OnDisable()
        {
            _eventChannel.UnsubscribeFromEvent(this);
        }
        
        public void RaiseEvent(T value)
        {
            _unityEvent?.Invoke(value);
        }
        
        #endregion
    }
    // Empty EventListener for "no type"
    public class EventListener : EventListener<Empty>{}
}