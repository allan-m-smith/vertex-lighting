using System.Collections.Generic;
using UnityEngine;

namespace Untold.VertexLighting
{
    /// <summary>
    /// This class should be attached to every object that should be painted by VertexLightingUtil. Can be added to
    /// static objects in the scene, dynamic pooled objects, anything, as long as this is attached to the parent of
    /// all models of this object. If this is not a dynamic object that instantiates models inside of it during runtime,
    /// it should be baked in editor time. Otherwise, Setup must be called whenever the object has been fully built.
    /// (Ex. Player character that loads armor pieces to build the final model)
    /// </summary>
    public class VertexLitObject : MonoBehaviour
    {
        [Header("Vertex Light Configuration")]
        [Tooltip("Check this if this model should look for dynamic meshes at runtime. This has a small performance hit so only use when necessary.")]
        public bool CheckForDynamicMeshes;
        [Tooltip("Check this if you want this character to flash white when it takes damage. This has a small performance cost.")]
        public bool HasTakeDamageEffect;
        [Tooltip("If this is checked, this model will automatically setup and register itself for update when it is enabled.")]
        public bool SetupAndRegisterOnEnable = true;
        [Tooltip("When updating objects, we check to see if they have moved enough for us to repaint it. If this is turned on, we will skip the distance checks, which will cause more repaints, which has a small cost, but sometimes can be useful for static objects.")]
        public bool IgnoreDistanceChecks;
        [Tooltip("When updating objects, we check to see if the color we would paint the object is too similar to the color the object has been previously painted as. If you check this, we will skip this check, causing more repaints, but can be useful in certain cases.")]
        public bool IgnoreColorProximityChecks;
        [Tooltip("You can add child objects to this list to make sure that they are not painted. If you have a Renderer with a material that will ignore vertex colors, or just want to skip one object being painted by the system, add a reference to it here.")]
        public GameObject[] IgnoredObjects;

        [Header("Automatically Configured")]
        [ReadOnly]
        public bool IsSetup;
        [ReadOnly]
        public bool HasAbnormalEffectColor;
        [ReadOnly]
        public Color32 AbnormalEffectColor;
        [ReadOnly]
        public bool IsVisible = true;
        [ReadOnly]
        public MeshFilter[] MeshFilters;
        [ReadOnly]
        public int MeshFiltersLength;
        [ReadOnly]
        public VertexLitObjectColors[] Colors;
        [ReadOnly]
        public uint FrameToUpdate;
        [ReadOnly]
        public Vector3 LastPosition;
        [ReadOnly]
        public Vector3 LastUpdatedPosition;
        [ReadOnly]
        public float ColorMultiplier = 0.2f;
        [ReadOnly]
        public Color32 LastColor;
        [ReadOnly]
        public bool IsRegistered;
        [ReadOnly]
        public bool IsPaused;
        
#if UNITY_EDITOR

        /// <summary>
        /// Should ALWAYS be called in editor time. Avoids lots of calculations that would otherwise be made in Awake.
        /// </summary>
        [ContextMenu("Bake")]
        private void Bake()
        {
            if (CheckForDynamicMeshes)
            {
                Debug.LogError("Since this object is flag to check for dynamic objects, it cannot be baked.");
                return;
            }

            var tempFilters = GetComponentsInChildren<MeshFilter>(true);
            var filtersToIgnoreList = new List<int>();
            if (IgnoredObjects != null)
            {
                for (int i = 0; i < tempFilters.Length; i++)
                {
                    for (int j = 0; j < IgnoredObjects.Length; j++)
                    {
                        if (tempFilters[i].gameObject == IgnoredObjects[j])
                        {
                            filtersToIgnoreList.Add(i);
                            break;
                        }
                    }
                }
            }

            var listOfFilters = new List<MeshFilter>();
            for (int i = 0; i < tempFilters.Length; i++)
            {
                if (filtersToIgnoreList.Contains(i))
                    continue;

                listOfFilters.Add(tempFilters[i]);
            }

            MeshFilters = listOfFilters.ToArray();
            MeshFiltersLength = MeshFilters.Length;
            Colors = new VertexLitObjectColors[MeshFiltersLength];
            for (var i = 0; i < MeshFiltersLength; i++)
            {
                var color = new VertexLitObjectColors();
                var meshFilter = MeshFilters[i];

                color.ColorsLength = meshFilter.sharedMesh.vertexCount;
                color.Colors = new Color32[color.ColorsLength];

                Colors[i] = color;
            }

            IsSetup = true;
        }

#endif

        #region Unity Events

        /// <summary>
        /// Necessary for objects that are already in the scene when the scene is loaded
        /// to make sure that it is called AFTER all the setup of the scene is complete.
        /// </summary>
        /*private void Start()
        {
            IsStarted = true;
            IsVisible = true;
            Register();
        }*/

        /// <summary>
        /// Makes sure that this object is all setup and registers it to be painted regularly by VertexLightingUtil.
        /// </summary>
        private void OnEnable()
        {
            //Unregister to make sure the light will be setup correctly
            Unregister();

            if (!SetupAndRegisterOnEnable)
                return;

            if (!IsSetup)
                Setup(false);

            IsVisible = true;
            Register();
        }

        /// <summary>
        /// Unregister this object from VertexLightingUtil to make sure that it will not be painted while inactive.
        /// </summary>
        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            IgnoredObjects = null;
            MeshFilters = null;
            Colors = null;
        }

        #endregion

        #region Setup

        /// <summary>
        /// Should ideally be called when the object is first instantiated, that is, when the pool containing this object is loaded or when the scene is loaded.
        /// Unity will call this again on Enable, but checks will ensure it does not run twice. It is bad to run this 'OnEnable' because it caches a lot of
        /// references, and if possible all this should be done when the new scene is loaded and the pool of this object is warmed up, to avoid spikes in
        /// performance.
        /// </summary>
        public void Setup(bool forced)
        {
            if (IsSetup && !forced)
                return;

            if (!CheckForDynamicMeshes && !forced)
            {
                if (MeshFilters == null || MeshFilters.Length == 0)
                    Debug.LogError("This object should have been baked already!!", gameObject);

                return;
            }

            var dynamicIgnoredObjects = GetComponentsInChildren<VertexDynamicUnlitObject>();
            var filters = GetComponentsInChildren<MeshFilter>();

            var filtersLength = filters.Length;
            var dynamicObjectsLength = dynamicIgnoredObjects.Length;
            var ignoredObjectsLength = IgnoredObjects.Length;

            var ignoredObjectsCount = 0;
            var indexesToIgnore = new int[dynamicObjectsLength + ignoredObjectsLength];
            var indexesToIgnoreLength = dynamicObjectsLength + ignoredObjectsLength;

            for (int i = 0; i < filtersLength; i++)
            {
                var filter = filters[i];

                var foundObject = false;
                for (int j = 0; j < dynamicObjectsLength; j++)
                {
                    if (filter.gameObject.GetInstanceID() == dynamicIgnoredObjects[j].gameObject.GetInstanceID())
                    {
                        indexesToIgnore[ignoredObjectsCount] = i;
                        ignoredObjectsCount++;
                        foundObject = true;
                        break;
                    }
                }

                if (foundObject || IgnoredObjects == null)
                    continue;

                for (int j = 0; j < ignoredObjectsLength; j++)
                {
                    if (filter.gameObject.GetInstanceID() == IgnoredObjects[j].gameObject.GetInstanceID())
                    {
                        indexesToIgnore[ignoredObjectsCount] = i;
                        ignoredObjectsCount++;
                        break;
                    }
                }
            }

            MeshFilters = new MeshFilter[filtersLength - indexesToIgnoreLength];

            var countedIgnoredObjects = 0;
            for (int i = 0; i < filtersLength; i++)
            {
                var ignoreItem = false;
                for (int j = 0; j < indexesToIgnoreLength; j++)
                {
                    if (i == indexesToIgnore[j])
                    {
                        ignoreItem = true;
                        break;
                    }
                }

                if (ignoreItem)
                {
                    countedIgnoredObjects++;
                    continue;
                }

                MeshFilters[i - countedIgnoredObjects] = filters[i];
            }

            MeshFiltersLength = MeshFilters.Length;
            Colors = new VertexLitObjectColors[MeshFiltersLength];
            for (var i = 0; i < MeshFiltersLength; i++)
            {
                var color = new VertexLitObjectColors();
                var meshFilter = MeshFilters[i];

                color.ColorsLength = meshFilter.sharedMesh.vertexCount;
                color.Colors = new Color32[color.ColorsLength];

                Colors[i] = color;
            }

            IsSetup = true;
        }

        #endregion

        #region Registering

        public void Register()
        {
            if (IsRegistered)
                return;

            IsRegistered = true;
            VertexLightingUtil.RegisterVertexLitObject(this);
        }

        public void Unregister()
        {
            if (!IsRegistered)
                return;

            IsRegistered = false;
            VertexLightingUtil.UnregisterVertexLitObject(this);
        }

        #endregion

        #region Abnormal Effects Color

        /// <summary>
        /// Sets a base color that will be used when painting this object. Useful for Abnormal Effects such as Poisoned and so on.
        /// Must call UnsetAbnormalEffectColor to return to the default behavior.
        /// </summary>
        /// <param name="color">Color.</param>
        public void SetAbnormalEffectColor(Color32 color)
        {
            AbnormalEffectColor = color;
            HasAbnormalEffectColor = true;
            VertexLightingUtil.ForcePaint(this);
        }

        /// <summary>
        /// If an Abnormal Effect Color has been set, this needs to be called to make sure that the previous color will be ignored from now on.
        /// </summary>
        public void UnsetAbnormalEffectColor()
        {
            HasAbnormalEffectColor = false;
            VertexLightingUtil.ForcePaint(this);
        }

        #endregion

        #region Damage Effect

        /// <summary>
        /// Plays the damage effect.
        /// </summary>
        public void PlayDamageEffect()
        {
            if (!HasTakeDamageEffect)
                return;

            // Only apply the multiplier color if the object isn't already
            // with a take damage effect
            // this is done because when there's too much damage, the object
            // almost never goes to its normal color
            if (ColorMultiplier < .3f)
                ColorMultiplier = 1;
        }

        #endregion
    }
}