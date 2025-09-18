using Foundation.Runtime;
using Player.Runtime;
using ServiceInterfaces.Runtime;
using Services.Runtime;
using SharedData.Runtime;
using Tools.Runtime;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace DialogSystem.Runtime
{
    /// <summary>
    /// DialogueController ties NodeManager and DialogueUIManager together.
    /// Drop this on a scene GameObject, assign references in Inspector.
    /// Handles player input, dialogue advancement, and response selection.
    /// </summary>
    public class DialogController : FMono
    {
        #region Variables

        #region Private

        // --- Start of Private Variables ---

        [Header("Input")]
        [SerializeField] private PlayerInputRouter _inputRouter;
        [SerializeField, Tooltip("Cooldown between repeated nav steps when holding string/keys")]
        private float _navRepeatCooldown = 0.2f;
        private IInputService _customInputService;
        
        [Header("Test")]
        [SerializeField] private DialogNode _testRootNode;
        
        private ChoiceSelectionController _choiceController;
        private bool _uiOpen;
        
        // Event subscription flags
        private bool _subscribedToNodeManager = false;
        private bool _subscribedToUiManager = false;

        // --- End of Private Variables --- 

        #endregion

        #region Public

        // --- Start of Public Variables ---

        [Header("Core References")]
        public NodeManager NodeManager;         // Handles dialogue logic and flags
        public DialogUIManager UiManager;     // Handles UI rendering and input

        [Header("UI References")]
        public Image PortraitImage;
        public Text NameText;
        public Text DialogueText;
        public Transform ResponsesParent;
        public Button ResponseButtonPrefab;

        [Header("Typewriter Settings")]
        public float CharactersPerSecond = 30f;
        
        public static DialogController Instance { get; private set; }
        
        // --- End of Public Variables --- 

        #endregion

        #endregion

        #region Unity API

        private void Awake()
        {
            if(NodeManager == null)
                NodeManager = NodeManager.Instance;
            
            // Find instance of this class, if existent -> destroy that instance
            if (Instance is not null && Instance != this)
            {
                Destroy(gameObject);
                Error("There is already an instance of this class! Destroying this one!");
                return;
            }

            // Assign instance as this current object
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Choice navigation controller
            // Selection highlight is handled via UiManager.HighlightResponse(index)

            // SelectionChanged can be used to update highlight if/when UI supports it
            // _choiceController.OnSelectionChanged += HandleChoiceSelectionChanged;

            //EnsureEventSubscriptions();
        }

        private void Start()
        {
            NodeManager = NodeManager.Instance;
            _choiceController = new ChoiceSelectionController(_navRepeatCooldown);
            _inputRouter = FindFirstObjectByType<PlayerInputRouter>();
            _customInputService = ServiceRegistry.Resolve<IInputService>();
        }

        private void Update()
        {
            // Nav repeat timer
            _choiceController.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
        }

        private void OnEnable()
        
        { 

            
            
            EnsureEventSubscriptions();
        }

        private void OnDisable()
        {
            

            EnsureEventUnsub();
        }

        

        private void OnDestroy()
        {
        }

        #endregion

        #region Main Methods
        
        /// <summary>
        /// Starts a conversation with a root node and NPC ID.
        /// </summary>
        public void StartConversation(DialogNode root, string npcId)
        {
            NodeManager ??= NodeManager.Instance;
            _customInputService.SwitchToUI();
            
            Assert.IsNotNull(UiManager);
            Assert.IsNotNull(NodeManager);
            
            // make sure event pipeline is wired before starting conversation
            EnsureEventSubscriptions();
            
            UiManager.Open();
            _uiOpen = true;
            NodeManager.StartConversation(root, npcId);
        }

        [ContextMenu("Test Conversation")]
        public void TestConversation()
        {
            StartConversation(_testRootNode, _testRootNode.Character.Id);
        }
        
        #region NodeManager Event Handlers

        private void HandleNodeEntered(DialogNode node)
        {
            _inputRouter = FindFirstObjectByType<PlayerInputRouter>();
            Assert.IsNotNull(UiManager);
            Assert.IsNotNull(_inputRouter);
            // Render node text with typewriter effect
            UiManager.SetTypewriterSpeed(CharactersPerSecond);
            UiManager.RenderNode(node);
            
            // Close any prior selection when a new node starts rendering
            _choiceController?.Close();
        }

        private void HandleNodeExited(DialogNode node)
        {
            // Optional: add visual effects when leaving a node
        }

        private void HandleConversationEnd()
        {
            _choiceController?.Close();
            _uiOpen = false;
            UiManager?.Close();
            _customInputService.SwitchToPlayer();
            EnsureEventUnsub();
        }

        #endregion

        #region UIManager Event Handlers

        private void HandleTextComplete()
        {
            EnsureEventSubscriptions();
            Assert.IsNotNull(_inputRouter);
            // Assert.IsNotNull(UiManager);
            // Assert.IsNotNull(NodeManager);
            
            // Only act if a conversation is active and NodeManager is ready
            if (!_uiOpen || NodeManager is null)
            {
                // Ignore spurious UI events that can occur before/after conversations
                return;
            }


            var node = NodeManager.CurrentNode;
            if (node is null)
            {
                Warning($"Node is null!");
                return;
            }

            // After text completes, show responses if any
            var responses = node.Responses;
            if (responses is { Count: > 0 })
            {
                UiManager.RenderResponses(node);
                // Open choice navigation over responses; default to first
                _choiceController?.Open(responses.Count, -1);
                // UiManager.HighlightResponse(0);
            }
            else
            {
                // No responses -> wait for submit to advance
                _choiceController?.Close();
            }
            // else if (NodeManager.CurrentNode.nextNode != null)
            // UiManager.ShowAdvancePrompt(); // No responses, but node has continuation
        }

        private void HandleResponseChosen(int index, Response response)
        {
            InfoInProgress($"Handling response: {index} -> {response.Text}");
            // Forward player choice to NodeManager
            NodeManager.SelectResponse(index);
            // Close choice navigation
            _choiceController?.Close();
        }

        private void HandleAdvanceRequested()
        {
            // Player submitted choice, advance to next node
            NodeManager.AdvanceToNextNode();
        }

        #endregion

        #endregion

        #region Helpers/Utils

        

        // Input routing to ChoiceSelectionController
        private void OnUINavigate(Vector2 dir)
        {
            Assert.IsNotNull(_choiceController);
            if(!_uiOpen) return;
            
            if(_choiceController.IsOpen)
                _choiceController.HandleNavigate(dir);
            
            UiManager.HighlightResponse(_choiceController.SelectedIndex);
        }
        
        private void OnUISubmit()
        {
            Assert.IsNotNull(_choiceController);
            if(!_uiOpen) return;

            if (_choiceController.IsOpen)
            {
                // Submit current selection
                _choiceController.Submit();
            }
            else
            {
                // No open choices -> treat submit as advance
                NodeManager?.AdvanceToNextNode();
            }
        }
        
        private void OnUICancel()
        {
            Assert.IsNotNull(_choiceController);
            if(!_uiOpen) return;

            if (_choiceController.IsOpen)
            {
                // Cancel current selection
                _choiceController.Cancel();
                return;
            }
            
            // If not selecting choices, cancel ends the conversation
            NodeManager?.EndConversation();
        }

        private void HandleChoiceSubmit(int index)
        {
            Assert.IsNotNull(UiManager);
            UiManager.SelectResponse(index);
        }

        private void HandleChoiceCancel()
        {
            Assert.IsNotNull(NodeManager);
            NodeManager.EndConversation();
        }
        
        // If you later add visual highlighting support in DialogUIManager, hook it here.
        private void HandleChoiceSelectionChanged(int index)
        {
            UiManager?.HighlightResponse(index);
        }

        private void EnsureEventSubscriptions()
        {
            if (_choiceController is not null)
            {
                _choiceController.OnSubmit += HandleChoiceSubmit;
                _choiceController.OnCancel += HandleChoiceCancel;
            }

            // Subscribe to NodeManager events
            if (NodeManager is not null && !_subscribedToNodeManager)
            {
                NodeManager.OnNodeEntered += HandleNodeEntered;
                NodeManager.OnNodeExited += HandleNodeExited;
                NodeManager.OnConversationEnded += HandleConversationEnd;
                _subscribedToNodeManager = true;
            }

            // Subscribe to UIManager events
            if (UiManager is not null && !_subscribedToUiManager)
            {
                UiManager.OnResponseChosen += HandleResponseChosen;
                UiManager.OnTextComplete += HandleTextComplete;
                UiManager.OnAdvanceRequested += HandleAdvanceRequested;
                _subscribedToUiManager = true;
            }
            if (_inputRouter == null)
                _inputRouter = FindAnyObjectByType<PlayerInputRouter>();
            if (_inputRouter != null)
            {
                _inputRouter.OnUINavigate += OnUINavigate;
                _inputRouter.OnUISubmit += OnUISubmit;
                _inputRouter.OnUICancel += OnUICancel;
            }
        }
        private void EnsureEventUnsub()
        {
            if (NodeManager != null && _subscribedToNodeManager)
            {
                NodeManager.OnNodeEntered -= HandleNodeEntered;
                NodeManager.OnNodeExited -= HandleNodeExited;
                NodeManager.OnConversationEnded -= HandleConversationEnd;
                _subscribedToNodeManager = false;
            }

            if (UiManager != null && _subscribedToUiManager)
            {
                UiManager.OnResponseChosen -= HandleResponseChosen;
                UiManager.OnTextComplete -= HandleTextComplete;
                UiManager.OnAdvanceRequested -= HandleAdvanceRequested;
                _subscribedToUiManager = false;
            }

            if (_choiceController != null)
            {
                _choiceController.OnSubmit -= HandleChoiceSubmit;
                _choiceController.OnCancel -= HandleChoiceCancel;
                // _choiceController.OnSelectionChanged -= HandleChoiceSelectionChanged;
            }
            if (_inputRouter == null)
                _inputRouter = FindAnyObjectByType<PlayerInputRouter>();
            if (_inputRouter != null)
            {
                _inputRouter.OnUINavigate -= OnUINavigate;
                _inputRouter.OnUISubmit -= OnUISubmit;
                _inputRouter.OnUICancel -= OnUICancel;
            }
        }
        #endregion
        
        
    }
}