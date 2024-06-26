using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Post-Jam Notes:
// At one point, we had the character controller also be a rigid body so that it could
// interact with the blocks. However, this felt awful with specular contacts, so we
// reverted to the kinematic character controller (albeit keeping some of the dynamic
// hover logic). The purpose of this system was to lock the simulation from modifying
// the rotation of the character controller. It did work. But nothing uses it anymore.
// In the PairStream, userByte = 0 was for collision, while userByte = 1 was for this.

using static Unity.Entities.SystemAPI;

namespace BB
{
    [BurstCompile]
    public partial struct LockRotationSystem : ISystem
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
            var pairStream      = latiosWorld.worldBlackboardEntity.GetCollectionComponent<PhysicsPairStream>(false).pairStream;
            new Job { deltaTime = Time.DeltaTime, pairStream = pairStream }.Schedule();
        }

        [WithAll(typeof(FirstPersonControllerStats))]
        [BurstCompile]
        partial struct Job : IJobEntity
        {
            public PairStream pairStream;
            public float      deltaTime;

            public void Execute(Entity entity, in RigidBody rigidBody)
            {
                ref var parameters = ref pairStream.AddPairAndGetRef<UnitySim.Rotation3DConstraintJacobianParameters>(entity,
                                                                                                                      rigidBody.bucketIndex,
                                                                                                                      true,
                                                                                                                      Entity.Null,
                                                                                                                      rigidBody.bucketIndex,
                                                                                                                      false,
                                                                                                                      out var pair);
                pair.userByte = 1;
                UnitySim.ConstraintTauAndDampingFrom(UnitySim.kStiffSpringFrequency, UnitySim.kStiffDampingRatio, deltaTime, 4, out var tau, out var damping);
                UnitySim.BuildJacobian(out parameters,
                                       rigidBody.inertialPoseWorldTransform.rot,
                                       quaternion.identity,
                                       rigidBody.inertialPoseWorldTransform.rot,
                                       quaternion.identity,
                                       0f,
                                       0f,
                                       tau,
                                       damping);
            }
        }
    }
}

