using System;
using System.Collections.Generic;
using Latios;
using Latios.Systems;
using Unity.Collections;
using Unity.Entities;

public class LatiosBootstrap : ICustomBootstrap
{
    public bool Initialize(string defaultWorldName)
    {
        var world                             = new LatiosWorld(defaultWorldName);
        World.DefaultGameObjectInjectionWorld = world;

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

        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);

        initializationSystemGroup.SortSystems();
        simulationSystemGroup.SortSystems();
        presentationSystemGroup.SortSystems();

        ScriptBehaviourUpdateOrder.AddWorldToCurrentPlayerLoop(world);

        //world.GetExistingSystem<DestroyEntitiesOnSceneChangeSystem>().Enabled = false;

        return true;
    }
}

