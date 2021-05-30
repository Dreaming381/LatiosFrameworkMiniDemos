using System.Text;
using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class DisplayDeathCountSystem : SubSystem
    {
        StringBuilder m_stringBuilder = new StringBuilder();

        protected override void OnUpdate()
        {
            Entities.ForEach((UIReferences references) =>
            {
                m_stringBuilder.Clear();
                m_stringBuilder.Append("Deaths: ");
                m_stringBuilder.Append(worldBlackboardEntity.GetComponentData<DeathCounter>().deathCount);
                references.text.SetText(m_stringBuilder);
            }).WithoutBurst().Run();
        }
    }
}

