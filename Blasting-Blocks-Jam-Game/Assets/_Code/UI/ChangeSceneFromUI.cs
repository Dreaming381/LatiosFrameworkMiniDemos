using Latios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Post-Jam Notes:
// For jams, I like to do hacks like this for events that need to be driven by the UI.
// It is really easy to feed things back into ECS through the blackboard entities.
// The other two UI scripts were added by the designer.

namespace BB
{
    public class ChangeSceneFromUI : MonoBehaviour
    {
        public void ChangeScene(string sceneName)
        {
            var sbe                                              = World.DefaultGameObjectInjectionWorld.Unmanaged.GetLatiosWorldUnmanaged().sceneBlackboardEntity;
            sbe.AddComponentData(new RequestLoadScene { newScene = sceneName });
        }
    }
}

