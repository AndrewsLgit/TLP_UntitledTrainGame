namespace ServiceInterfaces.Runtime
{
    // Input service – handy so FSM can switch control maps without referencing a concrete manager
    public interface IInputService
    {
        void SwitchToUI();
        void SwitchToPlayer();
    }

}