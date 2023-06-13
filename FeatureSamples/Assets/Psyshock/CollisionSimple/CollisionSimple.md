# CollisionSimple

![](media/95de3435be584a054f64560ce44ae892.gif)

A sample exploring the basics of collision detection and handling for rigidbody
spheres in 2D. The simulation explores iterations and substeps to improve the
simulation quality. The arena has a thick outer wall and a thin inner wall. With
too few substeps or iterations and a high enough elasticity, the spheres will
phase through the wall. In general, substeps offer more stable simulations than
iterations at the same quantity.

This sample also demonstrates several features of Psyshock, such as the Substep
API, using `DistanceBetween()` to detect collisions, and using `CollisionLayers`
and FindPairs to greatly improve performance. There are two different systems
for simulation, which are alternated by the *Use Find Pairs* setting in the
runtime UI.

The blue sphere is controllable with keyboard or gamepad inputs, and can push
the other spheres around.

**Note:** After modifying settings in the runtime UI, click somewhere off the UI
before moving the character to avoid accidentally changing settings.

This sample is based on Matthias Muller's Ten Minute Physics series.

<https://matthias-research.github.io/pages/tenMinutePhysics/index.html>

It recreates Part 3 of the series, while introducing substepping and iteration
controls from Part 5. While this is not an PBD simulation, substepping and
iterations can still improve the quality of the simulation.
