using System;
using UnityEngine;

namespace SharedData.Runtime
{
    [Serializable]
    public struct FlagChange
    {
        [Tooltip("Key used to set the flag's value.")]
        public string flagKey;

        [Tooltip("The value to assign to the flag (true/false).")]
        public bool value;

    //     [Tooltip("Determines the scope: global, local to NPC, or scene-specific.")]
    //     public ConditionScope scope;
    }
}

