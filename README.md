# 🎮 Crimson Slash 포트폴리오

# ⚔️ 프로젝트 개요

> 애니메이션 이벤트 기반 전투 판정과  
> 스킬 / 패링 / 웨이브 / 보스전을 중심으로 구현한 2D 플랫폼 액션 프로젝트

> PC / Steam 환경의 2D 액션 게임을 목표로,  
> 짧은 플레이 안에서 이동, 공격, 회피, 타격감, 성장형 전투 연출이 보이도록 구성했습니다.

• 개발 인원: 1인  
• 개발 기간: 2026.05.16 ~ 2026.05.20 (5일)
• 개발 환경: Unity 6, C#  
• 주요 기술: New Input System, Animation Event Hitbox, ScriptableObject, Object Pool, UGUI

=======
## 🎥 동영상

[![영상 미리보기](https://youtu.be/WMT9xRD1Y8A/0.jpg)](https://youtu.be/WMT9xRD1Y8A) 

## 📑 목차

- 🔍 Crimson Slash에서 중점적으로 구현한 것
- 🎮 Core Systems (핵심 시스템)
  - ⚔ 전투 판정 구조 (Animation Event Hitbox)
  - 🗡 기본 공격과 스킬 시스템
  - 🛡 패링 / 가드 시스템
  - 🧠 Enemy AI & Boss Wave
  - 🌊 Wave / Clear Flow
  - 🎒 버프 아이템 & 파괴 오브젝트
  - 💥 포커스 모드
  - 🔊 사운드 시스템
  - 🛠 오브젝트 풀 & 최적화
- 🧪 게임플레이 Showcase
- 📌 포트폴리오 등록용 요약

---

## 🔍 Crimson Slash에서 중점적으로 구현한 것

> 이 프로젝트는 단순히 기능을 많이 넣는 것보다,  
> 액션 게임에서 중요한 "입력 → 모션 → 판정 → 피드백" 흐름이 자연스럽게 이어지는 것을 목표로 했습니다.

> 특히 공격 버튼을 누르는 즉시 데미지를 주는 방식이 아니라,  
> 실제 검이 휘둘러지는 애니메이션 프레임에 맞춰 Hitbox를 열고 닫는 구조로 구현했습니다.

<details>
<summary><b>좀 더 자세한 설계 의도 펼치기/닫기</b></summary>

### 1️⃣ 타격 타이밍 분리

- 입력은 `PlayerInputReader`에서 수집
- 공격 실행은 `PlayerCombat`, `PlayerSkillController`에서 처리
- 실제 데미지 판정은 애니메이션 이벤트로 Hitbox를 여는 순간 발생
- 데미지 처리 이후 HitStop, Camera Shake, Damage Text, SFX를 한 흐름으로 연결

버튼 입력 시점과 데미지 적용 시점을 분리해  
공격 모션과 타격감이 어긋나지 않도록 구성했습니다.

### 2️⃣ 공통 데미지 구조

플레이어, 적, 파괴 오브젝트가 서로 다른 방식으로 데미지를 받지 않도록  
`Hitbox -> Hurtbox -> Health -> CombatFeedback` 흐름으로 통일했습니다.

이 구조 덕분에 평타, 스킬, 투사체, 상자 파괴가 같은 데미지 처리 흐름을 사용할 수 있습니다.

### 3️⃣ 데이터 중심 조절

공격 수치, 스킬 쿨타임, 웨이브 구성은 코드에 직접 박지 않고  
`AttackData`, `SkillData`, `WaveData` ScriptableObject로 분리했습니다.

밸런스 조절이나 스킬 교체가 필요할 때  
코드를 계속 수정하지 않고 데이터 수정으로 대응할 수 있도록 구성했습니다.

</details>

---

# 🎮 Core Systems (핵심 시스템)

## ⚔ 전투 판정 구조 (Animation Event Hitbox)

> 전투 판정은 단순히 버튼 입력 시점에 실행하지 않고,  
> 애니메이션 이벤트를 통해 실제 타격 프레임에서만 Hitbox를 활성화하는 방식으로 구현했습니다.

### 구현 방식

- `Hitbox`가 공격 데이터와 공격 팀 정보를 받아 판정 시작
- `Hurtbox`가 피격 대상의 데미지 수신 지점 역할 수행
- `Health`가 실제 체력 감소, 사망 이벤트, 피격 이벤트 처리
- `CombatFeedback`이 데미지 텍스트, 카메라 흔들림, 히트스톱, 피격 이펙트 처리
- Animation Event에서 `OpenAttackHitbox()`, `CloseAttackHitbox()` 호출

```text
Attack Animation
  -> Animation Event: OpenAttackHitbox()
  -> Hitbox detects Hurtbox
  -> Health.TakeDamage()
  -> CombatFeedback
  -> Animation Event: CloseAttackHitbox()
```

> 공격 모션 중 검이 실제로 지나가는 프레임에서만 데미지가 들어가도록 하여  
> 버튼 입력과 타격 타이밍이 분리된 액션 게임 구조를 만들었습니다.

🔗 Combat 코드: [Assets/3.Script/Combat](Assets/3.Script/Combat)  
🔗 Player Combat 코드: [PlayerCombat.cs](Assets/3.Script/Player/PlayerCombat.cs)

---

## 🗡 기본 공격과 스킬 시스템

> 기본 공격은 1타 / 2타 콤보로 구성하고,  
> 스킬은 서로 다른 전투 역할을 갖도록 4종으로 분리했습니다.

### 기본 공격

- 1타 / 2타 콤보
- 공격 중 이동 잠금
- 공격 타이밍에 짧은 전진 보정
- 애니메이션 이벤트 기반 Hitbox On / Off
- 피격 시 히트스톱, 데미지 텍스트, 피격 사운드 출력

### 스킬 구성

| 스킬 | 역할 | 구현 |
|------|------|------|
| Dash Attack | 관통형 돌진 공격 | 빠르게 이동하며 경로상의 적을 순서대로 타격 |
| Ground Slam | 광역 충격파 | 자동 점프 후 내려찍기, 착지 지점 원형 판정 |
| Projectile Slash | 원거리 검기 | 바라보는 방향으로 투사체 발사, 벽/수명 처리 |
| Rising Slash | 공중 연계 공격 | 1타로 적을 띄우고 2타로 밀어내기 |

> 스킬을 단순히 데미지만 다른 기술로 만들지 않고,  
> 이동기, 광역기, 원거리 견제, 공중 연계처럼 서로 다른 사용 목적을 갖도록 구성했습니다.

🔗 Skill 코드: [Assets/3.Script/Skill](Assets/3.Script/Skill)  
🔗 Skill Data: [Assets/9.SO/Skill](Assets/9.SO/Skill)

---

## 🛡 패링 / 가드 시스템

> 방어 입력은 성공 타이밍에 따라 패링과 가드로 나뉘도록 구현했습니다.

### 구성

- `X` 입력 시 Block 애니메이션 실행
- 정확한 타이밍에 막으면 패링 성공
- 패링 성공 시 데미지 무효
- 패링 타이밍은 늦었지만 가드 중이면 데미지 감소
- 투사체는 패링 성공 시 반사
- 근접 공격은 패링 성공 시 데미지만 막고, 적 공격 자체는 끊지 않음

| 결과 | 처리 |
|------|------|
| Parry Success | 데미지 0, Guard1 사운드, 투사체 반사 가능 |
| Guard Success | 데미지 감소, Guard2 사운드 |
| Fail | 일반 피격 처리 |

> 패링이 적 공격을 무조건 끊어버리면 전투가 쉽게 무너질 수 있어,  
> 근접 공격은 데미지만 방어하고 적의 공격 흐름은 유지하도록 처리했습니다.

🔗 Guard 코드: [PlayerGuard.cs](Assets/3.Script/Player/PlayerGuard.cs)

---

## 🧠 Enemy AI & Boss Wave

> 적 AI는 공통 상태 흐름을 `EnemyBrainBase`에 두고,  
> 적 종류별 차이는 파생 클래스에서 처리했습니다.

### 공통 상태

```text
Idle
Patrol
Chase
Attack
Hit
Dead
```

### 적 구성

| 적 | 역할 | 특징 |
|----|------|------|
| Flying Eye | 비행 근접 적 | 지형을 무시하고 플레이어에게 접근 |
| Goblin | 기본 근접 적 | 순찰하다가 플레이어 감지 시 추적 |
| Mushroom | 원거리 적 | 거리를 두고 포물선 투사체 발사 |
| Skeleton | 강한 지상 적 | 느리지만 높은 체력과 공격력 |
| Elite Skeleton Boss | 미니보스 | 2페이즈 전환, 누적 데미지 기반 경직 |

### Boss 설계

- 마지막 웨이브에 미니보스 등장
- 체력 50% 이하에서 2페이즈 진입
- 2페이즈 진입 시 이동 속도 / 색상 / 연출 변화
- 일반 몬스터처럼 매 타격마다 Hit 모션이 끊기지 않음
- 누적 데미지가 일정량 쌓였을 때만 짧은 경직 발생

> 보스가 매 타격마다 멈추면 압박감이 떨어지기 때문에,  
> 데미지는 정상적으로 받되 경직은 제한적으로만 발생하도록 설계했습니다.

🔗 Enemy 코드: [Assets/3.Script/Enemy](Assets/3.Script/Enemy)  
🔗 Wave Data: [Assets/9.SO/Wave](Assets/9.SO/Wave)

---

## 🌊 Wave / Clear Flow

> 단순 테스트 맵이 아니라, 시작부터 클리어까지 흐름이 보이도록 웨이브 구조를 구성했습니다.

### 흐름

```text
StartScene
  -> LoadingScene
    -> GameScene
      -> Wave Countdown
      -> Enemy Spawn
      -> Wave Clear
      -> Next Wave
      -> Boss Wave
      -> Game Clear
```

### 구현 내용

- 웨이브 시작 전 카운트다운
- 현재 처치 수 / 전체 적 수 UI 표시
- 웨이브 클리어 텍스트 출력
- 마지막 웨이브 클리어 시 Clear UI 표시
- 게임오버 / 클리어 후 다시하기, 처음으로 버튼 제공
- Esc 입력 시 일시정지 메뉴 표시

> 플레이 영상에서 게임의 시작, 진행, 마무리가 명확히 보이도록  
> 전투 루프를 웨이브 단위로 정리했습니다.

🔗 WaveManager 코드: [WaveManager.cs](Assets/3.Script/Wave/WaveManager.cs)

---

## 🎒 버프 아이템 & 파괴 오브젝트

> 상자 같은 월드 오브젝트를 공격하면 부서지고,  
> 일정 확률로 즉시 적용형 버프 아이템이 드랍되도록 구현했습니다.

### 파괴 오브젝트

- `BreakableObject`가 `Health.OnDead`를 구독
- 공격을 받으면 체력 감소
- 사망 시 파괴 이펙트, 카메라 쉐이크, 드랍 처리
- 웨이브 또는 스테이지 흐름에 맞춰 재배치 가능

### 버프 아이템

| 아이템 | 효과 |
|--------|------|
| Haste Orb | 일정 시간 이동 속도 증가, 대쉬 잔상 강화 |
| Power Orb | 일정 시간 공격력 증가, 붉은 잔상 연출 |
| Focus Orb | 포커스 게이지 획득 |
| Heal Orb | 체력 회복 |

> 인벤토리 없이 즉시 획득되는 구조로 만들고,  
> HUD에 남은 시간을 표시해 전투 중 현재 버프 상태를 확인할 수 있도록 했습니다.

🔗 Item 코드: [Assets/3.Script/Item](Assets/3.Script/Item)  
🔗 World 코드: [BreakableObject.cs](Assets/3.Script/World/BreakableObject.cs)

---

## 💥 포커스 모드

> 몬스터 처치로 게이지를 모으고,  
> 게이지가 가득 차면 전투 상황을 뒤집을 수 있는 포커스 모드를 사용할 수 있습니다.

### 구현 내용

- 몬스터 처치 시 포커스 오브가 사망 위치에서 생성
- 오브는 플레이어 중심으로 흡수되며 게이지 증가
- 포커스 모드 발동 시 플레이어 주변 이펙트 출력
- 일정 시간 동안 플레이어 입력 잠금 후 모드 진입
- 적, 투사체, 일부 연출은 슬로우 처리
- 주변 투사체 제거
- 주변 적에게 넉백 적용
- 배경 레이어 일부를 어둡게 처리해 집중 연출 강화

> 리소스가 많지 않은 상황에서도 전투의 "결정적인 순간"을 만들기 위해  
> 시간 조작, 이펙트, 넉백, 투사체 제거를 하나의 모드로 묶었습니다.

🔗 Focus 코드: [FocusModeController.cs](Assets/3.Script/Core/FocusModeController.cs)

---

## 🔊 사운드 시스템

> `Resources/Sound` 아래에 사운드 파일을 넣으면  
> `SoundManager`가 이름 기반으로 로드해 전투 상황에 맞게 재생하도록 구성했습니다.

### 사용 예시

| 사운드 | 사용 위치 |
|--------|-----------|
| MainBGM | GameScene BGM |
| weapon-sound | 기본 공격 휘두름 |
| beatt-sound1 / 2 | 일반 피격 랜덤 |
| beatt-sound-dashAttack | 대쉬공격 발동 |
| beatt-sound-dashAttack1 / 2 / 3 | 대쉬공격 피격 |
| beatt-sound-SwordAreaAttack | 검기 발사 |
| beatt-sound-SlamShockwave | 충격파 착지 |
| Guard1 | 패링 성공 |
| Guard2 | 가드 성공 |
| hitting | 플레이어 피격 |
| buff_1 | 버프 획득 |

> 기능마다 전용 AudioSource를 과하게 늘리지 않고,  
> 매니저에서 BGM과 SFX를 나누어 관리하는 방식으로 정리했습니다.

🔗 SoundManager 코드: [SoundManager.cs](Assets/3.Script/Manager/SoundManager.cs)

---

## 🛠 오브젝트 풀 & 최적화

> 전투 중 반복 생성되는 오브젝트는 `Instantiate / Destroy`를 반복하지 않고  
> Object Pool로 재사용하도록 구성했습니다.

### 풀링 대상

- 플레이어 / 적 투사체
- 피격 이펙트
- 패링 이펙트
- 포커스 오브
- 버프 아이템
- 대쉬 잔상
- 파괴 오브젝트 이펙트
- 데미지 텍스트

### 최적화 방향

- 자주 사용하는 프리팹은 GameScene의 ObjectPool에 미리 등록
- 수명이 있는 이펙트는 `TimedAutoRelease`로 자동 반환
- 대쉬 잔상은 생성 후 파괴하지 않고 풀에 반환
- 주요 컴포넌트는 인스펙터 참조 또는 Awake에서 1회 캐싱
- 런타임 `Find`는 씬 전환/복구용 fallback에만 제한

> 2D 액션 게임은 짧은 시간에 투사체, 피격 이펙트, 텍스트가 반복 생성되므로  
> 풀링 구조를 적용해 GC와 프레임 드랍 가능성을 줄였습니다.

🔗 ObjectPool 코드: [ObjectPool.cs](Assets/3.Script/Utility/ObjectPool.cs)
