using UnityEngine;

namespace SharedData.Runtime
{
    public interface IInteractable
    {
        public void Interact();
        public void AdvanceTime(GameTime time);
        public GameTime TimeToInteract { get; }
        public InteractionType InteractionType { get; }
    }
    public enum InteractionType {
        Dialog,
        Read,
        Inspect,
        PickUp,
        Train,
        EnterBuilding,
        Bench
    }
}
