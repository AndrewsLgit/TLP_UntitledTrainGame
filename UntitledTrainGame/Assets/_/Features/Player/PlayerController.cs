using System;
using Foundation.Runtime;
using SharedData.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Input))]
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerController : FMono
    {
        
        #region Variables
        
        #region Private
        // Private Variables
        
        // References
        private CharacterController _characterController;
        private GDControlPanel _controlPanel;
        private Transform _cameraTransform;
        
        // Player Interaction
        private IInteractable _interactable;
        private SphereCollider _detectionCollider;
        private bool _canInteract;
        
        private RaycastHit _detectionHit;
        
        // Movement Variables
        private float _turnSmoothVelocity;
        private Vector2 _inputMove;
        private float _moveSpeed;
        private float _turnSmoothTime;
        
        // Interaction Variables
        private float _interactionDistance = 10f;
        private float _interactionAngle = 45f;
        private Vector3 _directionToInteractable;
        
        // Private Variables
        #endregion
        
        #region Public
        // Public Variables
        
        
        
        // Public Variables
        #endregion
        
        #endregion
        
        #region Unity API

        private void Awake()
        {
            SetFact("playerTransform", transform, false);
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _characterController = GetComponent<CharacterController>();
            _detectionCollider = GetComponent<SphereCollider>();
            _controlPanel = GDControlPanel.Instance;
            _cameraTransform = Camera.main.transform;
            _inputMove = Vector2.zero;
            _moveSpeed = _controlPanel.PlayerMoveSpeed;
            _turnSmoothTime = _controlPanel.TurnSmoothTime;
            _interactionDistance = _controlPanel.DetectionDistance;
            _interactionAngle = _controlPanel.DetectionAngle;

            if (_detectionCollider != null)
            {
                _detectionCollider.radius = _interactionDistance;
                _detectionCollider.isTrigger = true;
                Info($"Detection collider configured: radius {_detectionCollider.radius}, isTrigger: {_detectionCollider.isTrigger}");
            }
            else Error("No detection collider found!");
            
        }

        // Update is called once per frame
        void Update()
        {
            _interactionDistance = _controlPanel.DetectionDistance;
            _interactionAngle = _controlPanel.DetectionAngle;
            HandleMovement();
            InteractionDetection();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform == transform || other.transform.IsChildOf(transform)) return;
            Info($"Entering trigger with {other.gameObject.name}");

            if (other.TryGetComponent(out IInteractable interactable))
            {
                Info("Found interactable near player");
                _interactable = interactable;
            }
            else
            {
                Info($"No interactable found on {other.gameObject.name}");
            }
            //var interactable = other.GetComponent<IInteractable>();
            // if (interactable == null) return;
            // _interactable = interactable;
            Info($"Passed interactable: {interactable.GetType()} to player");
            // _canInteract = true;
        }
        private void OnTriggerExit(Collider other)
        {
            if(other.transform == transform || other.transform.IsChildOf(transform)) return;
            
            Info($"Exiting trigger");
            // var interactable = other.GetComponent<IInteractable>();
            // if (interactable == null || interactable != _interactable) return;
            _interactable = null;
            _canInteract = false;
            // _canInteract = false;
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

            // Apply gravity to player
            direction.y = -2f;
            _characterController.Move(direction * (_moveSpeed * Time.deltaTime));
        }

        private void InteractionDetection()
        {
            if (_interactable == null)
            {
                _canInteract = false;
                return;
            }
            
            var interactablePosition = ((MonoBehaviour)_interactable).transform.position;
            _directionToInteractable = interactablePosition - transform.position;
            // calculate angle between player's forward and the interactable gameObject
            float angle = Vector3.Angle(transform.forward, _directionToInteractable);
            
            _canInteract = angle <= _interactionAngle;
            
            if (_canInteract) Info($"Found interactable: Dir({_directionToInteractable}) Angle({angle})");
        }

        public void InteractWith()
        {
            if (!_canInteract || _interactable == null) return;
            _interactable.Interact();
        }
        
        #region Debug
        // Debug
        private void OnDrawGizmosSelected()
        {
            // Visualize view radius
            // Gizmos.color = Color.yellow;
            // Gizmos.DrawWireSphere(transform.position, _interactionDistance);

            // Visualize view angle
            Vector3 viewAngleA = DirFromAngle(-_interactionAngle / 2);
            Vector3 viewAngleB = DirFromAngle(_interactionAngle / 2);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + viewAngleA * _interactionDistance);
            Gizmos.DrawLine(transform.position, transform.position + viewAngleB * _interactionDistance);
        }
        
        private Vector3 DirFromAngle(float angleInDegrees)
        {
            /*
                - represents the object's rotation around the vertical axis (`Y-axis`) in degrees. `transform.eulerAngles.y`
                - The input is adjusted by adding the current rotation of the object to it.
                This ensures the angle is relative to the direction the object is facing, instead of being fixed globally. `angleInDegrees`
                This creates a direction vector in the X-Z plane pointing in the direction of the given angle relative to the object's rotation.

             */
            angleInDegrees += transform.eulerAngles.y;
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }
        #endregion

        #endregion
    }
}
