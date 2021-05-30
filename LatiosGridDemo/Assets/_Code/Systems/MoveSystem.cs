using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class MoveSystem : SubSystem
    {
        EntityQuery m_query;

        protected override void OnUpdate()
        {
            var grid           = sceneBlackboardEntity.GetCollectionComponent<Grid>();
            var gridDimensions = grid.dimensions;

            int entityCount = m_query.CalculateEntityCountWithoutFiltering();

            // Remove entities which try to go out of bounds or have invalid movement
            Entities.ForEach((ref CharacterWantsToMove move, in CharacterActionDirection direction, in GridPosition position) =>
            {
                if (math.any(math.abs(direction.direction) > 1) || math.all(direction.direction == 0))
                {
                    move.wantsToMove = false;
                }

                if (math.any(position.position + direction.direction < 0) || math.any(position.position + direction.direction >= gridDimensions))
                {
                    move.wantsToMove = false;
                }
            }).WithStoreEntityQueryInField(ref m_query).ScheduleParallel();

            // Remove entities which try to occupy the same location
            var collisionMap         = new NativeMultiHashMap<int2, int>(entityCount, Allocator.TempJob);
            var collisionMapParallel = collisionMap.AsParallelWriter();

            Entities.ForEach((int entityInQueryIndex, ref CharacterWantsToMove move, in CharacterActionDirection direction, in GridPosition position) =>
            {
                if (move.wantsToMove)
                    collisionMapParallel.Add(direction.direction + position.position, entityInQueryIndex);
            }).ScheduleParallel();

            Entities.ForEach((ref CharacterWantsToMove move, in CharacterActionDirection direction, in GridPosition position) =>
            {
                if (collisionMap.CountValuesForKey(direction.direction + position.position) > 1)
                {
                    move.wantsToMove = false;
                }
            }).WithReadOnly(collisionMap).WithDisposeOnCompletion(collisionMap).ScheduleParallel();

            // Build follower chain
            var followedCdfe = GetComponentDataFromEntity<Followed>();

            Entities.ForEach((Entity entity, ref CharacterWantsToMove move, in CharacterActionDirection direction, in GridPosition position) =>
            {
                if (move.wantsToMove)
                {
                    var targetPosition = direction.direction + position.position;
                    var target         = grid[targetPosition.x, targetPosition.y];

                    if (target != Entity.Null)
                    {
                        followedCdfe[target] = new Followed { follower = entity };
                        move.wantsToMove                               = false;
                    }
                }
            }).WithNativeDisableParallelForRestriction(followedCdfe).WithReadOnly(grid).ScheduleParallel();

            // Move entities
            var gridParallel     = grid.AsParallelUnsafe();
            var gridPositionCdfe = GetComponentDataFromEntity<GridPosition>();
            Entities.ForEach((Entity entity, in CharacterWantsToMove move, in CharacterActionDirection direction) =>
            {
                if (move.wantsToMove)
                {
                    var position       = gridPositionCdfe[entity];
                    var targetPosition = direction.direction + position.position;
                    var target         = gridParallel[targetPosition.x, targetPosition.y];

                    if (target == Entity.Null)
                    {
                        gridPositionCdfe[entity]                         = new GridPosition { position = targetPosition };
                        gridParallel[targetPosition.x, targetPosition.y]                               = entity;
                        targetPosition                                                                 = position.position;
                        var followedEntity                                                             = entity;
                        while (followedCdfe[followedEntity].follower != Entity.Null)
                        {
                            followedEntity                                   = followedCdfe[followedEntity].follower;
                            var oldPosition                                  = gridPositionCdfe[followedEntity].position;
                            gridPositionCdfe[followedEntity]                 = new GridPosition { position = targetPosition };
                            gridParallel[targetPosition.x, targetPosition.y] = entity;
                            targetPosition                                   = oldPosition;
                        }
                        gridParallel[targetPosition.x, targetPosition.y] = Entity.Null;
                    }
                }
            }).WithNativeDisableParallelForRestriction(gridPositionCdfe).WithReadOnly(followedCdfe).ScheduleParallel();
        }
    }
}

