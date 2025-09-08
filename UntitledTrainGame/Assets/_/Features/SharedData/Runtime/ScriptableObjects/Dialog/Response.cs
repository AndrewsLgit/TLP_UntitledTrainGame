using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace SharedData.Runtime
{
    [Serializable]
    public struct Response
    {
        public string Text;
        [Tooltip("Next node to go to after choosing this response. Leave empty if this is the end of the dialog")] 
        [CanBeNull] public DialogNode NextNode;
        [Tooltip("Conditions needed for this response to be available. Leave empty if this response is always available.")]
        [CanBeNull] public List<Condition> Conditions;
        [Tooltip("Flags that will be set when choosing this response.")]
        public List<string> FlagsToSet;
        [Tooltip("Flags that will be cleared when choosing this response.")]
        public List<string> FlagsToClear;
        [Tooltip("Whether choosing this response should consume in-game time in addition to the time spent after the first time the dialog was shown.")]
        public bool ConsumeTime;
    }
}