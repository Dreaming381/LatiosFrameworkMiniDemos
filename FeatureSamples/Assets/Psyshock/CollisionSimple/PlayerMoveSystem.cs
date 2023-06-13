using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace Dragons.PsyshockSamples.CollisionSimple
{
    [BurstCompile]
    public partial struct PlayerMoveSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new Job
            {
                dt         = Time.DeltaTime,
                horizontal = UnityEngine.Input.GetAxis("Horizontal"),
                vertical   = UnityEngine.Input.GetAxis("Vertical")
            }.Schedule();
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            public float horizontal;
            public float vertical;
            public float dt;

            public void Execute(ref CharacterPhysicsState state, in PlayerStats stats)
            {
                state.velocity = Physics.StepVelocityWithInput(new float2(horizontal, vertical),
                                                               state.velocity,
                                                               stats.acceleration,
                                                               stats.deceleration,
                                                               stats.maxSpeed,
                                                               stats.acceleration,
                                                               stats.deceleration,
                                                               stats.maxSpeed,
                                                               dt);
            }
        }
    }
}

