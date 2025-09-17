using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace SharedData.Runtime
{
    [CreateAssetMenu(fileName = "SO_DialogNodeData", menuName = "Dialog/DialogNodeData", order = 0)]
    [Serializable]
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
        [CanBeNull] public List<DialogNode> NextNodes;

        [Tooltip("Flags whose value will be set when choosing this response.")] 
        [CanBeNull] public List<FlagChange> FlagsToChange;
        
        [Tooltip("Conditions needed for this response to be available. Leave empty if this response is always available.")] 
        [CanBeNull] public List<Condition> Conditions = new List<Condition>();

        [Tooltip("(Optional) Designer notes. Not used by the game.")]
        public string Notes;

        [Tooltip("Determines if this is an end node. DO NOT TOUCH! This is handled automatically.")]
        public bool IsEndNode => (Responses is {Count: <= 0} or null) && (NextNodes is {Count: <= 0} or null); 
    }
}