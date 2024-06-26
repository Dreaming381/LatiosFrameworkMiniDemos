using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

// Post-Jam Notes:
// The freezing mechanic wasn't part of the original plan, but rather something we added
// to mitigate the buggy physics at the time (the physics is fixed in this version).
// It was easy to implement, and had the added benefit of being an interesting mechanic
// for the game, especially with the visuals and sound. The freezing is based on the block
// being near-motionless, or after 10 seconds, whichever comes first.
// The color animation is played for all frozen blocks, and the job keeps running on them
// even after the transition is complete. That kept things simple, saving development time.

namespace BB
{
    [BurstCompile]
    public partial struct FreezeBlocksSystem : ISystem
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
            new DynamicJob
            {
                ecb       = latiosWorld.syncPoint.CreateEntityCommandBuffer().AsParallelWriter(),
                deltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();

            new StaticJob
            {
                deltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct DynamicJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;
            public float                              deltaTime;

            public void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndexInQuery, ref FiredBlock block, in RigidBody rb)
            {
                if ((math.length(rb.velocity.linear) < 0.0001f && math.length(rb.velocity.angular) < 0.0001f) || block.lifetime > block.maxTimeBeforeHardFreeze)
                {
                    ecb.AddComponent<StaticEnvironmentTag>(chunkIndexInQuery, entity);
                    ecb.RemoveComponent<RigidBody>(chunkIndexInQuery, entity);
                    if (block.freezeSound.entity != Entity.Null)
                    {
                        var sound                                                      = ecb.Instantiate(chunkIndexInQuery, block.freezeSound);
                        ecb.AddComponent(chunkIndexInQuery, sound, new Parent { parent = entity });
                    }
                }
                block.lifetime += deltaTime;
            }
        }

        [WithAll(typeof(StaticEnvironmentTag))]
        [BurstCompile]
        partial struct StaticJob : IJobEntity
        {
            public float deltaTime;

            public void Execute(ref URPMaterialPropertyBaseColor color, ref FiredBlock block)
            {
                if (block.colorAnimationTime < block.colorAnimationDuration)
                {
                    color.Value = math.lerp(block.dynamicColor, block.staticColor, block.colorAnimationTime / block.colorAnimationDuration);
                }
                else
                    color.Value           = block.staticColor;
                block.colorAnimationTime += deltaTime;
            }
        }
    }
}

