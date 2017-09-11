using System.Collections.Generic;
using UnityEngine;

namespace Untold.VertexLighting
{
    /// <summary>
    /// Util responsible for painting all Vertex Lit objects in the scene, as well as turning objects on/off based on their TimeOfDay conditions.
    /// </summary>
    public static class VertexLightingUtil
    {
        private static VertexLightingData _data;

        #region Setup

        /// <summary>
        /// Creates a new game object to be used as a Data Holder for this util. It should be called automatically by 'Setup',
        /// but since order of Awake initialization is not guaranteed in Unity, we need to check in multiple methods to see
        /// if this has been called already or not.
        /// </summary>
        private static void SetupData()
        {
            _data = new GameObject("VertexLightingData").AddComponent<VertexLightingData>();
        }

        /// <summary>
        /// Called by VertexLightingSceneConfig on Awake, which should be present in every scene that uses lightmaps and vertex lights.
        /// </summary>
        /// <param name="config">The config object.</param>
        public static void Setup(VertexLightingSceneConfig config)
        {
            if (_data == null)
                SetupData();

            _data.Config = config;
            _data.Lights = new List<VertexLightObject>(50);
            _data.LightsCount = 0;
            _data.LitObjects = new List<VertexLitObject>(50);
            _data.LitObjectsCount = 0;
        }

        /// <summary>
        /// Releases any resources currently held by this instance.
        /// </summary>
        public static void Release()
        {
            _data.Lights.Clear();
            _data.Lights = null;
            _data.LitObjects.Clear();
            _data.LitObjects = null;

            if (_data.QueuedLitObjects != null)
            {
                _data.QueuedLitObjects.Clear();
                _data.QueuedLitObjects = null;
                _data.QueuedLitObjectsCount = 0;
            }

            if (_data.QueuedLights != null)
            {
                _data.QueuedLights.Clear();
                _data.QueuedLights = null;
                _data.QueuedLightsCount = 0;
            }

            _data.Config = null;

            if (_data.LightmapDependentObjects != null)
            {
                _data.LightmapDependentObjects.Clear();
                _data.LightmapDependentObjects = null;
            }

            _data = null;
        }

        #endregion

        #region Set Vertex Light Variant

        /// <summary>
        /// Should only be called ONCE at the start of the scene. VertexLightingSceneHandler has a simple implementation that calls
        /// this method on enable, but you can create a different implementation to suit your needs.
        /// </summary>
        /// <param name="timeIndex"></param>
        /// <param name="variantIndex">Lightmap Variant index.</param>
        public static void SetVertexLightVariant(VertexLightingTimeIndex timeIndex)
        {
            //for (int i = 0; i < LightmapSettings.lightmaps.Length; i++)
            //    FHResources.UnloadAsset(LightmapSettings.lightmaps[i].lightmapColor);

            VertexLightingVariant variant = null;
            for (int i = 0; i < _data.Config.MapPresets.Length; i++)
            {
                variant = _data.Config.MapPresets[i];
                if (variant.Time == timeIndex)
                    break;
            }

            _data.CurrentLightingVariantIndex = timeIndex;

            var lightmapData = new LightmapData[variant.LightmapPaths.Length];
            for (byte i = 0; i < lightmapData.Length; i++)
            {
                lightmapData[i] = new LightmapData {
                    lightmapColor = Resources.Load<Texture2D>(variant.LightmapPaths[i])
                };
            }

            LightmapSettings.lightmaps = lightmapData;
            _data.AmbientColor = variant.AmbientColor;

            if (_data.LightmapDependentObjects != null)
            {
                while (_data.LightmapDependentObjects.Count > 0)
                {
                    var obj = _data.LightmapDependentObjects.Pop();
                    CheckVertexLightDependentObject(obj);
                }
            }

            if (_data.QueuedLights != null)
            {
                for (int i = 0; i < _data.QueuedLightsCount; i++)
                    RegisterVertexLight(_data.QueuedLights[i]);
                _data.QueuedLights.Clear();
                _data.QueuedLightsCount = 0;
            }

            if (_data.QueuedLitObjects != null)
            {
                for (int i = 0; i < _data.QueuedLitObjectsCount; i++)
                    RegisterVertexLitObject(_data.QueuedLitObjects[i]);
                _data.QueuedLitObjects.Clear();
                _data.QueuedLitObjectsCount = 0;
            }

            _data.ShouldUpdate = true;
        }

        #endregion

        #region Vertex Light Dependent objects

        /// <summary>
        /// Registers a new object that depends on the Lightmap Variant Index to know if it should be active or not.
        /// </summary>
        /// <param name="obj">The object.</param>
        public static void RegisterVertexLightDependentObject(VertexLightDependentObject obj)
        {
            if (obj.SupportedTimes == null || obj.SupportedTimes.Length == 0)
                return;

            if (_data == null)
                SetupData();

            if (_data.CurrentLightingVariantIndex == VertexLightingTimeIndex.None)
            {
                if (_data.LightmapDependentObjects == null)
                    _data.LightmapDependentObjects = new Stack<VertexLightDependentObject>(10);

                _data.LightmapDependentObjects.Push(obj);
                return;
            }

            CheckVertexLightDependentObject(obj);
        }

        /// <summary>
        /// Effectively makes the check to know if the object in question should or not be active at the current lightmap
        /// variant. If objects registered BEFORE a variant has been set, they will be queued in a stack, and then processed
        /// when the lightmap variant has been set.
        /// </summary>
        /// <param name="obj">The object.</param>
        private static void CheckVertexLightDependentObject(VertexLightDependentObject obj)
        {
            for (byte i = 0; i < obj.SupportedTimes.Length; i++)
            {
                if (obj.SupportedTimes[i] == _data.CurrentLightingVariantIndex)
                {
                    obj.LightsHolder.SetActive(true);
                    return;
                }
            }

            obj.LightsHolder.SetActive(false);
        }

        #endregion

        #region Vertex Lit Objects

        #region Registering Lights

        /// <summary>
        /// When a light register itself we already cache everything we need. Lights are not supposed to move so we also cache its position.
        /// </summary>
        /// <param name="light">Light to register.</param>
        public static void RegisterVertexLight(VertexLight light)
        {
            if (_data == null)
                SetupData();

            if (_data.Config == null)
            {
                //This needs to be done here because objects can try to register themselves before
                //this script's Setup has been ran... and it is impossible to guarantee the order.
                if (_data.QueuedLights == null)
                {
                    _data.QueuedLights = new List<VertexLight>(50);
                    _data.QueuedLightsCount = 0;
                }

                _data.QueuedLights.Add(light);
                _data.QueuedLightsCount++;
                return;
            }

            _data.Lights.Add(new VertexLightObject
            {
                InstanceId = light.GetInstanceID(),
                Color = light.Color,
                SqrRadius = light.Radius * light.Radius,
                Position = light.transform.position
            });
            _data.LightsCount++;
        }

        /// <summary>
        /// Lights are unregistered based on GetInstanceID for faster check. Once removed, it will no longer be considered for lighting calculation.
        /// </summary>
        /// <param name="light">Light.</param>
        public static void UnregisterVertexLight(VertexLight light)
        {
            //This is necessary because objects can unregister themselves after Release has been called.
            if (_data.Lights == null)
                return;

            for (int i = 0; i < _data.LightsCount; i++)
            {
                if (_data.Lights[i].InstanceId == light.GetInstanceID())
                {
                    _data.Lights.RemoveAt(i);
                    _data.LightsCount--;
                    break;
                }
            }
        }

        #endregion

        #region Registering Lit Objects

        /// <summary>
        /// Register the vertex lit object to receive Light calculations. It is also immediately painted to avoid it showing at an off color in the first frame.
        /// </summary>
        /// <param name="obj">Object to register.</param>
        public static void RegisterVertexLitObject(VertexLitObject obj)
        {
            if (_data == null)
                SetupData();

            if (_data.Config == null)
            {
                //This needs to be done here because objects can try to register themselves before
                //this script's Setup has been ran... and it is impossible to guarantee the order.
                if (_data.QueuedLitObjects == null)
                {
                    _data.QueuedLitObjects = new List<VertexLitObject>(50);
                    _data.QueuedLitObjectsCount = 0;
                }

                _data.QueuedLitObjects.Add(obj);
                _data.QueuedLitObjectsCount++;
                return;
            }

            _data.LitObjects.Add(obj);
            _data.LitObjectsCount++;
            ForcePaint(obj);
        }

        /// <summary>
        /// Once unregistered, this object's color will not receive any further light calculations.
        /// </summary>
        /// <param name="obj">Object to unregister.</param>
        public static void UnregisterVertexLitObject(VertexLitObject obj)
        {
            //This is necessary because objects can unregister themselves after Release has been called.
            if (_data.LitObjects == null)
                return;

            _data.LitObjects.Remove(obj);
            _data.LitObjectsCount--;
        }

        #endregion

        /// <summary>
        /// Since this is not a Mono Behaviour, this must be manually called. If you use VertexLightingSceneHandler, this will be
        /// automatically called by it, however, if you have a Game Loop and want to adjust the calling order or have this being
        /// updated by certain script of yours, or even pause its execution for any reason, you can make a custom implementation
        /// to fit your needs. Hint: Unity's Mono Behaviour Update method is more expensive than a direct method call. So if you have
        /// multiple 'Handler' scripts with Update implemented, you will have performance gains by creating a 'GameLoopManager'
        /// Mono Behaviour that will manually call the update of all your other Managers.
        /// </summary>
        public static void Update()
        {
            if (!_data.ShouldUpdate)
                return;
            
            for (int i = 0; i < _data.LitObjectsCount; i++)
            {
                var obj = _data.LitObjects[i];

                if (obj.ColorMultiplier > 0.2f)
                {
                    obj.ColorMultiplier -= Time.deltaTime * 4;

                    if (obj.ColorMultiplier < 0.2f)
                        obj.ColorMultiplier = 0.2f;
                }

                if (obj.IsPaused)
                    continue;

                if (_data.CurrentFrameCount < obj.FrameToUpdate)
                    continue;

                obj.FrameToUpdate = _data.CurrentFrameCount + _data.Config.FrameIntervalBetweenUpdates;

                if (!obj.IgnoreDistanceChecks && obj.ColorMultiplier <= 0.2f && (obj.transform.position - obj.LastUpdatedPosition).sqrMagnitude < _data.MinMoveDistance)
                    continue;

                Paint(obj);

                obj.LastPosition = obj.transform.position;
            }

            _data.CurrentFrameCount++;
        }

        /// <summary>
        /// Can be called to force an object to be painted regardless of anything else. Should be used only when certain object
        /// has been updated dynamically and is already registered but waiting to be repainted (frame delay). Example of usage is
        /// the ModelEquipmentSwitcher, when the user clicks on a different weapon on the Recipes screen, and the model is updated.
        /// </summary>
        /// <param name="obj">Vertex Lit Object.</param>
        public static void ForcePaint(VertexLitObject obj)
        {
            obj.FrameToUpdate = _data.CurrentFrameCount + 10;
            Paint(obj, true);
        }

        /// <summary>
        /// Effectively paints the object vertex colors to the new desired color. Takes into consideration light positions,
        /// checks if new color is too close to old color, checks if character has some kind of abnormal color to apply,
        /// and finally sets all vertex colors to all meshes of this object. If IgnoreProximity is set to true, object will be
        /// painted regardless of any conditions.
        /// </summary>
        /// <param name="obj">The object to paint.</param>
        /// <param name="ignoreProximity">If set to <c>true</c>, forces object to be painted.</param>
        private static void Paint(VertexLitObject obj, bool ignoreProximity = false)
        {
            var color = _data.AmbientColor;
            if (obj.HasAbnormalEffectColor)
                color = obj.AbnormalEffectColor;

            for (var i = 0; i < _data.LightsCount; i++)
            {
                var lightObj = _data.Lights[i];

                var distance = (obj.transform.position - lightObj.Position).sqrMagnitude;
                var radius = lightObj.SqrRadius;

                if (distance > radius)
                    continue;

                var result = 1 - (distance / radius);
                color = Color32.Lerp(color, lightObj.Color, result);
            }

            if (!obj.IgnoreColorProximityChecks && !ignoreProximity && Mathf.Abs(color.r - obj.LastColor.r) < _data.ProximityLevel && Mathf.Abs(color.g - obj.LastColor.g) < _data.ProximityLevel && Mathf.Abs(color.b - obj.LastColor.b) < _data.ProximityLevel && obj.ColorMultiplier <= 0.2f)
                return;

            color.r = (byte)(color.r * obj.ColorMultiplier);
            color.g = (byte)(color.g * obj.ColorMultiplier);
            color.b = (byte)(color.b * obj.ColorMultiplier);

            obj.LastColor = color;
            obj.LastUpdatedPosition = obj.transform.position;

            for (var i = 0; i < obj.MeshFiltersLength; i++)
            {
                var objColor = obj.Colors[i];

                for (var j = 0; j < objColor.ColorsLength; j++)
                    objColor.Colors[j] = color;

                if (obj.MeshFilters[i] != null)
                {
                    #if UNTOLD_DEBUG
                    if (obj.MeshFilters[i].mesh.vertices.Length != objColor.Colors.Length)
                        Debug.LogError("THE OBJ HAS NOT BEEN BAKED! [click me]", obj);
                    #endif
                    obj.MeshFilters[i].mesh.colors32 = objColor.Colors;
                }
            }   
        }

        #endregion

    }
}