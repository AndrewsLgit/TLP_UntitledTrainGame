using System;
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
        // --- End of Private Variables --- 
        #endregion

        #region Public
        // --- Start of Public Variables ---
        public static TransitionAnimationController Instance { get; private set; }
        // Fired when a transition animation finishes and the corresponding object is disabled.
        // The string argument is a key describing which transition finished: "FadeIn", "FadeOut", "Sleep", or "Wait".
        public event Action<string> OnTransitionEnded = delegate { };

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
        }

        #endregion

        #region Main Methods

        public void StartFadeIn(Action onComplete = null)
        {
            PlayAndDisable(_fadeIn, "Fade_in", onComplete);
        }
        public void StartFadeOut(Action onComplete = null)
        {
            PlayAndDisable(_fadeOut, "Fade_out", onComplete);
        }
        public void StartSleep(Action onComplete = null)
        {
            PlayAndDisable(_fadeIn, "Sleep", onComplete);
        }
        public void StartWait(Action onComplete = null)
        {
            PlayAndDisable(_fadeIn, "Wait", onComplete);
        }
        #endregion

        #region Helpers/Utils
        
        // Starts the object's animator (if present), waits until the current state's animation finishes,
        // then disables the object and notifies listeners.
        private void PlayAndDisable(GameObject target, string key, Action onComplete)
        {
            if (target is null)
            {
                Warning($"[{nameof(TransitionAnimationController)}] Target for '{key}' is null.");
                return;
            }

            // Ensure active to let Animator enter state
            target.SetActive(true);

            var anim = target.GetComponentInChildren<Animator>(true);
            if (anim == null || !anim.isActiveAndEnabled)
            {
                // If there's no animator, disable immediately and notify.
                target.SetActive(false);
                try { onComplete?.Invoke(); } catch (Exception e) { Error(e.Message); }
                OnTransitionEnded.Invoke(key);
                return;
            }

            // Optional: restart the default state
            // anim.Play(0, 0, 0f);

            StartCoroutine(WaitForAnimationThenDisable(anim, target, key, onComplete));
        }

        private System.Collections.IEnumerator WaitForAnimationThenDisable(Animator anim, GameObject target, string key, Action onComplete)
        {
            // Wait one frame so Animator can enter its first state
            yield return null;

            const int layer = 0;
            // Safety timeout in case of looping clips or misconfiguration (10 seconds unscaled)
            float safety = 10f;

            // If the current state is looping, this will exit on safety timeout.
            while (anim != null && anim.isActiveAndEnabled && target != null && target.activeInHierarchy)
            {
                var info = anim.GetCurrentAnimatorStateInfo(layer);

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

            if (target != null)
                target.SetActive(false);

            // Fire optional callback first, then global event
            try { onComplete?.Invoke(); } catch (Exception e) { Error(e.Message); }
            OnTransitionEnded.Invoke(key);
        }
        #endregion
    }
}
