# 2DCombatProject

Unity 기반 PC/Steam용 2D 플랫포머 액션 전투 프로토타입입니다.  
111퍼센트 PC 클라이언트 개발자 과제 제출을 목표로, 이동/점프/회피/공격/스킬/패링/적 AI/웨이브 구조를 우선 구현했습니다.

## 현재 구현 상태

- `StartScene`, `LoadingScene`, `GameScene` 3개 씬 구성
- New Input System 기반 키보드/게임패드 입력
- 방향키 이동, 2단 점프, 대시/회피
- 대시 잔상과 본체 반투명 연출
- 기본 공격, 스킬 2개 장착 및 드래그 교체 UI
- 가드/패링 입력 추가
- 패링 성공 시 데미지 취소, 히트스톱, 카메라 흔들림, 적 스턴, 투사체 반사
- One-Way Platform 기반 공중 발판
- 허수아비 타격 테스트 대상
- 월드 데미지 숫자와 콤보 카운터 UI
- 근접 적, 원거리 적, 5웨이브 스폰 구조
- Martial Hero 스프라이트 기반 플레이어 프리팹 생성
- KnightPtk 파티클 시트가 있을 경우 패링 이펙트에 연결 가능한 구조

## 조작

### Keyboard

```text
이동: 방향키
점프: Space
대시/회피: Z 또는 방향키 더블탭
공격: C
스킬 1: A
스킬 2: S
가드/패링: X
상호작용: 방향키 위 또는 E
```

### Gamepad

```text
이동: Left Stick / D-Pad
점프: South Button
대시/회피: East Button
공격: West Button
스킬 1: Left Shoulder
스킬 2: Right Shoulder
가드/패링: Left Trigger
상호작용: North Button
메뉴 시작: Start 또는 South Button
```

## 폴더 구조

```text
Assets/
  1.Scene/       StartScene, LoadingScene, GameScene
  3.Script/
    Core/        게임 상태, 시작 메뉴, 로딩
    Input/       New Input System 입력 래퍼
    Player/      이동, 전투, 스킬, 가드/패링
    Combat/      Health, Hitbox, Hurtbox, DamageInfo
    Skill/       SkillData, 스킬 실행, 투사체
    Enemy/       EnemyBrainBase, 근접/원거리 적 AI
    Wave/        WaveData, SpawnPoint, WaveManager
    Camera/      카메라 추적/흔들림
    UI/          결과 UI, 스킬 슬롯 UI
    Utility/     ObjectPool, 자동 반환 유틸리티
    Editor/      과제용 프로토타입 씬/프리팹 자동 생성 도구
  4.Sprite/      임시/교체용 스프라이트
  5.Animation/   AnimationClip, AnimatorController
  7.Prefab/      Player, Enemy, Projectile, Effect
  9.SO/          AttackData, SkillData, WaveData
```

## 실행 흐름

```text
StartScene
  StartMenuController
  - 마우스 클릭, Enter/Space, 게임패드 입력으로 LoadingScene 이동

LoadingScene
  LoadingSceneController
  - 최소 로딩 시간 후 GameScene 비동기 로드

GameScene
  GameManager
  - Ready / Playing / Clear / GameOver 상태 관리

  PlayerInputReader
  - 키보드와 게임패드 입력을 동일한 프로퍼티로 제공

  PlayerMovement2D
  - 좌우 이동, 점프, 2단 점프, 대시 처리

  PlayerCombat / PlayerSkillController
  - 기본 공격과 장착 스킬 2개 실행

  PlayerGuard
  - X / LT 입력 직후 짧은 패링 윈도우 생성
  - 실패 시 가드 상태로 데미지 감소
  - 성공 시 Hurtbox 단계에서 데미지 적용을 취소

  Hitbox -> Hurtbox -> Health
  - 공격 판정이 Hurtbox를 감지
  - Hurtbox가 패링/가드 여부를 먼저 확인
  - Health가 최종 체력 감소와 사망 이벤트 처리

  DamageTextSpawner / ComboCounterUI
  - 실제 데미지가 들어간 경우 월드 데미지 숫자 표시
  - 연속 타격 시간 안에 맞추면 콤보 카운터 갱신

  TrainingDummy
  - AI 없이 맞기만 하는 타격감 테스트 대상
  - 죽어도 즉시 체력을 회복해 계속 공격 테스트 가능

  WaveManager
  - WaveData 순서대로 적 스폰
  - 모든 웨이브 클리어 시 GameManager.ClearGame()
```

## 기존 프로젝트 참고 방향

- `Cuphead`: 2D 이동, 점프, 회피, 원거리 액션의 조작 감각 참고
- `WildTamer`: 전투 책임 분리, AI Brain, ObjectPool, Feedback 구조 참고

기존 프로젝트를 그대로 복사하지 않고, 과제 범위에 맞게 입력/전투/스킬/적/웨이브 책임을 작게 분리했습니다.

## 다음 작업 순서

1. 체력바/HUD 연결
   - `Health.OnDamaged` 이벤트를 받아 Player HP Slider 갱신
   - 적 HP Bar는 보스/엘리트부터 적용

2. 적 6종 확장
   - 현재: `MeleeChargerEnemy`, `RangedShooterEnemy`
   - 추가 후보: ShieldEnemy, JumperEnemy, BomberEnemy, EliteEnemy
   - 공통 기능은 `EnemyBrainBase`에 두고 패턴만 하위 클래스에서 분리

3. 플레이어 2캐릭터 전환
   - `PlayerCharacterData` ScriptableObject 생성
   - Tab 입력으로 Animator, AttackData, SkillData, 이동 수치 교체
   - 과제 제출용이면 체력은 공유 체력이 안전함

4. 스프라이트 교체
   - 시트는 `Sprite Mode: Multiple`, `Filter Mode: Point`
   - Idle/Run/Jump/Fall/Attack/Hit/Death 단위로 애니메이션 클립 생성
   - 공격 타이밍에 맞춰 Hitbox 위치와 active time 조정

5. 타격감 보강
   - 공격별 hit stop 시간 분리
   - 카메라 흔들림 강도 분리
   - 패링 성공 이펙트, 피격 플래시, 착지 먼지 추가

## Unity 메뉴

```text
2DCombatProject > Build Temporary Prototype
2DCombatProject > Import Knight Attack And Particles
```

Unity가 이미 열려 있으면 스크립트 리로드 후 자동 빌더가 한 번 실행됩니다. 자동 실행이 안 되면 위 메뉴를 직접 실행하면 현재 프리팹/씬 구조가 다시 생성됩니다.
