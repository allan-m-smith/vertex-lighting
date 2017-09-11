using UnityEngine;

namespace Untold.VertexLighting
{
    /// <summary>
    /// Attach this component to any dynamic objects that are nested inside VertexLitObjects to make sure that they will NOT receive Vertex Lighting.
    /// Useful if you have a part of a character that is loaded dinamically (like a different armor set) that has a part that should not be affected by
    /// vertex lighting, while the rest of the chracter should.
    /// </summary>
    public class VertexDynamicUnlitObject : MonoBehaviour
    {
        
    }
}