using System;
using Latios;
using Latios.Psyshock;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public struct DesiredActions : IComponentData
    {
        public float2 move;  //x right, y forward
        public float2 turn;
        public bool   fire;
        public bool   reload;
        public bool   jump;
        public bool   crouch;
    }

    public struct AimStats : IComponentData
    {
        public float turnTopSpeed;
        public float minAltitude;
        public float maxAltitude;
        public bool  restrictAltitude;
    }

    public struct MoveStats : IComponentData
    {
        public float groundForwardTopSpeed;
        public float groundReverseTopSpeed;
        public float groundStrafeTopSpeed;

        public float groundForwardAcceleration;
        public float groundForwardDeceleration;
        public float groundReverseAcceleration;
        public float groundReverseDeceleration;
        public float groundStrafeAcceleration;
        public float groundStrafeDeceleration;

        public float airForwardTopSpeed;
        public float airReverseTopSpeed;
        public float airStrafeTopSpeed;

        public float airForwardAcceleration;
        public float airForwardDeceleration;
        public float airReverseAcceleration;
        public float airReverseDeceleration;
        public float airStrafeAcceleration;
        public float airStrafeDeceleration;

        public float crouchDownSpeed;
        public float crouchUpSpeed;
        public float crouchTopSpeedAttenuation;
        public float normalHeight;
        public float crouchHeight;
        public float headNormalHeight;
        public float radius;

        public float gravity;
        public float jumpSpeed;
        public float cosNormalIsGround;
        public float groundCheckDistance;
    }

    public struct MoveState : IComponentData
    {
        public float  forwardReverseSpeed;
        public float  strafeSpeed;
        public float  height;
        public bool   isGrounded;
        public float  distanceGroundIsWithinThreshold;
        public float3 groundNormal;
        public float  verticalSpeed;
        public float3 desiredPosition;
    }

    public struct AimAltitude : IComponentData
    {
        public float altitude;  //radians
    }

    public struct AimHeadReference : IComponentData
    {
        public EntityWith<LocalToWorld> head;
    }

    public struct GunReferences : IComponentData
    {
        public EntityWith<GunState> timewarpedGun;
        public EntityWith<GunState> fireworksGun;
    }
}

