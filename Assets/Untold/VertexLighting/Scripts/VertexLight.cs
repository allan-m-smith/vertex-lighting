using UnityEngine;

namespace Untold.VertexLighting
{
    /// <summary>
    /// Much similar to a regular Light, but used for VertexLighting system.
    /// Draws a gizmo to facilitate scene building and should be positioned in the scene.
    /// </summary>
    public class VertexLight : MonoBehaviour
    {
        public Color32 Color;
        public float Radius;

        protected virtual void OnEnable()
        {
            VertexLightingUtil.RegisterVertexLight(this);
        }

        protected void OnDisable()
        {
            VertexLightingUtil.UnregisterVertexLight(this);
        }

        #if UNITY_EDITOR

        protected void OnDrawGizmos()
        {
            var c = Color;
            c.a = 100;
            Gizmos.color = c;
            Gizmos.DrawSphere(transform.position, 1);
            Gizmos.DrawWireSphere(transform.position, Radius);
        }

        #endif
    }
}