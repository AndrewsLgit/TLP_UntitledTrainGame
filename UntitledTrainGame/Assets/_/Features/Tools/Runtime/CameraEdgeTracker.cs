using Foundation.Runtime;
using SharedData.Runtime;
using Unity.Cinemachine;
using UnityEngine;

namespace Tools.Runtime
{
    public class CameraEdgeTracker : FMono
    {
        #region Private Variables

        private Transform _playerPos;
        
        private float _edgeThresholdY = 0.1f; 
        private float _edgeThresholdX = 0.1f;
        private float _maxRotationAngle = 10f;
        private float _rotationSpeed = 2f;

        private GDControlPanel _controlPanel;
        private Camera _mainCam;
        //private CinemachineRotationComposer _rotationComposer;
        private Quaternion _originalRotation;

        #endregion
        
        #region Unity API
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _controlPanel = GDControlPanel.Instance;
            _edgeThresholdY = _controlPanel.EdgeThresholdY;
            _edgeThresholdX = _controlPanel.EdgeThresholdX;
            _maxRotationAngle = _controlPanel.MaxRotationAngle;
            _rotationSpeed = _controlPanel.RotationSpeed;
            
            _playerPos = GetFact<Transform>("playerTransform");

            _mainCam = Camera.main;
            //_rotationComposer = GetComponent<CinemachineRotationComposer>();
            _originalRotation = transform.rotation;
        }

        // Update is called once per frame
        void Update()
        {
            _edgeThresholdY = _controlPanel.EdgeThresholdY;
            _edgeThresholdX = _controlPanel.EdgeThresholdX;
            _maxRotationAngle = _controlPanel.MaxRotationAngle;
            _rotationSpeed = _controlPanel.RotationSpeed;
            
            RotateCameraOnEdge();
        }
        
        #endregion
        
        #region Main Methods

        private void RotateCameraOnEdge()
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
    }
}
