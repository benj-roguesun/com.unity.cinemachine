using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(TargetTracking.TrackerSettings))]
    internal sealed class TrackerSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            TargetTracking.TrackerSettings def = new();

            var ux = new VisualElement();

            var modeProp = property.FindPropertyRelative(() => def.BindingMode);
            ux.Add(new PropertyField(modeProp));

            var rotDampingContainer = ux.AddChild(new VisualElement());
            var rotModeProp = property.FindPropertyRelative(() => def.AngularDampingMode);
            rotDampingContainer.Add(new PropertyField(rotModeProp));
            var rotDampingField = rotDampingContainer.AddChild(
                new PropertyField(property.FindPropertyRelative(() => def.RotationDamping)));
            var quatDampingField = rotDampingContainer.AddChild(
                new PropertyField(property.FindPropertyRelative(() => def.QuaternionDamping)));

            ux.Add(new PropertyField(property.FindPropertyRelative(() => def.PositionDamping)));

            TrackBindingMode(modeProp);
            ux.TrackPropertyValue(modeProp, TrackBindingMode);
            void TrackBindingMode(SerializedProperty modeProp)
            {
                var mode = (TargetTracking.BindingMode)modeProp.intValue;
                bool hideRot = mode == TargetTracking.BindingMode.WorldSpace 
                    || mode == TargetTracking.BindingMode.SimpleFollowWithWorldUp;
                rotDampingContainer.SetVisible(!hideRot);
            }

            TrackRotDampingMode(rotModeProp);
            ux.TrackPropertyValue(rotModeProp, TrackRotDampingMode);
            void TrackRotDampingMode(SerializedProperty modeProp)
            {
                var mode = (TargetTracking.AngularDampingMode)modeProp.intValue;
                quatDampingField.SetVisible(mode == TargetTracking.AngularDampingMode.Quaternion);
                rotDampingField.SetVisible(mode == TargetTracking.AngularDampingMode.Euler);
            }
            return ux;
        }
    }
}
