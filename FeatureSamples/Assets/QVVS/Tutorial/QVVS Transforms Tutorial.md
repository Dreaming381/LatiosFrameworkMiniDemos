# QVVS Transforms Tutorial

This tutorial provides a walkthrough of various pieces of the QVVS transforms module. Each step is discussed briefly here, but it is recommended you study the scene and code of each step to obtain understanding. Basic understanding of ECS (I.E. Unity’s ECS tanks tutorial) and setup of a Latios Framework project is assumed.

## Step 1: Spinning

This scene shows how to make a spinning cube using QVVS Transforms.

When baking, QVVS Transforms uses `TransformUsageFlags` similar to how Unity Transforms use them. As we intend to modify the cube’s rotation, we use Dynamic.

When attempting to modify the transform data of an existing entity, always use `TransformAspect`. Not doing so will often lead to bugs.

## Step 2: Hierarchy

This scene shows a player character composed of a root entity, and two descendant entities which provide the visual representation. The root is controlled by player input and shows more ways to use `TransformAspect`. The modifications to the root’s transform are automatically propagated to the descendants.

## Step 3: Motion History

This scene shows the player on a moving platform with motion history. The motion of the moving platform is accounted for in the player’s movement, using the motion history of the moving platform. The `PreviousTransform` component stores the `WorldTransform` value at the start of simulation. Then the `WorldTransform` of the moving platform is updated, which makes it different from the `PreviousTransform` value. This difference is read by the player to offset it.

## Step 4: Scale and Stretch

This scene shows an animation that applies scale and stretch to different entities in the hierarchy. Scale is applied to the Root, and is inherited by all entities. However, Root’s child Stem has animated stretch. This scales Stem’s child Top’s local position offset relative to Stem’s local origin, but otherwise does not affect Top in any way. Top can then be stretched independently.

## Step 5: Anchor Offsets

When stretching a parent, it is important to remember that this moves the child object’s position, which is dependent on the child object’s origin point. However, often times, it is desired for the child to maintain visual contact with the parent. This scene shows a common configuration mistake, and how to rectify it using intermediate anchor offset entities. The anchor entity defines a point on the parent’s visual surface that should be tracked. The child is offset from this tracked point to maintain visual contact.

## Step 6: Instantiating World Entities

This scene shows how to instantiate entities in world-space. Click to spawn small spheres behind the character. Because `TransformAspect` cannot be used in command buffers, `WorldTransform` is used here. It is safe to use `WorldTransform` directly for prefabs or root entities. This example also shows how to preserve the scale and stretch from the prefab, and how to use the static `qvvs` class to transform an offset used for the spawn location.

## Step 7: Instantiating Child Entities

This scene shows how to instantiate entities as children of other entities. Click to spawn a hat for the character. QVVS Transforms use the same `Parent` and `Child` concept as Unity Transforms. And you use them the same way. However, unlike Unity Transforms, QVVS Transforms will automatically add or remove `LocalTransform` based on the existence of the `Parent` component. This sample explicitly adds the `LocalTransform` to set the position and preserve scaling. But had it not, the entity would have been given an identity `LocalTransform`.

## Step 8: Hierarchy Update Modes

There are multiple scenes in this step. The first scene is named QVVS_Tutorial_Hierarchy_Update_Modes which shows how the influence a parent transform has on the child can be customized. `HierarchyUpdateMode` is an optional component that can be attached to a child entity and influences the mathematics used to update the child’s transform during the `TransformSuperSystem` update. Because this happens directly in the hierarchy update, it can eliminate awkward execution order issues or having to precompute parent world-space transforms prior to the `TransformSuperSystem` update. There is a special way to bake mode flags into entities, allowing for multiple bakers to specify flags (just like `TransformUsageFlags`). But you can also add, remove, and modify the flags at runtime.

The scene QVVS_Tutorial_Hierachy_Mode_Example_A shows a common case where locking the world-space rotations on entities can be very useful.
