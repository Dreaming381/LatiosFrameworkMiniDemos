using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Post-Jam Notes:
// This is the system that spawns the blocks from the gun. We have infinite fire rate.

using static Unity.Entities.SystemAPI;

namespace BB
{
    [BurstCompile]
    public partial struct FireGunSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new Job
            {
                icb                  = latiosWorld.syncPoint.CreateInstantiateCommandBuffer<RigidBody, AnimatedScale, WorldTransform>(),
                rigidBodyLookup      = GetComponentLookup<RigidBody>(true),
                worldTransformLookup = GetComponentLookup<WorldTransform>(true),
                actionsLookup        = GetComponentLookup<FirstPersonDesiredActions>(true)
            }.Schedule();
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            public InstantiateCommandBuffer<RigidBody, AnimatedScale, WorldTransform> icb;
            [ReadOnly] public ComponentLookup<RigidBody>                              rigidBodyLookup;
            [ReadOnly] public ComponentLookup<WorldTransform>                         worldTransformLookup;
            [ReadOnly] public ComponentLookup<FirstPersonDesiredActions>              actionsLookup;

            public void Execute(in WorldTransform transform, in GunFirer gun)
            {
                var actions = actionsLookup[gun.actionsEntity];
                if (!actions.fire)
                    return;

                var rigidBody             = rigidBodyLookup[gun.rigidBodyPrefab];
                rigidBody.velocity.linear = math.forward(transform.rotation) * gun.exitVelocity;
                var prefabTransform       = worldTransformLookup[gun.rigidBodyPrefab].worldTransform;
                prefabTransform.position  = transform.position;
                var scale                 = prefabTransform.scale;
                var animatedScale         = new AnimatedScale
                {
                    currentTime      = 0f,
                    minScale         = gun.minScale * scale,
                    scaleUpStartTime = gun.scaleUpStartTime,
                    scaleUpEndTime   = gun.scaleUpEndTime,
                    targetScale      = scale
                };
                icb.Add(gun.rigidBodyPrefab, rigidBody, animatedScale, transform);
            }
        }
    }
}

