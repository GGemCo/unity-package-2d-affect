# Affect 패키지 중요한 클래스 정리

이 문서는 업로드된 `affect_runtime.zip`, `affect_editor.zip`과 프로젝트 문서(`ARCHITECTURE.md`, `PACKAGE_DEPENDENCY.md`)를 기준으로, **Affect 패키지에서 우선적으로 이해해야 하는 핵심 클래스**를 Runtime / Editor로 나누어 정리한 문서입니다.

---

## 1. 패키지 역할 요약

Affect 패키지는 버프/디버프 중심의 **상태 효과 시스템**입니다.

핵심 책임은 다음과 같습니다.

- Affect 정의 로딩 및 런타임 저장소 구성
- 대상(Character)에 Affect 적용 / 갱신 / 해제
- Modifier 실행(Stat / Damage / Heal / State / CrowdControl / Affect 전파)
- Core 패키지의 VFX, Outline, UI, 캐릭터 상태 시스템과 브리지 연결
- Editor에서 Affect 테스트, 설명 검증, 테이블 편집 지원

프로젝트 문서 기준으로도 Affect는 정의, 실행기, Repository, Bridge, UI/Localization 계층으로 나뉘는 패키지이며, Core 시스템과 브리지 계층을 통해 연결되도록 설계되어 있습니다.

---

## 2. 전체 구조 한눈에 보기

### Runtime 흐름

1. `TableLoaderManagerAffect`가 Affect 관련 테이블을 로드합니다.
2. `AffectRuntimeBootstrapStep`가 테이블 데이터를 `AffectDefinition`, `AffectModifierDefinition`으로 변환하여 런타임 저장소를 초기화합니다.
3. `AffectBridgeRegistrar`가 Core 브리지, VFX 서비스, Outline 서비스를 등록합니다.
4. 각 캐릭터의 `AffectComponent`가 실제 Affect 인스턴스를 생성/업데이트/삭제합니다.
5. `AffectComponent` 내부에서 `DamageExecutor`, `HealExecutor`, `StatModifierExecutor`, `StateExecutor`, `CrowdControlExecutor`, `ApplyAffectToTargetExecutor`가 Modifier를 페이즈별로 실행합니다.
6. `PlayerAffectUiPresenter`, `PlayerAffectHudVisualStatePresenter`, `AffectDescriptionService` 등이 UI 및 표시 계층을 담당합니다.

### Editor 흐름

1. `UseAffect`가 Play Mode에서 Affect를 직접 테스트합니다.
2. `AffectDescriptionDebugWindow`가 설명 문자열과 로컬라이징 결과를 검증합니다.
3. `AffectTableEditorModule`이 범용 테이블 편집기와 Affect 테이블을 연결합니다.
4. `AddressableEditorAffect` 및 하위 설정 클래스가 Addressables / 리소스 설정을 지원합니다.

---

# 3. Runtime 핵심 클래스

## 3.1 가장 먼저 봐야 하는 클래스

### `AffectComponent`
**역할:** 캐릭터 단위의 실제 Affect 실행 컨트롤러

Affect 패키지에서 가장 중요한 런타임 클래스입니다. `MonoBehaviour`로 붙어 있으며, 특정 대상(`IAffectTarget`)에 적용된 어펙트를 실제로 관리합니다.

주요 책임:
- 활성 Affect 인스턴스 목록 관리
- runtimeId, affectUid, groupId 기준 인덱싱
- `Update()`에서 남은 시간 감소, Tick 누적, 만료 처리 수행
- OnApply / OnTick / OnExpire / OnHit 시점에 Modifier Executor 호출
- 구조 변경 시 `Changed` 이벤트 발행

핵심 포인트:
- 순회 중 컬렉션 변경 예외를 피하기 위해 snapshot/list를 사용합니다.
- 동일 그룹, 동일 UID, 스택/리프레시 정책까지 이 클래스가 실질적으로 책임집니다.
- UI나 HUD는 이 클래스의 `Changed` 이벤트와 현재 인스턴스 스냅샷을 기준으로 동기화됩니다.

이 클래스를 이해하면 Affect 시스템의 실제 실행 방식 대부분을 파악할 수 있습니다.

---

### `AffectInstance`
**역할:** 적용된 Affect 1건의 런타임 상태 객체

`AffectDefinition`의 정적 정의를 실제 대상에 적용한 결과물입니다.

주요 보관 데이터:
- 원본 정의(`Definition`)
- 적용 컨텍스트(`Context`)
- 현재 스택 수(`Stacks`)
- 남은 시간(`RemainingTime`)
- 총 지속시간(`TotalDuration`)
- Tick 누적치(`TickElapsed`)
- Stat / State 원복용 토큰

핵심 포인트:
- “정의 데이터”와 “실행 중 상태”를 분리하는 중심 객체입니다.
- 만료 시점에 어떤 것을 되돌려야 하는지 추적하기 위한 토큰 저장소 역할도 합니다.
- UI에서 남은 시간, 스택 수를 보여줄 때도 이 객체가 기준입니다.

---

### `AffectRuntime`
**역할:** Affect 런타임 전역 서비스 진입점

정적 클래스이며, Affect 런타임이 참조하는 공용 서비스들을 보관합니다.

보관 서비스:
- `IAffectDefinitionRepository AffectRepository`
- `IStatusDefinitionRepository StatusRepository`
- `IAffectVfxService VfxService`
- `IAffectOutlineService OutlineService`

핵심 포인트:
- 기본값은 InMemory / Null Object 구현입니다.
- 실제 게임 초기화 단계에서 부트스트랩이나 브리지 등록을 통해 교체됩니다.
- `AffectComponent`가 Awake 시점에 이 전역 진입점을 읽어 바인딩합니다.

---

## 3.2 부트스트랩 / 패키지 진입점

### `AffectRuntimeBootstrapStep`
**역할:** 테이블 데이터를 런타임 저장소로 변환하는 로딩 스텝

게임 로딩 파이프라인에서 실행되며, Affect 런타임이 실제로 동작하기 위한 저장소를 초기화합니다.

주요 책임:
- Core의 `stat`, `damage_type`, `state` 테이블을 읽어 `InMemoryStatusRepository` 구성
- Affect 테이블을 읽어 `AffectDefinition` 생성
- Affect Modifier 테이블을 읽어 `AffectModifierDefinition` 목록 구성
- Affect Visual Action 테이블을 읽어 `AffectVisualActionDefinition` 연결
- 최종 결과를 `AffectRuntime` 정적 진입점에 주입

핵심 포인트:
- Affect 데이터 모델과 실제 로드된 테이블 사이의 변환 계층입니다.
- 런타임에서는 raw table row를 직접 쓰기보다, 이 단계에서 정규화된 정의 객체를 사용합니다.

---

### `AffectPackageManager`
**역할:** 게임 씬 기준 Affect 패키지 수명 관리

패키지 단위 런타임 진입점입니다.

주요 책임:
- 게임 씬에서만 유효한 싱글톤 유지
- `SceneGame` 생명주기와 연동
- 씬 종료 시 자기 자신 정리

핵심 포인트:
- 시스템 로직을 직접 실행한다기보다, Affect 패키지가 씬 수명에 맞춰 살아있도록 하는 관리자입니다.
- 구조적으로 `SceneGame`과 비슷한 위치의 패키지 매니저입니다.

---

### `AffectBridgeRegistrar`
**역할:** Core 브리지 및 표시 서비스 자동 등록

런타임 초기화 시점(`BeforeSceneLoad`)에 자동 실행됩니다.

주요 책임:
- Core 쪽 `AffectBridge`에 `AffectDescriptionProvider` 등록
- `AffectRuntime.VfxService`에 `CoreAffectVfxService` 연결
- `AffectRuntime.OutlineService`에 `CoreAffectOutlineService` 연결

핵심 포인트:
- Core가 Affect를 직접 참조하지 않도록 브리지 패턴을 유지하는 핵심 클래스입니다.
- 패키지 의존성 계약을 지키기 위한 중요한 연결 지점입니다.

---

## 3.3 데이터 정의 / 테이블 계층

### `AffectDefinition`
**역할:** Affect 1건의 정규화된 런타임 정의

주요 필드:
- 식별: `uid`, `groupId`, `tags`
- 표시: `nameKey`, `iconKey`, `uiHudVisualStateKey`
- 지속/스택: `baseDuration`, `tickInterval`, `stackPolicy`, `maxStacks`, `refreshPolicy`
- 제거 규칙: `dispelType`
- 시각 요소: `visualActions`, `useOutline`, `outlinePixelSize`, `outlineColor`
- 적용 확률: `applyChance`

핵심 포인트:
- Affect의 “설계서”에 해당합니다.
- 런타임은 이 정의를 복사하지 않고 참조하면서, 변하는 값은 `AffectInstance`에 둡니다.

---

### `AffectModifierDefinition`
**역할:** Affect 내부 Modifier 1행 정의

주요 지원 종류:
- Stat 변경
- Damage / DoT
- Heal
- State 부여
- Crowd Control 실행
- 다른 Affect 추가 적용

핵심 포인트:
- `phase`와 `kind` 조합이 중요합니다.
- 어떤 시점(OnApply / OnTick / OnExpire / OnHit)에, 어떤 종류의 효과를 실행할지 표현합니다.
- 실제 실행은 Executor가 담당하고, 이 클래스는 순수 데이터 정의 역할을 합니다.

---

### `AffectVisualActionDefinition`
**역할:** Affect 시각 효과 실행 정의

주요 필드:
- `phase`
- `vfxUid`
- `vfxPlayMode`
- `vfxScale`
- `vfxOffsetY`
- `vfxPositionType`
- `vfxFollowType`
- `durationOverride`

핵심 포인트:
- 기존 Affect 자체 VFX 속성보다, 더 세밀하게 페이즈별 비주얼 액션을 구성하는 구조입니다.
- `AffectRuntimeBootstrapStep`에서 `TableAffectVisualAction` 데이터를 읽어 구성합니다.

---

### `TableLoaderManagerAffect`
**역할:** Affect 관련 테이블 로딩 관리자

관리 테이블:
- `TableAffect`
- `TableAffectModifier`
- `TableAffectVisualAction`

핵심 포인트:
- Affect 런타임이 필요로 하는 테이블 집합의 진입점입니다.
- 다른 시스템은 이 매니저를 통해 Affect 테이블에 접근하게 됩니다.

---

### `TableAffect`
**역할:** `affect.txt`를 `StruckTableAffect`로 파싱하는 메인 테이블 로더

주요 책임:
- 테이블 한 행을 `StruckTableAffect`로 변환
- 로딩 후 `LocalizationManagerAffect`를 통해 이름 보정
- Affect 기본 속성 파싱

핵심 포인트:
- 에디터 편집 데이터와 런타임 정의 사이의 첫 번째 raw row 계층입니다.
- 직접 게임 로직을 담당하지는 않지만, 전체 Affect 데이터의 출발점입니다.

---

### `TableAffectModifier`
**역할:** `affect_modifier.txt` 서브테이블 파서

주요 책임:
- 탭 구분 텍스트 파싱
- `AffectUid -> List<AffectModifierDefinition>` 구성
- modifier row를 종류별 필드를 가진 정의 객체로 변환

핵심 포인트:
- 테이블 구조상 Affect와 1:N 관계를 가진 Modifier 데이터를 묶어주는 클래스입니다.
- 실제 런타임에서는 `GetModifiers(affectUid)` 결과가 매우 자주 활용됩니다.

---

### `TableAffectVisualAction`
**역할:** Affect용 시각 액션 서브테이블 파서

주요 책임:
- `AffectUid -> List<StruckTableAffectVisualAction>` 구성
- 페이즈별 VFX 액션 정렬/조회 지원

핵심 포인트:
- VFX 액션이 Affect 본문에서 분리된 현재 구조에서는 매우 중요합니다.
- 복수의 비주얼 액션을 순서와 페이즈 기반으로 연결할 수 있게 합니다.

---

## 3.4 저장소 / 인터페이스 계층

### `InMemoryAffectRepository`
**역할:** Affect 정의와 Modifier 목록을 보관하는 인메모리 저장소

주요 책임:
- `AffectDefinition` 등록
- `AffectUid -> Modifier List` 관리
- `TryGetAffect`, `GetModifiers` 제공

핵심 포인트:
- 현 런타임의 기본 저장소 구현입니다.
- `AffectRuntimeBootstrapStep`이 구축한 결과가 최종적으로 이 저장소에 담깁니다.

---

### `InMemoryStatusRepository`
**역할:** Stat / DamageType / State 유효성 및 저항값 조회 저장소

주요 책임:
- 유효한 stat / damageType / state ID 등록
- 존재 여부 검증
- 특정 damageType에 대한 저항 수치 조회

핵심 포인트:
- Affect가 Core 테이블과 안전하게 연결되도록 중간 검증층 역할을 합니다.
- Damage, State, Stat 계열 Modifier 실행 전에 활용되는 기반 저장소입니다.

---

### 주요 인터페이스

#### `IAffectTarget`
Affect가 적용될 대상의 추상화입니다. Transform, 생존 여부, Stats, States, Damage 처리 진입점을 제공합니다.

#### `IStatMutable`
Stat Modifier 적용/해제/재계산을 위한 인터페이스입니다.

#### `IStateMutable`
상태 이상 부여/해제를 위한 인터페이스입니다.

#### `IDamageReceiver`
데미지/회복 계열 처리를 대상에게 전달하는 인터페이스입니다.

#### `IAffectVfxService`
Affect가 요청하는 VFX 재생/정지를 담당하는 서비스 인터페이스입니다.

#### `IAffectOutlineService`
Affect 지속 중 Outline 등의 외형 표시를 담당하는 서비스 인터페이스입니다.

핵심 포인트:
- Affect는 Core 구체 클래스를 직접 알지 않고 인터페이스를 통해 대상에 작동합니다.
- 패키지 의존성 분리 관점에서 매우 중요합니다.

---

## 3.5 Modifier 실행기 계층

### `DamageExecutor`
**역할:** Damage / DoT 계열 Modifier 실행

주요 시점:
- OnApply
- OnTick
- OnExpire
- OnHit

핵심 포인트:
- Dot 처리와 일반 피해 처리를 함께 담당합니다.
- 저항, 스케일링, 크리티컬 가능 여부 같은 Damage 계열 계산 진입점입니다.

---

### `HealExecutor`
**역할:** Heal 계열 Modifier 실행

핵심 포인트:
- 회복 기본값과 스케일링 스탯을 합산하여 대상에게 회복을 전달합니다.
- 구조상 Damage와 대칭적인 위치에 있습니다.

---

### `StatModifierExecutor`
**역할:** Stat Modifier 적용 / 해제

핵심 포인트:
- Apply 시 토큰을 저장하고, Expire 시 토큰을 제거하는 방식이 중요합니다.
- 즉, “증가”뿐 아니라 “되돌리기”까지 책임지는 실행기입니다.

---

### `StateExecutor`
**역할:** 상태 이상(State) 부여 실행

핵심 포인트:
- 확률, 지속시간 오버라이드, 상태 ID 검증 등이 핵심입니다.
- `DontControl`, `Stun` 같은 상태 제어와 연결되는 축입니다.

---

### `CrowdControlExecutor`
**역할:** Crowd Control UID 기반 CC 실행

핵심 포인트:
- Affect가 직접 KnockBack/KnockUp 데이터를 들고 있지 않고, CC UID를 통해 Core/별도 시스템에 위임하는 형태입니다.
- Affect와 Crowd Control 시스템의 연결 지점입니다.

---

### `ApplyAffectToTargetExecutor`
**역할:** 다른 Affect를 대상에게 연쇄 적용

핵심 포인트:
- 발동형 Modifier 구성에 중요합니다.
- 예를 들어 OnHit 시 추가 중독, OnExpire 시 다른 디버프 부여 같은 구조를 만들 수 있습니다.

---

### `IModifierExecutor`
**역할:** Modifier 실행기 공통 계약

핵심 포인트:
- 실행기 계층의 확장성을 위해 존재합니다.
- 새 Modifier Kind를 추가할 때 구조를 통일하는 기준점이 됩니다.

---

## 3.6 Core 브리지 / 어댑터 계층

### `CoreAffectTargetAdapter`
**역할:** Core의 `CharacterBase`를 `IAffectTarget`으로 변환하는 어댑터

주요 책임:
- CharacterBase의 생존 여부, Transform 노출
- Core Stat 시스템을 `IStatMutable` 형태로 감싸기
- Core State 시스템을 `IStateMutable` 형태로 감싸기
- 데미지 수신 시스템을 `IDamageReceiver` 형태로 감싸기

핵심 포인트:
- Affect 런타임이 실제 Core 캐릭터 위에서 동작하게 만드는 가장 중요한 어댑터입니다.
- 패키지 분리 관점에서 매우 중요한 클래스입니다.

---

### `CoreAffectVfxService`
**역할:** Affect의 VFX 요청을 Core VFX 시스템으로 전달

주요 책임:
- OneShot / Follow VFX 재생
- 위치 계산(기본/머리 위치, Y 오프셋)
- 재생 토큰 반환 및 정지 처리

핵심 포인트:
- Affect는 “무슨 VFX를 어느 시점에 재생할지”만 결정하고, 실제 생성/재생은 Core가 담당합니다.

---

### `CoreAffectOutlineService`
**역할:** Affect의 Outline 요청을 Core 렌더링 계층으로 전달

핵심 포인트:
- 지속형 표시 효과를 Affect 정의 기반으로 연결합니다.
- Null 서비스와 교체 가능한 구조라서, Outline이 없는 프로젝트에서도 안전합니다.

---

### `AffectDescriptionProvider`
**역할:** Core 브리지에서 사용할 Affect 설명 제공자

핵심 포인트:
- Core는 직접 `AffectDescriptionService`를 알지 않고, 브리지 등록을 통해 설명 기능만 주입받습니다.

---

## 3.7 UI / 설명 / 표시 계층

### `PlayerAffectUiPresenter`
**역할:** 플레이어 버프 UI 목록 동기화

주요 책임:
- `AffectComponent` 스냅샷 수집
- 같은 Affect UID 기준 집계(스택/최대 남은 시간 등)
- `UIWindowPlayerBuffInfo`에 렌더링 데이터 전달

핵심 포인트:
- UI가 `AffectInstance`를 직접 그리기보다, Presenter가 적절히 집계해서 전달합니다.
- 일정 sync interval로 업데이트하는 구조여서, 매 프레임 UI 재구성을 피합니다.

---

### `PlayerAffectHudVisualStatePresenter`
**역할:** HUD 시각 상태 키 동기화

주요 책임:
- 활성 Affect 중 우선순위가 높은 HUD visual state 선택
- `IAffectHudVisualStateReceiver`에 상태 키 전달

핵심 포인트:
- 단순 버프 아이콘 목록과 별개로, HUD의 큰 상태 표현을 담당합니다.
- 예: 특정 상태 이상일 때 HUD 전체 색감/배지/오버레이를 바꾸는 구조에 적합합니다.

---

### `AffectDescriptionService`
**역할:** Affect 설명 문자열 생성 서비스

주요 책임:
- Affect UID 기준 설명 생성
- Modifier를 순회하며 라인별 설명 조합
- 로컬라이징과 상태/스탯/데미지 타입 이름 해석
- 캐시 관리

핵심 포인트:
- “툴팁/설명문” 계층의 핵심 클래스입니다.
- 실제로는 단순 문자열 테이블 조회가 아니라, Modifier 정의를 읽어 문장을 조립하는 설명 빌더입니다.

---

### `LocalizationManagerAffect`
**역할:** Affect 전용 로컬라이징 접근 지점

핵심 포인트:
- `TableAffect`의 이름 보정이나 설명 생성 시 함께 사용됩니다.
- Affect 전용 키 체계를 캡슐화합니다.

---

## 3.8 부가적으로 중요하지만 2순위인 클래스

### `AffectApplyContext`
적용 시점 파라미터를 담는 컨텍스트 객체입니다. Source, SkillLevel, DurationOverride, ValueMultiplier 등을 보관합니다.

### `DispelQuery`
DispelType, 태그 포함/제외 조건, 최대 제거 수를 묶어서 dispel 동작을 질의형으로 표현하는 클래스입니다.

### `SceneLoadingAffect`
로딩 씬 단계에서 Affect 패키지 초기화에 관여하는 씬 연동 클래스입니다.

### `AddressableLoaderAffect`
Addressables 기반 Affect 리소스 로딩 계층입니다.

---

# 4. Editor 핵심 클래스

## 4.1 가장 먼저 봐야 하는 클래스

### `UseAffect`
**역할:** Play Mode에서 Affect를 직접 테스트하는 대표 툴

이 패키지 Editor 쪽에서 가장 중요한 클래스입니다.

주요 기능:
- 대상 캐릭터 선택
- Affect 선택 드롭다운 구성
- Duration Override, Value Multiplier 설정
- 선택한 Affect 적용
- Modifier 목록 확인
- Affect / Modifier / Core 테이블 재로딩

핵심 포인트:
- 실제 게임 플레이 중 Affect를 빠르게 검증할 수 있는 실전용 디버그 툴입니다.
- 패키지 유지보수 시 가장 먼저 확인할 Editor 클래스입니다.

---

### `AffectTableEditorModule`
**역할:** 범용 TableEditorWindow에 Affect 테이블 정의를 제공하는 모듈

주요 책임:
- Affect, AffectModifier, AffectVisualAction 테이블 정의 생성
- 컬럼/참조 관계 구성
- 각 row 구조를 에디터 편집용 모델로 노출

핵심 포인트:
- Affect 전용 독립 편집기가 아니라, 공용 테이블 편집 시스템에 Affect를 접목시키는 연결부입니다.
- 테이블 구조를 바꾸거나 참조 컬럼을 추가할 때 가장 중요합니다.

---

### `AffectDescriptionDebugWindow`
**역할:** Affect 설명 문자열을 검증하는 디버그 툴

주요 기능:
- Affect 선택
- 현재 설명 생성 결과 확인
- 전체 Affect 설명 txt 내보내기
- 관련 테이블 재로딩

핵심 포인트:
- 설명 생성 로직(`AffectDescriptionService`) 검증용으로 매우 중요합니다.
- 데이터 변경 후 툴팁/설명문이 깨졌는지 확인할 때 먼저 보는 클래스입니다.

---

## 4.2 설정 / 인프라 계층

### `AddressableEditorAffect`
**역할:** Affect Addressables 설정용 통합 EditorWindow

주요 하위 구성:
- `SettingScriptableObjectAffect`
- `SettingTableAffect`
- `SettingAffectImage`

핵심 포인트:
- Affect 패키지 리소스, 테이블, 이미지 관련 셋업을 한 화면에 모읍니다.
- 런타임 로직보다는 패키지 설치/초기 설정 관점에서 중요합니다.

---

### `ConfigEditorAffect`
**역할:** Affect Editor 메뉴 경로 및 정렬 순서 정의

핵심 포인트:
- `MenuItem` 경로를 중앙 관리합니다.
- 새 툴을 추가할 때 이 클래스를 먼저 수정하게 됩니다.

---

### `DefaultEditorWindowAffect`
**역할:** Affect EditorWindow 공통 기반 클래스

핵심 포인트:
- 개별 툴 윈도우의 공통 초기화 흐름을 캡슐화합니다.
- 여러 툴이 동일한 패키지 컨텍스트를 공유하게 합니다.

---

### `GGemCo2DAffectEditor.TableLoaderManagerAffect`
**역할:** Editor에서 Affect 테이블을 편의 로딩하는 정적 유틸리티

핵심 포인트:
- 런타임 싱글톤과 별개로, 에디터 툴이 손쉽게 Affect / Modifier / VisualAction / Core 테이블을 다시 읽을 수 있게 합니다.

---

## 4.3 2순위 설정 클래스

### `SettingTableAffect`
Affect 테이블 Addressables 설정을 담당하는 하위 UI 구성 클래스입니다.

### `SettingAffectImage`
Affect 관련 이미지 리소스 설정을 담당합니다.

### `SettingScriptableObjectAffect`
Affect 관련 ScriptableObject 설정을 담당합니다.

### `DefaultSceneEditorAffect`, `SceneEditorGameAffect`, `SceneEditorLoadingAffect`
씬 설정을 보조하는 에디터 유틸리티 계층입니다.

---

# 5. 실무 기준 우선순위 추천

## 5.1 Runtime 우선순위 TOP 10

1. `AffectComponent`
2. `AffectInstance`
3. `AffectRuntimeBootstrapStep`
4. `AffectDefinition`
5. `AffectModifierDefinition`
6. `InMemoryAffectRepository`
7. `CoreAffectTargetAdapter`
8. `DamageExecutor`
9. `StateExecutor`
10. `AffectDescriptionService`

---

## 5.2 Editor 우선순위 TOP 5

1. `UseAffect`
2. `AffectTableEditorModule`
3. `AffectDescriptionDebugWindow`
4. `AddressableEditorAffect`
5. `ConfigEditorAffect`

---

# 6. 클래스별 추천 읽기 순서

## Runtime 추천 읽기 순서

1. `AffectDefinition`
2. `AffectModifierDefinition`
3. `TableAffect` / `TableAffectModifier` / `TableAffectVisualAction`
4. `AffectRuntimeBootstrapStep`
5. `AffectRuntime`
6. `AffectComponent`
7. `AffectInstance`
8. `CoreAffectTargetAdapter`
9. `DamageExecutor` / `HealExecutor` / `StatModifierExecutor` / `StateExecutor` / `CrowdControlExecutor` / `ApplyAffectToTargetExecutor`
10. `PlayerAffectUiPresenter` / `AffectDescriptionService`

## Editor 추천 읽기 순서

1. `ConfigEditorAffect`
2. `UseAffect`
3. `AffectDescriptionDebugWindow`
4. `AffectTableEditorModule`
5. `AddressableEditorAffect`

---

# 7. 유지보수 관점에서 중요한 포인트

## 7.1 새로운 Modifier Kind를 추가할 때

주로 다음 위치를 함께 봐야 합니다.

- `AffectModifierDefinition`
- `TableAffectModifier.BuildModifier`
- 해당 Executor 추가 또는 기존 Executor 확장
- `AffectDescriptionService`
- `AffectTableEditorModule`
- `UseAffect` / 디버그 툴 확인

---

## 7.2 새로운 시각 효과 규칙을 추가할 때

주로 다음 위치를 함께 봐야 합니다.

- `AffectVisualActionDefinition`
- `TableAffectVisualAction`
- `AffectRuntimeBootstrapStep`
- `CoreAffectVfxService`
- 필요 시 UI/HUD Presenter

---

## 7.3 Core와의 연결이 깨질 때 먼저 볼 클래스

- `AffectBridgeRegistrar`
- `CoreAffectTargetAdapter`
- `CoreAffectVfxService`
- `CoreAffectOutlineService`
- `AffectRuntimeBootstrapStep`

---

# 8. 요약

Affect 패키지의 런타임 중심은 **`AffectComponent` + `AffectInstance` + `AffectRuntimeBootstrapStep`** 조합입니다.

- `AffectComponent`는 실제 적용/틱/만료를 관리합니다.
- `AffectInstance`는 적용된 1건의 상태를 담습니다.
- `AffectRuntimeBootstrapStep`은 테이블을 런타임 정의로 정규화합니다.

그리고 패키지 분리 관점에서는 다음 클래스가 특히 중요합니다.

- `AffectBridgeRegistrar`
- `CoreAffectTargetAdapter`
- `CoreAffectVfxService`
- `CoreAffectOutlineService`

Editor 쪽에서는 실무적으로 다음 3개를 먼저 보면 충분합니다.

- `UseAffect`
- `AffectTableEditorModule`
- `AffectDescriptionDebugWindow`

이 3개만 이해해도 Affect 데이터 테스트, 편집, 설명 검증 흐름을 대부분 따라갈 수 있습니다.
