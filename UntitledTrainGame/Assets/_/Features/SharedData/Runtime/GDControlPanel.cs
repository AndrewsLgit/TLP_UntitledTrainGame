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
        
        [Header("Camera Settings")]
        [SerializeField, Range(0,1f)] private float _edgeThresholdY = 0.1f; 
        [SerializeField, Range(0,1f)] private float _edgeThresholdX = 0.1f;
        
        [Header("Camera Rotation Settings")]
        [SerializeField, Range(0,10f)] private float _markerDistanceThreshold = 5f;
        [SerializeField, Range(0,45f)] private float _maxRotationAngle = 10f;
        [SerializeField, Range(0,10f)] private float _rotationSpeed = 2f;
        [SerializeField, Range(0,5f)] private float _smoothTime = .3f;
        [SerializeField] private AnimationCurve _cameraRotationCurve = new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(1, 1)
            );
        [SerializeField] private AnimationCurve _cameraReturnCurve = new AnimationCurve(
            new Keyframe(0,1),
            new Keyframe(1,0));
        
        [Header("Time System")]
        [SerializeField, Range(0,1)] private float _compressionFactor = 0.5f;
        
        // Private Variables
        #endregion

        #region Public
        // Public Variables
        public static GDControlPanel Instance { get; private set; } 
        
        public float PlayerMoveSpeed => _playerMoveSpeed;
        public float TurnSmoothTime => _turnSmoothTime;
        
        public float EdgeThresholdY => _edgeThresholdY;
        public float EdgeThresholdX => _edgeThresholdX;
        
        public float MarkerDistanceThreshold => _markerDistanceThreshold;
        public float MaxRotationAngle => _maxRotationAngle;
        public float RotationSpeed => _rotationSpeed;
        public float SmoothTime => _smoothTime;
        public AnimationCurve CameraRotationCurve => _cameraRotationCurve;
        public AnimationCurve CameraReturnCurve => _cameraReturnCurve;
        
        public float CompressionFactor => _compressionFactor;
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
