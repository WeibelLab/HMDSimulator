using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property |
                AttributeTargets.Class | AttributeTargets.Struct)]
public class ConditionalHideAttribute : PropertyAttribute
{
    //The name of the bool field that will be in control
    public string ConditionalSourceField = "";

    public int enumIndex = -1;
    //TRUE = Hide in inspector / FALSE = Disable in inspector 
    public bool HideInInspector = false;
 
    public ConditionalHideAttribute(string conditionalSourceField, int enumIndex)
    {
        this.ConditionalSourceField = conditionalSourceField;
        this.enumIndex = enumIndex;
        this.HideInInspector = false;
    }
 
    public ConditionalHideAttribute(string conditionalSourceField, int enumIndex, bool hideInInspector)
    {
        this.ConditionalSourceField = conditionalSourceField;
        this.enumIndex = enumIndex;
        this.HideInInspector = hideInInspector;
    }
}
 
[CustomPropertyDrawer(typeof(ConditionalHideAttribute))]
public class ConditionalHidePropertyDrawer : UnityEventDrawer 
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
        bool enabled = GetConditionalHideAttributeResult(condHAtt, property);
 
        bool wasEnabled = GUI.enabled;
        GUI.enabled = enabled;
        if (!condHAtt.HideInInspector || enabled)
        {
            //Debug.Log(property.propertyType);
            if (property.propertyType != SerializedPropertyType.Generic)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            else
            {
                base.OnGUI(position, property, label);
            }
        }
 
        GUI.enabled = wasEnabled;
    }
 
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
        bool enabled = GetConditionalHideAttributeResult(condHAtt, property);
 
        if (!condHAtt.HideInInspector || enabled)
        {
            if (property.propertyType != SerializedPropertyType.Generic)
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
            else
            {
                return base.GetPropertyHeight(property, label);
            }
        }
        else
        {
            return -EditorGUIUtility.standardVerticalSpacing;
        }
    }
 
    private bool GetConditionalHideAttributeResult(ConditionalHideAttribute condHAtt, SerializedProperty property)
    {
        bool enabled = true;
        string propertyPath = property.propertyPath; //returns the property path of the property we want to apply the attribute to
        string conditionPath = propertyPath.Replace(property.name, condHAtt.ConditionalSourceField); //changes the path to the conditionalsource property path
        SerializedProperty sourcePropertyValue = property.serializedObject.FindProperty(conditionPath);
 
        if (sourcePropertyValue != null)
        {
            enabled = (condHAtt.enumIndex == sourcePropertyValue.enumValueIndex);
        }
        else
        {
            Debug.LogWarning("Attempting to use a ConditionalHideAttribute but no matching SourcePropertyValue found in object: " + condHAtt.ConditionalSourceField);
        }
 
        return enabled;
    }
}
