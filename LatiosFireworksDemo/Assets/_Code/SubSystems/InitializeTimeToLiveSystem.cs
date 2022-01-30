using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class InitializeTimeToLiveSystem : SubSystem
    {
        Rng m_rng;

        EntityQuery m_query;

        public override void OnNewScene() => m_rng = new Rng("InitializeTimeToLiveSystem");

        protected override void OnUpdate()
        {
            var rng = m_rng.Shuffle();

            Entities.WithStoreEntityQueryInField(ref m_query).ForEach((int entityInQueryIndex, ref TimeToLive timeToLive, in TimeToLiveInitializer initializer) =>
            {
                var random            = rng.GetSequence(entityInQueryIndex);
                timeToLive.timeToLive = random.NextFloat(initializer.minMaxTimeToLive.x, initializer.minMaxTimeToLive.y);
            }).Run();

            EntityManager.RemoveComponent<TimeToLiveInitializer>(m_query);
        }
    }
}

