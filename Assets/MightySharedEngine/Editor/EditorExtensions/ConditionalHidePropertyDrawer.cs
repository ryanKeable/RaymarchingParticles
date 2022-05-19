using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ConditionalHideAttribute))]
public sealed class ConditionalHidePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
        bool enabled = GetConditionalHideAttributeResult(condHAtt, property);

        bool wasEnabled = GUI.enabled;
        GUI.enabled = enabled;
        if (!condHAtt.HideInInspector || enabled)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        GUI.enabled = wasEnabled;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
        bool enabled = GetConditionalHideAttributeResult(condHAtt, property);

        if (!condHAtt.HideInInspector || enabled)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
        else
        {
            // The property is not being drawn
            // We want to undo the spacing added before and after the property
            return -EditorGUIUtility.standardVerticalSpacing;
        }
    }

    private bool GetConditionalHideAttributeResult(ConditionalHideAttribute condHAtt, SerializedProperty property)
    {
        bool enabled = false;

        // Get the full relative property path of the sourcefield so we can have nested hiding
        string propertyPath = property.propertyPath; // Returns the property path of the property we want to apply the attribute to
        string conditionPath = propertyPath.Replace(property.name, condHAtt.ConditionalSourceField); // Changes the path to the conditionalsource property path
        SerializedProperty sourcePropertyValue = property.serializedObject.FindProperty(conditionPath);

        if (sourcePropertyValue != null)
        {
            enabled = CheckPropertyType(sourcePropertyValue);
        }

        conditionPath = propertyPath.Replace(property.name, condHAtt.ConditionalSourceField2); // Changes the path to the conditionalsource property path
        SerializedProperty sourcePropertyValue2 = property.serializedObject.FindProperty(conditionPath);

        if (sourcePropertyValue2 != null)
        {
            enabled = enabled && CheckPropertyType(sourcePropertyValue2);
        }

        if (condHAtt.Inverse) enabled = !enabled;

        return enabled;
    }

    private bool CheckPropertyType(SerializedProperty sourcePropertyValue)
    {
        switch (sourcePropertyValue.propertyType)
        {
            case SerializedPropertyType.Boolean:
                return sourcePropertyValue.boolValue;
            case SerializedPropertyType.ObjectReference:
                return sourcePropertyValue.objectReferenceValue != null;
            default:
                Debug.LogError("[ConditionalHidePropertyDrawer] Data type of the property used for conditional hiding [" + sourcePropertyValue.propertyType + "] is currently not supported");
                return true;
        }
    }
}
