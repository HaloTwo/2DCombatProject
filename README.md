# 2DCombatProject

Unity 기반 PC/Steam 대응 2D 플랫포머 액션 전투 프로토타입입니다.  
111퍼센트 PC 클라이언트 개발자 과제 제출을 목표로, 짧은 플레이 안에서 이동, 회피, 타격감, 스킬, 패링, 웨이브, 버프, 포커스 모드가 한 번에 보이도록 구성했습니다.

핵심 방향은 "기능을 많이 나열하는 것"보다, 실제 액션 게임처럼 입력과 애니메이션 타이밍, 피격 피드백, 화면 연출이 맞물리게 만드는 것입니다.

## 구현 요약

- New Input System 기반 키보드/게임패드 입력
- `StartScene -> LoadingScene -> GameScene` 3개 씬 흐름
- 2D 플랫포머 이동: 좌우 이동, 점프, 2단 점프, 대쉬, One-Way Platform 내려가기
- 기본 공격 1타/2타 콤보
- 애니메이션 이벤트 기반 Hitbox Open/Close
- 스킬 4종: 대쉬공격, 충격파, 검기, 공중공격
- 패링/가드, 투사체 반사, 패링 성공 이펙트
- 웨이브 기반 적 스폰/클리어 UI
- 근접/원거리/비행/강한 지상형 적 구조
- 파괴 가능한 오브젝트와 버프 아이템
- 포커스 게이지, 포커스 모드, 적/투사체 슬로우
- 체력바, 스킬 슬롯, 쿨다운, 버프 지속시간, 콤보/데미지 UI
- 사운드 매니저 기반 BGM/SFX 자동 로드
- 오브젝트 풀 기반 이펙트/투사체/드랍 연출 재사용

## 조작법

| 동작 | 키보드 | 게임패드 |
| --- | --- | --- |
| 이동 | 방향키 | Left Stick / D-Pad |
| 점프 | Space | South Button |
| 대쉬/회피 | Z | East Button |
| 기본 공격 | C | West Button |
| 스킬 1 | A | Left Shoulder |
| 스킬 2 | S | Right Shoulder |
| 가드/패링 | X | Left Trigger |
| 상호작용 | 방향키 위 또는 E | North Button |
| 포커스 모드 | Focus 입력 액션 | Focus 입력 액션 |
| 일시정지 | Esc | Start |

스킬 슬롯은 마우스로 클릭해 스킬 목록을 열 수 있고, 드래그로 슬롯 위치를 교체할 수 있습니다.

## 플레이 흐름

```text
StartScene
  -> LoadingScene
    -> GameScene
      -> Wave 1 / 2 / 3
        -> 전투, 버프, 포커스 모드, 파괴 오브젝트
          -> Game Clear 또는 Game Over
```

<details>
<summary>폴더 구조</summary>

```text
Assets/
  1.Scene/
    StartScene
    LoadingScene
    GameScene

  3.Script/
    Camera/      카메라 추적, 카메라 쉐이크
    Combat/      Health, Hitbox, Hurtbox, DamageInfo, CombatFeedback
    Core/        GameManager, GameEnums, FocusModeController
    Enemy/       EnemyBrainBase, 근접/원거리/비행 적 AI, 적 체력바
    Input/       PlayerInputReader, New Input System 입력 래핑
    Item/        BuffItem, BuffPickup, BuffPickupSpawner
    Manager/     Singleton, SoundManager, ObjectPool
    Player/      이동, 기본공격, 패링, 포커스 발동, 버프 잔상
    Skill/       SkillData, PlayerSkillController, Projectile
    UI/          HUD, 스킬 슬롯, 버프 슬롯, 콤보, 웨이브, 결과 UI
    Utility/     TimedAutoRelease, PooledObjectTag 등 재사용 유틸
    Wave/        WaveData, WaveManager, SpawnPoint
    World/       BreakableObject
    Editor/      프리팹/헬스바/프로토타입 생성 보조 코드

  7.Prefab/
    Effect/      피격, 패링, 포커스, 충격파, 검기 이펙트
    Enemy/       적 프리팹
    Item/        버프 아이템, 파괴 오브젝트
    Projectile/  플레이어/적 투사체

  9.SO/
    AttackData
    SkillData
    WaveData

  Resources/
    Sound/
      BGM/
      SFX/
```

</details>

<details>
<summary>전투 구조와 선택 이유</summary>

### Hitbox -> Hurtbox -> Health

전투 판정은 `Hitbox`, 피격 수신은 `Hurtbox`, 실제 체력 처리는 `Health`로 나눴습니다.

```text
공격자
  Hitbox.Open(team, attackData)
    -> Hurtbox.ApplyDamage(info, attacker)
      -> PlayerGuard.TryParry()
      -> Health.TakeDamage()
      -> OnDamaged / OnDead 이벤트
```

이렇게 나눈 이유:

- 공격 판정과 체력 처리를 분리하면 플레이어/적/오브젝트가 같은 데미지 구조를 사용할 수 있습니다.
- 패링은 데미지 직전에 `Hurtbox`에서 가로채야 하므로, `Health`에 직접 넣는 것보다 확장하기 쉽습니다.
- `AttackData` 하나로 데미지, 넉백, 히트스톱, 활성 시간을 관리할 수 있어서 스킬 밸런스 조절이 편합니다.
- 기본 공격과 스킬, 적 공격이 같은 `DamageInfo` 흐름을 사용하므로 포트폴리오에서 구조 설명이 쉽습니다.

### 애니메이션 이벤트 기반 타격

기본 공격과 일부 적 공격은 애니메이션 이벤트로 Hitbox를 켜고 끕니다.

```text
Attack Animation
  -> Animation Event: OpenAttackHitbox()
  -> 실제 칼이 지나가는 프레임에만 판정 활성
  -> Animation Event: CloseAttackHitbox()
```

이 방식을 선택한 이유:

- 버튼을 누른 순간 바로 데미지가 들어가는 문제를 막을 수 있습니다.
- 칼을 휘두르는 프레임과 피격 타이밍이 맞아 타격감이 좋아집니다.
- 나중에 새 모션을 받아도 이벤트 위치만 조정하면 코드 수정 없이 타격 타이밍을 맞출 수 있습니다.

### CombatFeedback

피격 시 다음 피드백을 묶어서 사용합니다.

- 데미지 텍스트
- 히트스톱
- 카메라 쉐이크
- 피격 이펙트
- 피격/파괴/스킬 사운드

타격감은 데미지 수치보다 "맞았다는 반응"이 중요하므로, 전투 기능과 연출을 같이 설계했습니다.

</details>

<details>
<summary>플레이어 구조</summary>

### PlayerInputReader

New Input System을 직접 여러 스크립트에서 읽지 않고, `PlayerInputReader`가 입력 상태를 한 번 정리합니다.

선택 이유:

- 키보드/패드를 동시에 대응하기 쉽습니다.
- 기존 `UnityEngine.Input` 사용으로 생기는 입력 시스템 충돌을 피할 수 있습니다.
- 이동/공격/스킬 코드가 입력 바인딩 세부사항을 몰라도 됩니다.

### PlayerMovement2D

담당 기능:

- 좌우 이동
- 점프/2단 점프
- 대쉬
- 대쉬 중 적 통과
- One-Way Platform 내려가기
- 이동속도 버프
- 피격/공격/포커스 발동 중 입력 잠금

선택 이유:

- 플랫포머 액션에서 이동은 전투 감각과 직접 연결되므로, 입력 잠금과 대쉬 통과 같은 전투 관련 이동도 이곳에서 관리합니다.
- 적과 플레이어는 물리적으로 길막하지 않고, 공격/피격 판정으로만 상호작용하도록 구성했습니다.

### PlayerCombat

기본 공격 담당:

- 1타/2타 콤보
- 공격 중 이동 잠금
- 공격 타이밍에 약간 앞으로 전진
- 애니메이션 이벤트로 Hitbox 제어
- 기본 피격 사운드 랜덤 재생

선택 이유:

- 기본 공격은 가장 자주 보는 액션이므로, 스킬보다 먼저 안정적으로 느껴져야 합니다.
- 무조건 연타가 아니라 모션 타이밍에 맞춰 이어지게 만들어 전투 템포를 살렸습니다.

### PlayerSkillController

스킬 4종을 관리합니다.

| 스킬 | 의도 | 구현 |
| --- | --- | --- |
| 대쉬공격 | 빠르게 지나가며 경로상 적을 베는 기술 | 대쉬 이동 후 경로 OverlapBox 판정, 순서대로 타격/사운드 |
| 충격파 | 점프 후 내려찍는 광역기 | 자동 점프, 낙하, 착지 지점 원형 판정 |
| 검기 | 전방 견제 원거리 기술 | 투사체 발사, 벽/거리/수명 처리 |
| 공중공격 | 적을 띄우고 연속 타격 | 1타 띄우기, 2타 밀어내기 |

선택 이유:

- 스킬 성격이 서로 다르게 보이도록 구성했습니다.
- 같은 `SkillData`를 사용하지만, 실행 로직은 `SkillType` 별로 분기해 영상에서 차이가 보이게 했습니다.
- 대쉬공격은 전용 피격음, 검기는 발사음, 충격파는 내려찍는 순간의 큰 소리로 구분했습니다.

</details>

<details>
<summary>적 AI 구조</summary>

`EnemyBrainBase`가 공통 기능을 담당하고, 적 종류별 클래스가 행동 차이를 만듭니다.

```text
EnemyBrainBase
  Idle
  Patrol
  Chase
  Attack
  Hit
  Dead
```

공통 기능:

- 플레이어 탐지
- 순찰
- 추적
- 공격 상태 전환
- 피격 경직
- 사망 처리
- 포커스 모드 슬로우 반영
- 적끼리/플레이어와 충돌 무시 처리

적 종류:

| 적 | 역할 | 특징 |
| --- | --- | --- |
| Flying Eye | 비행 근접 적 | 지형을 무시하고 플레이어에게 접근 |
| Goblin | 기본 근접 적 | 순찰하다가 플레이어를 감지하면 추적 |
| Mushroom | 원거리 적 | 거리를 두고 포물선 투사체 발사 |
| Skeleton | 강한 지상 적 | 느리지만 체력/공격력 높은 타입 |

선택 이유:

- 적 AI를 완전히 따로 만들면 유지보수가 어려우므로, 공통 상태 머신은 Base에 두었습니다.
- 비행/근접/원거리처럼 과제 영상에서 차이가 보이는 부분만 파생 클래스에서 처리했습니다.
- 원거리 적의 발사는 애니메이션 이벤트로 처리할 수 있게 만들어 공격 모션과 투사체 타이밍을 맞췄습니다.

</details>

<details>
<summary>웨이브와 게임 흐름</summary>

### WaveManager

`WaveData` 기준으로 적을 순서대로 스폰하고, 살아있는 적 수를 추적합니다.

```text
Wave Start
  -> 3, 2, 1 카운트다운
  -> 적 스폰
  -> 0 / total 진행도 표시
  -> 전부 처치
  -> Wave Clear
  -> 다음 웨이브
  -> 마지막 웨이브는 Boss Wave 연출
  -> 보스 처치 이후 Clear
```

선택 이유:

- 과제 영상에서 게임 흐름이 명확히 보입니다.
- 단순 샌드박스보다 시작/진행/클리어가 있어 완성도가 높아 보입니다.
- `WaveData`로 적 수와 종류를 바꿀 수 있어 밸런스 조절이 쉽습니다.
- 마지막 웨이브는 `Elite Skeleton` 미니보스를 배치해 일반 웨이브와 다른 마무리감을 만들었습니다.

### Elite Skeleton Boss

기존 적 구조를 버리지 않고 `EnemyBrainBase`, `MeleeChargerEnemy`, `Health`, `Hitbox`, `AttackData`를 재사용해 만든 미니보스입니다.

구성:

- `BossEnemy` 원본 프리팹
- `BossSkeletonEnemy` 실제 출현 프리팹
- `EliteBossAttack` 공격 데이터
- 체력 50% 이하에서 2페이즈 진입
- 2페이즈 진입 시 이동 속도 증가, 색상 변화, 카메라 흔들림
- 일반 피격마다 모션이 끊기지 않고, 누적 데미지가 일정량 쌓였을 때만 짧은 Hit 경직 발생

선택 이유:

- 과제 범위 안에서 보스전을 과하게 새로 만들기보다, 이미 검증한 적/피격/웨이브 구조를 확장했습니다.
- 마지막 웨이브가 단순 몹 정리로 끝나지 않고 제출 영상에서 클라이맥스처럼 보이게 했습니다.
- 보스 패턴을 추가할 여지를 남기되, 현재 빌드 안정성을 우선했습니다.
- 보스가 매 타격마다 Hurt 모션으로 끊기면 약해 보이므로, 데미지는 받되 누적 경직만 발생하도록 했습니다.

### ResultView

게임오버/클리어 후:

- 시간 정지
- 다시하기
- 처음으로

선택 이유:

- 제출 영상에서 실패/클리어 UI까지 보여줄 수 있습니다.
- 씬 재시작 시 남은 몹/웨이브 상태를 정리하는 구조와 연결됩니다.

</details>

<details>
<summary>버프, 파괴 오브젝트, 포커스 모드</summary>

### BreakableObject

상자/오브젝트가 공격을 받으면 `Health.OnDead`를 통해 파괴됩니다.

파괴 시:

- 오브젝트 비활성화
- 파괴 이펙트
- 약한 카메라 쉐이크
- 랜덤 버프 아이템 드랍 가능

선택 이유:

- 적이 아닌 월드 오브젝트도 전투 시스템에 반응하게 하여 게임 공간이 살아 보입니다.
- `Hurtbox + Health` 구조를 재사용하므로 별도 데미지 시스템이 필요 없습니다.

### BuffItem

즉시 획득형 아이템입니다.

| 버프 | 효과 | 연출 |
| --- | --- | --- |
| Haste | 이동속도 증가 | 초록 잔상 |
| Power | 공격력 증가 | 붉은 잔상/오라 |
| FocusGauge | 포커스 게이지 충전 | HUD 게이지 상승 |
| Invincible | 일정 시간 무적 | 체력 피해 무시 |

선택 이유:

- 인벤토리 없이도 전투 중 선택지가 생깁니다.
- 버프 지속시간 UI를 HUD에 띄워 플레이어가 현재 상태를 바로 알 수 있습니다.
- 같은 버프를 여러 번 먹어도 각각의 지속시간이 별도로 표시되도록 구성했습니다.

### Focus Mode

적 처치 시 포커스 게이지가 차고, 가득 차면 포커스 모드를 사용할 수 있습니다.

포커스 발동 시:

- 발동 이펙트
- 플레이어 입력 잠금
- 플레이어 무적
- 주변 적 넉백
- 주변 투사체 제거
- 적/투사체 속도 감소
- 배경 암전
- 포커스 게이지 감소

선택 이유:

- 리소스가 제한된 상태에서도 가장 눈에 띄는 창의 요소입니다.
- 전역 `Time.timeScale`을 직접 바꾸기보다, 적/투사체 배율을 따로 관리해 플레이어 조작감은 유지했습니다.
- 액션 게임에서 "결정적인 순간"을 보여주기 좋아 제출 영상용 임팩트가 큽니다.

</details>

<details>
<summary>UI 구조</summary>

### CombatHUD

게임 중 필요한 HUD를 한 곳에 모았습니다.

- HP
- Focus Gauge
- Skill Slot 2개
- Skill Cooldown
- Buff Status
- Combo / Damage Text
- Wave Text

### SkillSlotUI / SkillSlotBarUI

기능:

- 스킬 아이콘 표시
- 쿨다운 원형 오버레이
- 쿨다운 숫자
- 드래그 슬롯 교체
- 클릭으로 스킬 선택 패널 열기
- 교체/쿨다운 완료 피드백

선택 이유:

- 스킬을 코드로만 바꾸는 것이 아니라, 실제 게임처럼 UI에서 교체하는 흐름을 보여주기 위해 만들었습니다.
- 스킬 슬롯 크기가 런타임에 흔들리지 않도록 UI 요소를 고정 크기로 구성했습니다.

### BuffStatusView

버프마다 별도의 슬롯을 생성하고, 각 슬롯이 자신의 남은 시간을 원형 게이지로 표시합니다.

선택 이유:

- 여러 버프를 동시에 먹었을 때 상태가 분리되어 보입니다.
- 스킬 쿨다운 UI와 비슷한 형태라 HUD 스타일이 통일됩니다.

</details>

<details>
<summary>사운드 구조</summary>

### SoundManager

`Resources/Sound/BGM`, `Resources/Sound/SFX`를 자동 로드합니다.

구조:

```text
SoundManager
  bgmMap        BGMType enum 기반 BGM
  sfxMap        SFXType enum 기반 일반 SFX
  namedSfxMap   파일명 기반 특수 SFX
  bladeHitClips beatt-sound1/2 랜덤 피격음
  dashAttackHitClips 대쉬공격 전용 피격음
  boxHitClips   박스 피격음
```

선택 이유:

- `PlayDashAttackStart()`, `PlayFocusMode()`처럼 사운드가 늘 때마다 함수를 추가하면 코드가 지저분해집니다.
- 그래서 특수 사운드는 `PlayNamedSFX(clipName, fallbackType)` 하나로 통일했습니다.
- 이름 기반 재생이 실패하면 fallback enum 사운드로 빠지게 해서 리소스 누락에도 게임이 멈추지 않습니다.

예시:

```csharp
SoundManager.Instance?.PlayNamedSFX(SoundManager.SfxBuffPickup, SFXType.Buff);
SoundManager.Instance?.PlayRandomBladeHit();
SoundManager.Instance?.PlayRandomDashAttackHit();
```

현재 주요 사운드:

| 파일명 | 사용 위치 |
| --- | --- |
| MainBGM | 게임 BGM |
| weapon-sound | 기본 공격 휘두름 |
| beatt-sound1/2 | 일반 피격 랜덤 |
| beatt-sound-dashAttack | 대쉬공격 발동 |
| beatt-sound-dashAttack1/2/3 | 대쉬공격 피격 |
| beatt-sound-SwordAreaAttack | 검기 발사 |
| beatt-sound-SlamShockwave | 충격파 내려찍기 |
| Guard | 패링 성공 |
| focusMode | 포커스 모드 발동 |
| buff_1 | 버프 획득 |

</details>

<details>
<summary>오브젝트 풀과 최적화 방향</summary>

### ObjectPool

반복 생성되는 오브젝트는 풀 기반으로 재사용합니다.

대상:

- 투사체
- 피격 이펙트
- 포커스 오브
- 버프 아이템
- 파괴 이펙트
- 데미지 텍스트

선택 이유:

- 전투 중 `Instantiate/Destroy`를 반복하면 프레임 드랍과 GC가 발생할 수 있습니다.
- 제출 영상에서도 적 처치, 투사체, 이펙트가 여러 번 발생하므로 풀 구조가 필요합니다.

### 런타임 Find 최소화

주요 컴포넌트는 가능한 한 인스펙터 연결 또는 `Awake/Reset`에서 1회 캐싱합니다.

예외:

- 씬 전환 후 싱글톤 찾기
- UI 자동 복구
- 프로토타입 편의용 fallback

선택 이유:

- 포트폴리오용 코드에서 성능 의식이 보입니다.
- 동시에 인스펙터 연결이 빠져도 최소한 동작하도록 fallback을 남겼습니다.

</details>

<details>
<summary>제출 영상 기획안</summary>

권장 영상 길이: 1분 30초 ~ 2분 30초  
목표: "조작감, 타격감, 전투 구조, 창의 요소"가 빠르게 보이게 구성

### 0. 시작 화면 - 5초

보여줄 것:

- StartScene
- 마우스 클릭 또는 키 입력으로 시작
- LoadingScene 전환

자막 예시:

```text
Start / Loading / Game Scene Flow
```

### 1. 기본 이동과 플랫폼 - 10초

보여줄 것:

- 좌우 이동
- 점프/2단 점프
- One-Way Platform 위로 올라가기
- 아래 입력 + 점프로 플랫폼 내려가기
- 대쉬

자막 예시:

```text
New Input System 기반 키보드/패드 대응 2D 플랫폼 이동
```

### 2. 기본 공격과 타격감 - 15초

보여줄 것:

- 허수아비 또는 약한 몹에게 기본 공격 1타/2타
- 데미지 텍스트
- 피격 이펙트
- 카메라 쉐이크
- 히트스톱
- 콤보 UI

자막 예시:

```text
Animation Event 기반 Hitbox 타이밍 / HitStop / Camera Shake
```

촬영 포인트:

- 공격 모션 중간에 데미지가 들어가는 장면을 보여주면 좋습니다.
- 너무 멀리서 찍지 말고 플레이어와 적이 크게 보이게 카메라 위치를 조절합니다.

### 3. 스킬 4종 - 25초

순서:

1. 검기
   - 전방으로 투사체 발사
   - 벽 또는 적에게 충돌

2. 대쉬공격
   - 적 여러 마리를 지나가며 순서대로 타격
   - 대쉬 잔상
   - 대쉬 전용 피격 사운드

3. 공중공격
   - 적을 띄우고 2타로 밀어내기

4. 충격파
   - 점프 후 내려찍기
   - 원형 범위 공격
   - 충격파 이펙트와 큰 효과음

자막 예시:

```text
Dash Attack / Ground Slam / Projectile Slash / Rising Slash
```

촬영 포인트:

- 스킬 쿨다운 UI가 보이게 HUD를 포함해 찍습니다.
- 대쉬공격은 적 2~3마리가 일렬로 있을 때 쓰는 게 가장 보기 좋습니다.

### 4. 패링과 투사체 반사 - 15초

보여줄 것:

- 적 공격 타이밍에 가드/패링
- Guard 사운드
- 패링 이펙트
- 적 넉백
- 가능하면 원거리 투사체 반사

자막 예시:

```text
Parry cancels incoming damage and sends reaction to attacker
```

촬영 포인트:

- 패링은 성공 타이밍이 짧으므로 한 번 성공한 클립만 잘라 쓰는 게 좋습니다.

### 5. 파괴 오브젝트와 버프 - 15초

보여줄 것:

- 상자 파괴
- 버프 아이템 드랍
- 버프 획득 사운드
- 버프 UI 슬롯 생성
- 이동속도/공격력 증가 연출

자막 예시:

```text
Breakable Object / Buff Item / Buff Duration UI
```

촬영 포인트:

- Haste는 이동 잔상이 잘 보이게 넓은 구간에서 움직입니다.
- Power는 바로 적을 때려 데미지 증가 느낌을 보여줍니다.

### 6. 포커스 모드 - 20초

보여줄 것:

- 몹 처치 후 포커스 게이지 충전 오브가 플레이어에게 날아옴
- 게이지 100%
- 포커스 모드 발동
- 발동 이펙트
- 주변 적 넉백
- 투사체 제거
- 적/투사체 슬로우
- 배경 암전

자막 예시:

```text
Focus Mode: Enemy/Projectile Slow, Burst Knockback, Invincible Startup
```

촬영 포인트:

- 원거리 적 투사체가 날아오는 순간에 포커스를 켜면 효과가 가장 잘 보입니다.
- 포커스 발동 직후 스킬을 쓰면 전투 템포가 좋아 보입니다.

### 7. 보스 웨이브와 게임 클리어 - 25초

보여줄 것:

- Wave 1 시작 카운트다운
- `0 / 10` 진행도 갱신
- Wave Clear
- 마지막 웨이브 Boss Wave 표시
- Elite Skeleton 등장
- 보스 2페이즈 전환
- 보스는 매 타격마다 경직되지 않고, 누적 데미지 조건에서만 Hit 반응
- 마지막 웨이브 Clear
- Clear UI
- 다시하기/처음으로 버튼

자막 예시:

```text
Boss Wave / Phase Change / Wave Clear / Game Clear UI
```

촬영 포인트:

- 보스는 평타 몇 번에 계속 멈추는 장면보다, 공격을 받아도 버티다가 일정 데미지 후 짧게 경직되는 장면이 좋습니다.
- 2페이즈 색상 변화와 카메라 흔들림이 보이도록 체력을 절반 근처까지 깎은 뒤 컷을 이어 붙입니다.

### 영상 구성 추천

```text
00:00 Start / Loading
00:05 Movement
00:15 Basic Attack
00:30 Skills
00:55 Parry
01:10 Breakable & Buff
01:25 Focus Mode
01:50 Boss Wave
02:15 Wave Clear / Game Clear
02:25 Code Structure Summary
```

마지막 5~10초는 Unity 화면이 아니라 README 또는 코드 구조 화면을 잠깐 보여줘도 좋습니다.

보여주면 좋은 코드:

- `Hitbox.cs`
- `Hurtbox.cs`
- `PlayerSkillController.cs`
- `EnemyBrainBase.cs`
- `SoundManager.cs`

마지막 자막 예시:

```text
Combat flow is separated into Input / Movement / Hitbox / Health / Feedback / UI.
```

</details>

## 코드 설계 의도 정리

### 1. 기능보다 흐름을 먼저 보이게 만들기

과제 제출물은 기능 개수보다 "플레이가 되는가"가 중요하다고 판단했습니다.  
그래서 시작, 로딩, 웨이브, 클리어, 게임오버 흐름을 먼저 잡고 그 안에 전투 기능을 넣었습니다.

### 2. 타격감은 코드와 연출이 같이 움직여야 함

단순히 데미지를 주는 것만으로는 타격감이 부족합니다.  
그래서 공격 판정, 히트스톱, 카메라 쉐이크, 사운드, 피격 이펙트, 데미지 텍스트를 같이 묶었습니다.

### 3. 애니메이션 이벤트를 적극 사용

공격 타이밍은 코드 타이머보다 애니메이션 이벤트가 더 직관적입니다.  
특히 포트폴리오에서는 "애니메이션 프레임에 맞춰 판정을 열고 닫았다"는 설명이 명확합니다.

### 4. 스킬은 서로 다른 재미를 갖게 구성

검기, 대쉬공격, 충격파, 공중공격은 모두 같은 데미지 기술이 아니라 각자 역할이 다릅니다.

- 검기: 전방 견제
- 대쉬공격: 속도감과 관통
- 충격파: 광역 제압
- 공중공격: 띄우기와 연계

### 5. 확장 가능한 구조 유지

`AttackData`, `SkillData`, `WaveData`를 ScriptableObject로 분리해 수치 조절과 확장이 쉽도록 했습니다.  
새 스킬이나 적을 추가할 때 코드 수정량을 줄이는 것이 목적입니다.

### 6. 보스는 일반 몹과 다른 피격 규칙 적용

일반 몹은 타격감을 위해 맞을 때마다 짧게 Hit 모션이 나와도 괜찮지만, 보스가 매 타격마다 끊기면 전투가 너무 약하게 보입니다.  
그래서 `EliteMeleeBossEnemy`는 데미지는 정상적으로 받되, 누적 데미지가 일정 기준을 넘고 쿨타임이 지났을 때만 Hit 경직을 재생합니다.

의도:

- 보스가 공격을 받으면서도 압박감을 유지합니다.
- 플레이어 타격감은 데미지 텍스트, 사운드, 히트스톱으로 유지합니다.
- 큰 누적 데미지 순간에는 경직이 나와서 플레이어가 성과를 느낄 수 있습니다.

## 현재 한계와 다음 개선 방향

- 최종 아트 리소스가 확정되면 스킬 아이콘과 버프 아이콘을 실제 이미지로 교체해야 합니다.
- 적 AI는 현재 과제 영상용 추적/순찰 중심 구조이며, 더 자연스러운 추적을 위해 간단한 플랫폼 경로 탐색을 추가할 수 있습니다.
- 보스전은 미니보스 형태로 구현되어 있으며, 시간이 더 있으면 장판/돌진/소환 같은 패턴을 추가해 보스 고유성을 높일 수 있습니다.
- 사운드는 현재 파일명 기반 자동 로드 구조이며, 최종 단계에서는 AudioMixer로 볼륨 그룹을 분리할 수 있습니다.
- README와 영상에서는 "AI 생성 리소스"보다 "전투 구조와 구현 의도"를 중심으로 설명하는 것이 좋습니다.

## 빌드 / 확인

Unity 6000.3 계열 프로젝트입니다.  
스크립트 컴파일 확인:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

현재 확인 상태:

- 컴파일 오류 없음
- Unity MCP 관련 `System.Net.Http`, `System.IO.Compression` 참조 경고만 존재
