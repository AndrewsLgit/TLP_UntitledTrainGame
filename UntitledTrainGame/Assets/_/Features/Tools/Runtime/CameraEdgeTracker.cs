using Foundation.Runtime;
using SharedData.Runtime;
using Unity.Cinemachine;
using UnityEngine;

namespace Tools.Runtime
{
    public class CameraEdgeTracker : FMono
    {
        #region Private Variables
        
        private float _edgeThresholdY = 0.1f; 
        private float _edgeThresholdX = 0.1f;
        private float _markerDistanceThreshold = 5f;
        private float _maxRotationAngle = 10f;
        private float _rotationSpeed = 2f;
        
        private float _smoothedDistanceFactor = 0f;
        private float _smoothVelocity;
        private float _smoothTime = .3f;
        
        private AnimationCurve _rotationCurve = new AnimationCurve();
        private AnimationCurve _rotationCurveBackwards = new AnimationCurve();

        // References
        private GDControlPanel _controlPanel;
        private Camera _mainCam;
        //private CinemachineRotationComposer _rotationComposer;
        private Quaternion _originalRotation;
        private Transform _playerPos;
        private SceneLimitsManager _sceneLimitsManager;
        
        #endregion
        
        #region Unity API
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _controlPanel = GDControlPanel.Instance;
            _sceneLimitsManager = FindAnyObjectByType<SceneLimitsManager>();
            if (_sceneLimitsManager == null) Error("SceneLimitsManager not found! Please add it to the GameManager object!");
            
            _edgeThresholdY = _controlPanel.EdgeThresholdY;
            _edgeThresholdX = _controlPanel.EdgeThresholdX;
            _markerDistanceThreshold = _controlPanel.MarkerDistanceThreshold;
            _maxRotationAngle = _controlPanel.MaxRotationAngle;
            _rotationSpeed = _controlPanel.RotationSpeed;
            _smoothTime = _controlPanel.SmoothTime;
            _rotationCurve = _controlPanel.CameraRotationCurve;
            _rotationCurveBackwards = CreateInvertedAnimationCurve(_rotationCurve);
            
            _playerPos = GetFact<Transform>("playerTransform");

            _mainCam = Camera.main;
            //_rotationComposer = GetComponent<CinemachineRotationComposer>();
            _originalRotation = transform.rotation;
        }

        

        // Update is called once per frame
        void Update()
        {
            //todo: remove variable assignment when done
            _edgeThresholdY = _controlPanel.EdgeThresholdY;
            _edgeThresholdX = _controlPanel.EdgeThresholdX;
            _markerDistanceThreshold = _controlPanel.MarkerDistanceThreshold;
            _maxRotationAngle = _controlPanel.MaxRotationAngle;
            _rotationSpeed = _controlPanel.RotationSpeed;
            _smoothTime = _controlPanel.SmoothTime;
            
            // RotateCameraBasedOnEdge();
            RotateCameraBasedOnMarker();
        }
        
        #endregion
        
        #region Main Methods

        // TODO: Add animation curve to GDControlPanel and use it on camera rotation for smoother effect
        private void RotateCameraBasedOnMarker()
        {
            if (_sceneLimitsManager == null || _playerPos == null) return;
            //InfoInProgress($"PlayerPos: {_playerPos.position}");
            
            Vector3 closestMarker = _sceneLimitsManager.GetClosestLimitPoint(_playerPos.position);
            float distanceToMarker = Vector3.Distance(_playerPos.position, closestMarker);
            
            // When player is at marker, distance = 0, factor should be 1
            // When player is at threshold, distance = threshold, factor should be 0
            float distanceFactor = Mathf.Clamp01(1f - (distanceToMarker / _markerDistanceThreshold));
            _smoothedDistanceFactor = Mathf.SmoothDamp(_smoothedDistanceFactor, distanceFactor, ref _smoothVelocity, _smoothTime);
            
            
            // if (distanceFactor < 0f || distanceFactor > 0f) InfoInProgress($"DistanceFactor: {distanceFactor}");
            bool isNearMarker = distanceToMarker < _markerDistanceThreshold;

            Quaternion targetRotation = _originalRotation;

            if (isNearMarker)
            {
                // Direction from camera to closest limit marker
                Vector3 directionToMarker = (closestMarker - transform.position).normalized;
                targetRotation = Quaternion.LookRotation(directionToMarker);
                
                // Limit rotation angle
                var angle = Quaternion.Angle(_originalRotation, targetRotation);
                if (angle > _maxRotationAngle)
                    targetRotation = Quaternion.RotateTowards(_originalRotation, targetRotation, _maxRotationAngle);
                
                // float curveValue = _rotationCurve.Evaluate(distanceFactor);
                // InfoDone($"Curve value near: {curveValue:F2} || Distance factor: {distanceFactor}");
                // transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, curveValue * Time.deltaTime * _rotationSpeed);
            }
            // Set animation curve progress to distanceFactor value
            // float curveValue = _rotationCurve.Evaluate(isNearMarker? _smoothedDistanceFactor : 1f - _smoothedDistanceFactor);
            float curveValue = isNearMarker ? 
                _rotationCurve.Evaluate(_smoothedDistanceFactor) : 
                _rotationCurveBackwards.Evaluate(_smoothedDistanceFactor);
            
            InfoDone($"Curve value: {curveValue} || Distance factor: {distanceFactor}");
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, curveValue * Time.deltaTime * _rotationSpeed);

            // else
            // {
            //     float curveValue = Mathf.Max(_rotationCurve.Evaluate(distanceFactor), .8f);
            //     // InfoDone($"Curve Value outside marker: {curveValue:F2}");
            //     InfoDone($"Curve value far: {curveValue:F2} || Distance factor: {distanceFactor}");
            //     transform.rotation = Quaternion.Slerp(transform.rotation, _originalRotation,  Time.deltaTime * _rotationSpeed);
            // }
        }

        private void RotateCameraBasedOnEdge()
        {
            Vector3 screenPos = _mainCam.WorldToViewportPoint(_playerPos.position);;

            bool isAtEdge = screenPos.x < _edgeThresholdX || screenPos.x > 1 - _edgeThresholdX ||
                            screenPos.y < _edgeThresholdY || screenPos.y > 1 - _edgeThresholdY;

            if (isAtEdge)
            {
                Vector3 directionToPlayer = (_playerPos.position - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

                // Limit the rotation angle
                float angle = Quaternion.Angle(_originalRotation, targetRotation);
                if (angle > _maxRotationAngle)
                    targetRotation = Quaternion.RotateTowards(_originalRotation, targetRotation, _maxRotationAngle);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotationSpeed);
            }
            else
            {
                // Smoothly return to original rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, _originalRotation, Time.deltaTime * _rotationSpeed);
            }
        }
        #endregion
        
        #region Utils
        
        private AnimationCurve CreateInvertedAnimationCurve(AnimationCurve originalCurve)
        {
            AnimationCurve invertedCurve = new AnimationCurve();
            foreach (var keyframe in originalCurve.keys)
            {
                invertedCurve.AddKey(new Keyframe(
                    keyframe.time, 
                    1f - keyframe.value,
                    -keyframe.inTangent,
                    -keyframe.outTangent
                ));
            }

            return invertedCurve;
        }
        
        #endregion
    }
}
