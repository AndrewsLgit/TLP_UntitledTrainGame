using Tools.Runtime;
using UnityEngine;

[CreateAssetMenu(menuName = "Event Channels/EventChannel")]
     public class EmptyEventChannel : EventChannel<Empty>
     {
          public void Invoke()
          {
               foreach (var gameEventListener in _listeners)
               {
                    gameEventListener.RaiseEvent(new Empty());
               }
          }
     }