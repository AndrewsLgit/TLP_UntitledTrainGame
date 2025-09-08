namespace SharedData.Runtime
{
    public interface IInteractionPopupPresenter
    {
        void Show(InteractionType type);
        void Hide();
    }
}