using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace SharedData.Runtime
{
    [CreateAssetMenu(fileName = "SO_DialogNodeData", menuName = "Dialog/DialogNodeData", order = 0)]
    public class DialogNode : ScriptableObject
    {
        public string Id;
        public CharacterData Character;
        [Tooltip("Text to display in dialog box.")]
        [TextArea] public string DialogText;
        
        [Tooltip("(Optional) Overrides the character's portrait sprite")]
        [CanBeNull] public Sprite PortraitSpriteOverride;
        [Tooltip("(Optional) Overrides the character's nameplate sprite")]
        [CanBeNull] public Sprite NamePlateSpriteOverride;
        
        [Tooltip("(Optional) Player can choose from these responses. Leave empty if this is the end of the dialog or if this dialog leads to another dialog.")] 
        [CanBeNull] public List<Response> Responses = new List<Response>();
        
        [Tooltip("(Optional) Next node to go to after this dialog is over. Leave empty if this is the end of the dialog or if the player needs to choose a response.")] 
        [CanBeNull] public DialogNode NextNode;

        [Tooltip("Flags whose value will be set when choosing this response.")] 
        [CanBeNull] public List<FlagChange> FlagsToChange;
        
        [Tooltip("Conditions needed for this response to be available. Leave empty if this response is always available.")] 
        [CanBeNull] public List<Condition> Conditions = new List<Condition>();

        [Tooltip("(Optional) Designer notes. Not used by the game.")]
        public string Notes;

        [Tooltip("Determines if this is an end node. DO NOT TOUCH! This is handled automatically.")]
        public bool IsEndNode => (Responses == null || Responses.Count == 0) && (NextNode == null); 
    }
    // [Serializable]
    // public struct Response
    // {
    //     private string Id;
    //     public string Text;
    //     [Tooltip("Next node to go to after choosing this response. Leave empty if this is the end of the dialog")] 
    //     [CanBeNull] public DialogNode NextNode;
    //     [Tooltip("Conditions needed for this response to be available. Leave empty if this response is always available.")]
    //     [CanBeNull] public List<Condition> Conditions;
    //     [Tooltip("Flags that will be set when choosing this response.")]
    //     public List<string> FlagsToSet;
    //     [Tooltip("Flags that will be cleared when choosing this response.")]
    //     public List<string> FlagsToClear;
    // }
    //
    // [Serializable]
    // public struct Condition 
    // {
    //     [Tooltip("Key used to check the flag system.")]
    //     public string flagKey;
    //     [Tooltip("The expected state of the flag (true/false).")]
    //     public bool requiredValue;
    //     [Tooltip("Determines the scope: global, local to NPC, or scene-specific.")]
    //     public conditionscope scope;
    // }
}