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


## 1. Affect 테이블 컬럼 추가(예: PositionType/FollowType/SortingLayer)

1) Table/Struct 수정
2) 로더/파서 수정
3) VFX 적용 코드(Bridge)에서 신규 컬럼 반영
4) UseAffect UI 필드 추가
5) Export/Reload 확인
6) 테스트
- [ ] Default/Head 위치 확인
- [ ] Follow None/Follow 동작 확인
- [ ] SortingLayer 키 검증 및 잘못된 값 처리

---

## 2. 새로운 Modifier Kind 추가

1) Kind enum 추가
2) Executor 구현 추가(단일 책임)
3) 테이블 입력 규칙 문서화(필수 컬럼/조합)
4) UI 표시(필요 시)
5) 샘플 데이터 추가
6) 테스트
- [ ] OnApply/OnTick/OnRemove 각각 정상 동작
- [ ] 중첩/갱신 정책(스택/리프레시) 확인

---

## 3. Localization 관련 변경

1) 키/테이블 추가
2) 로딩 순서(SceneLoading 단계) 재검증
3) 사용자 테이블 존재 체크 완료 시점 보장
4) UI(LocalizeStringEvent 등) 갱신 확인
