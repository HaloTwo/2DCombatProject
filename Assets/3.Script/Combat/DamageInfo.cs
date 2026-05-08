using UnityEngine;

public readonly struct DamageInfo
{
    public readonly Team AttackerTeam;
    public readonly float Damage;
    public readonly Vector2 HitPoint;
    public readonly Vector2 Knockback;
    public readonly float HitStopTime;

    public DamageInfo(Team attackerTeam, float damage, Vector2 hitPoint, Vector2 knockback, float hitStopTime)
    {
        AttackerTeam = attackerTeam;
        Damage = damage;
        HitPoint = hitPoint;
        Knockback = knockback;
        HitStopTime = hitStopTime;
    }
}
