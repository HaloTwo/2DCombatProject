# 2DCombatProject

Unity 기반 PC 2D 플랫폼 액션 전투 프로토타입입니다.  
111퍼센트 PC(스팀) 클라이언트 개발자 과제 요구사항에 맞춰 `이동 / 점프 / 회피 / 공격 / 스킬 / 적 AI / 웨이브 클리어` 중심으로 구성했습니다.

## 현재 구현 상태

현재 단계는 최종 아트가 들어가기 전, 전투 구조와 조작 흐름을 먼저 검증하는 임시 프로토타입입니다.

- `StartScene`: 시작 메뉴
- `LoadingScene`: 게임 씬 로딩
- `GameScene`: 실제 전투 프로토타입
- 키보드 + 게임패드 입력 지원
- 방향키 더블탭 또는 `Z` 회피
- `Space` 점프, 공중 2단 점프
- `C` 기본 공격
- `A`, `S` 스킬 2개
- 스킬 슬롯 UI 드래그 교체
- 근접 돌진형 적
- 원거리 투사체형 적
- 5웨이브 클리어 구조
- 피격 플래시, 넉백, 히트스톱, 카메라 쉐이크 기반 타격감 구조

## 조작

### Keyboard

```text
이동: 방향키
점프: Space
대시/회피: Z 또는 방향키 더블탭
공격: C
스킬 1: A
스킬 2: S
상호작용: 방향키 위 또는 E
```

### Gamepad

```text
이동: Left Stick / D-Pad
점프: South Button (Xbox A)
대시/회피: East Button (Xbox B)
공격: West Button (Xbox X)
스킬 1: Left Shoulder (LB)
스킬 2: Right Shoulder (RB)
상호작용: North Button (Xbox Y)
메뉴 시작: Start 또는 South Button
```

## 참고한 기존 프로젝트 방향

기존 포트폴리오 프로젝트를 그대로 복사하지 않고, 구조와 구현 의도만 과제용으로 줄여서 재구성했습니다.

- `Cuphead`
  - 2D 이동, 점프, 대시, 투사체, 피격 반응 감각 참고
  - 단, 원거리 슈팅 중심 구조는 이번 과제와 맞지 않아 직접 재작성
- `WildTamer`
  - 전투 주체 분리, AI Brain 구조, ObjectPool, VisualFeedback 구조 참고
  - 단, `CombatAgent`처럼 많은 책임이 한 클래스에 몰리는 부분은 분리

## 폴더 구조

```text
Assets/
  1.Scene/          StartScene, LoadingScene, GameScene
  2.Model/          추후 원본 캐릭터/모델 리소스 보관
  3.Script/
    Core/           게임 상태, 시작 메뉴, 로딩
    Input/          New Input System 기반 입력
    Player/         이동, 점프, 대시, 기본 공격
    Combat/         체력, 데미지, Hitbox/Hurtbox, 피격 피드백
    Skill/          스킬 데이터, 스킬 실행, 투사체
    Enemy/          적 상태, 근접/원거리 AI
    Wave/           웨이브 데이터, 스폰 포인트, 웨이브 진행
    Camera/         카메라 쉐이크
    UI/             결과 UI, HUD, 스킬 슬롯 UI
    Utility/        오브젝트 풀, 공통 헬퍼
    Editor/         임시 프로토타입 씬/프리팹 생성 도구
  4.Sprite/         임시/최종 2D 스프라이트
  5.Animation/      Animator Controller, Animation Clip
  6.Materials/      Material
  7.Prefab/         Player, Enemy, Projectile Prefab
  8.Audio/          SFX/BGM
  9.SO/             AttackData, SkillData, WaveData
```

## 전체 흐름

```text
StartScene
  StartMenuController
  - 마우스 클릭, Enter/Space, 게임패드 Start/A 입력을 받음
  - LoadingScene으로 이동

LoadingScene
  LoadingSceneController
  - 최소 로딩 시간 후 GameScene을 비동기 로드

GameScene
  GameManager
  - 게임 상태 Ready / Playing / Clear / GameOver 관리

  PlayerInputReader
  - New Input System InputAction으로 키보드와 게임패드 입력을 통합

  PlayerMovement2D
  - 좌우 이동, 점프, 2단 점프, 대시 처리

  PlayerCombat
  - C 입력으로 기본 공격 Hitbox 활성화

  PlayerSkillController
  - A/S 또는 LB/RB 입력으로 스킬 2개 실행
  - SkillSlotBarUI에서 슬롯을 드래그하면 실제 스킬 순서도 교체

  Hitbox -> Hurtbox -> Health
  - 공격 판정이 Hurtbox를 감지
  - Hurtbox가 Health에 DamageInfo 전달
  - Health가 팀 체크, 체력 감소, 사망 이벤트 처리

  CombatFeedback
  - 피격 시 넉백, 히트스톱, 피격 플래시, 카메라 쉐이크 실행

  WaveManager
  - WaveData 순서대로 적 스폰
  - 모든 적 처치 시 다음 웨이브
  - 모든 웨이브 클리어 시 GameManager.ClearGame()
```

## 현재 코드 구조 의도

### 입력

`PlayerInputReader`는 입력만 담당합니다.  
이동, 점프, 대시, 공격, 스킬 입력을 다른 시스템이 읽기 쉬운 값으로 제공합니다.

이 구조를 사용하면 키보드/패드 바인딩을 바꿔도 `PlayerMovement2D`, `PlayerCombat`, `PlayerSkillController` 코드는 거의 건드리지 않아도 됩니다.

### 전투 판정

전투 판정은 `Hitbox / Hurtbox / Health`로 나눴습니다.

- `Hitbox`: 공격하는 쪽 판정
- `Hurtbox`: 맞는 쪽 판정
- `Health`: 체력, 팀 체크, 사망 처리
- `DamageInfo`: 데미지, 넉백, 히트스톱 정보를 묶어서 전달

이 방식은 적 종류가 6종으로 늘어나도 공통 피격 처리를 재사용할 수 있습니다.

### 스킬

스킬은 `SkillData` ScriptableObject로 분리했습니다.

현재는 다음 2개가 들어가 있습니다.

- `DashAttack`
- `AreaAttack`

추후 투사체, 내려찍기, 공중 공격, 패링 같은 스킬도 `SkillData`와 실행 로직을 추가하는 방식으로 확장할 수 있습니다.

### 적 AI

현재 적 AI는 과제 필수 조건에 맞춰 단순하게 구성했습니다.

- `MeleeChargerEnemy`: 접근 후 근접 공격
- `RangedShooterEnemy`: 거리 유지 후 투사체 발사

공통 상태 흐름은 다음과 같습니다.

```text
Idle -> Chase -> Attack -> Dead
```

## 앞으로 추가할 기능 계획

### 적 6종 확장

추천 구성:

```text
1. MeleeChargerEnemy
   - 현재 구현됨
   - 플레이어 접근 후 근접 공격

2. RangedShooterEnemy
   - 현재 구현됨
   - 거리 유지 후 투사체 공격

3. ShieldEnemy
   - 전방 공격 방어
   - 뒤나 회피 후 공격 유도

4. JumperEnemy
   - 점프 접근 / 내려찍기
   - 플랫폼 액션 느낌 강화

5. BomberEnemy
   - 접근 후 자폭 또는 범위 공격
   - 회피 타이밍 테스트용

6. EliteEnemy
   - 근접 + 원거리 패턴 혼합
   - 마지막 웨이브용
```

구현 순서는 `MeleeChargerEnemy`, `RangedShooterEnemy`를 기준으로 새 클래스를 추가하고, 공통 기능은 `EnemyBrainBase`에 유지하는 방식이 좋습니다.

### 플레이어 캐릭터 2종 + 탭 교체

추가할 구조:

```text
PlayerCharacterData
  - 캐릭터 이름
  - Sprite / Animator
  - 기본 공격 AttackData
  - SkillData 2개
  - 이동 속도 / 점프력 / 체력

PlayerCharacterSwitcher
  - Tab 입력 감지
  - 현재 캐릭터 데이터 교체
  - Sprite / Animator / Skill / Attack / Stat 적용
  - 교체 순간 전용 스킬 또는 짧은 무적 적용 가능
```

추천 방향:

- 플레이어 GameObject는 하나만 유지
- 내부 데이터와 비주얼만 교체
- 체력은 공유할지, 캐릭터별 체력으로 나눌지 먼저 결정

과제 제출용으로는 공유 체력이 더 안전합니다.  
포트폴리오 강조용이면 캐릭터별 역할을 나누는 게 좋습니다.

```text
Character A: 빠른 근접형
Character B: 느리지만 범위/투사체 강한 타입
```

### 체력바 / HUD

추가할 UI:

```text
Player HP Bar
Wave 표시
남은 적 수
Skill Slot Cooldown
Game Clear / Game Over Panel
```

현재 `HUDView` 기본 구조가 있으므로 다음 단계에서는 `Health.OnDamaged` 이벤트를 받아 HP Slider를 갱신하면 됩니다.

## 2D 스프라이트를 받은 뒤 진행 순서

스프라이트를 받으면 바로 코드부터 건드리지 말고, 아래 순서로 진행합니다.

1. `Assets/4.Sprite/Player`, `Assets/4.Sprite/Enemy` 폴더 정리
2. 스프라이트 Import Setting 확인
   - Texture Type: `Sprite (2D and UI)`
   - Sprite Mode: 단일이면 `Single`, 시트면 `Multiple`
   - Pixels Per Unit 통일
   - Filter Mode는 픽셀아트면 `Point`, 일반 일러스트면 `Bilinear`
3. Player 프리팹의 `SpriteRenderer.sprite` 교체
4. Enemy 프리팹 6종 생성
5. Animator Controller 생성
   - Idle
   - Run
   - Jump
   - Dash
   - Attack
   - Skill
   - Hit
   - Dead
6. 공격 애니메이션에 Animation Event 추가
   - `Hitbox_On`
   - `Hitbox_Off`
7. Hitbox 위치/크기 조정
8. 카메라 쉐이크, 히트스톱, 넉백 수치 조정
9. 웨이브 배치 조정
10. 플레이 영상 녹화

## 다음 작업 우선순위

1. Player HP Bar 연결
2. 기본 공격을 애니메이션 이벤트 기반으로 변경
3. 스킬 쿨다운 UI 표시
4. 적 6종 설계 및 프리팹화
5. 플레이어 캐릭터 2종 데이터 구조 추가
6. Tab 캐릭터 교체
7. 실제 스프라이트/애니메이션 적용
8. 타격 이펙트와 SFX 추가

## 제출 전 체크리스트

- 시작 메뉴에서 마우스 클릭으로 시작 가능
- 게임패드로 메뉴 시작 가능
- 키보드/패드 이동, 점프, 대시, 공격, 스킬 동작
- 5웨이브 클리어 가능
- 게임오버 처리 가능
- 콘솔 Error 없음
- `Library/` 폴더 제외
- 플레이 영상 포함
