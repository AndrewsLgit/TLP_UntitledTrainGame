using JetBrains.Annotations;
using UnityEngine;

namespace SharedData.Runtime
{
    [CreateAssetMenu(fileName = "SO_CharacterData", menuName = "Dialog/CharacterData")]
    public class CharacterData : ScriptableObject
    {
        public string Id;
        public string Name;
        [CanBeNull] public Sprite PortraitSprite;
        [CanBeNull] public GameObject NamePlatePrefab;
        public Sprite DefaultTextBoxSprite;
        public GameObject UIPrefab;
        public AudioClip VoiceClip;
        [CanBeNull] public string[] MetaData;
    }
}
