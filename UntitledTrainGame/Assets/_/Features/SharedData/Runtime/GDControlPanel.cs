using Foundation.Runtime;
using UnityEngine;

namespace SharedData.Runtime
{
    public class GDControlPanel : FMono
    {
        #region Variables

        #region Private
        // Private Variables
        
        // Private Variables
        #endregion

        #region Public
        // Public Variables
        public static GDControlPanel Instance { get; private set; } 
        // Public Variables
        #endregion
        
        #endregion

        #region Main Methods

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Awake()
        {
            // Find instance of this class, if existent -> destroy that instance
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                Error("There is already an instance of this class! Destroying this one!");
                return;
            }

            // Assign instance as this current object
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        #endregion
    }
}
