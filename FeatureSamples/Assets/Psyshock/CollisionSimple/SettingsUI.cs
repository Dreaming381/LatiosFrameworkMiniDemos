using System.Collections;
using System.Collections.Generic;
using Latios;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Dragons.PsyshockSamples.CollisionSimple
{
    public class SettingsUI : MonoBehaviour
    {
        public Slider elasticity;
        public Slider substeps;
        public Slider iterations;
        public Toggle useFindPairs;

        public TMP_Text substepsLabel;
        public TMP_Text iterationsLabel;

        BlackboardEntity sceneBlackboardEntity;

        void Start()
        {
            sceneBlackboardEntity = (World.DefaultGameObjectInjectionWorld as LatiosWorld).sceneBlackboardEntity;
            var settings          = new Settings
            {
                elasticity   = elasticity.value,
                substeps     = (int)substeps.value,
                iterations   = (int)iterations.value,
                useFindPairs = useFindPairs.isOn
            };
            sceneBlackboardEntity.AddComponentData(settings);

            substepsLabel.SetText($"Substeps - {settings.substeps}");
            iterationsLabel.SetText($"Iterations - {settings.iterations}");
        }

        void Update()
        {
            var settings = new Settings
            {
                elasticity   = elasticity.value,
                substeps     = (int)substeps.value,
                iterations   = (int)iterations.value,
                useFindPairs = useFindPairs.isOn
            };
            var oldSettings = sceneBlackboardEntity.GetComponentData<Settings>();
            sceneBlackboardEntity.SetComponentData(settings);

            if (settings.substeps != oldSettings.substeps)
                substepsLabel.SetText($"Substeps - {settings.substeps}");
            if (settings.iterations != oldSettings.iterations)
                iterationsLabel.SetText($"Iterations - {settings.iterations}");
        }
    }
}

