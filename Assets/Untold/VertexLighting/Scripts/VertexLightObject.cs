using UnityEngine;

namespace Untold.VertexLighting
{
    /// <summary>
    /// Used to cache VertexLight information for faster calculations and access.
    /// </summary>
    [System.Serializable]
    public struct VertexLightObject
    {
        public int InstanceId;
        public Color32 Color;
        public float SqrRadius;
        public Vector3 Position;
    }
}