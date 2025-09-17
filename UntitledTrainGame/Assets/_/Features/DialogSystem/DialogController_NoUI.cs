using Foundation.Runtime;
using SharedData.Runtime;
using UnityEngine;

namespace DialogSystem.Runtime
{
    /// <summary>
    /// Minimal DialogueController variant without UIManager.
    /// Useful for testing NodeManager and dialogue logic in isolation.
    /// Logs dialog flow to the Unity console instead of updating UI.
    /// </summary>
    public class DialogController_NoUI : FMono
    {
        #region Variables

        #region Private

        // --- Start of Private Variables ---

        [Header("Core References")]
        [SerializeField] private NodeManager _nodeManager;

        [Header("Test")] 
        [SerializeField] private DialogNode _testRootNode;
        [SerializeField] private int _testResponseIndex = 0;

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
            _nodeManager = NodeManager.Instance;
        }

        private void Start()
        {
            _nodeManager.OnNodeEntered += HandleNodeEntered;
            _nodeManager.OnNodeExited += HandleNodeExited;
            _nodeManager.OnConversationEnded += HandleConversationEnd;
        }
        
        private void OnDestroy()
        {
        }

        #endregion

        #region Main Methods

        [ContextMenu("Start Test Conversation")]
        public void StartTestConversation()
        {
            StartConversation(_testRootNode, _testRootNode.Character.Id);
        }

        /// <summary>
        /// Starts a conversation with a root node and NPC ID.
        /// </summary>
        public void StartConversation(DialogNode rootNode, string npcId)
        {
            _nodeManager.StartConversation(rootNode, npcId);
        }
        
        #region Event Handlers

        private void HandleNodeEntered(DialogNode node)
        { 
            Info($"Entered node: {node.Id} | Character: {node.Character.Name} | Text: {node.DialogText}");
            if (node.Responses.Count > 0)
            {
                for(int i = 0; i < node.Responses.Count; i++)
                    Info($"Response {i}: {node.Responses[i].Text}");
            }
            else if (node.NextNodes is {Count: > 0})
            {
                Info($"Node has continuation. Call AdvanceToNextNode() to continue.");
            }
            else
            {
                Info("End of conversation reached.");
            }
        }

        private void HandleNodeExited(DialogNode node)
        {
            Info($"Exited node: {node.Id}");
        }

        private void HandleConversationEnd()
        {
            Info("Conversation ended.");
        }
        #endregion
        
        #endregion

        #region Helpers/Utils

        [ContextMenu("Choose Response from serialized index")]
        public void ChooseResponse()
        {
            ChooseResponse(_testResponseIndex);
        }

        /// <summary>
        /// Simulate player selecting a response by index.
        /// </summary>
        public void ChooseResponse(int index)
        {
            _nodeManager.SelectResponse(index);
        }

        /// <summary>
        /// Advance conversation to next node if no responses are available.
        /// </summary>
        [ContextMenu("Advance To Next Node")]
        public void Advance()
        {
            _nodeManager.AdvanceToNextNode();
        }
        
        #endregion
    }
}