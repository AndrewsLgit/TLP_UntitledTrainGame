using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Foundation.Runtime;
using UnityEngine;

namespace Manager.Runtime
{

    public class TransitionAnimationController : FMono
    {
        #region Variables

        #region Private
        // --- Start of Private Variables ---
        [SerializeField] private GameObject _fadeIn;
        [SerializeField] private GameObject _fadeOut;
        [SerializeField] private GameObject _sleep;
        [SerializeField] private GameObject _wait;
        
        private Dictionary<TransitionType, GameObject> _transitions = new Dictionary<TransitionType, GameObject>();
        // --- End of Private Variables --- 
        #endregion

        #region Public
        // --- Start of Public Variables ---
        public static TransitionAnimationController Instance { get; private set; }
        // Fired when a transition animation finishes and the corresponding object is disabled.
        // The string argument is a key describing which transition finished: "FadeIn", "FadeOut", "Sleep", or "Wait".
        public event Action<string> OnTransitionEnded = delegate { };
        public enum TransitionType { FadeIn, FadeOut, Sleep, Wait }

        // --- End of Public Variables --- 
        #endregion

        #endregion

        #region Unity API

        private void Awake()
        {
            if (Instance is not null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _transitions = new Dictionary<TransitionType, GameObject>
            {
                { TransitionType.FadeIn, _fadeIn },
                { TransitionType.FadeOut, _fadeOut },
                { TransitionType.Sleep, _sleep },
                { TransitionType.Wait, _wait }
            };
        }

        private void Start()
        {
            foreach (var kvp in _transitions)
            {
                kvp.Value?.SetActive(false);
            }
        }

        #endregion

        #region Main Methods

        public void PlayTransition(TransitionType type, Action onComplete = null)
        {
            if (!_transitions.TryGetValue(type, out var target) || target is null)
            {
                Warning($"{nameof(TransitionAnimationController)}: No target for transition type '{type}'.)");
                return;
            }
            
            Info($"Starting {type} transition.");
            StartCoroutine(PlayAfterEnable(target, type.ToString(), onComplete));
        }

        public void StartFadeIn(Action onComplete = null) => PlayTransition(TransitionType.FadeIn, onComplete);
        public void StartFadeOut(Action onComplete = null) => PlayTransition(TransitionType.FadeOut, onComplete);
        public void StartSleep(Action onComplete = null) => PlayTransition(TransitionType.Sleep, onComplete);
        public void StartWait(Action onComplete = null) => PlayTransition(TransitionType.Wait, onComplete);
        
        #endregion

        #region Helpers/Utils
        
        // Starts the object's animator (if present), waits until the current state's animation finishes,
        // then disables the object and notifies listeners.
        private IEnumerator PlayAfterEnable(GameObject target, string key, Action onComplete)
        {
            target?.SetActive(true);
            yield return null; // wait one frame so animators are enabled
            
            var anim = GetChildAnimator(target);
            if (anim is null)
            {
                // If there's no animator, disable immediately and notify.
                target.SetActive(false);
                try { onComplete?.Invoke(); } catch (Exception e) { Error(e.Message); }
                OnTransitionEnded.Invoke(key);
                yield break;
            }

            // Optional: restart the default state
            anim.Play(0, 0, 0f);

            StartCoroutine(WaitForAnimationThenDisable(anim, target, key, onComplete));
        }

        private Animator GetChildAnimator(GameObject parent)
        {
            var anims = parent.GetComponentsInChildren<Animator>(true);
            return anims.FirstOrDefault(anim => anim.gameObject != parent);
        }

        private System.Collections.IEnumerator WaitForAnimationThenDisable(Animator anim, GameObject target, string key, Action onComplete)
        {
            // Wait one frame so Animator can enter its first state
            yield return null;

            const int layer = 0;
            // Safety timeout in case of looping clips or misconfiguration (10 seconds unscaled)
            float safety = 10f;
            
            bool actionCalled = false;

            // If the current state is looping, this will exit on safety timeout.
            while (anim is not null && anim.isActiveAndEnabled && target is not null && target.activeInHierarchy)
            {
                var info = anim.GetCurrentAnimatorStateInfo(layer);

                if ((info.normalizedTime is > .5f and < 1f) && !actionCalled)
                {
                    actionCalled = true;
                    try { onComplete?.Invoke(); } catch (Exception e) { Error(e.Message); }

                    yield return null;
                }
                // Exit when not in transition and animation reached its end (normalizedTime >= 1)
                if (!anim.IsInTransition(layer) && info.normalizedTime >= 1f)
                    break;

                safety -= Time.unscaledDeltaTime;
                if (safety <= 0f)
                {
                    Warning($"[{nameof(TransitionAnimationController)}] Timeout while waiting '{key}' to finish. Disabling anyway.");
                    break;
                }

                yield return null;
            }

            target?.SetActive(false);

            // Fire optional callback first, then global event
            // try { onComplete?.Invoke(); } catch (Exception e) { Error(e.Message); }
            OnTransitionEnded.Invoke(key);
        }
        #endregion
    }
}
