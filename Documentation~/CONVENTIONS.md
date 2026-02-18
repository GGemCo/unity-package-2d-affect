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


## 1. 데이터 중심 설계

- Affect/Modifier는 반드시 테이블(또는 정의) 기반으로 관리합니다.
- 런타임 로직은 “Kind 별 Executor”로 분리하여 if-else 누적을 방지합니다.

## 2. Core 연동 규칙

- 스탯 변경/데미지/상태이상은 Core의 표준 API를 통해서만 반영합니다.
- VFX/사운드 출력은 Bridge를 통해 Core Effect/Sound 시스템으로 위임합니다.
- Follow 타입 이펙트는 대상 생명주기(Disable/Destroy)에 안전해야 합니다.

## 3. Localization/UI

- 문구 키는 테이블의 UID 기반으로 파생하지 말고, “명시적 키”를 사용합니다.
- 사용자 테이블(User tables) 존재 여부 확인은 비동기/타이밍 이슈가 없도록 “완료 시점”을 보장합니다.

## 4. 성능/안정성

- Tick은 고정된 주기(예: 0.2s)로 묶고, per-frame 작업을 최소화합니다.
- Affect 인스턴스는 풀링 또는 재사용을 고려합니다(대량 전투 상황).

## 5. Editor Tool

- UseAffect는 “대상 선택 + 지속시간 override + 즉시 적용/해제”가 표준입니다.
- 테이블 리로드/Export 버튼을 제공하고, 변경 사항이 런타임에 즉시 반영되는지 확인합니다.
