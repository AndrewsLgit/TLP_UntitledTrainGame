using Foundation.Runtime;
using UnityEngine;

namespace SharedData.Runtime
{
    public class GDControlPanel : FMono
    {
        #region Variables

        #region Private
        // Private Variables
        
        [Header("Player Movement")]
        [SerializeField] private float _playerMoveSpeed = 3f;
        
        [SerializeField, Range(0,1)] private float _turnSmoothTime = .1f;
        
        // Private Variables
        #endregion

        #region Public
        // Public Variables
        public static GDControlPanel Instance { get; private set; } 
        
        public float PlayerMoveSpeed => _playerMoveSpeed;
        public float TurnSmoothTime => _turnSmoothTime;
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
