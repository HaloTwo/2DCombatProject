using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(KoreanLabelAttribute))]
public class KoreanLabelDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        KoreanLabelAttribute koreanLabel = (KoreanLabelAttribute)attribute;
        EditorGUI.PropertyField(position, property, new GUIContent(koreanLabel.Label), true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
