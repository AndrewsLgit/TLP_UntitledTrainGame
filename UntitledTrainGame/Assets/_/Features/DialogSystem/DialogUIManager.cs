using System;
using System.Collections;
using Foundation.Runtime;
using JetBrains.Annotations;
using SharedData.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogSystem.Runtime
{

    public class DialogUIManager : FMono
    {
        #region Variables

        #region Private
        // --- Start of Private Variables ---
        [Header("UI References - Dialog UI")]
        [SerializeField] private GameObject _dialogUI;
        [SerializeField] private GameObject _characterPortraitContainer;
        [SerializeField] private GameObject _dialogTextContainer;
        [SerializeField] private GameObject _characterNameContainer;
        [SerializeField] private GameObject _dialogResponsesContainer;
        
        [SerializeField] private GameObject _classicResponsePrefab;
        [SerializeField] private GameObject _actionResponsePrefab;
        
        [Header("Dialog Tags")]
        [SerializeField] private const string _dialogTextTag = "DialogText";
        [SerializeField] private const string _dialogResponsesTag = "DialogResponses";
        [SerializeField] private const string _dialogCharacterPortraitTag = "DialogCharacter";
        [SerializeField] private const string _dialogCharacterNameTag = "DialogName";

        
        private float _cps = 30f;
        private Coroutine _typewriterCoroutine;
        
        // Protected raisers so derived classes (like tests) can trigger events
        protected void RaiseTextComplete() => OnTextComplete?.Invoke();
        protected void RaiseResponseChosen(int index, Response response) => OnResponseChosen?.Invoke(index, response);
        protected void RaiseAdvanceRequested() => OnAdvanceRequested?.Invoke();
        protected void RaiseConversationEnd() => OnConversationEnd?.Invoke();

        // --- End of Private Variables --- 
        #endregion

        #region Public
        // --- Start of Public Variables ---
        [Header("UI References")] [CanBeNull]
        public GameObject DialogUI;
        public Sprite CharacterPortrait;
        public Text CharacterName;
        [CanBeNull] public GameObject CharacterNameContainer;
        
        public event Action OnTextComplete;               // Triggered when typewriter finishes.
        public event Action<int, Response> OnResponseChosen; // Triggered when player selects a response.
        public event Action OnConversationEnd;            // Triggered when UI closes.
        public event Action OnAdvanceRequested;           // Triggered when player wants to advance to next node.
        
        // --- End of Public Variables --- 
        #endregion

        #endregion

        #region Unity API
    
        private void Awake() { }

        private void Start()
        {
            if (_dialogUI != null)
            {
                AssignUIReferences();
            }
        }

        private void Update() { }

        private void FixedUpdate() { }

        private void OnEnable() { }

        private void OnDisable() { }

        private void OnDestroy() { }

        #endregion

        #region Main Methods

        public virtual void Open()
        {
            gameObject.SetActive(true);
        }
        public virtual void Close()
        {
            gameObject.SetActive(false);
            if(_dialogTextContainer != null) _dialogTextContainer.GetComponent<TextMeshProUGUI>().text = "";
            
            if(_dialogResponsesContainer != null)
                foreach (Transform child in _dialogResponsesContainer.transform)
                    Destroy(child.gameObject);
            
            // OnConversationEnd?.Invoke();
            RaiseConversationEnd();
        }

        public virtual void RenderNode(DialogNode node)
        {
            CharacterPortrait = node.Character.PortraitSprite;
            CharacterName.text = node.Character.Name;
            _characterPortraitContainer.GetComponentInChildren<Image>().sprite = CharacterPortrait;
            _dialogTextContainer.GetComponentInChildren<Image>().sprite = node.Character.DefaultTextBoxSprite;
            _dialogTextContainer.GetComponentInChildren<TextMeshProUGUI>().text = "";
            // _dialogTextContainer.GetComponentInChildren<TextMeshProUGUI>().text = node.DialogText;
            Instantiate(node.Character.NamePlatePrefab, _characterNameContainer.transform);

            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(TypeWriter(node.DialogText));
        }

        public virtual void RenderResponses(DialogNode node)
        {
            for (int i = 0; i < _dialogResponsesContainer.transform.childCount; i++)
            {
                var go = Instantiate(_classicResponsePrefab, _dialogResponsesContainer.transform.GetChild(i));
                go.GetComponentInChildren<TextMeshProUGUI>().text = node.Responses[i].Text;
                go.transform.GetChild(0).gameObject.SetActive(true);
                go.transform.GetChild(1).gameObject.SetActive(false);
            }
        }

        public virtual void SelectResponse(int index)
        {
            RaiseResponseChosen(index, _dialogResponsesContainer.transform.GetChild(index).GetComponent<Response>());
            // OnResponseChosen?.Invoke(index, _dialogResponsesContainer.transform.GetChild(index).GetComponent<Response>());
        }
        #endregion

        #region Helpers/Utils

        public virtual void SetTypewriterSpeed(float cps)
        {
            _cps = Mathf.Max(1f, cps);
        }
        
        private void AssignUIReferences()
        {
            foreach (Transform child in _dialogUI.transform.GetChild(0).transform)
            {
                switch (child.tag)
                {
                    case _dialogTextTag:
                        _dialogTextContainer = child.gameObject;
                        Info($"Found dialog text container: {_dialogTextContainer.name} with tag {_dialogTextTag}");
                        break;
                    case _dialogResponsesTag:
                        _dialogResponsesContainer = child.gameObject;
                        Info($"Found dialog responses container: {_dialogResponsesContainer.name} with tag {_dialogResponsesTag}");
                        break;
                    case _dialogCharacterPortraitTag:
                        _characterPortraitContainer = child.gameObject;
                        Info($"Found character portrait container: {_characterPortraitContainer.name} with tag {_dialogCharacterPortraitTag}");
                        break;
                    case _dialogCharacterNameTag:
                        _characterNameContainer = child.gameObject;
                        Info($"Found character name container: {_characterNameContainer.name} with tag {_dialogCharacterNameTag}");
                        break;
                }
            }
        }

        private IEnumerator TypeWriter(string text)
        {
            _dialogTextContainer.GetComponentInChildren<TextMeshProUGUI>().text = "";
            foreach (char letter in text)
            {
                _dialogTextContainer.GetComponentInChildren<TextMeshProUGUI>().text += letter;
                yield return new WaitForSeconds(1f / _cps);
            }
            RaiseTextComplete();
        }
        #endregion
    }
}