using System;
using System.Collections.Generic;
using System.Linq;
using Foundation.Runtime;
using SharedData.Runtime;
using UnityEngine.SceneManagement;

namespace DialogSystem.Runtime
{

    public class NodeManager : FMono
    {
        #region Variables

        #region Private

        // --- Start of Private Variables ---
        private readonly IFlagProvider _flagProvider;

        // Track which nodes have been talked to already
        private readonly HashSet<string> _firstTalkSet = new HashSet<string>();

        private List<Response> _responses = new List<Response>();
        // --- End of Private Variables --- 

        #endregion

        #region Public

        // --- Start of Public Variables ---
        public DialogNode CurrentNode { get; private set; }
        public string CurrentSpeakerId { get; private set; }

        // Events
        public event Action<DialogNode> OnNodeEntered;
        public event Action<DialogNode> OnNodeExited;
        public event Action<string> OnFirstTimeTalk;
        public event Action OnConversationEnded;

        public static NodeManager Instance { get; private set; }
        // --- End of Public Variables --- 

        #endregion

        #endregion

        #region Unity API

        private void Awake()
        {
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
        }

        #endregion

        #region Main Methods

        public void StartConversation(DialogNode root, string speakerId)
        {
            CurrentNode = null;
            if (root is null) return;
            CurrentSpeakerId = speakerId;
            EnterNode(root);
        }

        private void EnterNode(DialogNode node)
        {
            if (node is null)
            {
                EndConversation();
                return;
            }

            // Check node conditions
            if (!PassedNodeConditions(node)) return;

            CurrentNode = node;
            OnNodeEntered?.Invoke(CurrentNode);

            HandleFlags(CurrentNode);

            // Consume time once per npc per scene
            ConsumeTimeIfNotRepeat(CurrentNode);

            _responses.Clear();
            _responses = BuildResponses(CurrentNode);

            if (_responses.Count <= 0 && CurrentNode.NextNodes is {Count: > 0})
            {
                // todo: wait for player input with UI Map
                OnNodeExited?.Invoke(CurrentNode);
                //EnterNode(CurrentNode.NextNode);
            }
        }

        public void SelectResponse(int index)
        {
            if (CurrentNode is null || index < 0 || index >= _responses.Count) return;

            var response = _responses[index];
            InfoInProgress($"Selecting response: {index} -> {response.Text}");
            HandleFlags(response);

            if (response.NextNode is not null)
            {
                EnterNode(response.NextNode);
            }
            else if (CurrentNode.NextNodes is {Count: > 0})
            {
                // Choose the first eligible next node base on conditions, or a condition-less node if none pass
                var eligible = GetNextEligibleNodesInternal(CurrentNode);
                if (eligible is {Count: > 0})
                    EnterNode(eligible[0]);
                else EndConversation();
            }
            else EndConversation();
        }

        public void EndConversation()
        {
            if (CurrentNode is not null)
                OnNodeExited?.Invoke(CurrentNode);

            CurrentNode = null;
            CurrentSpeakerId = null;
            OnConversationEnded?.Invoke();
        }

        private void ResolveFallback(DialogNode node)
        {
            if (node?.NextNodes is {Count: > 0})
            {
                EnterNode(node.NextNodes[0]);
                return;
            }
            EndConversation();
        }

        public void ClearScopedFlags(string sceneName)
        {
            // Clear all flags that are scoped to this scene and reset first talked set
            _flagProvider?.ClearFlagsForScene(sceneName);

            _firstTalkSet.RemoveWhere(key => key.StartsWith(sceneName));
        }

        public void AdvanceToNextNode()
        {
            if (CurrentNode?.NextNodes is { Count: > 0 } && _responses.Count == 0)
            {
                // Iterate over next nodes, pick the first that passes conditions;
                var eligible = GetNextEligibleNodesInternal(CurrentNode);
                if (eligible is { Count: > 0 })
                {
                    EnterNode(eligible[0]);
                }
                else EndConversation();
            }
        }
        
        // Allows callers (like UI or Tools) to get all eligible next nodes at this point of the conversation
        public IReadOnlyList<DialogNode> GetNextEligibleNodes()
        {
            if (CurrentNode is null) return Array.Empty<DialogNode>();
            return GetNextEligibleNodesInternal(CurrentNode);
        }

        #endregion

        #region Helpers/Utils

        private void HandleFlags(DialogNode node)
        {
            if (node.FlagsToChange is {Count: <= 0} or null) return;
            foreach (var f in node.FlagsToChange)
            {
                if (_flagProvider is not null)
                    _flagProvider.SetFlag(f.flagKey, f.value);
                else Warning($"FlagProvider is null! Not setting flag: {f.flagKey}");
            }
        }

        private void HandleFlags(Response response)
        {
            if (response.FlagsToChange is {Count: <= 0} or null) return;
            foreach (var f in response.FlagsToChange)
            {
                if (_flagProvider is not null)
                    _flagProvider.SetFlag(f.flagKey, f.value);
                else Warning($"FlagProvider is null! Not setting flag: {f.flagKey}");
            }
        }

        private bool PassedNodeConditions(DialogNode node)
        {
            if (node.Conditions is {Count: <= 0} or null) return true;
            foreach (var condition in node.Conditions)
            {
                if (_flagProvider is not null)
                {
                    bool value = _flagProvider.GetFlag(condition.flagKey);
                    if (value != condition.requiredValue)
                    {
                        ResolveFallback(node);
                        return false;
                    }
                }
                else Warning($"FlagProvider is null! Not checking condition: {condition.flagKey}");
            }

            return true;
        }
        // Side-effect-free condition eval
        private bool NodeConditionsMet(DialogNode node)
        {
            if (node is null) return false;
            if (node.Conditions is {Count: <= 0} or null) return true;
            
            foreach (var condition in node.Conditions)
            {
                if (_flagProvider is not null)
                {
                    bool value = _flagProvider.GetFlag(condition.flagKey);
                    if (value != condition.requiredValue) return false;
                }
                else Warning($"FlagProvider is null! Not checking condition: {condition.flagKey}");
            }
            return true;
        }
        
        // Returns nodes that pass conditions; if none, returns condition-less nodes.
        private List<DialogNode> GetNextEligibleNodesInternal(DialogNode fromNode)
        {
            var result = new List<DialogNode>();
            if (fromNode?.NextNodes is not {Count: > 0}) return result;
            
            // Nodes whose conditions pass
            foreach (var next in fromNode.NextNodes)
            {
                if (NodeConditionsMet(next)) result.Add(next);
            }
            
            if (result.Count > 0) return result;
            
            // fallback: Nodes with no conditions
            foreach (var next in fromNode.NextNodes)
            {
                if(next?.Conditions is {Count: <= 0} or null) result.Add(next);
            }
            
            return result;
        }

        private void ConsumeTimeIfNotRepeat(DialogNode node)
        {
            string key = SceneManager.GetActiveScene().name;
            if (_firstTalkSet.Contains(key)) return;


            _firstTalkSet.Add(key);
            OnFirstTimeTalk?.Invoke(key); // todo: subscribe ClockManager to this event in order to advance time
        }
        
        private List<Response> BuildResponses(DialogNode node)
        {
            var responses = new List<Response>();
            if (node.Responses is {Count: <= 0} or null) return responses;
            
            // foreach (var response in node.Responses)
            // {
            //     bool valid = PassedResponseConditions(response);
            //     if (valid) responses.Add(response);
            // }
            responses.AddRange(from response in node.Responses let valid = PassedResponseConditions(response) where valid select response);

            return responses;
        }

        private bool PassedResponseConditions(Response response)
        {
            if (response.Conditions is {Count: <= 0} or null) return true;
            foreach (var condition in response.Conditions)
            {
                if (_flagProvider is not null)
                {
                    bool value = _flagProvider.GetFlag(condition.flagKey);
                    if (value != condition.requiredValue)
                    {
                        return false;
                    }
                }
                else Warning($"FlagProvider is null! Not checking condition: {condition.flagKey}");
            }

            return true;
        }

        #endregion
    }
}