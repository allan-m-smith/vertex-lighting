using UnityEngine;

namespace Untold.VertexLighting
{
    /// <summary>
    /// Defines a Lightmap Variant, used by VertexLightingSceneConfig.
    /// This stores the lightmaps and ambient color that will be used for Vertex Lighting calculations.
    /// If you want ot use direct references to the Lightmap assets instead of dinamically loading them
    /// from the Resources folder, use VertexLightingVariant script.
    /// </summary>
    [System.Serializable]
    public class VertexLightingResourcesVariant
    {
        public VertexLightingTimeIndex Time;
        public Color32 AmbientColor;
        public string[] LightmapPaths;
    }
}