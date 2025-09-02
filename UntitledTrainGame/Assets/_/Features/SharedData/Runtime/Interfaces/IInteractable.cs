using UnityEngine;

namespace SharedData.Runtime
{
    public interface IInteractable
    {
        public void Interact();
        public GameTime TimeToInteract { get;}
    }
}
