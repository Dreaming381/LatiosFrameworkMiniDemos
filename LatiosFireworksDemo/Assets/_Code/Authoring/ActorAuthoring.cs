using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.Authoring
{
    [DisallowMultipleComponent]
    public class ActorAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [Header("Aim")]
        public float turnTopSpeed = 1000f;
        public float minAltitude  = -80f;
        public float maxAltitude  = 80f;

        [Header("Ground Move")]
        public float groundForwardTopSpeed     = 1f;
        public float groundForwardAcceleration = 1f;
        public float groundForwardDeceleration = 1f;
        [Space]
        public float groundReverseTopSpeed     = 1f;
        public float groundReverseAcceleration = 1f;
        public float groundReverseDeceleration = 1f;
        [Space]
        public float groundStrafeTopSpeed     = 1f;
        public float groundStrafeAcceleration = 1f;
        public float groundStrafeDeceleration = 1f;
        [Space, Range(0f, 1f)]
        public float crouchSpeedFactor = 0.5f;

        [Header("Air Move")]
        public float airForwardTopSpeed     = 1f;
        public float airForwardAcceleration = 1f;
        public float airForwardDeceleration = 1f;
        [Space]
        public float airReverseTopSpeed     = 1f;
        public float airReverseAcceleration = 1f;
        public float airReverseDeceleration = 1f;
        [Space]
        public float airStrafeTopSpeed     = 1f;
        public float airStrafeAcceleration = 1f;
        public float airStrafeDeceleration = 1f;

        [Header("Jump")]
        public float gravity             = -9.81f;
        public float jumpSpeed           = 5f;
        public float maxIncline          = 45f;
        public float groundCheckDistance = 0.1f;

        [Header("Shape")]
        public float radius       = 0.5f;
        public float height       = 2f;
        public float crouchHeight = 1f;

        [Header("ActionSpeeds")]
        public float crouchDownTime = 0.3f;
        public float crouchUpTime   = 0.3f;
        public float reloadTime     = 1f;

        [Header("Child References")]
        public GameObject aimHead;
        public GameObject timewarpGun;
        public GameObject fireworksGun;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<DesiredActions>(entity);
            dstManager.AddComponentData(entity, new AimStats
            {
                turnTopSpeed     = turnTopSpeed,
                minAltitude      = math.radians(minAltitude),
                maxAltitude      = math.radians(maxAltitude),
                restrictAltitude = true
            });
            dstManager.AddComponentData(entity, new MoveStats
            {
                groundForwardTopSpeed     = groundForwardTopSpeed,
                groundForwardAcceleration = groundForwardAcceleration,
                groundForwardDeceleration = groundForwardDeceleration,
                groundReverseTopSpeed     = groundReverseTopSpeed,
                groundReverseAcceleration = groundReverseAcceleration,
                groundReverseDeceleration = groundReverseDeceleration,
                groundStrafeTopSpeed      = groundStrafeTopSpeed,
                groundStrafeAcceleration  = groundStrafeAcceleration,
                groundStrafeDeceleration  = groundStrafeDeceleration,
                airForwardTopSpeed        = airForwardTopSpeed,
                airForwardAcceleration    = airForwardAcceleration,
                airForwardDeceleration    = airForwardDeceleration,
                airReverseTopSpeed        = airReverseTopSpeed,
                airReverseAcceleration    = airReverseAcceleration,
                airReverseDeceleration    = airReverseDeceleration,
                airStrafeTopSpeed         = airStrafeTopSpeed,
                airStrafeAcceleration     = airStrafeAcceleration,
                airStrafeDeceleration     = airStrafeDeceleration,
                normalHeight              = height,
                crouchHeight              = crouchHeight,
                crouchDownSpeed           = -(height - crouchHeight) / crouchDownTime,
                crouchUpSpeed             = (height - crouchHeight) / crouchUpTime,
                headNormalHeight          = aimHead.transform.localPosition.y,
                gravity                   = gravity,
                jumpSpeed                 = jumpSpeed,
                cosNormalIsGround         = math.radians(90f - maxIncline),
                groundCheckDistance       = groundCheckDistance,
                radius                    = radius,
                crouchTopSpeedAttenuation = crouchSpeedFactor
            });
            dstManager.AddComponent<MoveState>(  entity);
            dstManager.AddComponent<AimAltitude>(entity);
            dstManager.AddComponentData(entity, new AimHeadReference { head = conversionSystem.GetPrimaryEntity(aimHead) });
            dstManager.AddComponentData(entity, new GunReferences
            {
                timewarpedGun = conversionSystem.GetPrimaryEntity(timewarpGun),
                fireworksGun  = conversionSystem.GetPrimaryEntity(fireworksGun)
            });
        }
    }
}

