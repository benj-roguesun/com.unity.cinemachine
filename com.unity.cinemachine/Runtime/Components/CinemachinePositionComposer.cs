using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// This is a Cinemachine Component in the Body section of the component pipeline.
    /// Its job is to position the camera in a fixed screen-space relationship to
    /// the camera's Tracking target object, with offsets and damping.
    ///
    /// The camera will be first moved along the camera Z axis until the target
    /// is at the desired distance from the camera's X-Y plane.  The camera will then
    /// be moved in its XY plane until the target is at the desired point on
    /// the camera's screen.
    ///
    /// The Position Composer will only change the camera's position in space.  It will not
    /// re-orient or otherwise aim the camera.
    ///
    /// For this component to work properly, the camera's tracking target must not be null.
    /// The tracking target will define what the camera is looking at.
    /// </summary>
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    [CameraPipeline(CinemachineCore.Stage.Body)]
    public class CinemachinePositionComposer : CinemachineComponentBase
        , CinemachineFreeLookModifier.IModifiablePositionDamping
        , CinemachineFreeLookModifier.IModifiableDistance
        , CinemachineFreeLookModifier.IModifiableComposition
    {
        /// <summary>
        /// Offset from the target object (in target-local co-ordinates).  The camera will attempt to
        /// frame the point which is the target's position plus this offset.  Use it to correct for
        /// cases when the target's origin is not the point of interest for the camera.
        /// </summary>
        [Tooltip("Offset from the target object (in target-local co-ordinates).  "
            + "The camera will attempt to frame the point which is the target's position plus "
            + "this offset.  Use it to correct for cases when the target's origin is not the "
            + "point of interest for the camera.")]
        public Vector3 TrackedObjectOffset;

        /// <summary>This setting will instruct the composer to adjust its target offset based
        /// on the motion of the target.  The composer will look at a point where it estimates
        /// the target will be a little into the future.</summary>
        [FoldoutWithEnabledButton]
        public LookaheadSettings Lookahead;

        /// <summary>The distance along the camera axis that will be maintained from the target</summary>
        [Tooltip("The distance along the camera axis that will be maintained from the target")]
        public float CameraDistance = 10f;

        /// <summary>The camera will not move along its z-axis if the target is within
        /// this distance of the specified camera distance</summary>
        [Tooltip("The camera will not move along its z-axis if the target is within "
            + "this distance of the specified camera distance")]
        public float DeadZoneDepth = 0;
        
        /// <summary>How aggressively the camera tries to follow the target in screen space.
        /// Small numbers are more responsive, rapidly orienting the camera to keep the target in
        /// the dead zone. Larger numbers give a more heavy slowly responding camera.
        /// Using different vertical and horizontal settings can yield a wide range of camera behaviors.</summary>
        [Tooltip("How aggressively the camera tries to follow the target in the screen space. "
            + "Small numbers are more responsive, rapidly orienting the camera to keep the target in "
            + "the dead zone. Larger numbers give a more heavy slowly responding camera. Using different "
            + "vertical and horizontal settings can yield a wide range of camera behaviors.")]
        public Vector3 Damping;

        /// <summary>Settings for screen-space composition</summary>
        [HideFoldout]
        public ScreenComposerSettings Composition = new ScreenComposerSettings { SoftZoneSize = new Vector2(0.8f, 0.8f) };

        /// <summary>If enabled, then then soft zone will be unlimited in size</summary>
        [Tooltip("If enabled, then then soft zone will be unlimited in size.")]
        public bool UnlimitedSoftZone = false;

        /// <summary>Force target to center of screen when this camera activates.  
        /// If false, will clamp target to the edges of the dead zone</summary>
        [Tooltip("Force target to center of screen when this camera activates.  If false, will "
            + "clamp target to the edges of the dead zone")]
        public bool CenterOnActivate = true;

        const float kMinimumCameraDistance = 0.01f;

        /// <summary>State information for damping</summary>
        Vector3 m_PreviousCameraPosition = Vector3.zero;
        internal PositionPredictor m_Predictor = new PositionPredictor();
        float m_prevFOV; // State for frame damping
        Quaternion m_prevRotation;

        bool m_InheritingPosition;

        void Reset()
        {
            TrackedObjectOffset = Vector3.zero;
            Lookahead = new LookaheadSettings();
            Damping = Vector3.one;
            CameraDistance = 10;
            Composition = new ScreenComposerSettings { SoftZoneSize = new Vector2(0.8f, 0.8f) };
            DeadZoneDepth = 0;
            UnlimitedSoftZone = false;
            CenterOnActivate = true;
        }

        void OnValidate()
        {
            Damping.x = Mathf.Max(0, Damping.x);
            Damping.y = Mathf.Max(0, Damping.y);
            Damping.z = Mathf.Max(0, Damping.z);
            CameraDistance = Mathf.Max(kMinimumCameraDistance, CameraDistance);
            DeadZoneDepth = Mathf.Max(0, DeadZoneDepth);
            Composition.Validate();
        }
        
        ScreenComposerSettings CinemachineFreeLookModifier.IModifiableComposition.Composition
        {
            get => Composition;
            set => Composition = value;
        }
        
        Vector3 CinemachineFreeLookModifier.IModifiablePositionDamping.PositionDamping
        {
            get => Damping;
            set => Damping = value;
        }

        float CinemachineFreeLookModifier.IModifiableDistance.Distance
        {
            get => CameraDistance;
            set => CameraDistance = value;
        }
        
        /// <summary>Internal API for the inspector editor</summary>
        internal Rect SoftGuideRect
        {
            get => new Rect(
                    Composition.ScreenPosition - Composition.DeadZoneSize / 2 + new Vector2(0.5f, 0.5f),
                    Composition.DeadZoneSize);
            set
            {
                Composition.DeadZoneSize = new Vector2(Mathf.Clamp(value.width, 0, 2), Mathf.Clamp(value.height, 0, 2));
                Composition.ScreenPosition = new Vector2(
                    Mathf.Clamp(value.x - 0.5f + Composition.DeadZoneSize.x / 2, -1.5f,  1.5f), 
                    Mathf.Clamp(value.y - 0.5f + Composition.DeadZoneSize.y / 2, -1.5f,  1.5f));
                Composition.SoftZoneSize = new Vector2(
                    Mathf.Max(Composition.SoftZoneSize.x, Composition.DeadZoneSize.x),
                    Mathf.Max(Composition.SoftZoneSize.y, Composition.DeadZoneSize.y));
            }
        }

        /// <summary>Internal API for the inspector editor</summary>
        internal Rect HardGuideRect
        {
            get
            {
                Rect r = new Rect(
                    Composition.ScreenPosition - Composition.SoftZoneSize / 2 + new Vector2(0.5f, 0.5f),
                    Composition.SoftZoneSize);
                r.position += new Vector2(
                    Composition.Bias.x * (Composition.SoftZoneSize.x - Composition.DeadZoneSize.x),
                    Composition.Bias.y * (Composition.SoftZoneSize.y - Composition.DeadZoneSize.y));
                return r;
            }
            set
            {
                Composition.SoftZoneSize.x = Mathf.Clamp(value.width, 0, 2f);
                Composition.SoftZoneSize.y = Mathf.Clamp(value.height, 0, 2f);
                Composition.DeadZoneSize.x = Mathf.Min(Composition.DeadZoneSize.x, Composition.SoftZoneSize.x);
                Composition.DeadZoneSize.y = Mathf.Min(Composition.DeadZoneSize.y, Composition.SoftZoneSize.y);
            }
        }

        /// <summary>True if component is enabled and has a valid Follow target</summary>
        public override bool IsValid { get { return enabled && FollowTarget != null; } }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Body stage</summary>
        public override CinemachineCore.Stage Stage { get { return CinemachineCore.Stage.Body; } }

        /// <summary>FramingTransposer's algorithm tahes camera orientation as input, 
        /// so even though it is a Body component, it must apply after Aim</summary>
        public override bool BodyAppliesAfterAim { get { return true; } }

        /// <summary>Internal API for inspector</summary>
        internal Vector3 TrackedPoint { get; private set; }

        /// <summary>This is called to notify the user that a target got warped,
        /// so that we can update its internal state to make the camera
        /// also warp seamlessly.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            base.OnTargetObjectWarped(target, positionDelta);
            if (target == FollowTarget)
            {
                m_PreviousCameraPosition += positionDelta;
                m_Predictor.ApplyTransformDelta(positionDelta);
            }
        }

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation
        /// </summary>
        /// <param name="pos">Worldspace position to take</param>
        /// <param name="rot">Worldspace orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            base.ForceCameraPosition(pos, rot);
            m_PreviousCameraPosition = pos;
            m_prevRotation = rot;
        }
        
        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => Mathf.Max(Damping.x, Mathf.Max(Damping.y, Damping.z)); 

        /// <summary>Notification that this virtual camera is going live.
        /// Base class implementation does nothing.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <param name="transitionParams">Transition settings for this vcam</param>
        /// <returns>True if the vcam should do an internal update as a result of this call</returns>
        public override bool OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime,
            ref CinemachineVirtualCameraBase.TransitionParams transitionParams)
        {
            if (fromCam != null && transitionParams.InheritPosition
                 && !CinemachineCore.Instance.IsLiveInBlend(VirtualCamera))
            {
                m_PreviousCameraPosition = fromCam.State.RawPosition;
                m_prevRotation = fromCam.State.RawOrientation;
                m_InheritingPosition = true;
                return true;
            }
            return false;
        }

        // Convert from screen coords to normalized orthographic distance coords
        private Rect ScreenToOrtho(Rect rScreen, float orthoSize, float aspect)
        {
            Rect r = new Rect();
            r.yMax = 2 * orthoSize * ((1f-rScreen.yMin) - 0.5f);
            r.yMin = 2 * orthoSize * ((1f-rScreen.yMax) - 0.5f);
            r.xMin = 2 * orthoSize * aspect * (rScreen.xMin - 0.5f);
            r.xMax = 2 * orthoSize * aspect * (rScreen.xMax - 0.5f);
            return r;
        }

        private Vector3 OrthoOffsetToScreenBounds(Vector3 targetPos2D, Rect screenRect)
        {
            // Bring it to the edge of screenRect, if outside.  Leave it alone if inside.
            Vector3 delta = Vector3.zero;
            if (targetPos2D.x < screenRect.xMin)
                delta.x += targetPos2D.x - screenRect.xMin;
            if (targetPos2D.x > screenRect.xMax)
                delta.x += targetPos2D.x - screenRect.xMax;
            if (targetPos2D.y < screenRect.yMin)
                delta.y += targetPos2D.y - screenRect.yMin;
            if (targetPos2D.y > screenRect.yMax)
                delta.y += targetPos2D.y - screenRect.yMax;
            return delta;
        }

        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for damping.  If less than 0, no damping is done.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            LensSettings lens = curState.Lens;
            Vector3 followTargetPosition = FollowTargetPosition + (FollowTargetRotation * TrackedObjectOffset);
            bool previousStateIsValid = deltaTime >= 0 && VirtualCamera.PreviousStateIsValid;
            if (!previousStateIsValid || VirtualCamera.FollowTargetChanged)
                m_Predictor.Reset();
            if (!previousStateIsValid)
            {
                m_PreviousCameraPosition = curState.RawPosition;
                m_prevFOV = lens.Orthographic ? lens.OrthographicSize : lens.FieldOfView;
                m_prevRotation = curState.RawOrientation;
                if (!m_InheritingPosition && CenterOnActivate)
                {
                    m_PreviousCameraPosition = FollowTargetPosition
                        + (curState.RawOrientation * Vector3.back) * CameraDistance;
                }
            }
            if (!IsValid)
            {
                m_InheritingPosition = false;
                return;
            }

            var verticalFOV = lens.FieldOfView;

            TrackedPoint = followTargetPosition;
            if (Lookahead.Enabled && Lookahead.Time > Epsilon)
            {
                m_Predictor.Smoothing = Lookahead.Smoothing;
                m_Predictor.AddPosition(followTargetPosition, deltaTime, Lookahead.Time);
                var delta = m_Predictor.PredictPositionDelta(Lookahead.Time);
                if (Lookahead.IgnoreY)
                    delta = delta.ProjectOntoPlane(curState.ReferenceUp);
                var p = followTargetPosition + delta;
                TrackedPoint = p;
            }

            if (!curState.HasLookAt())
                curState.ReferenceLookAt = followTargetPosition;

            // Allow undamped camera orientation change
            Quaternion localToWorld = curState.RawOrientation;
            if (previousStateIsValid)
            {
                var q = localToWorld * Quaternion.Inverse(m_prevRotation);
                m_PreviousCameraPosition = TrackedPoint + q * (m_PreviousCameraPosition - TrackedPoint);
            }
            m_prevRotation = localToWorld;

            // Work in camera-local space
            Vector3 camPosWorld = m_PreviousCameraPosition;
            Quaternion worldToLocal = Quaternion.Inverse(localToWorld);
            Vector3 cameraPos = worldToLocal * camPosWorld;
            Vector3 targetPos = (worldToLocal * TrackedPoint) - cameraPos;
            Vector3 lookAtPos = targetPos;

            // Move along camera z
            Vector3 cameraOffset = Vector3.zero;
            float cameraMin = Mathf.Max(kMinimumCameraDistance, CameraDistance - DeadZoneDepth/2);
            float cameraMax = Mathf.Max(cameraMin, CameraDistance + DeadZoneDepth/2);
            float targetZ = Mathf.Min(targetPos.z, lookAtPos.z);
            if (targetZ < cameraMin)
                cameraOffset.z = targetZ - cameraMin;
            if (targetZ > cameraMax)
                cameraOffset.z = targetZ - cameraMax;

            // Move along the XY plane
            float screenSize = lens.Orthographic 
                ? lens.OrthographicSize 
                : Mathf.Tan(0.5f * verticalFOV * Mathf.Deg2Rad) * (targetZ - cameraOffset.z);
            Rect softGuideOrtho = ScreenToOrtho(SoftGuideRect, screenSize, lens.Aspect);
            if (!previousStateIsValid)
            {
                // No damping or hard bounds, just snap to central bounds, skipping the soft zone
                Rect rect = softGuideOrtho;
                if (CenterOnActivate && !m_InheritingPosition)
                    rect = new Rect(rect.center, Vector2.zero); // Force to center
                cameraOffset += OrthoOffsetToScreenBounds(targetPos, rect);
            }
            else
            {
                // Move it through the soft zone, with damping
                cameraOffset += OrthoOffsetToScreenBounds(targetPos, softGuideOrtho);
                cameraOffset = VirtualCamera.DetachedFollowTargetDamp(cameraOffset, Damping, deltaTime);

                // Make sure the real target (not the lookahead one) is still in the frame
                if (!UnlimitedSoftZone 
                    && (deltaTime < 0 || VirtualCamera.FollowTargetAttachment > 1 - Epsilon))
                {
                    Rect hardGuideOrtho = ScreenToOrtho(HardGuideRect, screenSize, lens.Aspect);
                    var realTargetPos = (worldToLocal * followTargetPosition) - cameraPos;
                    cameraOffset += OrthoOffsetToScreenBounds(
                        realTargetPos - cameraOffset, hardGuideOrtho);
                }
            }
            curState.RawPosition = localToWorld * (cameraPos + cameraOffset);
            m_PreviousCameraPosition = curState.RawPosition;

            m_InheritingPosition = false;
        }
    }
}
