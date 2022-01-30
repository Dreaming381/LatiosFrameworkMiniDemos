using Latios;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class GunActionSystem : SubSystem
    {
        protected override void OnUpdate()
        {
            var bulletCreateBuffer = latiosWorld
                                     .syncPoint
                                     .CreateInstantiateCommandBuffer<Translation, Rotation, PreviousTranslation,SphericalBallisticsState, Firer>()
                                     .AsParallelWriter();

            var effectCreateBuffer = latiosWorld
                                     .syncPoint
                                     .CreateInstantiateCommandBuffer<Translation, Rotation, TimeToLive>()
                                     .AsParallelWriter();

            Entity sbe = sceneBlackboardEntity;

            Entities.ForEach((Entity entity, int entityInQueryIndex, in DesiredActions desiredActions, in GunReferences gunReferences) =>
            {
                var mode     = GetComponent<GunMode>(sbe);
                var gun      = mode.mode == GunMode.Mode.Fireworks ? gunReferences.fireworksGun : gunReferences.timewarpedGun;
                var state    = GetComponent<GunState>(gun);
                var gunStats = GetComponent<GunStats>(gun);

                if (!state.shooting)
                {
                    if (desiredActions.fire && state.magazineCount > 0)
                    {
                        state.reloading    = false;
                        state.shooting     = true;
                        state.shotCooldown = gunStats.shotCooldown;
                        state.magazineCount--;

                        if (state.magazineCount == 0)
                        {
                            state.reloading    = true;
                            state.timeToReload = gunStats.reloadTime;
                        }

                        var ltw = GetComponent<LocalToWorld>(gunStats.gunBarrelReference);
                        var rot = quaternion.LookRotationSafe(ltw.Forward, ltw.Up);

                        bulletCreateBuffer.Add(gunStats.bulletPrefab,
                                               new Translation { Value                         = ltw.Position },
                                               new Rotation { Value                            = rot },
                                               new PreviousTranslation { previousTranslation   = ltw.Position },
                                               new SphericalBallisticsState { unwarpedVelocity = math.forward(rot) * gunStats.shotExitSpeed },
                                               new Firer { firerEntity                         = entity },
                                               entityInQueryIndex);

                        if (gunStats.effectPrefab != Entity.Null)
                        {
                            effectCreateBuffer.Add(gunStats.effectPrefab,
                                                   new Translation { Value     = ltw.Position },
                                                   new Rotation { Value        = rot },
                                                   new TimeToLive { timeToLive = gunStats.shotEffectLifetime },
                                                   entityInQueryIndex);
                        }
                    }
                    else if (desiredActions.reload && !state.reloading && state.magazineCount < gunStats.magazineCapacity)
                    {
                        state.reloading    = true;
                        state.timeToReload = gunStats.reloadTime;
                    }

                    SetComponent(gun, state);
                }
            }).Schedule();
        }
    }
}

