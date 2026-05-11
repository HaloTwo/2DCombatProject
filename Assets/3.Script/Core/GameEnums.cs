public enum Team
{
    Player,
    Enemy
}

public enum GameState
{
    Ready,
    Playing,
    Paused,
    Clear,
    GameOver
}

public enum EnemyState
{
    Idle,
    Chase,
    Attack,
    Hit,
    Dead
}

public enum SkillType
{
    DashAttack,
    Projectile,
    RisingSlash,
    GroundSlam,
}

public enum BuffItemType
{
    MoveSpeed,
    AttackPower,
    FocusGauge,
    Invincible
}

public enum BuffPickupType
{
    Heal,
    FocusCharge,
    SpeedBoost,
    Invincible
}

public enum BGMType
{
    Menu,
    Combat,
    Focus
}

public enum SFXType
{
    Attack,
    Dash,
    Skill,
    Projectile,
    Slam,
    Hit,
    PlayerHit,
    Guard,
    Parry,
    Buff,
    Break,
    Clear,
    UI
}
