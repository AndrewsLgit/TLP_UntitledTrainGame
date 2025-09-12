using Foundation.Runtime;
using UnityEngine;

namespace Tools.Runtime
{
    public class WorldSpaceFollow : FMono
    {
        #region Variables

        #region Private

        // --- Start of Private Variables ---

        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 2f, 0f); // tweak height above target
        [SerializeField] private bool _faceCamera = true;
        
        private Camera _cam;
        
        // --- End of Private Variables --- 

        #endregion

        #region Public

        // --- Start of Public Variables ---


        // --- End of Public Variables --- 

        #endregion

        #endregion

        #region Unity API

        private void Awake()
        {
            _cam = Camera.main;
            _target = GameObject.FindGameObjectWithTag("Player").transform;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                _target = GameObject.FindGameObjectWithTag("Player").transform;
                
            }
            if(_cam == null)
                _cam = Camera.main;

            transform.position = _target.position + _offset;

            if (_faceCamera && _cam != null)
            {
                // billboard to camera
                transform.forward = (_cam.transform.position - transform.position).normalized * -1f;
            }
        }


        #endregion

        #region Main Methods

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        #endregion

    }
}