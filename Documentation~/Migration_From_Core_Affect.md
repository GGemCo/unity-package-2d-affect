# Migration (A안): Core 기존 AffectController 완전 교체 -> com.ggemco.2d.affect

## 1. 요약
Core의 기존 설계(`CharacterStat -> AffectController -> AffectBase`)는 **"Affect 1개 = Status 1개"** 모델입니다.
새 패키지는 **"Affect 1개 = Modifier N개"**를 정석으로 하며, Tick/Damage/State 등 다양한 효과를 동일 파이프라인으로 처리합니다.

## 2. A안(권장) 변경 순서

> A안은 **Core의 기존 AffectController/AffectBase 흐름을 더 이상 사용하지 않고**,
> 모든 어펙트 적용을 `com.ggemco.2d.affect`의 `AffectComponent`로 완전히 전환합니다.

### 2.1 Core 코드 레벨 전환
1) 기존 API 사용처 수집
- `ApplyAffect(...)`, `RemoveAffect(...)`, `HasAffect(...)`, `UpdateAffect(...)` 등 기존 Affect 관련 호출 지점을 전체 검색합니다.

2) 캐릭터(공통 베이스)에서 Affect 진입점을 단일화
- Player/Monster/NPC 공용 베이스(예: `CharacterBase`)에 다음과 같은 진입점을 추가합니다.
  - `AffectComponent Affect { get; }` 또는 `GetComponent<AffectComponent>()` 캐싱
  - (선택) `ApplyAffect(int uid, float durationOverride = 0)` 같은 래퍼 메서드

3) 기존 AffectController/AffectBase는 Deprecated 처리 후 제거
- 1차: 컴파일 에러를 피하기 위해 `[Obsolete]` 적용 + 내부에서 아무 동작도 하지 않게 만든 뒤, 신규 시스템 전환 완료 후 삭제합니다.
- 2차: 완전 삭제(필드/클래스/테이블 의존 코드 포함)

### 2.2 프리팹/씬 레벨 전환
4) 모든 캐릭터 프리팹에 컴포넌트 추가
- `AffectComponent`
- `IAffectTarget` 구현 컴포넌트(프로젝트 상황에 따라)
  - Core에 공용 `CharacterAffectTargetAdapter : MonoBehaviour, IAffectTarget`를 두는 방식이 가장 간단합니다.
  - 샘플은 본 패키지 `Samples~/CoreIntegration`에 포함되어 있습니다.

5) 기존 호출부를 신규 API로 일괄 교체
- 기존: `characterStat.ApplyAffect(affectUid, duration)`
- 신규: `character.GetComponent<AffectComponent>().ApplyAffect(affectUid, new AffectApplyContext { DurationOverride = duration })`

### 2.3 데이터(테이블) 전환
6) Status 3분리
- 기존 `status.txt`를 아래 3개로 분리합니다.
  - `stat.txt` : 속성(스탯/저항)
  - `damage_type.txt` : 데미지 타입
  - `state.txt` : 상태이상/면역 플래그

7) Affect 정석 확장(Modifier 서브테이블)
- 기존 `affect.txt`(Core 포맷) -> 신규 `affect.txt` + `affect_modifier.txt`

## 3. 테이블 변환 규칙(기본)
### 3.1 Core `affect.txt` 1행 -> 신규 2행(affect + affect_modifier)
- 신규 `affect.txt`
  - `Uid` = 기존 Uid
  - `DispelType` = 기존 Type(Buff/Debuff)
  - `GroupId` = 기존 Group
  - `BaseDuration` = 기존 Duration
  - `TickInterval` = 기존 TickTime
  - `StackPolicy` = 기본 `Refresh` (프로젝트 룰에 맞게 조정)
  - `VfxUid/VfxScale/VfxOffsetY` = 기존 EffectUid/Scale/PositionY
- 신규 `affect_modifier.txt`
  - `AffectUid` = 기존 Uid
  - `Kind` = 기존 StatusID가 스탯이면 `Stat`
  - `StatId` = 기존 StatusID
  - `StatValueType` = 기존 Suffix(Plus/Minus => Flat, Increase/Decrease => Percent)
  - `StatValue` = 기존 Value
  - `Phase` = `OnApply`

### 3.2 TickTime>0 이면서 DOT/회복을 구성할 경우
- `Kind=Damage` 또는 회복을 별도 DamageType/HealType으로 처리
- `Phase=OnTick`

## 4. 주의 사항
- 새 패키지는 Core의 `TableLoaderManager`에 의존하지 않습니다.
  - 로딩은 게임/코어/스킬의 로더에서 수행하고, `AffectRuntime.AffectRepository`에 주입하는 방식이 정석입니다.
- `State`는 Core에 별도 시스템이 없으므로, 프로젝트에서 상태이상 컴포넌트를 먼저 설계한 뒤 `IStateMutable` 어댑터로 연결하십시오.

## 5. 권장: Core 통합 패턴(간단 버전)
1) Core에 `CharacterAffectTargetAdapter` 추가(IAffectTarget 구현)
- `CharacterBase`의 스탯/피해/상태 시스템을 `IStatMutable / IDamageReceiver / IStateMutable`로 매핑

2) Core의 로딩 단계에서 테이블 로드 후 레포지토리 주입
- `AffectRuntime.AffectRepository = new InMemoryAffectRepository();`
- `AffectRuntime.StatusRepository = new InMemoryStatusRepository();`
- 테이블 파싱 후 `Register(...)`로 모두 등록

3) 스킬/AI/이벤트 등에서 어펙트는 항상 `AffectComponent`로 적용
- “어디서 적용하든 결과가 동일”하도록, 어펙트 적용의 단일 진입점을 지킵니다.
