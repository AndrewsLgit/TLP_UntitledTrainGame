using UnityEngine;

namespace SharedData.Runtime
{
    public interface IInteractable
    {
        public void Interact();
        public float TimeToInteract { get;}
    }
}
