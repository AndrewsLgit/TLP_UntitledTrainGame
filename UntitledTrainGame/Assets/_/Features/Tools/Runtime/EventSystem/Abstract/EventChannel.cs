using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tools.Runtime
{
    public abstract class EventChannel<T> : ScriptableObject
    {
        
        #region Public Variables

        public bool m_enabled;
        public T SourceValue
        {
            get => _sourceValue; set => SetSourceValue(value);
        }
        #endregion
        
        #region Private Variables

        [SerializeField] private T _sourceValue;
        protected readonly HashSet<EventListener<T>> _listeners = new HashSet<EventListener<T>>();

        #endregion
        
        #region Unity API

        private void OnEnable()
        {
            m_enabled = true;
        }

        private void OnDisable()
        {
            m_enabled = false;
        }
        #endregion
        
        #region Main Methods

        private void SetSourceValue(T value)
        {
            _sourceValue = value;
            Invoke(_sourceValue);
        }

        public T SubscribeToEvent(EventListener<T> listener)
        {
            _listeners.Add(listener);
            return _sourceValue;
        }

        public void UnsubscribeFromEvent(EventListener<T> listener) => _listeners.Remove(listener);

        public void Invoke(T value)
        {
            if (!m_enabled) return;
            
            foreach (var gameEventListener in _listeners)
            {
                gameEventListener.RaiseEvent(value);
            }
        }
        
        #endregion
    }
    
    public readonly struct Empty {}

    
}