using System;
using System.Collections;
using System.Collections.Generic;
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
        
        // Local state bound to current node so UI is display-only but can emit correct selection events
        private DialogNode _currentNode;
        private readonly List<GameObject> _spawnedResponses = new List<GameObject>();
        
        // Protected raisers so derived classes (like tests) can trigger events
        protected void RaiseTextComplete() => OnTextComplete?.Invoke();
        protected void RaiseResponseChosen(int index, Response response) => OnResponseChosen?.Invoke(index, response);
        protected void RaiseAdvanceRequested() => OnAdvanceRequested?.Invoke();
        protected void RaiseConversationEnd() => OnConversationEnd?.Invoke();
        protected void RaiseOpened() => OnOpened?.Invoke();
        protected void RaiseClosed() => OnClosed?.Invoke();

        // --- End of Private Variables --- 
        #endregion

        #region Public
        // --- Start of Public Variables ---
        [Header("UI References")] [CanBeNull]
        public GameObject DialogUI;
        public Sprite CharacterPortrait;
        public TextMeshProUGUI CharacterName;
        [CanBeNull] public GameObject CharacterNameContainer;
        
        public event Action OnTextComplete;               // Triggered when typewriter finishes.
        public event Action<int, Response> OnResponseChosen; // Triggered when player selects a response.
        public event Action OnConversationEnd;            // Triggered when UI closes.
        public event Action OnAdvanceRequested;           // Triggered when player wants to advance to next node.
        public event Action OnOpened;                     // Triggered when UI opens.
        public event Action OnClosed;
        
        // --- End of Public Variables --- 
        #endregion

        #endregion

        #region Unity API
    
        private void Awake() { }

        private void Start()
        {
            if (_dialogUI != null)
            {
                _dialogUI.SetActive(false);
                AssignUIReferences();
            }
        }

        #endregion

        #region Main Methods

        public virtual void Open()
        {
            gameObject.SetActive(true);
            // OnOpened?.Invoke();
            RaiseOpened();
        }
        public virtual void Close()
        {
            gameObject.SetActive(false);
            _currentNode = null;
            ClearTypeWriterText();
            ClearResponses();
            // if(_dialogTextContainer != null) _dialogTextContainer.GetComponent<TextMeshProUGUI>().text = "";
            //
            // if(_dialogResponsesContainer != null)
            //     foreach (Transform child in _dialogResponsesContainer.transform)
            //         Destroy(child.gameObject);
            
            // OnConversationEnd?.Invoke();
            RaiseClosed();
            RaiseConversationEnd();
        }

        // Render a full dialog node: portrait, name/nameplate, textbook bg, text (typewriter)
        public virtual void RenderNode(DialogNode node)
        {
            if (node is null) return;
            _currentNode = node;
 
            // Portrait (supports override)
            Sprite portrait = node.PortraitSpriteOverride != null ? node.PortraitSpriteOverride : node.Character?.PortraitSprite;
            var portraitImg = _characterPortraitContainer?.GetComponentInChildren<Image>();
            if (portraitImg is not null) portraitImg.sprite = portrait;
            
            // Dialog textbox background (from character data)
            var textboxBg = _dialogTextContainer?.GetComponentInChildren<Image>();
            if (textboxBg != null && node.Character is not null && node.Character.DefaultTextBoxSprite is not null)
                textboxBg.sprite = node.Character.DefaultTextBoxSprite;

            // Character name + optional nameplate sprite override
            if (CharacterNameContainer is null && _characterNameContainer is not null)
                CharacterNameContainer = _characterNameContainer;
            
            if (CharacterName is null && _characterNameContainer is not null)
                CharacterName = _characterNameContainer.GetComponentInChildren<TextMeshProUGUI>();
            
            if (CharacterName is not null && node.Character is not null)
                CharacterName.text = node.Character.Name;
            
            // If name container has an Image, support NamePlateSpriteOverride
            var nameplateImage = CharacterNameContainer?.GetComponentInChildren<Image>();
            if (nameplateImage is not null)
                nameplateImage.sprite = node.Character.NamePlatePrefab.transform.GetChild(0).GetComponent<Image>().sprite;
                //nameplateImage.sprite = node.NamePlateSpriteOverride ?? nameplateImage.sprite;
            
            // Clear text and (re)start typewriter
            ClearTypeWriterText();
            if (_typewriterCoroutine is not null)
                StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(TypeWriter(node.DialogText));
            
            // Clear any stale responses; they'll be rendered on TextComplete if present
            ClearResponses();
            // DEPRECATED
//             CharacterPortrait = node.Character.PortraitSprite;
//             // CharacterName.text = node.Character.Name;
//             _characterPortraitContainer.GetComponentInChildren<Image>().sprite = CharacterPortrait;
//             _dialogTextContainer.GetComponentInChildren<Image>().sprite = node.Character.DefaultTextBoxSprite;
//             _dialogTextContainer.GetComponentInChildren<TextMeshProUGUI>().text = "";
//             // _dialogTextContainer.GetComponentInChildren<TextMeshProUGUI>().text = node.DialogText;
// //            Instantiate(node.Character.NamePlatePrefab, _characterNameContainer.transform);
//
//             if (_typewriterCoroutine != null)
//                 StopCoroutine(_typewriterCoroutine);
//             _typewriterCoroutine = StartCoroutine(TypeWriter(node.DialogText));
        }

        public virtual void RenderResponses(DialogNode node)
        {
            // === DEPRECATED ===
            // for (int i = 0; i < _dialogResponsesContainer.transform.childCount; i++)
            // {
            //     var go = Instantiate(_classicResponsePrefab, _dialogResponsesContainer.transform.GetChild(i));
            //     go.GetComponentInChildren<TextMeshProUGUI>().text = node.Responses[i].Text;
            //     go.transform.GetChild(0).gameObject.SetActive(true);
            //     go.transform.GetChild(1).gameObject.SetActive(false);
            // }
            // === END DEPRECATED ===
            
            if (node is null) return;
            ClearResponses();
            
            if (_dialogResponsesContainer is null) return;
            if (node.Responses is not { Count: > 0 }) return;
            
            // Instantiate a row per response under the responses container (one layout child or direct? -> direct under container)
            // Expect prefab to have:
            // - TextMeshProUGUI for the label
            // - Two children (index 0 = unselected visuals, index 1 = selected visuals) as per current setup
            for (int i = 0; i < node.Responses.Count; i++)
            {
                var prefab = _classicResponsePrefab ?? _actionResponsePrefab;
                if (prefab is null)
                {
                    Warning($"No response prefab set on DialogUIManager.");
                    break;
                }

                GameObject go = null;
                if (i < _dialogResponsesContainer.transform.childCount)
                {
                    go = Instantiate(prefab, _dialogResponsesContainer.transform.GetChild(i));
                }
                _spawnedResponses.Add(go);
                
                TextMeshProUGUI label = null;
                // Default to unselected visuals on render
                if (go.transform.childCount >= 2)
                {
                    go.transform.GetChild(0).gameObject.SetActive(true);
                    go.transform.GetChild(1).gameObject.SetActive(false);

                    if (go.transform.GetChild(2).GetComponent<TextMeshProUGUI>() is not null)
                    {
                        label = go.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
                        label.text = node.Responses[i].Text;
                        label.transform.gameObject.SetActive(true);
                    }
                }
                
                //var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label is not null) label.text = node.Responses[i].Text;
                
                
                
                // Optional: allow clicking a response to select (controller remains source of truth)
                int idx = i;
                var btn = go.GetComponentInChildren<Button>();
                if (btn is not null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SelectResponse(idx));
                }
            }
        }
        
        // Controller calls this to update highlight state as the player navigates responses
        public virtual void HighlightResponse(int index)
        {
            if (_spawnedResponses.Count <= 0) return;

            for (int i = 0; i < _spawnedResponses.Count; i++)
            {
                var go = _spawnedResponses[i];
                if (go is null) continue;
                
                bool isSelected = (i == index);
                if (go.transform.childCount >= 2)
                {
                    // child(0) -> unselected, child(1) -> selected
                    go.transform.GetChild(0).gameObject.SetActive(!isSelected);
                    go.transform.GetChild(1).gameObject.SetActive(isSelected);
                }
            }
        }

        public virtual void SelectResponse(int index)
        {
            // === DEPRECATED ===
            // RaiseResponseChosen(index, _dialogResponsesContainer.transform.GetChild(index));
            // OnResponseChosen?.Invoke(index, _dialogResponsesContainer.transform.GetChild(index).GetComponent<Response>());
            // === END DEPRECATED ===
            
            if (_currentNode is null) return;
            if (_currentNode.Responses is not {Count: > 0}) return;
            if (index < 0 || index >= _currentNode.Responses.Count) return;
            
            RaiseResponseChosen(index, _currentNode.Responses[index]);;
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
            //CharacterName = _characterNameContainer.GetComponent<TextMeshProUGUI>();
        }

        private IEnumerator TypeWriter(string text)
        {
            // === DEPRECATED ===
            // _dialogTextContainer.GetComponentInChildren<TextMeshProUGUI>().text = "";
            // foreach (char letter in text)
            // {
            //     _dialogTextContainer.GetComponentInChildren<TextMeshProUGUI>().text += letter;
            //     yield return new WaitForSeconds(1f / _cps);
            // }
            // RaiseTextComplete();
            // === END DEPRECATED ===

            if (_dialogTextContainer is null)
            {
                RaiseTextComplete();
                yield break;
            }
            
            var tmp = _dialogTextContainer.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp is null)
            {
                RaiseTextComplete();
                yield break;
            }
            
            tmp.text = "";
            if (string.IsNullOrEmpty(text))
            {
                RaiseTextComplete();
                yield break;
            }

            foreach (char letter in text)
            {
                tmp.text += letter;
                yield return new WaitForSeconds(1f / _cps);
            }
            RaiseTextComplete();
        }

        private void ClearTypeWriterText()
        {
            if (_dialogTextContainer is not null)
            {
                var tmp = _dialogTextContainer.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp is not null) tmp.text = "";
            }
        }

        private void ClearResponses()
        {
            _spawnedResponses.Clear();
            if (_dialogResponsesContainer is null) return;
            foreach (Transform responseContainer in _dialogResponsesContainer.transform)
            foreach (Transform child in responseContainer)
                Destroy(child.gameObject);
        }
        #endregion
    }
}