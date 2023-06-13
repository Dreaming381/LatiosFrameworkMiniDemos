using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

// This sample is based on Matthias Muller's Ten Minute Physics series.
// https://matthias-research.github.io/pages/tenMinutePhysics/index.html
//
// It recreates Part 3 of the series, while introducing substepping and iteration controls
// from Part 5. While this is not an PBD simulation, substepping and iterations can still improve
// the quality of the simulation.
//
// The implementation in this system uses a brute-force search for collisions on the main thread.
// It demonstrates the basic concepts of the Latios Framework and Psyshock queries.
// Excluding these explanation comments, the full simulation is less than 100 lines of code.

namespace Dragons.PsyshockSamples.CollisionSimple
{
    [BurstCompile]
    public partial struct BruteForceCollisionSystem : ISystem, ISystemShouldUpdate
    {
        LatiosWorldUnmanaged latiosWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
        }

        public bool ShouldUpdateSystem(ref SystemState state)
        {
            return !latiosWorld.sceneBlackboardEntity.GetComponentData<Settings>().useFindPairs;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var settings = latiosWorld.sceneBlackboardEntity.GetComponentData<Settings>();

            // Physics.Substep subdivides our delta time in the form of an enumerator which enumerates each subdivision.
            foreach (var substep in Physics.Substep(Time.DeltaTime, settings.substeps))
            {
                // This is the "integrator" or "update" step. If we had gravity, we would update it here too.
                foreach ((var transform, var vel) in Query<TransformAspect, CharacterPhysicsState>())
                {
                    transform.TranslateWorld(new float3(vel.velocity, 0f) * substep);
                }

                for (int iteration = 0; iteration < settings.iterations; iteration++)
                {
                    // Because we are doing chunk iteration rather than array iteration, we have to track indices manually.
                    // This is the "i" loop.
                    int indexA = 0;
                    foreach ((var transformA, var velA, var massA, var colliderA) in
                             Query<TransformAspect, RefRW<CharacterPhysicsState>, RefRO<CharacterPhysicsStats>, RefRO<Collider> >())
                    {
                        // This is the "j" loop.
                        int indexB = 0;
                        foreach ((var transformB, var velB, var massB, var colliderB) in
                                 Query<TransformAspect, RefRW<CharacterPhysicsState>, RefRO<CharacterPhysicsStats>, RefRO<Collider> >())
                        {
                            // This emulates the "int j = i + 1" step of array iteration.
                            if (indexB++ <= indexA)
                                continue;

                            // This method queries if the objects are within a specified distance of each other. If they are overlapping, the distance will be negative.
                            // We pass in 0f to request either touches or overlaps to return as "true". If this method returns true, it also provides details about the
                            // query from the final out argument.
                            if (!Physics.DistanceBetween(in colliderA.ValueRO, transformA.worldTransform, in colliderB.ValueRO, transformB.worldTransform, 0f, out var hit))
                                continue;

                            // We have a hit. Separate the colliders so that they aren't overlapping.
                            transformA.TranslateWorld(new float3((-hit.distance / 2f * -hit.normalA).xy, 0f));
                            transformB.TranslateWorld(new float3((-hit.distance / 2f * -hit.normalB).xy, 0f));

                            // If they were just touching, don't update velocities. This only matters when there are multiple iterations.
                            // In that case, not having this check would lead to velocity updates being evaluated multiple times for the same colliding pair.
                            // If we only have substeps, this check is not necessary.
                            if (hit.distance > -math.EPSILON)
                                continue;

                            // Now it is time to update the velocities.

                            // hit.normalA.xy represents the direction vector from A to B.
                            // This only works because A and B are sphere colliders.
                            var va = math.dot(velA.ValueRO.velocity, hit.normalA.xy);
                            var vb = math.dot(velB.ValueRO.velocity, hit.normalA.xy);

                            var sharedNumerator   = massA.ValueRO.mass * va + massB.ValueRO.mass * vb;
                            var sharedDenomenator = massA.ValueRO.mass + massB.ValueRO.mass;

                            var numeratorA = massB.ValueRO.mass * (va - vb) * settings.elasticity;
                            var numeratorB = massA.ValueRO.mass * (vb - va) * settings.elasticity;

                            velA.ValueRW.velocity += hit.normalA.xy * ( -va + (sharedNumerator - numeratorA) / sharedDenomenator);
                            velB.ValueRW.velocity += hit.normalA.xy * ( -vb + (sharedNumerator - numeratorB) / sharedDenomenator);
                        }
                        indexA++;

                        // Handle walls
                        // Unlike in the Ten Minute Physics sample, walls here are their own collider types that we check for overlap with.
                        // When a sphere collides with it, the wall's hit normal tells us the direction to move the sphere as well as the
                        // vector to reflect the velocity from. As an experiment, add more wall colliders to the scene using sphere, capsule,
                        // and box colliders with unique orientations to see just how expressive this logic actually is.
                        // Note: You may need to normalize wallHit.normalB to account for Z drift if your colliders aren't placed and oriented
                        // within the confines of 2D space.
                        foreach ((var wallCollider, var wallTransform) in Query<RefRO<Collider>, RefRO<WorldTransform> >().WithAll<WallTag>())
                        {
                            if (!Physics.DistanceBetween(in colliderA.ValueRO, transformA.worldTransform, in wallCollider.ValueRO, in wallTransform.ValueRO.worldTransform, 0f,
                                                         out var wallHit))
                                continue;

                            transformA.TranslateWorld(new float3((-wallHit.distance * wallHit.normalB).xy, 0f));
                            if (math.dot(velA.ValueRO.velocity, wallHit.normalB.xy) >= 0f)
                                continue; // Object is already trying to move away from the wall.
                            velA.ValueRW.velocity = math.reflect(velA.ValueRO.velocity, wallHit.normalB.xy);
                        }
                    }
                }
            }
        }
    }
}

