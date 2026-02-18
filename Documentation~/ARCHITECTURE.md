# Affect 문서

이 폴더는 **Affect 패키지**의 구조/규칙/변경 절차를 표준화하기 위한 문서입니다.

- Runtime 네임스페이스: `GGemCo2DAffect`
- Editor 네임스페이스: `GGemCo2DAffectEditor`

Unity 공식 문서 참고 링크:
- Assembly Definition(런타임/에디터 분리): https://docs.unity3d.com/6000.3/Documentation/Manual/cus-asmdef.html
- ScriptableObject(데이터 컨테이너/저장 특성): https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html
- EditorWindow(커스텀 툴): https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorWindow.html
- EditorWindow(UI Toolkit 가이드): https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-HowTo-CreateEditorWindow.html
- Addressables(패키지): https://docs.unity3d.com/Packages/com.unity.addressables%40latest/
- Addressables(개요): https://docs.unity3d.com/Packages/com.unity.addressables%401.24/manual/AddressableAssetsOverview.html
- Undo(에디터 Undo/Redo): https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Undo.html
- Serialization(직렬화 규칙): https://docs.unity3d.com/Manual/script-Serialization.html


## 1. 역할

Affect는 버프/디버프(상태 효과) 시스템입니다.

- Affect 정의(테이블/정의 클래스)
- 실행(Executors) 및 적용/틱/해제 라이프사이클
- Repository/Interface로 런타임 접근을 표준화
- Core(이펙트/사운드/스탯/상태)와 연동
- Localization/표시(UI) 연동

## 2. 구성 개요

- `Definitions/` : Affect/Modifier 정의 및 데이터 구조
- `Executors/` : OnApply/OnTick/OnRemove 등 실행기
- `Repositories/` : 런타임 조회/캐시
- `Interfaces/` : 의존성 역전을 위한 인터페이스
- `Bridge/` : Core 시스템(이펙트/사운드/스탯 등) 연결부
- `TableLoader/` : Affect 관련 테이블 로딩
- `Localization/`, `UI/` : 표시/문구 연동
- `Scene/` : 씬 로딩/초기화 시점 영향(있는 경우)
- `System/` : Affect 시스템 오케스트레이션

Editor(AffectEditor)는 UseAffect 같은 테스트/편집 도구를 제공합니다.

## 3. 라이프사이클(표준)

1) Apply
- 대상 캐릭터에 Affect 인스턴스 생성/등록
- 즉시 실행(OnApply) 처리

2) Tick
- 일정 주기 또는 프레임 기반으로 OnTick 처리
- Dot/상태 확률 등은 “정확한 기준(시간)”을 고정

3) Remove
- 종료(OnRemove) 처리
- VFX/사운드가 Follow 타입이면 해제 처리

## 4. 확장 포인트(권장)

- 새로운 Modifier Kind 추가 시: Executor 확장 + 테이블/정의 갱신
- VFX 표시 정책(PositionType/FollowType/SortingLayer 등)은 Core Effect 시스템과 브리지 레이어에서 일괄 처리
