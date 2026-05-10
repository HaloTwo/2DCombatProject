# 2DCombatProject

Unity 기반 PC/Steam 타겟 2D 플랫포머 액션 전투 프로토타입입니다.  
111퍼센트 PC 클라이언트 개발자 과제 제출을 목표로, 입력, 이동, 전투, 스킬, 패링, 포커스 모드, 웨이브, 버프, 파괴 오브젝트까지 전투 손맛이 보이도록 구성했습니다.

## 핵심 구현

- New Input System 기반 키보드/게임패드 입력
- 3개 씬 구성: `StartScene`, `LoadingScene`, `GameScene`
- 2D 플랫포머 이동: 좌우 이동, 점프, 2단 점프, 대시, One-Way Platform 하강
- 기본 공격 1타/2타 콤보와 애니메이션 이벤트 기반 Hitbox 타이밍
- 스킬 2슬롯 구조: 쿨다운, 드래그 교체, 클릭 선택 패널, 원형 UI
- 검기/투사체, Ground Slam, Rising Slash, Dash Attack 계열 스킬
- 패링/가드, 투사체 반사, 히트스톱, 카메라 쉐이크
- 포커스 게이지와 포커스 모드: 적/투사체 슬로우, 배경 암전, 시작 충격파
- 적 4종 구조: 근접, 원거리, 비행, 강한 지상형
- WaveManager 기반 웨이브 진행과 Wave Clear / Clear UI
- 파괴 가능한 상자와 버프 아이템
- Haste / Power / Focus 버프와 CombatHUD 원형 버프 타이머
- 콤보 UI, 데미지 텍스트, 적 체력바, 게임오버/재시작 UI

## 조작

| 동작 | 키보드 | 게임패드 |
| --- | --- | --- |
| 이동 | 방향키 | Left Stick / D-Pad |
| 점프 | Space | South Button |
| 대시 / 회피 | Z | East Button |
| 기본 공격 | C | West Button |
| 스킬 1 | A | Left Shoulder |
| 스킬 2 | S | Right Shoulder |
| 패링 / 가드 | X | Left Trigger |
| 상호작용 | ↑ 또는 E | North Button |
| 포커스 모드 | 설정된 Focus 입력 | 설정된 Focus 입력 |

스킬 슬롯은 마우스로 클릭하면 스킬 선택 패널이 열립니다. 슬롯끼리 드래그하면 두 스킬이 교체됩니다.

## 현재 게임 흐름

```text
StartScene
  -> LoadingScene
    -> GameScene
      -> 전투 / 웨이브 / 버프 / 포커스 모드
        -> Game Clear 또는 Game Over
```

<details>
<summary>폴더 구조 보기</summary>

```text
Assets/
  1.Scene/                 StartScene, LoadingScene, GameScene
  2.Model/                 모델/외부 리소스 예비 폴더
  3.Script/
    Camera/                카메라 추적, 카메라 쉐이크
    Combat/                Health, Hitbox, Hurtbox, DamageInfo, CombatFeedback
    Core/                  GameManager, 씬 전환, 포커스 모드
    Enemy/                 EnemyBrainBase, 적 AI, 적 체력바, 적 공격 이벤트
    Input/                 New Input System 입력 래퍼
    Item/                  BuffItem, BuffPickup, 버프 스포너
    Player/                이동, 기본공격, 스킬, 패링, 포커스 충격파, 버프 잔상
    Skill/                 SkillData, SkillType, Projectile
    UI/                    HUD, 스킬 슬롯, 버프 타이머, 콤보, 결과 UI, 웨이브 알림
    Utility/               ObjectPool, TimedAutoRelease, RendererCache
    Wave/                  WaveData, WaveManager, SpawnPoint
    World/                 BreakableObject
    Editor/                프로토타입/프리팹 생성 보조 에디터 코드
  4.Sprite/                HUD, 맵, 이펙트, 임시 스프라이트
  5.Animation/             이펙트/캐릭터 애니메이션
  6.Materials/             머티리얼
  7.Prefab/
    Effect/                Slash, Slam, Parry, FocusOrb 등
    Enemy/                 적 프리팹
    Item/                  HasteOrb, PowerOrb, FocusOrbPickup
    Projectile/            플레이어/적 투사체
  8.Audio/                 오디오 리소스 예비 폴더
  9.SO/                    AttackData, SkillData, WaveData
```

</details>

<details>
<summary>전투 처리 흐름 보기</summary>

```text
PlayerInputReader
  -> PlayerMovement2D
       이동 / 점프 / 대시 / One-Way Platform 처리

  -> PlayerCombat
       기본공격 1타/2타
       애니메이션 이벤트에서 Hitbox Open/Close

  -> PlayerSkillController
       스킬 쿨다운 체크
       스킬 애니메이션 재생
       스킬별 Hitbox / Projectile / Effect 처리

Hitbox
  -> Hurtbox
       패링/가드 여부 확인

  -> Health
       데미지 적용
       OnDamaged / OnDead 이벤트 발생

CombatFeedback
  -> 데미지 텍스트
  -> 카메라 쉐이크
  -> 히트스톱

ComboCounterUI
  -> Hitbox.OnAnyHit / Health.OnAnyDead 기반 콤보 표시
```

</details>

<details>
<summary>스킬 / 버프 UI 구조 보기</summary>

```text
CombatHUD
  SkillSlotBar
    SkillSlotUI A
    SkillSlotUI S
    - 클릭: 스킬 선택 패널 열기
    - 드래그: 슬롯 교체
    - 쿨다운: 원형 Radial Fill + 숫자

  BuffStatusView
    - Haste / Power / Focus 버프 표시
    - 원형 아이콘
    - 남은 시간 숫자
    - 스킬 쿨다운처럼 Radial Fill 감소
    - RectTransform 위치를 에디터에서 직접 조절 가능
```

`BuffStatusView`는 `Canvas/CombatHUD` 하위에 배치되어 있습니다. 위치를 바꾸고 싶으면 Hierarchy에서 `CombatHUD > BuffStatusView`를 선택한 뒤 RectTransform을 옮기면 됩니다.

</details>

<details>
<summary>버프 / 파괴 오브젝트 보기</summary>

### 버프

| 버프 | 효과 | 연출 |
| --- | --- | --- |
| Haste Orb | 5초 동안 이동속도 증가 | 초록 잔상 |
| Power Orb | 5초 동안 공격력 증가 | 연한 붉은 틴트 + 붉은 잔상 |
| Focus Orb | 포커스 게이지 충전 | 핑크 UI 표시 |

### 파괴 오브젝트

`BreakableObject`는 `Health.OnDead`를 구독해서 파괴 처리를 합니다.

```text
BreakableObject
  Health
  Hurtbox
  CombatFeedback
  SpriteRenderer
  Collider2D
```

파괴 시:

- 파괴 이펙트 생성
- 먼지/파편 이펙트 생성
- 약한 카메라 쉐이크
- 설정된 드랍 프리팹 생성
- 오브젝트 비활성화

GameScene에는 테스트용으로 아래 오브젝트가 배치되어 있습니다.

```text
Interactables
  BreakableCrate_HasteDrop
  BreakableCrate_PowerDrop
  BreakableCrate_FocusDrop
  HasteOrb_FieldPickup
  PowerOrb_FieldPickup
```

</details>

<details>
<summary>포커스 모드 구조 보기</summary>

포커스 모드는 적과 투사체만 느려지게 하고, 플레이어는 정상 속도로 움직이도록 구성했습니다.

```text
HUDView
  포커스 게이지 충전 / 소비
  FocusModeController.Activate 호출

PlayerFocusBurst
  발동 시작 이펙트
  주변 적 넉백
  주변 투사체 제거
  플레이어 무적
  입력 잠금

FocusModeController
  EnemySpeedMultiplier
  ProjectileSpeedMultiplier
  포커스 지속 시간 관리

FocusBackgroundDimmer
  Map/BG, Map/FG만 어둡게 처리
  플레이어, 적, 투사체, 이펙트는 어둡게 하지 않음
```

</details>

<details>
<summary>적 / 웨이브 구조 보기</summary>

### 적 AI

`EnemyBrainBase`가 공통 상태와 이동/추적/피격/사망 처리를 담당합니다.

```text
EnemyBrainBase
  Idle
  Patrol
  Chase
  Attack
  Hit
  Dead
```

구현된 적 타입:

- `FlyingMeleeEnemy`: 지형을 무시하고 플레이어에게 접근하는 비행 근접 몹
- `MeleeChargerEnemy`: 지상 근접 몹
- `RangedShooterEnemy`: 투사체를 발사하는 원거리 몹
- 강한 지상형 프리팹 구조: 느리지만 체력/공격력이 높은 적 구성 가능

### 웨이브

```text
WaveManager
  WaveData 순서대로 적 스폰
  aliveEnemies 추적
  웨이브 전멸 시 Wave Clear UI
  마지막 웨이브 종료 시 GameManager.ClearGame()
```

</details>

<details>
<summary>주요 ScriptableObject 보기</summary>

### AttackData

```text
damage
knockback
hitStopTime
activeTime
cooldown
```

공격의 실제 데미지, 넉백, 히트스톱, 판정 시간을 정의합니다.

### SkillData

```text
displayName
icon
skillType
attackData
cooldown
duration
range
force
projectilePrefab
```

스킬 UI 표시와 실제 스킬 동작을 연결합니다.

### WaveData

```text
enemyPrefab
count
interval
nextWaveDelay
```

웨이브별 적 종류, 수량, 스폰 간격을 정의합니다.

</details>

## 포트폴리오 관점에서 강조할 점

- 입력, 이동, 전투, UI, 연출, 웨이브를 분리해 유지보수하기 쉽게 구성
- 공격 판정은 애니메이션 이벤트로 열고 닫을 수 있게 설계
- `Hitbox -> Hurtbox -> Health` 흐름으로 데미지 책임 분리
- 버프와 파괴 오브젝트는 기존 전투 구조를 재사용
- 투사체/이펙트는 `ObjectPool` 기반 재사용 가능 구조
- 포커스 모드는 전역 TimeScale에만 의존하지 않고 적/투사체 배율을 별도 관리

## 현재 한계 / 다음 개선

- 일부 UI는 프로토타입 영상용 런타임 보완 로직을 포함합니다.
- 캐릭터/적 애니메이션은 보유 스프라이트 기준으로 연결되어 있어, 리소스 추가 시 클립 보강이 필요합니다.
- 버프 아이콘은 임시 원형 색상 UI입니다. 최종 리소스가 있으면 `Sprite` 아이콘으로 교체하는 것이 좋습니다.
- 사운드 연출은 필드만 열어둔 상태라, 실제 효과음 연결이 필요합니다.
- 스킬별 밸런스 수치와 웨이브 난이도는 제출 영상 기준으로 추가 조정이 필요합니다.

## 빌드 / 확인

Unity 6000.3 계열 프로젝트입니다.  
스크립트 컴파일 확인은 아래 명령으로 진행했습니다.

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

현재 확인된 빌드 상태:

- 컴파일 오류 없음
- Unity MCP 관련 `System.Net.Http`, `System.IO.Compression` 버전 경고만 존재

