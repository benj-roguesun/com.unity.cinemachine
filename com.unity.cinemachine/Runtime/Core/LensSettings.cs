using UnityEngine;
using System;

#if CINEMACHINE_HDRP || CINEMACHINE_LWRP_7_3_1
    #if CINEMACHINE_HDRP_7_3_1
    using UnityEngine.Rendering.HighDefinition;
    #else
        #if CINEMACHINE_LWRP_7_3_1
        using UnityEngine.Rendering.Universal;
        #else
        using UnityEngine.Experimental.Rendering.HDPipeline;
        #endif
    #endif
#endif

namespace Cinemachine
{
    /// <summary>
    /// Describes the FOV and clip planes for a camera.  This generally mirrors the Unity Camera's
    /// lens settings, and will be used to drive the Unity camera when the vcam is active.
    /// </summary>
    [Serializable]
    public struct LensSettings
    {
        /// <summary>Default Lens Settings</summary>
        public static LensSettings Default = new LensSettings(40f, 10f, 0.1f, 5000f, 0);

        /// <summary>
        /// This is the camera view in degrees. For cinematic people, a 50mm lens
        /// on a super-35mm sensor would equal a 19.6 degree FOV
        /// </summary>
        [RangeSlider(1f, 179f)]
        [Tooltip("This is the camera view in degrees. Display will be in vertical degress, unless the "
            + "associated camera has its FOV axis setting set to Horizontal, in which case display will "
            + "be in horizontal degress.  Internally, it is always vertical degrees.  "
            + "For cinematic people, a 50mm lens on a super-35mm sensor would equal a 19.6 degree FOV")]
        public float FieldOfView;

        /// <summary>
        /// When using an orthographic camera, this defines the half-height, in world
        /// co-ordinates, of the camera view.
        /// </summary>
        [Tooltip("When using an orthographic camera, this defines the half-height, in world "
            + "coordinates, of the camera view.")]
        public float OrthographicSize;

        /// <summary>
        /// The near clip plane for this LensSettings
        /// </summary>
        [Tooltip("This defines the near region in the renderable range of the camera frustum. "
            + "Raising this value will stop the game from drawing things near the camera, which "
            + "can sometimes come in handy.  Larger values will also increase your shadow resolution.")]
        public float NearClipPlane;

        /// <summary>
        /// The far clip plane for this LensSettings
        /// </summary>
        [Tooltip("This defines the far region of the renderable range of the camera frustum. Typically "
            + "you want to set this value as low as possible without cutting off desired distant objects")]
        public float FarClipPlane;

        /// <summary>
        /// The dutch (tilt) to be applied to the camera. In degrees
        /// </summary>
        [Tooltip("Camera Z roll, or tilt, in degrees.")]
        public float Dutch;

        /// <summary>
        /// This enum controls how the Camera seetings are driven.  Some settings
        /// can be pulled from the main camera, or pushed to it, depending on these values.
        /// </summary>
        public enum OverrideModes
        {
            /// <summary> Perspective/Ortho, IsPhysical 
            /// will not be changed in Unity Camera.  This is the default setting.</summary>
            None = 0,
            /// <summary>Orthographic projection mode will be pushed to the Unity Camera</summary>
            Orthographic,
            /// <summary>Perspective projection mode will be pushed to the Unity Camera</summary>
            Perspective,
            /// <summary>A physically-modeled Perspective projection type will be pushed 
            /// to the Unity Camera</summary>
            Physical
        }

        /// <summary>
        /// Allows you to select a different camera mode to apply to the Camera component
        /// when Cinemachine activates this Virtual Camera.  The changes applied to the Camera
        /// component through this setting will remain after the Virtual Camera deactivation.
        /// </summary>
        [Tooltip("Allows you to select a different camera mode to apply to the Camera component "
            + "when Cinemachine activates this Virtual Camera.  The changes applied to the Camera "
            + "component through this setting will remain after the Virtual Camera deactivation.")]
        public OverrideModes ModeOverride;

        /// <summary>
        /// This is set every frame by the virtual camera, based on the value found in the
        /// currently associated Unity camera.
        /// Do not set this property.  Instead, use the ModeOverride field to set orthographic mode.
        /// </summary>
        public bool Orthographic 
        { 
            get => ModeOverride == OverrideModes.Orthographic || ModeOverride == OverrideModes.None && m_OrthoFromCamera;

            /// Obsolete: do not use
            private set { m_OrthoFromCamera = value; ModeOverride = value 
                ? OverrideModes.Orthographic : OverrideModes.Perspective; } 
        }

        /// <summary>
        /// For physical cameras, this is the actual size of the image sensor (in mm); it is used to 
        /// convert between focal length and field of view.  For nonphysical cameras, it is the aspect ratio.
        /// </summary>
        public Vector2 SensorSize
        { 
            get { return m_SensorSize; } 
            set { m_SensorSize = value; } 
        }

        /// <summary>
        /// Sensor aspect, not screen aspect.  For nonphysical cameras, this is the same thing.
        /// </summary>
        public float Aspect { get { return SensorSize.y == 0 ? 1f : (SensorSize.x / SensorSize.y); } }

        /// <summary>
        /// This property will be true if the camera mode is set, either directly or 
        /// indirectly, to Physical Camera
        /// Do not set this property.  Instead, use the ModeOverride field to set physical mode.
        /// </summary>
        public bool IsPhysicalCamera 
        { 
            get { return ModeOverride == OverrideModes.Physical 
                || ModeOverride == OverrideModes.None && m_PhysicalFromCamera; } 

            /// Obsolete: do not use
            set { m_PhysicalFromCamera = value; ModeOverride = value 
                ? OverrideModes.Physical : OverrideModes.Perspective; } 
        }

#if UNITY_EDITOR
        internal bool UseHorizontalFOV { get; private set; }
#endif

        /// <summary>For physical cameras only: position of the gate relative to 
        /// the film back</summary>
        public Vector2 LensShift;

        /// <summary>For physical cameras only: how the image is fitted to the sensor 
        /// if the aspect ratios differ</summary>
        public Camera.GateFitMode GateFit;

        [SerializeField] Vector2 m_SensorSize;

        bool m_OrthoFromCamera;
        bool m_PhysicalFromCamera;


#if CINEMACHINE_HDRP
        public int Iso;
        public float ShutterSpeed;
        [RangeSlider(Camera.kMinAperture, Camera.kMaxAperture)]
        public float Aperture;
        public float FocusDistance;
        [RangeSlider(Camera.kMinBladeCount, Camera.kMaxBladeCount)]
        public int BladeCount;
        [MinMaxRangeSlider(Camera.kMinAperture, Camera.kMaxAperture)]
        public Vector2 Curvature;
        [RangeSlider(0, 1)]
        public float BarrelClipping;
        [RangeSlider(-1, 1)]
        public float Anamorphism;
#endif

        /// <summary>
        /// Creates a new LensSettings, copying the values from the
        /// supplied Camera
        /// </summary>
        /// <param name="fromCamera">The Camera from which the FoV, near
        /// and far clip planes will be copied.</param>
        /// <returns>The LensSettings as extracted from the supplied Camera</returns>
        public static LensSettings FromCamera(Camera fromCamera)
        {
            LensSettings lens = Default;
            if (fromCamera != null)
            {
                lens.FieldOfView = fromCamera.fieldOfView;
                lens.OrthographicSize = fromCamera.orthographicSize;
                lens.NearClipPlane = fromCamera.nearClipPlane;
                lens.FarClipPlane = fromCamera.farClipPlane;
                lens.LensShift = fromCamera.lensShift;
                lens.GateFit = fromCamera.gateFit;
                lens.SnapshotCameraReadOnlyProperties(fromCamera);

#if CINEMACHINE_HDRP
                if (lens.IsPhysicalCamera)
                {
                    lens.Iso = fromCamera.iso;
                    lens.ShutterSpeed = fromCamera.shutterSpeed;
                    lens.Aperture = fromCamera.aperture;
                    lens.BladeCount = fromCamera.bladeCount;
                    lens.Curvature = fromCamera.curvature;
                    lens.BarrelClipping = fromCamera.barrelClipping;
                    lens.Anamorphism = fromCamera.anamorphism;
                }
#endif
            }
            return lens;
        }

        /// <summary>
        /// Snapshot the properties that are read-only in the Camera
        /// </summary>
        /// <param name="camera">The Camera from which we will take the info</param>
        public void SnapshotCameraReadOnlyProperties(Camera camera)
        {
            m_OrthoFromCamera = false;
            m_PhysicalFromCamera = false;
            UseHorizontalFOV = false;
            if (camera != null && ModeOverride == OverrideModes.None)
            {
                m_OrthoFromCamera = camera.orthographic;
                m_PhysicalFromCamera = camera.usePhysicalProperties;
                m_SensorSize = camera.sensorSize;
                GateFit = camera.gateFit;
            }
            if (IsPhysicalCamera)
            {
                // If uninitialized, do an initial pull from the camera
                if (camera != null && m_SensorSize == Vector2.zero)
                {
                    m_SensorSize = camera.sensorSize;
                    GateFit = camera.gateFit;
                }
            }
            else
            {
                if (camera != null)
                    m_SensorSize = new Vector2(camera.aspect, 1f);
                LensShift = Vector2.zero;
            }
#if UNITY_EDITOR
            // This should really be a global setting, but for now there is no better way than this!
            var p = new UnityEditor.SerializedObject(camera).FindProperty("m_FOVAxisMode");
            UseHorizontalFOV = (p != null && p.intValue == (int)Camera.FieldOfViewAxis.Horizontal);
#endif
        }

        /// <summary>
        /// Snapshot the properties that are read-only in the Camera
        /// </summary>
        /// <param name="lens">The LensSettings from which we will take the info</param>
        public void SnapshotCameraReadOnlyProperties(ref LensSettings lens)
        {
            if (ModeOverride == OverrideModes.None)
            {
                m_OrthoFromCamera = lens.Orthographic;
                m_SensorSize = lens.m_SensorSize;
                m_PhysicalFromCamera = lens.IsPhysicalCamera;
            }
            if (!IsPhysicalCamera)
                LensShift = Vector2.zero;
        }

        /// <summary>
        /// Explicit constructor for this LensSettings
        /// </summary>
        /// <param name="verticalFOV">The Vertical field of view</param>
        /// <param name="orthographicSize">If orthographic, this is the half-height of the screen</param>
        /// <param name="nearClip">The near clip plane</param>
        /// <param name="farClip">The far clip plane</param>
        /// <param name="dutch">Camera roll, in degrees.  This is applied at the end
        /// after shot composition.</param>
        public LensSettings(
            float verticalFOV, float orthographicSize,
            float nearClip, float farClip, float dutch) : this()
        {
            FieldOfView = verticalFOV;
            OrthographicSize = orthographicSize;
            NearClipPlane = nearClip;
            FarClipPlane = farClip;
            Dutch = dutch;
            m_SensorSize = new Vector2(1, 1);
            GateFit = Camera.GateFitMode.Horizontal;

#if CINEMACHINE_HDRP
            Iso = 200;
            ShutterSpeed = 0.005f;
            Aperture = 16;
            BladeCount = 5;
            Curvature = new Vector2(2, 11);
            BarrelClipping = 0.25f;
            Anamorphism = 0;
#endif
        }

        /// <summary>
        /// Linearly blends the fields of two LensSettings and returns the result
        /// </summary>
        /// <param name="lensA">The LensSettings to blend from</param>
        /// <param name="lensB">The LensSettings to blend to</param>
        /// <param name="t">The interpolation value. Internally clamped to the range [0,1]</param>
        /// <returns>Interpolated settings</returns>
        public static LensSettings Lerp(LensSettings lensA, LensSettings lensB, float t)
        {
            t = Mathf.Clamp01(t);
            // non-lerpable settings taken care of here
            if (t < 0.5f)
            {
                var blendedLens = lensA; 
                blendedLens.Lerp(lensB, t);
                return blendedLens;
            }
            else
            {
                var blendedLens = lensB; 
                blendedLens.Lerp(lensA, t);
                return blendedLens;
            }
        }

        /// <summary>
        /// Lerp the lerpable values.  Nonlerpable values remain intact.
        /// </summary>
        /// <param name="lensB">The lens containing the values to compine with this one</param>
        /// <param name="t">The weight of LensB's values.</param>
        public void Lerp(in LensSettings lensB, float t)
        {
            FarClipPlane = Mathf.Lerp(FarClipPlane, lensB.FarClipPlane, t);
            NearClipPlane = Mathf.Lerp(NearClipPlane, lensB.NearClipPlane, t);
            FieldOfView = Mathf.Lerp(FieldOfView, lensB.FieldOfView, t);
            OrthographicSize = Mathf.Lerp(OrthographicSize, lensB.OrthographicSize, t);
            Dutch = Mathf.Lerp(Dutch, lensB.Dutch, t);
            m_SensorSize = Vector2.Lerp(m_SensorSize, lensB.m_SensorSize, t);
            LensShift = Vector2.Lerp(LensShift, lensB.LensShift, t);

#if CINEMACHINE_HDRP
            Iso = Mathf.RoundToInt(Mathf.Lerp((float)Iso, (float)lensB.Iso, t));
            ShutterSpeed = Mathf.Lerp(ShutterSpeed, lensB.ShutterSpeed, t);
            Aperture = Mathf.Lerp(Aperture, lensB.Aperture, t);
            BladeCount = Mathf.RoundToInt(Mathf.Lerp(BladeCount, lensB.BladeCount, t));;
            Curvature = Vector2.Lerp(Curvature, lensB.Curvature, t);
            BarrelClipping = Mathf.Lerp(BarrelClipping, lensB.BarrelClipping, t);
            Anamorphism = Mathf.Lerp(Anamorphism, lensB.Anamorphism, t);
#endif
        }

        /// <summary>Make sure lens settings are sane.  Call this from OnValidate().</summary>
        public void Validate()
        {
            FarClipPlane = Mathf.Max(FarClipPlane, NearClipPlane + 0.001f);
            FieldOfView = Mathf.Clamp(FieldOfView, 0.01f, 179f);
            m_SensorSize.x = Mathf.Max(m_SensorSize.x, 0.1f);
            m_SensorSize.y = Mathf.Max(m_SensorSize.y, 0.1f);
#if CINEMACHINE_HDRP
            ShutterSpeed = Mathf.Max(0, ShutterSpeed);
            Aperture = Mathf.Clamp(Aperture, Camera.kMinAperture, Camera.kMaxAperture);
            BladeCount = Mathf.Clamp(BladeCount, Camera.kMinBladeCount, Camera.kMaxBladeCount);
            BarrelClipping = Mathf.Clamp01(BarrelClipping);
            Curvature.x = Mathf.Clamp(Curvature.x, Camera.kMinAperture, Camera.kMaxAperture);
            Curvature.y = Mathf.Clamp(Curvature.y, Curvature.x, Camera.kMaxAperture);
            Anamorphism = Mathf.Clamp(Anamorphism, -1, 1);
#endif
        }

        /// <summary>
        /// Compare two lens settings objects for approximate equality
        /// </summary>
        /// <param name="a">First LensSettings</param>
        /// <param name="b">Second Lens Settigs</param>
        /// <returns>True if the two lenses are approximately equal</returns>
        public static bool AreEqual(ref LensSettings a, ref LensSettings b)
        {
            return Mathf.Approximately(a.NearClipPlane, b.NearClipPlane)
                && Mathf.Approximately(a.FarClipPlane, b.FarClipPlane)
                && Mathf.Approximately(a.OrthographicSize, b.OrthographicSize)
                && Mathf.Approximately(a.FieldOfView, b.FieldOfView)
                && Mathf.Approximately(a.Dutch, b.Dutch)
                && Mathf.Approximately(a.LensShift.x, b.LensShift.x)
                && Mathf.Approximately(a.LensShift.y, b.LensShift.y)

                && Mathf.Approximately(a.SensorSize.x, b.SensorSize.x)
                && Mathf.Approximately(a.SensorSize.y, b.SensorSize.y)
                && a.GateFit == b.GateFit
#if CINEMACHINE_HDRP
                && Mathf.Approximately(a.Iso, b.Iso)
                && Mathf.Approximately(a.ShutterSpeed, b.ShutterSpeed)
                && Mathf.Approximately(a.Aperture, b.Aperture)
                && a.BladeCount == b.BladeCount
                && Mathf.Approximately(a.Curvature.x, b.Curvature.x)
                && Mathf.Approximately(a.Curvature.y, b.Curvature.y)
                && Mathf.Approximately(a.BarrelClipping, b.BarrelClipping)
                && Mathf.Approximately(a.Anamorphism, b.Anamorphism)
#endif
                ;
        }
    }
}
