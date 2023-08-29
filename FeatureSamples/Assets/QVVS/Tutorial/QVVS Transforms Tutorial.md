# QVVS Transforms Tutorial

This tutorial provides a walkthrough of various pieces of the QVVS transforms
module. Each step is discussed briefly here, but it is recommended you study the
scene and code of each step to obtain understanding. Basic understanding of ECS
(I.E. Unity’s ECS tanks tutorial) and setup of a Latios Framework project is
assumed.

## Step 1: Spinning

This scene shows how to make a spinning cube using QVVS Transforms.

When baking, QVVS Transforms uses `TransformUsageFlags` similar to how Unity
Transforms use them. As we intend to modify the cube’s rotation, we use Dynamic.

When attempting to modify the transform data of an existing entity, always use
`TransformAspect`. Not doing so will often lead to bugs.

## Step 2: Hierarchy

This scene shows a player character composed of a root entity, and two
descendant entities which provide the visual representation. The root is
controlled by player input and shows more ways to use `TransformAspect`. The
modifications to the root’s transform are automatically propagated to the
descendants.
