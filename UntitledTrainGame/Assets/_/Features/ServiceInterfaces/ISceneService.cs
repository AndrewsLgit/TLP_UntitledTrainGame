using System;
using SharedData.Runtime;

namespace ServiceInterfaces.Runtime
{
    // Scene loading service (you can add more as needed)
    public interface ISceneService
    {
        string CurrentActiveScene { get; }
        event Action OnSceneActivated;
        SceneReference StartScene { get; }

        void PreloadScene(string sceneName);
        void PreloadScene(SceneReference sceneRef);
        void ActivateScene();
        void ResetFromStartScene();
    }
}