using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Post-Jam Notes:
// This is the character controller which handles movement and horizontal mouselook.
// This particular controller tries to hover above the ground with a spring.
// This code saw a fair amount of iteration and changes during the jam as a few things
// didn't feel right. It still isn't amazing, but it is good enough.

using static Unity.Entities.SystemAPI;

namespace BB
{
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct FirstPersonControllerSystem : ISystem
    {
        LatiosWorldUnmanaged           latiosWorld;
        EntityQuery                    m_otherBodiesQuery;
        BuildCollisionLayerTypeHandles m_handles;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld        = state.GetLatiosWorldUnmanaged();
            m_otherBodiesQuery = state.Fluent().With<RigidBody>(true).Without<FirstPersonControllerStats>().PatchQueryForBuildingCollisionLayer().Build();
            m_handles          = new BuildCollisionLayerTypeHandles(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_handles.Update(ref state);
            state.Dependency = Physics.BuildCollisionLayer(m_otherBodiesQuery, m_handles).ScheduleParallel(out var bodiesCollisionLayer,
                                                                                                           state.WorldUpdateAllocator,
                                                                                                           state.Dependency);
            new Job
            {
                rigidBodyLookup                 = GetComponentLookup<RigidBody>(true),
                previousTransformLookup         = GetComponentLookup<PreviousTransform>(true),
                staticEnvironmentCollisionLayer = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<StaticEnvironmentCollisionLayer>().layer,
                bodiesCollisionLayer            = bodiesCollisionLayer,
                deltaTime                       = Time.DeltaTime,
                soundIcb                        = latiosWorld.syncPoint.CreateInstantiateCommandBuffer<Parent>()
            }.Schedule();
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            [ReadOnly] public CollisionLayer                                            staticEnvironmentCollisionLayer;
            [ReadOnly] public CollisionLayer                                            bodiesCollisionLayer;
            [NativeDisableContainerSafetyRestriction] public ComponentLookup<RigidBody> rigidBodyLookup;
            [ReadOnly] public ComponentLookup<PreviousTransform>                        previousTransformLookup;

            public InstantiateCommandBuffer<Parent> soundIcb;

            public float deltaTime;

            public void Execute(Entity entity,
                                TransformAspect transform,
                                ref FirstPersonControllerState state,
                                in FirstPersonControllerStats stats,
                                in FirstPersonDesiredActions desiredActions)
            {
                bool   wasPreviouslyGrounded = state.isGrounded;
                float3 startingPosition      = transform.worldPosition;

                var initialGroundCheckDistance = stats.targetHoverHeight + math.select(stats.extraGroundCheckDistanceWhileInAir,
                                                                                       stats.extraGroundCheckDistanceWhileGrounded,
                                                                                       wasPreviouslyGrounded);
                var initialGroundCheckResult = CheckGround(startingPosition, initialGroundCheckDistance, in stats, state.accumulatedJumpTime);

                ref var velocity = ref state.velocity;

                velocity -= state.groundVelocity;

                // Todo: Does it feel better to apply rotation now or at the end?
                ApplyRotation(transform, in desiredActions, initialGroundCheckResult.groundFound);
                var velA = velocity;
                ApplyJump(desiredActions.jump, ref state, ref velocity, in stats, entity);
                var velB = velocity;
                if (state.accumulatedJumpTime > 0f)
                    initialGroundCheckResult.groundFound = false;
                ApplyGravity(initialGroundCheckResult.groundFound, ref state, ref velocity, in stats);
                var velC = velocity;
                ApplyMoveInput(desiredActions.move, transform.worldRotation, in initialGroundCheckResult, ref velocity, in stats);
                if (initialGroundCheckResult.groundFound)
                    velocity += initialGroundCheckResult.groundVelocity;
                var velD      = velocity;
                CollideAndSlide(startingPosition, ref velocity, in stats);
                var velE                    = velocity;
                var collideAndSlidePosition = startingPosition + velocity * deltaTime;

                var afterMoveGroundCheckDistance = stats.targetHoverHeight + math.select(stats.extraGroundCheckDistanceWhileInAir,
                                                                                         stats.extraGroundCheckDistanceWhileGrounded,
                                                                                         initialGroundCheckResult.groundFound);
                var afterMoveGroundCheckResult = CheckGround(collideAndSlidePosition, afterMoveGroundCheckDistance, in stats, state.accumulatedJumpTime);
                if (afterMoveGroundCheckResult.groundFound && state.accumulatedJumpTime == 0f)
                {
                    //if (afterMoveGroundCheckResult.distance < stats.targetHoverHeight)
                    {
                        var targetY = collideAndSlidePosition.y - afterMoveGroundCheckResult.distance + stats.targetHoverHeight;
                        ApplySpring(ref velocity, collideAndSlidePosition, targetY, in stats);
                    }
                    state.groundVelocity = afterMoveGroundCheckResult.groundVelocity;
                }
                else
                    state.groundVelocity = float3.zero;
                var velF                 = velocity;
                if (!math.all(math.isfinite(velF)))
                    UnityEngine.Debug.Log($"velocity broke: a: {velA}, b: {velB}, c: {velC}, d: {velD}, e: {velE}, f: {velF}");
                state.isGrounded        = afterMoveGroundCheckResult.groundFound;
                transform.worldPosition = startingPosition + velocity * deltaTime;

                if (!wasPreviouslyGrounded && state.isGrounded && stats.landSound.entity != Entity.Null)
                {
                    soundIcb.Add(stats.landSound, new Parent { parent = entity });
                }
            }

            void ApplyRotation(TransformAspect transform, in FirstPersonDesiredActions desiredActions, bool grounded)
            {
                var deltaX              = desiredActions.lookDirectionFromForward.x;
                var deltaForward        = new float3(deltaX, 0f, math.sqrt(1f - deltaX * deltaX));
                var deltaRotation       = quaternion.LookRotation(deltaForward, math.up());
                transform.localRotation = math.mul(deltaRotation, transform.localRotation);
            }

            struct CheckGroundResult
            {
                public float3 normal;
                public float3 groundVelocity;
                public float  distance;
                public bool   groundFound;
            }

            unsafe CheckGroundResult CheckGround(float3 start, float checkDistance, in FirstPersonControllerStats stats, float accumulatedJumpTime)
            {
                if (accumulatedJumpTime > 0f)
                    return default;

                start.y                 += stats.capsuleRadius + stats.skinWidth;
                checkDistance           += stats.skinWidth;
                Collider sphere          = new SphereCollider(float3.zero, stats.capsuleRadius);
                var      startTransform  = new TransformQvvs(start, quaternion.identity);
                var      end             = start;
                end.y                   -= checkDistance;
                var staticHit            =
                    Physics.ColliderCast(in sphere, in startTransform, end, in staticEnvironmentCollisionLayer, out var staticResult, out var staticInfo);
                var dynamicHit = Physics.ColliderCast(in sphere, in startTransform, end, in bodiesCollisionLayer, out var dynamicResult, out var dynamicInfo);
                if (staticHit || dynamicHit)
                {
                    var  result          = staticResult;
                    var  info            = staticInfo;
                    var  firstLayer      = staticEnvironmentCollisionLayer;
                    var  secondLayer     = bodiesCollisionLayer;
                    bool firstIsDynamic  = false;
                    bool secondIsDynamic = true;
                    if (!staticHit && dynamicHit)
                    {
                        result         = dynamicResult;
                        info           = dynamicInfo;
                        firstLayer     = bodiesCollisionLayer;
                        firstIsDynamic = true;
                    }
                    if (staticHit && dynamicHit && dynamicResult.distance < staticResult.distance)
                    {
                        result          = dynamicResult;
                        info            = dynamicInfo;
                        firstLayer      = bodiesCollisionLayer;
                        secondLayer     = staticEnvironmentCollisionLayer;
                        firstIsDynamic  = true;
                        secondIsDynamic = false;
                    }

                    startTransform.position.y     -= result.distance;
                    var accumulatedNormal          = -result.normalOnCaster;
                    var accumulatedGroundVelocity  = float3.zero;
                    int hitCount                   = 0;
                    var processor                  = new CheckGroundProcessor
                    {
                        bodyLookup                = rigidBodyLookup,
                        previousTransformLookup   = previousTransformLookup,
                        deltaTime                 = deltaTime,
                        testSphere                = sphere,
                        testSphereTransform       = startTransform,
                        accumulatedNormal         = &accumulatedNormal,
                        accumulatedGroundVelocity = &accumulatedGroundVelocity,
                        hitCount                  = &hitCount,
                        skinWidth                 = stats.skinWidth,
                        isDynamic                 = firstIsDynamic,
                        hitColliderBody           = info.bodyIndex,
                        hitSubcollider            = result.subColliderIndexOnTarget
                    };
                    var aabb  = Physics.AabbFrom(sphere, startTransform);
                    aabb.min -= stats.skinWidth;
                    aabb.max += stats.skinWidth;
                    Physics.FindObjects(aabb, in firstLayer, processor).RunImmediate();
                    if (staticHit && dynamicHit)
                    {
                        processor.isDynamic       = secondIsDynamic;
                        processor.hitColliderBody = -1;
                        Physics.FindObjects(aabb, in secondLayer, processor).RunImmediate();
                    }
                    var finalNormal = math.normalizesafe(accumulatedNormal, math.down());

                    return new CheckGroundResult
                    {
                        distance       = result.distance,
                        normal         = finalNormal,
                        groundFound    = finalNormal.y >= stats.minSlopeY,
                        groundVelocity = accumulatedGroundVelocity / math.max(hitCount, 1)
                    };
                }
                return default;
            }

            unsafe struct CheckGroundProcessor : IDistanceBetweenAllProcessor, IFindObjectsProcessor
            {
                public ComponentLookup<RigidBody>         bodyLookup;
                public ComponentLookup<PreviousTransform> previousTransformLookup;
                public Collider                           testSphere;
                public TransformQvvs                      testSphereTransform;
                public float3*                            accumulatedNormal;
                public float3*                            accumulatedGroundVelocity;
                public int*                               hitCount;
                public float                              skinWidth;
                public float                              deltaTime;
                public bool                               isDynamic;
                public int                                hitColliderBody;
                public int                                hitSubcollider;

                Entity        currentEntity;
                TransformQvvs currentTransform;
                bool          checkSubcollider;

                public void Execute(in ColliderDistanceResult result)
                {
                    if (checkSubcollider && hitSubcollider != result.subColliderIndexB)
                        *accumulatedNormal -= result.normalA;
                    if (isDynamic)
                    {
                        ref readonly var body               = ref bodyLookup.GetRefRO(currentEntity).ValueRO;
                        var              vel                = VelocityAtPointFrom(result.hitpointB, body.inertialPoseWorldTransform, body.velocity);
                        var              previousTransform  = previousTransformLookup[currentEntity].worldTransform;
                        previousTransform.position          = currentTransform.position;
                        previousTransform.rotation          = currentTransform.rotation;
                        var shiftedPoint                    = qvvs.TransformPoint(currentTransform, qvvs.InverseTransformPoint(previousTransform, result.hitpointB));
                        *accumulatedGroundVelocity         += vel + (shiftedPoint - result.hitpointB) / deltaTime;
                        *hitCount                          += 1;
                    }
                }

                public void Execute(in FindObjectsResult result)
                {
                    currentEntity    = result.entity;
                    currentTransform = result.transform;
                    checkSubcollider = result.bodyIndex == hitColliderBody;
                    Physics.DistanceBetweenAll(in testSphere, in testSphereTransform, result.collider, result.transform, skinWidth, ref this);
                }
            }

            void ApplySpring(ref float3 velocity, float3 currentPosition, float targetY, in FirstPersonControllerStats stats)
            {
                UnitySim.ConstraintTauAndDampingFrom(stats.springFrequency, stats.springDampingRatio, deltaTime, 1, out var tau, out var damping);
                var inertialA = new RigidTransform(quaternion.identity, currentPosition);
                var inertialB = new RigidTransform(quaternion.identity, new float3(currentPosition.x, targetY, currentPosition.z));
                UnitySim.BuildJacobian(out UnitySim.PositionConstraintJacobianParameters parameters, inertialA, float3.zero,
                                       inertialB, RigidTransform.identity, 0f, 0f, tau, damping, new bool3(false, true, false));
                var               simVelocity   = new UnitySim.Velocity { linear = velocity, angular = float3.zero };
                UnitySim.Velocity dummyVelocity                                                      = default;
                // The inverse mass will cancel itself out when the impulse is applied to the velocity. So we just pass 1f here.
                UnitySim.SolveJacobian(ref simVelocity, inertialA, new UnitySim.Mass { inverseMass = 1f, inverseInertia = float3.zero },
                                       ref dummyVelocity, inertialB, default,
                                       in parameters, deltaTime, 1f / deltaTime);
                velocity = simVelocity.linear;
            }

            void ApplyJump(bool jump, ref FirstPersonControllerState state, ref float3 velocity, in FirstPersonControllerStats stats, Entity thisEntity)
            {
                if (state.isGrounded)
                    state.accumulatedCoyoteTime  = 0f;
                state.accumulatedCoyoteTime     += deltaTime;
                if (jump && state.accumulatedCoyoteTime <= stats.coyoteTime && state.accumulatedJumpTime == 0f)
                {
                    velocity.y                += stats.jumpVelocity;
                    state.accumulatedJumpTime  = math.EPSILON;
                    if (stats.jumpSound.entity != Entity.Null)
                        soundIcb.Add(stats.jumpSound, new Parent { parent = thisEntity });
                }
                else if (state.accumulatedJumpTime > 0f)
                    state.accumulatedJumpTime += deltaTime;
                if (state.accumulatedJumpTime > stats.jumpInitialMaxTime || (!jump && state.accumulatedJumpTime >= stats.jumpInitialMinTime))
                    state.accumulatedJumpTime  = 0f;
                state.isGrounded              &= state.accumulatedJumpTime > 0f;
            }

            void ApplyGravity(bool isGrounded, ref FirstPersonControllerState state, ref float3 velocity, in FirstPersonControllerStats stats)
            {
                if (!isGrounded)
                {
                    var gravity = stats.fallGravity;
                    if (state.accumulatedJumpTime >= stats.jumpInitialMaxTime)
                        gravity = stats.jumpGravity;
                    else if (state.accumulatedJumpTime > 0f)
                        gravity  = stats.jumpInitialGravity;
                    velocity.y  -= gravity * deltaTime;
                }
                velocity.y = math.max(velocity.y, -stats.maxFallSpeed);
            }

            void ApplyMoveInput(float2 move, quaternion rotation, in CheckGroundResult checkGroundResult, ref float3 velocity, in FirstPersonControllerStats stats)
            {
                if (!checkGroundResult.groundFound)
                {
                    var forwardVelocity = math.dot(math.forward(rotation), velocity);
                    var rightVelocity   = math.dot(math.rotate(rotation, math.right()), velocity);
                    var newVelocities   = Physics.StepVelocityWithInput(move.yx,
                                                                        new float2(forwardVelocity, rightVelocity),
                                                                        new float2(stats.airStats.forwardAcceleration, stats.airStats.strafeAcceleration),
                                                                        new float2(stats.airStats.forwardDeceleration, stats.airStats.strafeDeceleration),
                                                                        new float2(stats.airStats.forwardTopSpeed,     stats.airStats.strafeTopSpeed),
                                                                        new float2(stats.airStats.reverseAcceleration, stats.airStats.strafeAcceleration),
                                                                        new float2(stats.airStats.reverseDeceleration, stats.airStats.strafeDeceleration),
                                                                        new float2(stats.airStats.reverseTopSpeed,     stats.airStats.strafeTopSpeed),
                                                                        deltaTime);
                    velocity.zx = math.rotate(rotation, newVelocities.yx.x0y()).zx;
                }
                else
                {
                    var forward                  = math.mul(quaternion.LookRotation(-checkGroundResult.normal, math.forward(rotation)), math.up());
                    var right                    = math.mul(quaternion.LookRotation(-checkGroundResult.normal, math.rotate(rotation, math.right())), math.up());
                    var slopeCompensatedRotation = quaternion.LookRotation(forward, math.cross(forward, right));
                    var slopeHeadingVelocity     = math.InverseRotateFast(slopeCompensatedRotation, velocity);
                    if (math.length(velocity.xz) < math.EPSILON)
                        slopeHeadingVelocity.xz = 0f; // This prevents getting stuck when the spring is driving the velocity on a slope.
                    slopeHeadingVelocity.zx     = Physics.StepVelocityWithInput(move.yx,
                                                                                slopeHeadingVelocity.zx,
                                                                                new float2(stats.walkStats.forwardAcceleration, stats.walkStats.strafeAcceleration),
                                                                                new float2(stats.walkStats.forwardDeceleration, stats.walkStats.strafeDeceleration),
                                                                                new float2(stats.walkStats.forwardTopSpeed,     stats.walkStats.strafeTopSpeed),
                                                                                new float2(stats.walkStats.reverseAcceleration, stats.walkStats.strafeAcceleration),
                                                                                new float2(stats.walkStats.reverseDeceleration, stats.walkStats.strafeDeceleration),
                                                                                new float2(stats.walkStats.reverseTopSpeed,     stats.walkStats.strafeTopSpeed),
                                                                                deltaTime);
                    velocity = math.rotate(slopeCompensatedRotation, slopeHeadingVelocity);
                }
            }

            void CollideAndSlide(float3 startPosition, ref float3 velocity, in FirstPersonControllerStats stats)
            {
                var      moveVector        = velocity * deltaTime;
                var      distanceRemaining = math.length(moveVector);
                var      currentTransform  = new TransformQvvs(startPosition, quaternion.identity);
                var      moveDirection     = math.normalize(moveVector);
                Collider collider          = new CapsuleCollider(new float3(0f, stats.capsuleRadius, 0f),
                                                                 new float3(0f, stats.capsuleHeight - stats.capsuleRadius, 0f),
                                                                 stats.capsuleRadius);

                for (int iteration = 0; iteration < 32; iteration++)
                {
                    if (distanceRemaining < math.EPSILON)
                        break;
                    var end = currentTransform.position + moveDirection * distanceRemaining;
                    if (Physics.ColliderCast(in collider, in currentTransform, end, in staticEnvironmentCollisionLayer, out var hitInfo, out _))
                    {
                        currentTransform.position += moveDirection * (hitInfo.distance - stats.skinWidth);
                        distanceRemaining         -= hitInfo.distance;
                        if (math.dot(hitInfo.normalOnTarget, moveDirection) < -0.9f) // If the obstacle directly opposes our movement
                            break;
                        // LookRotation corrects an "up" vector to be perpendicular to the "forward" vector. We cheat this to get a new moveDirection perpendicular to the normal.
                        moveDirection = math.mul(quaternion.LookRotation(hitInfo.normalOnCaster, moveDirection), math.up());
                    }
                    else
                    {
                        currentTransform.position += moveDirection * distanceRemaining;
                        distanceRemaining          = 0f;
                    }
                }
                velocity = (currentTransform.position - startPosition) / deltaTime;
                if (math.length(currentTransform.position - startPosition) < math.EPSILON)
                    velocity = float3.zero;
            }

            // Rotation logic needs to be vetted before this can be added to UnitySim proper.
            public static float3 VelocityAtPointFrom(float3 point, in RigidTransform inertialPoseWorldTransform, in UnitySim.Velocity velocity)
            {
                var pointLocal = math.transform(math.inverse(inertialPoseWorldTransform), point);
                return velocity.linear + math.rotate(inertialPoseWorldTransform.rot, math.cross(velocity.angular, pointLocal));
            }
        }
    }
}

