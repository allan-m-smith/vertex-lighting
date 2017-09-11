using UnityEngine;

namespace Untold.VertexLighting
{
    /// <summary>
    /// Needs to be present in every scene that uses VertexLighting. Here you define all the possible Lightmap Variants
    /// and the desired Ambient Color for this scene. If no object is present with this script, the scene will not simulate Vertex Lights.
    /// </summary>
    [System.Serializable]
    public class VertexLightingSceneConfig
    {
        [Tooltip("Configure here all the presets for this scene. How many Light Variants are possible, where the Lightmap Textures are, the Ambient Color that should be used, etc.")]
        public VertexLightingVariant[] MapPresets;
        [Tooltip("Use this number to customize the update rate of objects. Default value is 10, meaning registered objects will check to be repainted every 10 frames. Important to note that this is measured in FRAMES.")]
        public uint FrameIntervalBetweenUpdates = 10;
    }
}