using Foundation.Runtime;
using SharedData.Runtime;
using UnityEngine;

namespace Interactable.Runtime
{

    public class BenchSleepAction : FMono, IInteractable
    {
        #region Variables

        #region Private
        // --- Start of Private Variables ---
        // --- End of Private Variables --- 
        #endregion

        #region Public
        // --- Start of Public Variables ---
        public GameTime TimeToInteract => default;
        public InteractionType InteractionType => InteractionType.Bench;
        // --- End of Public Variables --- 
        #endregion

        #endregion

        #region Main Methods
        public void Interact()
        {
            var bench = GetComponentInParent<Bench>();
            if (bench == null)
            {
                Error($"Bench not found in parent");
                return;
            }
            bench.Sleep();
        }

        public void AdvanceTime(GameTime time)
        {
            throw new System.NotImplementedException();
        }
        #endregion

    }
}
