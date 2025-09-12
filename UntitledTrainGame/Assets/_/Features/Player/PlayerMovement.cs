using Foundation.Runtime;
using SharedData.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerInputRouter))]
    public class PlayerMovement : FMono
    {
        #region Variables
        
        #region Private
        // Private Variables
        
        // References
        private CharacterController _characterController;
        private Animator _animator;
        private GDControlPanel _controlPanel;
        private Transform _cameraTransform;
        private Camera _camera;
        private PlayerInputRouter _inputRouter;
        
        
        private Ray _wallDetectionRay;
        private RaycastHit _detectionHit;
        
        // Movement Variables
        private float _turnSmoothVelocity;
        private Vector2 _inputMove;
        private float _maxMoveSpeed;
        private float _turnSmoothTime;
        private Vector3 direction;
        
        // Raycast Variables
        [Header("Obstacle detection")]
        [SerializeField] private LayerMask _obstacleMask;
        
        //End of Private Variables
        #endregion
        
        #region Public
        // Public Variables
        
        //End of Public Variables
        #endregion
        
        #endregion
        
        #region Unity API
        
        private void Awake()
        {
            SetFact("playerTransform", transform, false);
        }
        
        void Start()
        {
            _camera = Camera.main;
            _characterController = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();
            _controlPanel = GDControlPanel.Instance;
            GDControlPanel.OnValuesUpdated += OnControlPanelUpdated;
            GetFromControlPanel();
            
            _cameraTransform = _camera.transform;
            // _inputMove = Vector2.zero;

            _inputRouter = GetComponent<PlayerInputRouter>();
            if (_inputRouter != null)
                _inputRouter.OnMove += OnMove;

        }
        private void OnDestroy()
        {
            GDControlPanel.OnValuesUpdated -= OnControlPanelUpdated;
            _inputRouter.OnMove -= OnMove;
        }

        void Update()
        {
            var last_pos = transform.position;
            HandleMovement();
            
            var current_pos = transform.position;
            var velocity = (current_pos - last_pos) / Time.deltaTime;
            
            _animator.SetFloat("Velocity", velocity.magnitude);
            Info($"Setting velocity in animator: {velocity.magnitude} / {_maxMoveSpeed} = {(velocity.magnitude/ _maxMoveSpeed)}");
            
        }
        
        #endregion
        
        #region Main Methods

        private void OnMove(Vector2 move)
        {
            _inputMove = move;
        }

        private void HandleMovement()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                _cameraTransform = _camera.transform;
            }
            
            var direction = new Vector3(_inputMove.x, 0f, _inputMove.y).normalized;
            
            var forward = _cameraTransform.forward;
            var right = _cameraTransform.right;
            
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            direction = right * _inputMove.x + forward * _inputMove.y;
            direction = direction.normalized;
            
            _wallDetectionRay = new Ray(transform.position, direction);

            if (Physics.Raycast(_wallDetectionRay, out _detectionHit, 0.5f, _obstacleMask))
            {
                Debug.DrawLine(transform.position, _detectionHit.point, Color.cyan, 0.05f);
                Info($"Finding new direction to glide");
                var wallNormal = _detectionHit.normal;
                direction = Vector3.ProjectOnPlane(direction, wallNormal);
                // direction = Vector3.Lerp(
                //     direction,
                //     Vector3.ProjectOnPlane(direction, wallNormal),
                //     _turnSmoothVelocity * Time.deltaTime);
                // direction.Normalize();
                // direction = newDirection;
                // direction = Vector3.RotateTowards(direction, newDirection, _turnSmoothVelocity * Time.deltaTime,0f);
                Info($"New direction: {direction} : normal {wallNormal}");     
            }

            if (!(direction.magnitude >= 0.1f)) return;
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime);
                
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Apply gravity to player
            direction.y = -2f;
            _characterController.Move(direction * (_maxMoveSpeed * Time.deltaTime));
        }
        
        
        #endregion
        
        #region Utils
        
        private void OnControlPanelUpdated(GDControlPanel controlPanel)
        {
            GetFromControlPanel();
        }

        private void GetFromControlPanel()
        {
            _maxMoveSpeed = _controlPanel.PlayerMoveSpeed;
            _turnSmoothTime = _controlPanel.TurnSmoothTime;
        }
        
        #endregion
    }
}
