using Foundation.Runtime;
using Player.Runtime;
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
        
        // --- End of Public Variables --- 

        #endregion

        #endregion

        #region Unity API

        private void Awake()
        {
            if(NodeManager == null)
                NodeManager = NodeManager.Instance;
            
            
            
            // Choice navigation controller
            _choiceController = new ChoiceSelectionController(_navRepeatCooldown);
            _choiceController.OnSubmit += HandleChoiceSubmit;
            _choiceController.OnCancel += HandleChoiceCancel;
            // SelectionChanged can be used to update highlight if/when UI supports it
            // _choiceController.OnSelectionChanged += HandleChoiceSelectionChanged;

            EnsureEventSubscriptions();
        }

        private void Start()
        {
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
            if (_inputRouter != null)
            {
                _inputRouter.OnUINavigate += OnUINavigate;
                _inputRouter.OnUISubmit += OnUISubmit;
                _inputRouter.OnUICancel += OnUICancel;
            }
            
            EnsureEventSubscriptions();
        }

        private void OnDisable()
        {
            if (_inputRouter != null)
            {
                _inputRouter.OnUINavigate -= OnUINavigate;
                _inputRouter.OnUISubmit -= OnUISubmit;
                _inputRouter.OnUICancel -= OnUICancel;
            }
        }

        private void OnDestroy()
        {
            if (NodeManager != null && _subscribedToNodeManager)
            {
                NodeManager.OnNodeEntered -= HandleNodeEntered;
                NodeManager.OnNodeExited -= HandleNodeExited;
                NodeManager.OnConversationEnded -= HandleConversationEnd;
            }

            if (UiManager != null && _subscribedToUiManager)
            {
                UiManager.OnResponseChosen -= HandleResponseChosen;
                UiManager.OnTextComplete -= HandleTextComplete;
                UiManager.OnAdvanceRequested -= HandleAdvanceRequested;
            }

            if (_choiceController != null)
            {
                _choiceController.OnSubmit -= HandleChoiceSubmit;
                _choiceController.OnCancel -= HandleChoiceCancel;
                // _choiceController.OnSelectionChanged -= HandleChoiceSelectionChanged;
            }

        }

        #endregion

        #region Main Methods
        
        /// <summary>
        /// Starts a conversation with a root node and NPC ID.
        /// </summary>
        public void StartConversation(DialogNode root, string npcId)
        {
            Assert.IsNotNull(UiManager);
            Assert.IsNotNull(NodeManager);
            
            // make sure event pipeline is wired before starting conversation
            EnsureEventSubscriptions();
            
            UiManager.Open();
            _uiOpen = true;
            NodeManager.StartConversation(root, npcId);
        }
        
        #region NodeManager Event Handlers

        private void HandleNodeEntered(DialogNode node)
        {
            Assert.IsNotNull(UiManager);
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
        }

        #endregion

        #region UIManager Event Handlers

        private void HandleTextComplete()
        {
            Assert.IsNotNull(UiManager);
            Assert.IsNotNull(NodeManager);

            var node = NodeManager.CurrentNode;
            if (node == null)
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
                _choiceController?.Open(responses.Count, 0);
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

        private void EnsureEventSubscriptions()
        {
            // Subscribe to NodeManager events
            if (NodeManager != null && !_subscribedToNodeManager)
            {
                NodeManager.OnNodeEntered += HandleNodeEntered;
                NodeManager.OnNodeExited += HandleNodeExited;
                NodeManager.OnConversationEnded += HandleConversationEnd;
                _subscribedToNodeManager = true;
            }

            // Subscribe to UIManager events
            if (UiManager != null && !_subscribedToUiManager)
            {
                UiManager.OnResponseChosen += HandleResponseChosen;
                UiManager.OnTextComplete += HandleTextComplete;
                UiManager.OnAdvanceRequested += HandleAdvanceRequested;
                _subscribedToUiManager = true;
            }
        }

        // Input routing to ChoiceSelectionController
        private void OnUINavigate(Vector2 dir)
        {
            Assert.IsNotNull(_choiceController);
            if(!_uiOpen) return;
            
            if(_choiceController.IsOpen)
                _choiceController.HandleNavigate(dir);
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
        // private void HandleChoiceSelectionChanged(int index)
        // {
        //     UiManager?.HighlightResponse(index);
        // }

        #endregion
        
        
    }
}