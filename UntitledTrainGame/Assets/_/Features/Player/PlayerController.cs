using Foundation.Runtime;
using SharedData.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Input))]
    public class PlayerController : FMono
    {
        
        #region Variables
        
        #region Private
        // Private Variables
        
        // References
        private CharacterController _characterController;
        private GDControlPanel _controlPanel;
        private Transform _cameraTransform;
        
        // Movement Variables
        private float _turnSmoothVelocity;
        private Vector2 _inputMove;
        private float _moveSpeed;
        private float _turnSmoothTime;
        
        // Private Variables
        #endregion
        
        #region Public
        // Public Variables
        
        // Public Variables
        #endregion
        #endregion
        
        #region Unity API
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _characterController = GetComponent<CharacterController>();
            _controlPanel = GDControlPanel.Instance;
            _cameraTransform = Camera.main.transform;
            _inputMove = Vector2.zero;
            _moveSpeed = _controlPanel.PlayerMoveSpeed;
            _turnSmoothTime = _controlPanel.TurnSmoothTime;
        }

        // Update is called once per frame
        void Update()
        {
            HandleMovement();
        }
        #endregion

        #region Main Methods

        public void Move(InputAction.CallbackContext context)
        {
            _inputMove = context.ReadValue<Vector2>();
        }

        private void HandleMovement()
        {
            // var speed = _controlPanel.PlayerMoveSpeed;
            // var turnSmoothTime = _controlPanel.TurnSmoothTime;
            var direction = new Vector3(_inputMove.x, 0f, _inputMove.y).normalized;
            
            var forward = _cameraTransform.forward;
            var right = _cameraTransform.right;
            
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            direction = right * _inputMove.x + forward * _inputMove.y;
            direction = direction.normalized;

            if (!(direction.magnitude >= 0.1f)) return;
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime);
                
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            _characterController.Move(direction * (_moveSpeed * Time.deltaTime));
        }

        #endregion
    }
}
