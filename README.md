# 2DCombatProject

Unity PC 2D platform action combat prototype for a client developer assignment.

## Reference Direction

- `Cuphead`: 2D movement, jump, dash, projectile, hit reaction timing.
- `WildTamer`: combat actor separation, enemy brain structure, object pooling, visual feedback.

The current project does not copy those projects directly. It keeps the same portfolio-style folder convention and rebuilds the combat prototype with a smaller assignment-focused structure.

## Folder Structure

```text
Assets/
  1.Scene/          Main gameplay scenes
  2.Model/          Model or imported character source assets
  3.Script/
    Core/           Game state and global flow
    Input/          Player input reading
    Player/         Movement, attack, skill control
    Combat/         Health, hitbox, hurtbox, damage data, feedback
    Skill/          Skill data, player skill execution, projectile
    Enemy/          Idle -> Chase -> Attack enemy AI
    Wave/           Wave data, spawn points, wave manager
    Camera/         Camera feedback
    UI/             HUD and result UI
    Utility/        Object pool and shared helpers
  4.Sprite/         2D sprites
  5.Animation/      Animator controllers and clips
  6.Materials/      Materials
  7.Prefab/         Player, enemy, projectile, manager prefabs
  8.Audio/          SFX/BGM
  9.SO/             ScriptableObject data assets
```

## First Playable Setup

1. Create `AttackData` assets in `Assets/9.SO`.
2. Create `SkillData` assets for at least two skills:
   - DashAttack
   - AreaAttack
3. Player prefab components:
   - `Rigidbody2D`
   - `PlayerInputReader`
   - `PlayerMovement2D`
   - `PlayerCombat`
   - `PlayerSkillController`
   - `Health`
   - `Hurtbox`
   - `CombatFeedback`
4. Enemy prefabs:
   - `MeleeChargerEnemy`
   - `RangedShooterEnemy`
   - `Health`
   - `Hurtbox`
   - `CombatFeedback`
5. Scene managers:
   - `GameManager`
   - `ObjectPool`
   - `WaveManager`

## Assignment Scope

Implemented structure targets:

- Player movement, jump, dash
- Basic melee attack
- Minimum two skill slots
- Melee charger enemy
- Ranged projectile enemy
- Wave clear flow
- Hit flash, knockback, hit stop, camera shake hook
