using UnityEngine;

public class KoreanLabelAttribute : PropertyAttribute
{
    public string Label { get; }

    public KoreanLabelAttribute(string label)
    {
        Label = label;
    }
}
