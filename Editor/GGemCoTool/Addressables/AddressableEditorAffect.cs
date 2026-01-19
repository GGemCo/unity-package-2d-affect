using UnityEditor;
using UnityEngine;

namespace GGemCo2DAffectEditor
{
    /// <summary>
    /// Addressables 관련 리소스/데이터 셋팅을 한 화면에서 수행하기 위한 Unity EditorWindow입니다.
    /// </summary>
    /// <remarks>
    /// 내부적으로 각 기능별 서브 뷰(SettingScriptableObjectAffect, SettingTableAffect, SettingAffectImage)를 생성해
    /// OnGUI에서 배치/호출합니다.
    /// </remarks>
    public class AddressableEditorAffect : DefaultEditorWindowAffect
    {
        /// <summary>
        /// 에디터 윈도우의 표시 제목입니다.
        /// </summary>
        private const string Title = "Addressable 셋팅하기";

        /// <summary>
        /// UI 버튼의 폭(현재 윈도우 폭에 따라 OnGUI에서 계산)입니다.
        /// </summary>
        public float buttonWidth;

        /// <summary>
        /// UI 버튼의 높이(초기화 시 기본값 설정)입니다.
        /// </summary>
        public float buttonHeight;

        /// <summary>
        /// ScriptableObject 셋팅 UI를 담당하는 구성 요소입니다.
        /// </summary>
        private SettingScriptableObjectAffect _settingScriptableObjectAffect;

        /// <summary>
        /// 테이블(데이터) 셋팅 UI를 담당하는 구성 요소입니다.
        /// </summary>
        private SettingTableAffect _settingTableAffect;

        /// <summary>
        /// Affect 이미지 셋팅 UI를 담당하는 구성 요소입니다.
        /// </summary>
        private SettingAffectImage _settingAffectImage;

        /// <summary>
        /// 스크롤 뷰의 현재 스크롤 위치입니다.
        /// </summary>
        private Vector2 _scrollPosition;

        /// <summary>
        /// 메뉴 항목에서 호출되어 Addressable 설정 창을 엽니다.
        /// </summary>
        [MenuItem(ConfigEditorAffect.NameToolSettingAddressable, false, (int)ConfigEditorAffect.ToolOrdering.SettingAddressable)]
        public static void ShowWindow()
        {
            GetWindow<AddressableEditorAffect>(Title);
        }

        /// <summary>
        /// 윈도우가 활성화될 때 초기 UI 상태 및 서브 뷰 인스턴스를 준비합니다.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            buttonHeight = 40f;

            _settingScriptableObjectAffect = new SettingScriptableObjectAffect(this);
            _settingTableAffect = new SettingTableAffect(this);
            _settingAffectImage = new SettingAffectImage(this);
        }

        /// <summary>
        /// 에디터 윈도우 UI를 그립니다(스크롤 뷰 + 가로 배치).
        /// </summary>
        private void OnGUI()
        {
            // 현재 윈도우 폭을 기준으로 버튼 폭을 반으로 나눠 사용합니다.
            buttonWidth = position.width / 2f - 10f;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.BeginHorizontal();

            // NOTE: 필요 시 ScriptableObject 셋팅 UI를 활성화하세요.
            // _settingScriptableObjectAffect.OnGUI();

            _settingTableAffect.OnGUI();
            _settingAffectImage.OnGUI();

            EditorGUILayout.EndHorizontal();

            // NOTE: 추후 추가 UI 섹션이 필요하면 아래 블록을 사용합니다.
            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }
    }
}
