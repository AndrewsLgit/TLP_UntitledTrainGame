using System;
using UnityEngine;

namespace SharedData.Runtime
{
    [Serializable]
    public struct Condition
    {
        [Tooltip("Key used to check the flag system.")]
        public string flagKey;
        [Tooltip("The expected state of the flag (true/false).")]
        public bool requiredValue;
        [Tooltip("Flag default value.")]
        public bool defaultValue;
        [Tooltip("Determines the scope: global, local to NPC, or scene-specific.")]
        public ConditionScope scope;
    }

    // public enum ConditionScope
    // {
    //     Global,
    //     SingleNPC,
    //     Scene
    // }
}