using System;
using UnityEngine;

namespace SharedData.Runtime
{
    // Public UI interface for the 2-choice bench dialog.
    // PlayerInteraction opens this UI and switches to the UI action map before calling Open,
    // and switches back to the Player action map after we invoke a callback and Close.
    public interface IBenchChoiceUI
    {
        // Fired when user selects an option.
        public event Action<int> OnChoiceSelected; // 0 or 1
        
        // Fired when user cancels (e.g., ESC/B button).
        public event Action OnCancelled;
        
        // Open the UI with the provided model; PlayerInteraction has already switched to UI map.
        public void Open(BenchChoiceModel model);
        
        // Close the UI; PlayerInteraction will switch back to Player map after this is called.
        public void Close();
        

        // Simple data model: two option labels and (optionally) IInteractable targets (index 0/1)
        // The UI only emits selection; PlayerInteraction decides which IInteractable to call.
        public sealed class BenchChoiceModel
        {
            public string[] Options;           // e.g., ["Sleep", "Wait"]
            public IInteractable[] Targets;    // optional: map selection -> specific interactable
        }

    
    }
}