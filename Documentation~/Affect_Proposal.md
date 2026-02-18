# GGemCo Affect 시스템 확장 제안서 (정석 확장)

## 1. 목적
- Core 패키지의 기존 Affect(버프/디버프) 시스템을 **스킬 시스템과 공용으로 재사용**할 수 있도록 확장한다.
- Player/Monster/NPC 구분 없이 **모든 Character에 동일한 적용 모델**을 제공한다.
- Status를 **속성(Stat) / 데미지 타입(DamageType) / 상태이상(State)** 로 분리하여 책임을 명확히 한다.

## 2. 현재 구조 요약 (Core 기준)
- `CharacterStat` 안에 `AffectController`가 포함되어 있으며, Affect 적용 시 `TableAffect`의 **단일 StatusID + Suffix + Value**를 스탯에 더하는 구조로 동작한다.
- Tick 기반 DOT/상태이상/복합 효과를 표현하기 어렵고, 스킬 시스템과의 결합 포인트가 제한적이다.

## 3. 확장 원칙 (요구사항 반영)
1) **정석 확장**
- 데이터(Definition)와 실행 상태(Instance)를 분리한다.
- 적용/틱/해제의 라이프사이클을 명확히 정의한다.
- 스킬/아이템/AI 등 외부 시스템이 동일한 API로 Affect를 호출하도록 한다.

2) **캐릭터 단위 중앙 스케줄러**
- Character(GameObject)마다 `AffectComponent`를 부착하고, 해당 컴포넌트가 Update에서 **모든 Affect 인스턴스의 Duration/Tick을 중앙 관리**한다.
- 프레임당 처리량을 줄이기 위해 컴포넌트는 Affect가 0개일 때 자동으로 `enabled=false`로 꺼진다.

3) **패키지 분리**
- 신규 패키지: `com.ggemco.2d.affect`
- Core/게임 프로젝트와의 결합은 인터페이스(`IAffectTarget`, `IStatMutable`, `IDamageReceiver`, `IStateMutable`)로만 이루어지며, Core에 대한 직접 의존은 없다.
- Core 연동 예시는 `Samples~/CoreIntegration`에 제공한다.

4) **Status 3분리**
- `Stat`: 수치 기반(공격력, 이동속도, 저항 등)
- `DamageType`: 데미지 분류(물리/불/냉기/번개 등)
- `State`: 상태이상/플래그(기절, 침묵, 무적 등)

## 4. 신규 데이터 모델
### 4.1 AffectDefinition (affect.txt)
- Affect 자체의 규칙과 공통 파라미터
- 주요 필드
  - `Uid`
  - `DispelType` (Buff/Debuff)
  - `GroupId` (동일 그룹 단일성)
  - `BaseDuration`, `TickInterval`
  - `StackPolicy`, `MaxStacks`, `RefreshPolicy`
  - `Tags` (해제/필터/조건)
  - `VfxUid`, `VfxScale`, `VfxOffsetY`
  - `ApplyChance`

### 4.2 AffectModifierDefinition (affect_modifier.txt)
- Affect에 종속되는 **실제 효과 단위(여러 개 가능)**
- `Phase`: OnApply / OnTick / OnExpire
- `Kind`: Stat / Damage / State
- 예시
  - 이동속도 증가: OnApply + Stat
  - 화상 DOT: OnTick + Damage
  - 기절: OnApply + State

### 4.3 Status 3분리 테이블
- `stat.txt` : `STAT_*`, 저항용 `RESIST_<DamageTypeId>` 등
- `damage_type.txt` : `DAMAGE_*`
- `state.txt` : `STATE_*`

## 5. 런타임 아키텍처 (핵심 클래스)
- `AffectComponent` (MonoBehaviour)
  - Character 단위 중앙 스케줄러
  - API: `ApplyAffect(uid, ctx)`, `RemoveAffect(uid)`, `Dispel(query)`, `RemoveAll()`
- `AffectInstance`
  - 실행 중 상태(잔여시간, 스택, Tick 누적)
  - Stat/State 적용 토큰 보관(만료 시 되돌리기)
- Executors
  - `StatModifierExecutor` : `IStatMutable`로 스탯 변경
  - `DamageExecutor` : `IDamageReceiver`로 데미지 적용(저항 반영)
  - `StateExecutor` : `IStateMutable`로 상태 부여(확률/면역 지원)
- `AffectRuntime`
  - 전역 Repository/VFX 서비스 주입 지점

## 6. Core 연동 전략
### 6.1 기존 `AffectController` 처리
- 신규 시스템으로 전환 후, `CharacterStat.AffectController`는 **Deprecated**로 두고 단계적으로 제거한다.
- `CharacterBase.Dead()`에서 호출하던 `RemoveAllAffects()`는 `AffectComponent.RemoveAll()`로 대체한다.

### 6.2 Core 어댑터 제공 (샘플)
- `Samples~/CoreIntegration/CoreAffectTargetAdapter.cs`
  - `CharacterBase`를 `IAffectTarget`으로 연결
  - 데미지: `CharacterBase.TakeDamage(MetadataDamage)` 사용
  - 스탯: `CharacterStat.ApplyStatModifiers/RemoveStatModifiers`를 토큰 기반으로 래핑
  - 상태이상: Core에 State 시스템이 없다는 가정하에, 간단한 Dictionary 기반 예시 제공(실제 프로젝트에 맞춰 교체 권장)

## 7. 스킬 시스템 연동 가이드
- 스킬 정의(SkillDefinition SO 또는 skill.txt)에서 Affect UID 리스트를 보유한다.
- 스킬 실행 시:
  1) 타겟의 `AffectComponent` 획득
  2) `ApplyAffect(affectUid, new AffectApplyContext { Source = caster, SkillLevel = n, ValueMultiplier = ... })`
- DOT/상태이상/버프/디버프가 모두 동일한 파이프라인으로 작동한다.

## 8. 마이그레이션 (TableAffect -> 신규 스키마)
- 기존 `TableAffect`는 1행=1효과(단일 Stat) 구조이므로,
  - 신규 `affect.txt`에 기본 규칙을 기록하고,
  - `affect_modifier.txt`에 기존 StatusID/Suffix/Value를 `Kind=Stat`로 1개 레코드로 변환한다.
- 기존 TickTime이 0이 아니면 `Kind=Damage` 또는 `Kind=State`로 확장할 수 있다.

## 9. 확장 포인트
- **면역/저항/정화**: `IStateMutable.IsImmune`, `IStatusDefinitionRepository.GetResistancePercent` 확장
- **우선순위/충돌 룰**: GroupId + Tags 기반 필터 강화
- **네트워크/리플레이**: Apply/Remove 이벤트를 스트림으로 기록
- **VFX/SFX**: `IAffectVfxService`를 게임 프로젝트에서 구현하여 연결

---

### 패키지 산출물
- `com.ggemco.2d.affect` (Runtime/Editor/Samples~/Documentation~/Tables~)
- 테이블 템플릿: `Tables~/stat.txt`, `damage_type.txt`, `state.txt`, `affect.txt`, `affect_modifier.txt`
