using Foundation.Runtime;
using UnityEngine;

namespace Tools.Runtime
{
    public class LookAtCamera : FMono
    {
        #region Private Variables
    
        private Camera _camera;
        //[SerializeField] private PlayerCharacter _player;
        [SerializeField] private float _speed = 15f;
        private float _step;
    
        #endregion
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _camera = Camera.main;
            SetFact("cameraTransform", _camera.transform, false);
            //_player = FindAnyObjectByType<PlayerController>();
        }

        // Update is called once per frame
        void Update()
        {
            _step = Time.deltaTime * _speed;

            if (_camera == null)
            {
                _camera = Camera.main;
            }
            //LookAtTargetSmoothly(_camera.transform);
            LookAtTarget(_camera.transform);
            //var test = LookAtTarget(_player.transform);
        }
    
        #region Main Methods
    
        private void LookAtTargetSmoothly(Transform target)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                LookAtTarget(target), 
                _step
            );
        }
    
        #endregion
    
        #region Utils
    
        private Quaternion LookAtTarget(Transform target)
        {
            return transform.rotation = Quaternion.LookRotation(target.transform.forward);
        }
    
        #endregion
    }
}
