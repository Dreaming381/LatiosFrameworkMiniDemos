using Latios;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class LocomotionSystem : SubSystem
    {
        EntityQuery m_query;

        protected override void OnUpdate()
        {
            var count         = m_query.CalculateEntityCountWithoutFiltering();
            var capsuleBodies = new NativeArray<ColliderBody>(count, Allocator.TempJob);
            var aabbs         = new NativeArray<Aabb>(count, Allocator.TempJob);
            Entities.WithStoreEntityQueryInField(ref m_query).ForEach((Entity entity, int entityInQueryIndex, ref MoveState state, in MoveStats stats,
                                                                       in Translation trans, in Rotation rot) =>
            {
                var transform = new RigidTransform(rot.Value, trans.Value);
                var capsule   =
                    new CapsuleCollider(new float3(0f, -stats.groundCheckDistance + stats.radius, 0f), new float3(0f, state.height - stats.radius, 0f), stats.radius);
                capsuleBodies[entityInQueryIndex] = new ColliderBody
                {
                    transform = transform,
                    collider  = capsule,
                    entity    = entity
                };

                state.isGrounded                      = false;
                state.distanceGroundIsWithinThreshold = float.MaxValue;
            }).ScheduleParallel();

            Dependency = Physics.BuildCollisionLayer(capsuleBodies).ScheduleParallel(out var groundCheckLayer, Allocator.TempJob, Dependency);

            var environmentLayer = sceneBlackboardEntity.GetCollectionComponent<EnvironmentCollisionLayer>(true).layer;

            var checkGroundProcessor = new CheckGroundProcessor
            {
                stateCdfe = GetComponentDataFromEntity<MoveState>(),
                statsCdfe = GetComponentDataFromEntity<MoveStats>()
            };

            Dependency = Physics.FindPairs(groundCheckLayer, environmentLayer, checkGroundProcessor).ScheduleParallel(Dependency);

            var dt = Time.DeltaTime;
            Entities.ForEach((int entityInQueryIndex, ref MoveState state, in MoveStats stats, in Translation trans, in Rotation rot, in DesiredActions actions) =>
            {
                bool crouch = actions.crouch;
                if (!state.isGrounded)
                {
                    state.verticalSpeed += stats.gravity * dt;
                }
                if (state.isGrounded && actions.jump)
                {
                    crouch               = false;
                    state.verticalSpeed += stats.jumpSpeed;
                    state.isGrounded     = false;
                }

                state.height += math.select(stats.crouchUpSpeed, stats.crouchDownSpeed, crouch) * dt;
                state.height  = math.clamp(state.height, stats.crouchHeight, stats.normalHeight);
                float factor  = math.lerp(stats.crouchTopSpeedAttenuation, 1f, math.unlerp(stats.crouchHeight, stats.normalHeight, state.height));

                if (state.isGrounded)
                {
                    state.forwardReverseSpeed = Physics.StepVelocityWithInput(actions.move.y,
                                                                              state.forwardReverseSpeed,
                                                                              stats.groundForwardAcceleration,
                                                                              stats.groundForwardDeceleration,
                                                                              stats.groundForwardTopSpeed,
                                                                              stats.groundReverseAcceleration,
                                                                              stats.groundReverseDeceleration,
                                                                              stats.groundReverseTopSpeed,
                                                                              dt);
                    state.strafeSpeed = Physics.StepVelocityWithInput(actions.move.x,
                                                                      state.strafeSpeed,
                                                                      stats.groundStrafeAcceleration,
                                                                      stats.groundStrafeDeceleration,
                                                                      stats.groundStrafeTopSpeed,
                                                                      stats.groundStrafeAcceleration,
                                                                      stats.groundStrafeDeceleration,
                                                                      stats.groundStrafeTopSpeed,
                                                                      dt);

                    //state.desiredPosition = trans.Value + math.forward(rot.Value) * state.forwardReverseSpeed * dt;
                    //state.desiredPosition -= math.cross(math.forward(rot.Value), state.groundNormal) * state.strafeSpeed * dt;
                    var right              = math.normalize(-math.cross(math.forward(rot.Value), state.groundNormal));
                    state.desiredPosition  = trans.Value - math.cross(math.forward(rot.Value), state.groundNormal) * state.strafeSpeed * dt;
                    state.desiredPosition += math.cross(right, state.groundNormal) * state.forwardReverseSpeed * dt;
                }
                else
                {
                    state.forwardReverseSpeed = Physics.StepVelocityWithInput(actions.move.y,
                                                                              state.forwardReverseSpeed,
                                                                              stats.airForwardAcceleration,
                                                                              stats.airForwardDeceleration,
                                                                              stats.airForwardTopSpeed,
                                                                              stats.airReverseAcceleration,
                                                                              stats.airReverseDeceleration,
                                                                              stats.airReverseTopSpeed,
                                                                              dt);
                    state.strafeSpeed = Physics.StepVelocityWithInput(actions.move.x,
                                                                      state.strafeSpeed,
                                                                      stats.airStrafeAcceleration,
                                                                      stats.airStrafeDeceleration,
                                                                      stats.airStrafeTopSpeed,
                                                                      stats.airStrafeAcceleration,
                                                                      stats.airStrafeDeceleration,
                                                                      stats.airStrafeTopSpeed,
                                                                      dt);

                    state.desiredPosition    = trans.Value + math.forward(rot.Value) * state.forwardReverseSpeed * dt;
                    state.desiredPosition   -= math.cross(math.forward(rot.Value), math.up()) * state.strafeSpeed * dt;
                    state.desiredPosition.y += state.verticalSpeed * dt;
                }

                var             body               = capsuleBodies[entityInQueryIndex];
                CapsuleCollider cap                = body.collider;
                cap.pointA                        += new float3(0f, stats.groundCheckDistance, 0f);
                body.collider                      = cap;
                var aabbInitial                    = Physics.AabbFrom(cap, body.transform);
                var aabbFinal                      = Physics.AabbFrom(cap, new RigidTransform(rot.Value, state.desiredPosition));
                var aabb                           = new Aabb(math.min(aabbInitial.min, aabbFinal.min), math.max(aabbInitial.max, aabbFinal.max));
                aabb.min.y                        -= stats.groundCheckDistance;
                capsuleBodies[entityInQueryIndex]  = body;
                aabbs[entityInQueryIndex]          = aabb;
            }).ScheduleParallel();

            Dependency = Physics.BuildCollisionLayer(capsuleBodies, aabbs).ScheduleParallel(out var actorLayer, Allocator.TempJob, Dependency);

            var collisionProcessor = new ResolveCollisionsProcessor
            {
                stateCdfe = checkGroundProcessor.stateCdfe,
                statsCdfe = checkGroundProcessor.statsCdfe
            };

            Dependency = Physics.FindPairs(actorLayer, environmentLayer, collisionProcessor).ScheduleParallel(Dependency);

            Entities.ForEach((ref Translation trans, ref MoveState state, in MoveStats stats, in Rotation rot) =>
            {
                if (state.isGrounded)
                {
                    var right          = math.normalize(-math.cross(math.forward(rot.Value), state.groundNormal));
                    var desiredRight   = -math.cross(math.forward(rot.Value), state.groundNormal) * state.strafeSpeed * dt;
                    var desiredForward = math.cross(right, state.groundNormal) * state.forwardReverseSpeed * dt;

                    var delta                  = state.desiredPosition - trans.Value;
                    var projectedRight         = math.projectsafe(delta, desiredRight);
                    state.strafeSpeed         *= math.saturate(math.length(projectedRight) / math.max(0.00001f, math.length(desiredRight)));
                    var projectedForward       = math.projectsafe(delta, desiredForward);
                    state.forwardReverseSpeed *= math.saturate(math.length(projectedForward) / math.max(0.00001f, math.length(desiredForward)));
                }
                else
                {
                    var desiredForward = math.forward(rot.Value) * state.forwardReverseSpeed * dt;
                    var desiredRight   = -math.cross(math.forward(rot.Value), math.up()) * state.strafeSpeed * dt;
                    var desiredUp      = state.verticalSpeed * dt;

                    var delta                  = state.desiredPosition - trans.Value;
                    var projectedForward       = math.projectsafe(delta, desiredForward);
                    state.forwardReverseSpeed *= math.saturate(math.length(projectedForward) / math.max(0.0000001f, math.length(desiredForward)));
                    var projectedRight         = math.projectsafe(delta, desiredRight);
                    state.strafeSpeed         *= math.saturate(math.length(projectedRight) / math.max(0.0000001f, math.length(desiredRight)));
                    state.verticalSpeed       *= math.saturate(math.abs(delta.y) / math.max(0.0000001f, desiredUp));
                    //UnityEngine.Debug.Log($"desiredUp: {desiredUp}, verticalSpeed: {state.verticalSpeed}, delta.y: {delta.y}");
                }
                trans.Value = state.desiredPosition;
            }).ScheduleParallel();

            var transCdfe = GetComponentDataFromEntity<Translation>();

            Entities.WithNativeDisableParallelForRestriction(transCdfe).ForEach((in MoveState state, in MoveStats stats, in AimHeadReference headRef) =>
            {
                float offset            = stats.headNormalHeight - stats.normalHeight;
                var   headTrans         = transCdfe[headRef.head];
                headTrans.Value.y       = state.height + offset;
                transCdfe[headRef.head] = headTrans;
            }).ScheduleParallel();

            Dependency = capsuleBodies.Dispose(Dependency);
            Dependency = aabbs.Dispose(Dependency);
            Dependency = groundCheckLayer.Dispose(Dependency);
            Dependency = actorLayer.Dispose(Dependency);
        }

        // Assumes actor is A and environment is B
        struct CheckGroundProcessor : IFindPairsProcessor
        {
            public PhysicsComponentDataFromEntity<MoveState>     stateCdfe;
            [ReadOnly] public ComponentDataFromEntity<MoveStats> statsCdfe;

            public void Execute(FindPairsResult result)
            {
                if (Physics.DistanceBetween(result.bodyA.collider, result.bodyA.transform, result.bodyB.collider, result.bodyB.transform, 0f, out var distanceResult))
                {
                    var stats = statsCdfe[result.entityA];
                    if (distanceResult.normalB.y >= stats.cosNormalIsGround)
                    {
                        var state = stateCdfe[result.entityA];
                        if (distanceResult.distance < state.distanceGroundIsWithinThreshold)
                        {
                            state.distanceGroundIsWithinThreshold = distanceResult.distance;
                            state.groundNormal                    = distanceResult.normalB;
                            state.isGrounded                      = state.verticalSpeed <= 0f;
                            state.verticalSpeed                   = math.max(0f, state.verticalSpeed);
                            stateCdfe[result.entityA]             = state;
                        }
                    }
                }
            }
        }

        // Assumes actor is A and environment is B
        struct ResolveCollisionsProcessor : IFindPairsProcessor
        {
            public PhysicsComponentDataFromEntity<MoveState>     stateCdfe;
            [ReadOnly] public ComponentDataFromEntity<MoveStats> statsCdfe;

            public void Execute(FindPairsResult result)
            {
                var state = stateCdfe[result.entityA];

                var transform = result.bodyA.transform;
                transform.pos = state.desiredPosition;

                if (state.isGrounded)
                {
                    var stats = statsCdfe[result.entityA];
                    if (Physics.DistanceBetween(result.bodyA.collider, transform, result.bodyB.collider, result.bodyB.transform, stats.groundCheckDistance, out var distanceResult))
                    {
                        if (distanceResult.normalB.y >= stats.cosNormalIsGround && distanceResult.normalA.y < 0f)
                        {
                            var sphere   = new SphereCollider(0f, stats.radius);
                            var start    = transform;
                            start.pos.y += state.height - stats.radius;
                            var end      = transform.pos + new float3(0f, stats.radius - stats.groundCheckDistance, 0f);
                            if (Physics.ColliderCast(sphere, start, end, result.bodyB.collider, result.bodyB.transform, out var castResult))
                            {
                                state.desiredPosition     = castResult.distance * new float3(0f, -1f, 0f) + start.pos - new float3(0f, stats.radius, 0f);
                                stateCdfe[result.entityA] = state;
                                return;
                            }
                        }
                    }
                }

                if (Physics.DistanceBetween(result.bodyA.collider, transform, result.bodyB.collider, result.bodyB.transform, 0f, out var overlapResult))
                {
                    state.desiredPosition     += (overlapResult.hitpointB - overlapResult.hitpointA);
                    stateCdfe[result.entityA]  = state;
                }
            }
        }
    }
}

