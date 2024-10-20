using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

// This sample is based on Matthias Muller's Ten Minute Physics series.
// https://matthias-research.github.io/pages/tenMinutePhysics/index.html
//
// It recreates Part 3 of the series, while introducing substepping and iteration controls
// from Part 5. While this is not an PBD simulation, substepping and iterations can still improve
// the quality of the simulation.
//
// The implementation in this system uses Psyshock FindPairs and parallel jobs to speed up
// collision checks compared to the brute force method. It is recommended you review
// BruteForceCollisionSystem.cs first to understand the fundamental pieces of the simulation
// before exploring how it is optimized in this implementation.
//
// The big difference here is that instead of checking every sphere collider against every other
// sphere collider, this method uses FindPairs, which is a "broadphase" algorithm that can rule
// out pairs of spheres that are far away from each other in less than O(n^2) time. Whenever it
// finds a pair, it reports it to a "Processor" immediately, and the Processor can interact with
// the components of both entities safely, even when FindPairs is scheduled in parallel mode.
//
// FindPairs operates on the CollisionLayer type, which is an immutable collection of colliders.
// However, because it is immutable, the result of moving a collider inside the processor would
// not be recognized by the FindPairs algorithm. To help account for this, we build our CollisionLayer
// using custom AABBs for our spheres where the AABBs are doubled in size. This means that any sphere
// can be moved by its full radius inside a Processor before FindPairs stops finding collisions for it.
// In cases where a sphere does need to move a lot, using FindPairs may require more substeps or iterations
// in order to meet the quality of the brute-force method. However, the performance advantage of FindPairs
// more than compensates for this detail.
//
// Excluding these explanation comments, the full simulation is 200 lines of code.

namespace Dragons.PsyshockSamples.CollisionSimple
{
    [BurstCompile]
    public partial struct FindPairsCollisionSystem : ISystem, ISystemShouldUpdate, ISystemNewScene
    {
        LatiosWorldUnmanaged latiosWorld;
        EntityQuery          m_wallQuery;
        EntityQuery          m_characterQuery;

        // Building a CollisionLayer using an EntityQuery requires type handles, which are wrapped in this struct.
        BuildCollisionLayerTypeHandles m_handles;
        // PhysicsTransformAspectLookup lets us read and write to TransformAspect in a Processor inside FindPairs
        // scheduled in parallel mode.
        PhysicsTransformAspectLookup m_transformLookup;
        // CollisionLayerSettings customize the how colliders are grouped spatially and can be optimized for a specific world.
        // It is optional, but when it is present, all CollisionLayers must be built with the same settings.
        CollisionLayerSettings m_settings;
        // We keep track of when we need to rebuild the wall CollisionLayer, which is upon a new scene.
        bool m_requiresWallLayerRebuild;

        // CollisionLayers are collection types, and so we store it in a collection component so that it is automatically disposed.
        partial struct WallCollisionLayer : ICollectionComponent
        {
            public CollisionLayer layer;
            public JobHandle TryDispose(JobHandle inputDeps) => layer.IsCreated ? layer.Dispose(inputDeps) : inputDeps;
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld       = state.GetLatiosWorldUnmanaged();
            m_wallQuery       = state.Fluent().With<WallTag>(true).PatchQueryForBuildingCollisionLayer().Build();
            m_characterQuery  = state.Fluent().With<CharacterPhysicsState>(false).With<CharacterPhysicsStats>(true).PatchQueryForBuildingCollisionLayer().Build();
            m_handles         = new BuildCollisionLayerTypeHandles(ref state);
            m_transformLookup = new PhysicsTransformAspectLookup(ref state);

            // Here, we divide up the world into cells. One for positive y space, and one for negative y space.
            // Any collider that intersects this y=0 plane will be added to a third cell.
            // Cells are processed in two phases when scheduled in parallel, and the first phase will use as many
            // threads as there are cells. In this case, there will be three threads used.
            // The second phase handles special-case checks (here, it would be testing the first two cells against
            // the third) and will use either one or two thread depending on if one or two CollisionLayers are being
            // tested respectfully.
            // Typically, having more cell subdivisions improves parallelism and efficiency. However, if two many
            // colliders lie on cell boundaries, performance will deteriorate. It is for this reason that only a
            // single plane subdivision along the y-axis was used. In addition, subdividing the x-axis offers less
            // performance benefit than subdividing the other axes.
            m_settings = new CollisionLayerSettings
            {
                // The outer boundary cells (all of them in this case) extend off towards infinity,
                // so a smaller bounds than the actual simulation is fine. This setting only really
                // matters when the subdivisions for a given axis is greater than 2.
                worldAabb                = new Aabb(-1f, 1f),
                worldSubdivisionsPerAxis = new int3(1, 2, 1)
            };

            m_requiresWallLayerRebuild = true;
        }

        public bool ShouldUpdateSystem(ref SystemState state)
        {
            return latiosWorld.sceneBlackboardEntity.GetComponentData<Settings>().useFindPairs;
        }

        public void OnNewScene(ref SystemState state)
        {
            // On a new scene, specify we need to rebuild the wall CollisionLayer.
            // Also, we add an empty layer to the sceneBlackboardEntity to avoid a sync point later in OnUpdate().
            m_requiresWallLayerRebuild = true;
            latiosWorld.sceneBlackboardEntity.AddOrSetCollectionComponentAndDisposeOld<WallCollisionLayer>(default);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var settings = latiosWorld.sceneBlackboardEntity.GetComponentData<Settings>();

            // The walls are static in the scene. If we haven't built a CollisionLayer for them yet, we do so here.
            if (m_requiresWallLayerRebuild)
            {
                m_handles.Update(ref state);
                // We can build a CollisionLayer using an EntityQuery. Normally, this is how most CollisionLayers are built, especially
                // for colliders that have "trigger" behavior.
                state.Dependency = Physics.BuildCollisionLayer(m_wallQuery, in m_handles).WithSettings(m_settings)
                                   .ScheduleParallel(out var newWallLayer, Allocator.Persistent, state.Dependency);
                latiosWorld.sceneBlackboardEntity.SetCollectionComponentAndDisposeOld(new WallCollisionLayer { layer = newWallLayer });
            }

            m_transformLookup.Update(ref state);
            var wallLayer = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<WallCollisionLayer>(true).layer;

            // For our spheres, we'll be constructing the CollisionLayer with custom data. This version requires a ColliderBody per collider
            // and optionally an override Aabb. We are using this mode specifically to utilize the override Aabbs.
            var count  = m_characterQuery.CalculateEntityCount();
            var bodies = CollectionHelper.CreateNativeArray<ColliderBody>(count, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
            var aabbs  = CollectionHelper.CreateNativeArray<Aabb>(count, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);

            // These are Processors for the FindPairs algorithm.
            var characterCharacterProcessor = new CharacterCharacterProcessor
            {
                transformLookup = m_transformLookup,
                // ComponentLookup implicitly casts to PhysicsComponentLookup.
                // PhysicsComponentLookup will be explained in the Processor implementation.
                stateLookup = GetComponentLookup<CharacterPhysicsState>(false),
                statsLookup = GetComponentLookup<CharacterPhysicsStats>(true),
                elasticity  = settings.elasticity
            };

            var characterWallProcessor = new CharacterWallProcessor
            {
                transformLookup = m_transformLookup,
                stateLookup     = GetComponentLookup<CharacterPhysicsState>(false),
                elasticity      = settings.elasticity
            };

            foreach (var substep in Physics.Substep(Time.DeltaTime, settings.substeps))
            {
                for (int iteration = 0; iteration < settings.iterations; iteration++)
                {
                    // This is our "Integrator" job. To avoid re-reading data, we combine it with the setup of data for CollisionLayer.
                    // Because of this, we need to have this job inside the iterations loop. So only if this is the first iteration do
                    // we actually want to do integration. The other iterations we nullify integrations by setting the substep to 0f.
                    state.Dependency = new UpdateJob
                    {
                        bodies  = bodies,
                        aabbs   = aabbs,
                        substep = math.select(0f, substep, iteration == 0)
                    }.ScheduleParallel(m_characterQuery, state.Dependency);

                    // This is the other version of BuildCollisionLayer which uses NativeArrays for building the layer.
                    // Note that at this moment, the data in the NativeArrays are still in a pending job. This is fine
                    // as long as we provide the correct JobHandle for it.
                    // Also, we are building a temporary CollisionLayer for every iteration of every substep.
                    // While it is often a good idea to cache CollisionLayers between systems to avoid rebuilding them,
                    // in this case because the objects are moving between iterations, we have to rebuild the CollisionLayer
                    // every time. BuildCollisionLayer is an O(n) operation and it is still a good idea to rebuild it frequently
                    // to take advantage of FindPairs.
                    state.Dependency = Physics.BuildCollisionLayer(bodies, aabbs).WithSettings(m_settings)
                                       .ScheduleParallel(out var layer, state.WorldUpdateAllocator, state.Dependency);
                    // When testing spheres against each other, we invoke FindPairs with only a single CollisionLayer. We also pass in our Processor instance.
                    state.Dependency = Physics.FindPairs(in layer, in characterCharacterProcessor).ScheduleParallel(state.Dependency);
                    // When we want to test for collisions between layers, we use the two-layer version of FindPairs. Only pairs that have a collider from each
                    // layer are reported. And the "A" object in a pair is always from the first layer passed into FindPairs.
                    state.Dependency = Physics.FindPairs(in layer, in wallLayer, in characterWallProcessor).ScheduleParallel(state.Dependency);
                }
            }
        }

        [BurstCompile]
        partial struct UpdateJob : IJobEntity
        {
            public NativeArray<ColliderBody> bodies;
            public NativeArray<Aabb>         aabbs;
            public float                     substep;

            public void Execute([EntityIndexInQuery] int entityIndexInQuery,
                                Entity entity,
                                TransformAspect transform,
                                ref CharacterPhysicsState state,
                                in Collider collider)
            {
                // This is the "Integrator" part of the update.
                transform.TranslateWorld(substep * new float3(state.velocity, 0f));

                // Here we build the ColliderBody using the Collider, WorldTransform, and Entity.
                // This is equivalent to what the EntityQuery version of BuildCollisionLayer would do.
                var body = new ColliderBody
                {
                    collider  = collider,
                    transform = transform.worldTransform,
                    entity    = entity
                };
                bodies[entityIndexInQuery] = body;

                // We can get the World-Space Aabb around any Collider with this method.
                // This is what BuildCollisionLayer would do if we weren't overriding Aabbs.
                var aabb = Physics.AabbFrom(in body.collider, in body.transform);
                // Now we double the size of the Aabb for our override.
                var half                   = (aabb.max - aabb.min) / 2f;
                aabb.min                  -= half;
                aabb.max                  += half;
                aabbs[entityIndexInQuery]  = aabb;
            }
        }

        // This is a Processor. It implements the IFindPairsProcessor interface.
        // Processors behave much like job structs, except that they don't require
        // the [BurstCompile] attribute. They always run in Burst.
        struct CharacterCharacterProcessor : IFindPairsProcessor
        {
            // "Physics Lookups" are special types of lookups that can safely write to components in pairs
            // produced by the FindPairs algorithm. To do this, they use a SafeEntity as an indexer instead
            // of a normal Entity. The SafeEntity can be implicitly casted to a normal Entity but not vice-versa.
            // The FindPairsResult provides SafeEntity instances that are safe for accessing the components.
            public PhysicsTransformAspectLookup                  transformLookup;
            public PhysicsComponentLookup<CharacterPhysicsState> stateLookup;
            // Components that are only read can use a normal Lookup with the [ReadOnly] attribute.
            [ReadOnly] public ComponentLookup<CharacterPhysicsStats> statsLookup;

            public float elasticity;

            // The Execute method gets invoked for every found pair, with the pair data contained in the passed in result.
            public void Execute(in FindPairsResult result)
            {
                // The result contains the transforms stored in the CollisionLayer, but we want the most up-to-date
                // transforms since we'll be modifying them inside this Processor.
                var transformA = transformLookup[result.entityA];
                var transformB = transformLookup[result.entityB];

                // FindPairs is only a broadphase, which means it only knows that the Aabbs overlap or are touching.
                // We still need to test if the colliders are actually overlapping.
                if (!Physics.DistanceBetween(result.colliderA, transformA.worldTransform, result.colliderB, transformB.worldTransform, 0f, out var hit))
                    return;

                // We have a hit. Separate the colliders so that they aren't overlapping.
                // Note how we are writing to both transforms here in parallel. This is guaranteed safe, and is also
                // guaranteed to be deterministic for a single CPU (that is, results will be the same every run).
                transformA.TranslateWorld(new float3((-hit.distance / 2f * -hit.normalA).xy, 0f));
                transformB.TranslateWorld(new float3((-hit.distance / 2f * -hit.normalB).xy, 0f));

                // If they were just touching, don't update velocities. This only matters when there are multiple iterations.
                // In that case, not having this check would lead to velocity updates being evaluated multiple times for the same colliding pair.
                // If we only have substeps, this check is not necessary.
                if (hit.distance > -math.EPSILON)
                    return;

                // We can get a RefRW from a PhysicsComponentLookup via GetRW() and passing in the safe entity.
                // For components we intend to both read and write, this is often faster than indexer lookups.
                ref var velA = ref stateLookup.GetRW(result.entityA).ValueRW.velocity;
                ref var velB = ref stateLookup.GetRW(result.entityB).ValueRW.velocity;

                var massA = statsLookup[result.entityA].mass;
                var massB = statsLookup[result.entityB].mass;

                var va = math.dot(velA, hit.normalA.xy);
                var vb = math.dot(velB, hit.normalA.xy);

                var sharedNumerator   = massA * va + massB * vb;
                var sharedDenomenator = massA + massB;

                var numeratorA = massB * (va - vb) * elasticity;
                var numeratorB = massA * (vb - va) * elasticity;

                velA += hit.normalA.xy * (-va + (sharedNumerator - numeratorA) / sharedDenomenator);
                velB += hit.normalA.xy * (-vb + (sharedNumerator - numeratorB) / sharedDenomenator);
            }
        }

        // This processor is designed to work with two different types of objects.
        // It aggressively assumes that the "A" in any pair will be a dynamic sphere,
        // whereas the "B" will be a static wall collider. This is guaranteed when
        // passing in the CollisionLayers into the FindPairs method in the same order.
        // There's no need to perform guesswork, and that has significant performance benefits.
        struct CharacterWallProcessor : IFindPairsProcessor
        {
            public PhysicsTransformAspectLookup                  transformLookup;
            public PhysicsComponentLookup<CharacterPhysicsState> stateLookup;

            public float elasticity;

            public void Execute(in FindPairsResult result)
            {
                var transformA = transformLookup[result.entityA];

                // For the wall colliders, we can read the transform directly from the CollisionLayer via FindPairsResult.
                // We assume the wall collider hasn't moved since the CollisionLayer containing it was built.
                if (!Physics.DistanceBetween(result.colliderA, transformA.worldTransform, result.colliderB, result.transformB, 0f, out var wallHit))
                    return;

                ref var velA = ref stateLookup.GetRW(result.entityA).ValueRW.velocity;

                transformA.TranslateWorld(new float3((-wallHit.distance * wallHit.normalB).xy, 0f));
                if (math.dot(velA, wallHit.normalB.xy) >= 0f)
                    return; // Object is already trying to move away from the wall.
                velA = math.reflect(velA, wallHit.normalB.xy);
            }
        }
    }
}

