using System;
using System.Collections.Generic;
using System.Linq;
using Foundation.Runtime;
using SharedData.Runtime;
using UnityEngine.SceneManagement;


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
        if (Instance != null && Instance != this)
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
        if(root == null) return;
        CurrentSpeakerId = speakerId;
        EnterNode(root);
    }

    private void EnterNode(DialogNode node)
    {
        if (node == null)
        {
            EndConversation();
            return;
        }
        
        // Check node conditions
        if(!PassedNodeConditions(node)) return;

        CurrentNode = node;
        OnNodeEntered?.Invoke(node);

        HandleFlags(node);
        
        // Consume time once per npc per scene
        ConsumeTimeIfNotRepeat(node);
        
        _responses.Clear();
        _responses = BuildResponses(node);

        if (_responses.Count == 0 && node.NextNode != null)
        {
            // todo: wait for player input with UI Map
            EnterNode(node.NextNode);
        }
    }

    public void SelectResponse(int index)
    {
        if(CurrentNode == null || index < 0 || index >= _responses.Count) return;
        
        var response = _responses[index];
        HandleFlags(response);

        if (response.NextNode != null)
        {
            EnterNode(response.NextNode);
        }
        else if (CurrentNode.NextNode != null)
        {
            EnterNode(CurrentNode.NextNode);
        }
        else EndConversation();
    }

    public void EndConversation()
    {
        if (CurrentNode != null)
            OnNodeExited?.Invoke(CurrentNode);
        
        CurrentNode = null;
        CurrentSpeakerId = null;
        OnConversationEnded?.Invoke();
    }

    private void ResolveFallback(DialogNode node)
    {
        EndConversation();
    }

    public void ClearScopedFlags(string sceneName)
    {
        // Clear all flags that are scoped to this scene and reset first talked set
        if (_flagProvider != null)
        {
            _flagProvider.ClearFlagsForScene(sceneName);
        }

        _firstTalkSet.RemoveWhere(key => key.StartsWith(sceneName));
    }
    public void AdvanceToNextNode()
    {
        if(CurrentNode != null && _responses.Count == 0 && CurrentNode.NextNode != null) 
            EnterNode(CurrentNode.NextNode);
    }
    #endregion

    #region Helpers/Utils

    private void HandleFlags(DialogNode node)
    {
        foreach (var f in node.FlagsToSet)
        {
            if(_flagProvider != null)
                _flagProvider.SetFlag(f, true);
            else Warning($"FlagProvider is null! Not setting flag: {f}");
        }
        foreach (var f in node.FlagsToClear)
        {
            if(_flagProvider != null)
                _flagProvider.SetFlag(f, false);
            else Warning($"FlagProvider is null! Not clearing flag: {f}");
        }
    }
    private void HandleFlags(Response response)
    {
        foreach (var f in response.FlagsToSet)
        {
            if(_flagProvider != null)
                _flagProvider.SetFlag(f, true);
            else Warning($"FlagProvider is null! Not setting flag: {f}");
        }
        foreach (var f in response.FlagsToClear)
        {
            if(_flagProvider != null)
                _flagProvider.SetFlag(f, false);
            else Warning($"FlagProvider is null! Not clearing flag: {f}");
        }
    }

    private bool PassedNodeConditions(DialogNode node)
    {
        foreach (var condition in node.Conditions)
        {
            if (_flagProvider != null)
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

    private void ConsumeTimeIfNotRepeat(DialogNode node)
    {
        string key = SceneManager.GetActiveScene().name;
        if (_firstTalkSet.Contains(key)) return;
        
        bool consumeTime = node.Responses.Any(r => r.ConsumeTime);

        if (!consumeTime) return;
        _firstTalkSet.Add(key);
        OnFirstTimeTalk?.Invoke(key); // todo: subscribe ClockManager to this event in order to advance time
    }

    private List<Response> BuildResponses(DialogNode node)
    {
        var responses = new List<Response>();
        foreach (var response in node.Responses)
        {
            bool valid = true;
            valid = PassedResponseConditions(response);
            // foreach (var cond in response.Conditions)
            // {
            //     if (_flagProvider != null)
            //     {
            //         bool flag = _flagProvider.GetFlag(cond.flagKey);
            //         if (flag != cond.requiredValue)
            //         {
            //             valid = false;
            //             break;
            //         }
            //     }
            //     else Warning($"FlagProvider is null! Not checking condition: {cond.flagKey}");
            // }
            if(valid) responses.Add(response);
        }
        
        return responses;
    }
    
    private bool PassedResponseConditions(Response response)
    {
        foreach (var condition in response.Conditions)
        {
            if (_flagProvider != null)
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
