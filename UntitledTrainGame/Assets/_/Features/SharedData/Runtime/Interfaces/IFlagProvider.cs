namespace SharedData.Runtime
{
    public interface IFlagProvider
    {
        // Query Flag state
        bool GetFlag(string key);
        // Update or create flag
        void SetFlag(string key, bool value);
        // Reset scoped flags when scenes change
        void ClearFlagsForScene(string sceneName);
    }
}