using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Latios.Transforms;
using UnityEngine;
using Latios.Calci;
using Unity.Jobs.LowLevel.Unsafe;
using System.Collections.Generic;
using JetBrains.Annotations;



namespace Testing
{
    // Accretion disk Inspiration
    // https://github.com/DeadlockCode/barnes-hut/blob/master/src/utils.rs

    public class AccretionDiskTest : MonoBehaviour
    {
        const int SIZE_BODY = 10 * sizeof(float);

        [Header("Distribution Settings")]
        [Tooltip("Radius before the accretion disk spirals begin.")]
        public float innerRadius = 1.0f;
        [Tooltip("Distance from the start of the spiral to the outer edge of the disk.")]
        public float outerRadius = 20.0f;
        [Tooltip("Number of spirals in the accretion disk.")]
        public int numSpirals = 21;
        [Tooltip("How close each spiral is to neighboring spirals. Recommended to leave at 1.")]
        public float spiralTightness = 1.0f;

        [Header("Physics Settings")]
        [Tooltip("Mass of the central black hole. Effects become noticeable around 1,000,000+. Increase by a factor of 10 for faster orbital movement.")]
        public float blackHoleMass = 10000000.0f;

        [Header("Visual Settings")]
        [Tooltip("Number of particles representing light in the accretion disk. Tested up to 1,000,000 but can likely handle more.")]
        public int bodyCount = 1000000;
        [Tooltip("Visual size of each particle.")]
        public float particleSize = 1.0f;

        [Header("Shaders")]
        public Material material;
        public ComputeShader shader;

        [Header("Simulation Mode")]
        [Tooltip("For fun: Creates a flower pattern over time instead of realistic accretion disk movement. Discovered accidentally when particles had no initial velocity.")]
        public bool useFlowerPattern = false;


        int kernelID;
        ComputeBuffer bodyBuffer;

        int groupSizeX;

        RenderParams rp;

        NativeArray<Body> bodies;

        
        void Start()
        {
            Cursor.visible = false;

            bodies = new NativeArray<Body>(bodyCount, Allocator.Persistent);
            
            uint state = (uint)UnityEngine.Random.Range(0, bodyCount);
            Rng rng = new Rng(state);

            // Generate positions using Calci
            var positions = new NativeArray<float3>(bodyCount, Allocator.TempJob);

            // Generate accretion disk using custom job
            var job = new InitAccretionDiskJob
            {
                bodies = bodies,
                blackHoleMass = blackHoleMass,
                innerRadius = innerRadius,
                outerRadius = outerRadius,
                numSpirals = numSpirals,
                spiralTightness = spiralTightness,
                useFlowerPattern = useFlowerPattern,
                rng = rng
            };

            job.Schedule(bodyCount, 64).Complete();

            // create compute buffer
            bodyBuffer = new ComputeBuffer(bodyCount, SIZE_BODY);

            bodyBuffer.SetData(bodies);

            // find the id of the kernel
            kernelID = shader.FindKernel("CSNBodyTest");

            uint threadsX;
            shader.GetKernelThreadGroupSizes(kernelID, out threadsX, out _, out _);
            groupSizeX = Mathf.CeilToInt((float)bodyCount / (float)threadsX);

            // bind the compute buffer to the shader and the compute shader
            shader.SetBuffer(kernelID, "bodyBuffer", bodyBuffer);
            shader.SetInt("bodyCount", bodyCount);
            shader.SetFloat("blackHoleMass", blackHoleMass);  // Tune this value
            shader.SetVector("blackHolePos", new Vector3(0, 0, 0));
            shader.SetFloat("softening", 0.1f);  // Prevents particles from accelerating infinitely at r=0

            material.SetBuffer("bodyBuffer", bodyBuffer);
            material.SetFloat("_BodySize", particleSize);

            rp = new RenderParams(material);
            rp.worldBounds = new Bounds(Vector3.zero, 100 * Vector3.one);

            positions.Dispose();
        }

        [BurstCompile]
        struct InitAccretionDiskJob : IJobParallelFor
        {
            [WriteOnly]
            public NativeArray<Body> bodies;

            public float innerRadius;
            public float outerRadius;
            public int numSpirals;
            public float spiralTightness;
            public float blackHoleMass;
            public bool useFlowerPattern;
            public Rng rng;

            public void Execute(int index)
            {
                var sequence = rng.GetSequence(index);

                // Use Calci's extension method to generate the point
                float3 pos = sequence.NextAccretionDiskPoint(
                    innerRadius,
                    outerRadius,
                    numSpirals,
                    spiralTightness);
                
                float3 vel = float3.zero;

                if(!useFlowerPattern)
                {
                    // Calculate orbital velocity for circular orbit
                    float r = math.length(pos);
                    float3 dir = math.normalize(pos);

                    // Tangent direction (perpendicular to radial, in the disk plane)
                    // Assuming disk is in XY plane, rotating clockwise
                    float3 tangent = new float3(dir.y, -dir.x, 0);

                    // Orbital velocity: v = sqrt(GM/r)
                    float G = 1.0f; // Gravitational constant (tune this)
                    float orbitalSpeed = math.sqrt(G * blackHoleMass / r);
                    vel = tangent * orbitalSpeed;

                    // Add small random perturbations for realism (optional)
                    vel += sequence.NextFloat3(-0.1f, 0.1f);
                }

                bodies[index] = new Body(pos, vel, 1.0f);
            }
        }


        // Update is called once per frame
        void Update()
        {
            // Send data to compute shader
            shader.SetFloat("deltaTime", 0.000001f);

            // Update the bodies
            shader.Dispatch(kernelID, groupSizeX, 1, 1);

            Graphics.RenderPrimitives(rp, MeshTopology.Points, 1, bodyCount);
        }

        private void OnDestroy()
        {
            if(bodies.IsCreated)
                bodies.Dispose();

            if(bodyBuffer.IsValid())
                bodyBuffer.Dispose();
        }


    }

}
