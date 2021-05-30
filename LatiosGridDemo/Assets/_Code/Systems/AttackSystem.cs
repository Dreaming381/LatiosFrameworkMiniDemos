using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class AttackSystem : SubSystem
    {
        protected override void OnUpdate()
        {
            var grid = sceneBlackboardEntity.GetCollectionComponent<Grid>(false);

            var dcb         = new DestroyCommandBuffer(Allocator.TempJob);
            var dcbParallel = dcb.AsParallelWriter();

            var killQueue         = new NativeQueue<int2>(Allocator.TempJob);
            var killQueueParallel = killQueue.AsParallelWriter();

            Entities.ForEach((int entityInQueryIndex, in GridPosition position, in CharacterWantsToAttack attack, in CharacterActionDirection direction) =>
            {
                if (attack.wantsToAttack)
                {
                    var targetPosition = position.position + direction.direction;
                    var target         = grid[targetPosition.x, targetPosition.y];
                    if (target != Entity.Null)
                    {
                        dcbParallel.Add(target, entityInQueryIndex);
                        killQueueParallel.Enqueue(targetPosition);
                    }
                }
            }).WithReadOnly(grid).ScheduleParallel();

            // This is a hard sync point. But in this case we're ok with it because our jobs are parallel and this game isn't framerate sensitive.
            // Having an up-to-date view for the next action-processing system is much more useful.
            CompleteDependency();
            dcb.Playback(EntityManager);
            Dependency = dcb.Dispose(default);

            Job.WithCode(() =>
            {
                while (killQueue.TryDequeue(out var position))
                {
                    grid[position.x, position.y] = Entity.Null;
                }
            }).WithDisposeOnCompletion(killQueue).Schedule();
        }
    }
}

