using UnityEngine;

namespace Untold.VertexLighting
{
    /// <summary>
    /// Defines a Lightmap Variant, used by VertexLightingSceneConfig.
    /// This stores the lightmaps and ambient color that will be used for Vertex Lighting calculations.
    /// If you want to dinamically load the lightmap assets as to not use extra memory in the scene,
    /// use VertexLightingResourcesVariant script.
    /// </summary>
    [System.Serializable]
    public class VertexLightingVariant
    {
        public VertexLightingTimeIndex Time;
        public Color32 AmbientColor;
        public string[] LightmapPaths;
    }
}