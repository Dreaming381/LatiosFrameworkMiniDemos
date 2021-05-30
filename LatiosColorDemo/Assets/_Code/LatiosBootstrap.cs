using System;
using System.Collections.Generic;
using Latios;
using Latios.Systems;
using Unity.Collections;
using Unity.Entities;

namespace Dragons
{
    public class LatiosBootstrap : ICustomBootstrap
    {
        public bool Initialize(string defaultWorldName)
        {
            var world                             = new LatiosWorld(defaultWorldName);
            World.DefaultGameObjectInjectionWorld = world;
            world.useExplicitSystemOrdering       = true;

            var initializationSystemGroup = world.initializationSystemGroup;
            var simulationSystemGroup     = world.simulationSystemGroup;
            var presentationSystemGroup   = world.presentationSystemGroup;
            var systems                   = new List<Type>(DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default));

            systems.RemoveSwapBack(typeof(LatiosInitializationSystemGroup));
            systems.RemoveSwapBack(typeof(LatiosSimulationSystemGroup));
            systems.RemoveSwapBack(typeof(LatiosPresentationSystemGroup));
            systems.RemoveSwapBack(typeof(InitializationSystemGroup));
            systems.RemoveSwapBack(typeof(SimulationSystemGroup));
            systems.RemoveSwapBack(typeof(PresentationSystemGroup));

            BootstrapTools.InjectUnitySystems(systems, world, simulationSystemGroup);
            BootstrapTools.InjectRootSuperSystems(systems, world, simulationSystemGroup);

            // The documentation provides one way of initializing Myri when using Explicit Workflow.
            // This is another way. ;)
            BootstrapTools.InjectSystem(typeof(Latios.Myri.Systems.AudioSystem), world, initializationSystemGroup);

            initializationSystemGroup.SortSystems();
            simulationSystemGroup.SortSystems();
            presentationSystemGroup.SortSystems();

            ScriptBehaviourUpdateOrder.AddWorldToCurrentPlayerLoop(world);
            return true;
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class SimulationRootSuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddSystem<MoveCameraSystem>();
            GetOrCreateAndAddSystem<CameraManagerCameraSyncHackSystem>();

            GetOrCreateAndAddSystem<SpawnSystem>();
            GetOrCreateAndAddSystem<SimulateSystem>();
            GetOrCreateAndAddSystem<CollisionSystem>();
            GetOrCreateAndAddSystem<PropagateColorSystem>();

            // It seems Unity can only draw about 200 AABBs using the default number of Debug lines.
            //GetOrCreateAndAddSystem<DebugDrawCollidersSystem>();
        }
    }
}

