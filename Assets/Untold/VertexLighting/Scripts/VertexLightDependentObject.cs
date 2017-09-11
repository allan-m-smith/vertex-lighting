using UnityEngine;

namespace Untold.VertexLighting
{
    /// <summary>
    /// This script can be attached to an object to disable/enable it depending on the time of day that was selected.
    /// Useful if your scene have more than one baked Lightmap and you want objects to exist in one time of day but not
    /// the other. Eg. A torch is lit up during the night, but it does not exist during the day.
    /// </summary>
    public class VertexLightDependentObject : MonoBehaviour
    {
        public VertexLightingTimeIndex[] SupportedTimes;
        public GameObject LightsHolder;

        public void Start()
        {
            VertexLightingUtil.RegisterVertexLightDependentObject(this);
        }
    }
}