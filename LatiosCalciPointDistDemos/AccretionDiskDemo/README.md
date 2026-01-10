# Point Distribution Algorithms

## Accretion Disk Demo

This demo showcases the **Calci** module's accretion disk point distribution algorithm from the Latios Framework. It demonstrates how to integrate Calci's `NextAccretionDiskPoint()` method with Unity's Burst compiler, Jobs system, and GPU rendering to create a high-performance particle simulation.

Key features demonstrated:
- **Calci RNG Integration**: Using `Rng` and `RngSequence` for deterministic parallel random number generation
- **Burst-Compiled Parallel Jobs**: `IJobParallelFor` for efficient point generation across multiple threads
- **GPU Compute Shaders**: Physics simulation running entirely on the GPU with structured buffers
- **Instanced Rendering**: Rendering 1 million+ particles using `Graphics.RenderPrimitives` with point topology
- **Accretion Disk Physics**: Orbital velocity calculation for realistic black hole accretion disk behavior

### Controls

All parameters are configured via the Unity Inspector before entering Play mode:

**Distribution Settings**
- Inner/Outer Radius: Define the disk dimensions
- Number of Spirals: Control spiral arm count
- Spiral Tightness: Adjust spacing between spiral arms (recommended: 1.0)

**Physics Settings**
- Black Hole Mass: Affects orbital velocity (noticeable effects start around 1,000,000+, adjust by factors of 10)

**Visual Settings**
- Body Count: Number of particles (tested up to 1,000,000)
- Particle Size: Visual size of each point

**Simulation Mode**
- Flower Pattern: Enable for an artistic "flower" effect created by zero initial velocity (discovered accidentally during development)

**Camera Controls** (QWERTY keyboard)
- W/S: Move forward/backward
- A/D: Strafe left/right
- Q/E: Rotate counter-clockwise/clockwise
- Spacebar: Ascend
- C: Descend
- Mouse: Look direction

*Tip: If you want to be hypnotized, try moving closer to the center.*

### Technical Details

The demo uses three main components:

1. **AccretionDiskTest.cs**: C# job that calls Calci's distribution algorithm to generate particle positions and calculate initial orbital velocities
2. **NBodyTest.compute**: GPU compute shader performing basic Euler integration for particle motion
3. **NBodyTest.shader**: URP shader for rendering particles as point primitives

### Performance

Tested on Intel Core i7-10750H @ 2.60GHz with NVIDIA RTX 2060:
- **1,000,000 particles**: 200-300 FPS

The project uses Unity 6000.3.3f1 and Latios Framework 0.14.8.

![](media/accretion-disk-demo.png)
