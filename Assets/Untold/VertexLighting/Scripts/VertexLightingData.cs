using System.Collections.Generic;
using UnityEngine;

namespace Untold.VertexLighting
{
    /// <summary>
    /// This class is only a data holder used by VertexLightingUtil. All of these could be static variables in VertexLightingUtil
    /// but have been declared here for easier debugging. When set up, VertexLightingUtil creates a new Game Object with this script
    /// and starts populating and using these variables, which can be seen from the inspector.
    /// </summary>
    public class VertexLightingData : MonoBehaviour
    {
        [ReadOnly] public bool ShouldUpdate;
        [ReadOnly] public VertexLightingSceneConfig Config;
        [ReadOnly] public VertexLightingTimeIndex CurrentLightingVariantIndex = VertexLightingTimeIndex.None;
        [ReadOnly] public Color32 AmbientColor;
        [ReadOnly] public byte ProximityLevel = 50;
        [ReadOnly] public float MinMoveDistance = 1;

        [ReadOnly] public List<VertexLightObject> Lights;
        [ReadOnly] public int LightsCount;
        [ReadOnly] public List<VertexLitObject> LitObjects;
        [ReadOnly] public int LitObjectsCount;

        [ReadOnly] public List<VertexLight> QueuedLights;
        [ReadOnly] public int QueuedLightsCount;
        [ReadOnly] public List<VertexLitObject> QueuedLitObjects;
        [ReadOnly] public int QueuedLitObjectsCount;
        [ReadOnly] public Stack<VertexLightDependentObject> LightmapDependentObjects;

        [ReadOnly] public uint CurrentFrameCount;
    }
}