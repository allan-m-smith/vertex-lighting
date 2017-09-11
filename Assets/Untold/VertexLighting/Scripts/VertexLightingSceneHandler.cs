using UnityEngine;

namespace Untold.VertexLighting
{
    public class VertexLightingSceneHandler : MonoBehaviour
    {
        public VertexLightingSceneConfig SceneConfig;
        public VertexLightingTimeIndex TimeIndex;

        private void Awake()
        {
            VertexLightingUtil.Setup(SceneConfig);
        }

        private void OnDestroy()
        {
            VertexLightingUtil.Release();
        }

        private void OnEnable()
        {
            VertexLightingUtil.SetVertexLightVariant(TimeIndex);
        }

        private void Update()
        {
            VertexLightingUtil.Update();
        }
    }
}