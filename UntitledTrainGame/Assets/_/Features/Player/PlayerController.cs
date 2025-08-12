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
        private CharacterController _characterController;

        private GDControlPanel _controlPanel;
        
        private float _turnSmoothVelocity;
        
        private Vector2 _inputMove;
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
            var speed = _controlPanel.PlayerMoveSpeed;
            var turnSmoothTime = _controlPanel.TurnSmoothTime;
            var direction = new Vector3(_inputMove.x, 0f, _inputMove.y).normalized;

            if (!(direction.magnitude >= 0.1f)) return;
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, turnSmoothTime);
                
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            _characterController.Move(direction * (speed * Time.deltaTime));
        }

        #endregion
    }
}
