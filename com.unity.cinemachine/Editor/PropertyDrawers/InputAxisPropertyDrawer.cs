using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(InputAxis))]
    internal sealed class InputAxisWithNamePropertyDrawer : PropertyDrawer
    {
        InputAxis def = new InputAxis(); // to access name strings

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            rect.height = height;

            property.isExpanded = EditorGUI.Foldout(
                new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth - 2 * height, rect.height),
                property.isExpanded, label, true);

            if (property.isExpanded)
            {
                ++EditorGUI.indentLevel;

                rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Value));

                var flags = property.FindPropertyRelative(() => def.Restrictions).intValue;

                var enabled = GUI.enabled;
                GUI.enabled = (flags & (int)InputAxis.RestrictionFlags.RangeIsDriven) == 0;

                rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Center));

                rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                InspectorUtility.MultiPropertyOnLine(
                    rect, null,
                    new [] {
                            property.FindPropertyRelative(() => def.Range),
                            property.FindPropertyRelative(() => def.Wrap)}, 
                    new [] { GUIContent.none, null });

                GUI.enabled = enabled;
                --EditorGUI.indentLevel;
            }
            else
            {
                // Draw the input value on the same line as the foldout, for convenience
                var valueProp = property.FindPropertyRelative(() => def.Value);

                int oldIndent = EditorGUI.indentLevel;
                float oldLabelWidth = EditorGUIUtility.labelWidth;

                rect.x += EditorGUIUtility.labelWidth - 2 * EditorGUIUtility.singleLineHeight;
                rect.width -= EditorGUIUtility.labelWidth - 2 * EditorGUIUtility.singleLineHeight;

                EditorGUI.indentLevel = 0;
                EditorGUIUtility.labelWidth = 2 * EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(rect, valueProp, new GUIContent(" ", valueProp.tooltip));
                EditorGUI.indentLevel = oldIndent;
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var height = lineHeight;
            if (property != null && property.isExpanded)
                height += 3 * lineHeight;
            return height - EditorGUIUtility.standardVerticalSpacing;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // When foldout is closed, we display the axis value on the same line, for convenience
            var foldout = new Foldout { text = property.displayName, tooltip = property.tooltip, value = property.isExpanded };
            foldout.RegisterValueChangedCallback((evt) => 
            {
                property.isExpanded = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
                evt.StopPropagation();
            });
            var valueProp = property.FindPropertyRelative(() => def.Value);
            var valueLabel = new Label(" ") { style = { minWidth = InspectorUtility.SingleLineHeight * 2}};
            var valueField =  new InspectorUtility.CompactPropertyField(valueProp, "") { style = { flexGrow = 1}};
            valueLabel.AddPropertyDragger(valueProp, valueField);

            var ux = new InspectorUtility.FoldoutWithOverlay(foldout, valueField, valueLabel);

            foldout.Add(new PropertyField(valueProp));
            var centerField = foldout.AddChild(new PropertyField(property.FindPropertyRelative(() => def.Center)));
            var rangeContainer = foldout.AddChild(new VisualElement() { style = { flexDirection = FlexDirection.Row }});
            rangeContainer.Add(new PropertyField(property.FindPropertyRelative(() => def.Range)) { style = { flexGrow = 1 }});
            var wrapProp = property.FindPropertyRelative(() => def.Wrap);
            rangeContainer.Add(new PropertyField(wrapProp, "") 
                { style = { alignSelf = Align.Center, marginLeft = 5, marginRight = 5 }});
            rangeContainer.Add(new Label(wrapProp.displayName) 
                { tooltip = wrapProp.tooltip, style = { alignSelf = Align.Center }});

            var flagsProp = property.FindPropertyRelative(() => def.Restrictions);
            TrackFlags(flagsProp);
            ux.TrackPropertyValue(flagsProp, TrackFlags);

            void TrackFlags(SerializedProperty prop)
            {
                var flags = prop.intValue;
                var rangeDisabled = (flags & (int)InputAxis.RestrictionFlags.RangeIsDriven) != 0;
                centerField.SetEnabled(!rangeDisabled);
                rangeContainer.SetEnabled(!rangeDisabled);
            }

            return ux;
        }
    }
}
