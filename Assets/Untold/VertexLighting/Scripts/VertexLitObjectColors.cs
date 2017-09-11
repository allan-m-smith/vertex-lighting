using UnityEngine;

namespace Untold.VertexLighting
{
    /// <summary>
    /// Used to store VertexColors from VertexLitObjects. Will be constantly updated through VertexLightingUtil to update the object color.
    /// </summary>
    [System.Serializable]
    public class VertexLitObjectColors
    {
        public int ColorsLength;
        public Color32[] Colors;
    }
}