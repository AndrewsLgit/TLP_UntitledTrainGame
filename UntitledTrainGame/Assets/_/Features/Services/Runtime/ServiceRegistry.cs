
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Services.Runtime
{

    // Tiny, explicit registry for interface-based services
    // - Register in bootstrap scene (Awake)
    // - Resolve anywhere after registration
    // - Keep this simple and explicit to avoid hidden dependencies
    public static class ServiceRegistry 
    {
        #region Variables

        #region Private
        // --- Start of Private Variables ---
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        // --- End of Private Variables --- 
        #endregion

        #region Public
        // --- Start of Public Variables ---
        // --- End of Public Variables --- 
        #endregion

        #endregion

        #region Main Methods
        // Register an instance for an interface (or base type)
        public static void Register<T>(T instance) where T : class
        {
            var key = typeof(T);
            Assert.IsNotNull(instance, $"Attempted to register null for {key.Name}");
            if (instance == null)
            {
                Debug.LogError($"[ServiceRegistry] Attempted to register null for {key.Name}");
                return;
            }

            if (_services.TryGetValue(key, out var existing))
            {
                if (!ReferenceEquals(existing, instance))
                    Debug.LogWarning($"[ServiceRegistry] Overwriting existing service for {key.Name}");
            }

            _services[key] = instance;
        }
        
        // TryResolve with boolean result
        public static bool TryResolve<T>(out T instance) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var obj))
            {
                instance = obj as T;
                return instance != null;
            }
            instance = null;
            return false;
        }

        // Resolve or throw – useful when a service is required to run
        public static T Resolve<T>() where T : class
        {
            if (!_services.TryGetValue(typeof(T), out var obj))
                throw new InvalidOperationException($"[ServiceRegistry] No service registered for {typeof(T).Name}");
            return (T)obj;
        }

        // Optional: clear all (useful in tests or domain reloads if you rebuild the registry)
        public static void Clear() => _services.Clear();

        #endregion
    }
}
