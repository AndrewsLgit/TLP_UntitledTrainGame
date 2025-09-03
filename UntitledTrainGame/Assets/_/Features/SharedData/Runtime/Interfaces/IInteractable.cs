using UnityEngine;

namespace SharedData.Runtime
{
    public interface IInteractable
    {
        public void Interact();
        public void AdvanceTime(GameTime time);
        public GameTime TimeToInteract { get; }
    }
}
