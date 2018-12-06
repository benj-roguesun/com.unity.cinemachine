using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;
using Cinemachine.Utility;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineGroupComposer))]
    internal class CinemachineGroupComposerEditor : CinemachineComposerEditor
    {
        // Specialization
        private CinemachineGroupComposer MyTarget { get { return target as CinemachineGroupComposer; } }
        protected string FieldPath<TValue>(Expression<Func<CinemachineGroupComposer, TValue>> expr)
        {
            return ReflectionHelpers.GetFieldPath(expr);
        }

        protected override List<string> GetExcludedPropertiesInInspector()
        {
            List<string> excluded = base.GetExcludedPropertiesInInspector();
            CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(MyTarget.VirtualCamera);
            bool ortho = brain != null ? brain.OutputCamera.orthographic : false;
            if (ortho)
            {
                excluded.Add(FieldPath(x => x.m_AdjustmentMode));
                excluded.Add(FieldPath(x => x.m_MinimumFOV));
                excluded.Add(FieldPath(x => x.m_MaximumFOV));
                excluded.Add(FieldPath(x => x.m_MaxDollyIn));
                excluded.Add(FieldPath(x => x.m_MaxDollyOut));
                excluded.Add(FieldPath(x => x.m_MinimumDistance));
                excluded.Add(FieldPath(x => x.m_MaximumDistance));
            }
            else
            {
                excluded.Add(FieldPath(x => x.m_MinimumOrthoSize));
                excluded.Add(FieldPath(x => x.m_MaximumOrthoSize));
                switch (MyTarget.m_AdjustmentMode)
                {
                    case CinemachineGroupComposer.AdjustmentMode.DollyOnly:
                        excluded.Add(FieldPath(x => x.m_MinimumFOV));
                        excluded.Add(FieldPath(x => x.m_MaximumFOV));
                        break;
                    case CinemachineGroupComposer.AdjustmentMode.ZoomOnly:
                        excluded.Add(FieldPath(x => x.m_MaxDollyIn));
                        excluded.Add(FieldPath(x => x.m_MaxDollyOut));
                        excluded.Add(FieldPath(x => x.m_MinimumDistance));
                        excluded.Add(FieldPath(x => x.m_MaximumDistance));
                        break;
                    default:
                        break;
                }
            }
            return excluded;
        }

        public override void OnInspectorGUI()
        {
            if (MyTarget.IsValid && MyTarget.LookAtTargetGroup == null)
                EditorGUILayout.HelpBox(
                    "The Framing settings will be ignored because the LookAt target is not a kind of CinemachineTargetGroup", 
                    MessageType.Info);

            base.OnInspectorGUI();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineGroupComposer))]
        private static void DrawGroupComposerGizmos(CinemachineGroupComposer target, GizmoType selectionType)
        {
            // Show the group bounding box, as viewed from the camera position
            if (target.LookAtTargetGroup != null)
            {
                Matrix4x4 m = Gizmos.matrix;
                Bounds b = target.LastBounds;
                Gizmos.matrix = target.LastBoundsMatrix;
                Gizmos.color = Color.yellow;

                if (target.VcamState.Lens.Orthographic)
                    Gizmos.DrawWireCube(b.center, b.size);
                else
                {
#if true // until Gizmos.DrawFrustum gets fixed properly
                    float z = b.center.z;
                    float e = b.extents.z;
                    if (z > e)
                    {
                        Bounds b0 = b;
                        b0.extents = Vector2.Lerp(Vector2.zero, b0.extents, (z - e) / z);
                        b0.center -= new Vector3(0, 0, e);
                        Bounds b1 = b;
                        b1.extents = Vector2.LerpUnclamped(Vector2.zero, b1.extents, (z + e) / z);
                        b1.center += new Vector3(0, 0, e);
                        Gizmos.DrawWireCube(b0.center, b0.size);
                        Gizmos.DrawWireCube(b1.center, b1.size);

                        Vector3 e0 = b0.extents;
                        Vector3 e1 = b1.extents;
                        Gizmos.DrawLine(b0.center - e0, b1.center - e1);
                        Gizmos.DrawLine(b0.center + e0, b1.center + e1);
                        e0.y = -e0.y; 
                        e1.y = -e1.y; 
                        Gizmos.DrawLine(b0.center - e0, b1.center - e1);
                        Gizmos.DrawLine(b0.center + e0, b1.center + e1);
                    }

#else
                    float z = b.center.z;
                    Vector3 e = b.extents;
                    Gizmos.DrawFrustum(
                        new Vector3(0, 0, z - e.z), 
                        Mathf.Atan2(e.y, z - e.z) * Mathf.Rad2Deg * 2, 
                        z + e.z, z - e.z, e.x / e.y);
#endif
                }
                Gizmos.matrix = m;
            }
        }
    }
}
