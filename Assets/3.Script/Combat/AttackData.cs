using UnityEngine;

[CreateAssetMenu(menuName = "2D Combat/Attack Data")]
public class AttackData : ScriptableObject
{
    [Header("Damage")]
    public float damage = 10f;
    public Vector2 knockback = new Vector2(6f, 2f);
    public float hitStopTime = 0.05f;

    [Header("Timing")]
    public float activeTime = 0.12f;
    public float cooldown = 0.25f;
}
