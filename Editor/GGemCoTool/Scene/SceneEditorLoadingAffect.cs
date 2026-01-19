using GGemCo2DCore;
using GGemCo2DCoreEditor;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DAffectEditor
{
    /// <summary>
    /// 로딩 씬(SceneLoading)에 Affect 전용 필수 오브젝트/컴포넌트를 배치하는 에디터 셋업 윈도우입니다.
    /// </summary>
    /// <remarks>
    /// 현재 로드된 씬이 ConfigDefine.SceneNameLoading인지 확인한 뒤,
    /// 필수 항목(예: SceneLoadingAffect 컴포넌트)을 생성/추가합니다.
    /// </remarks>
    public class SceneEditorLoadingAffect : DefaultSceneEditorAffect
    {
        /// <summary>
        /// 에디터 윈도우의 표시 제목입니다.
        /// </summary>
        private const string Title = "로딩 씬 셋팅하기";

        /// <summary>
        /// 패키지 루트(공용) 오브젝트 참조입니다. 필요 시 생성됩니다.
        /// </summary>
        private GameObject _objGGemCoCore;

        /// <summary>
        /// 메뉴 항목에서 호출되어 로딩 씬 셋업 창을 엽니다.
        /// </summary>
        [MenuItem(ConfigEditorAffect.NameToolSettingSceneLoading, false, (int)ConfigEditorAffect.ToolOrdering.SettingSceneLoading)]
        public static void ShowWindow()
        {
            GetWindow<SceneEditorLoadingAffect>(Title);
        }

        /// <summary>
        /// 에디터 윈도우 UI를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            if (!CheckCurrentLoadedScene(ConfigDefine.SceneNameLoading))
            {
                EditorGUILayout.HelpBox("로딩 씬을 불러와 주세요.", MessageType.Error);
            }
            else
            {
                DrawRequiredSection();
            }
        }

        /// <summary>
        /// 로딩 씬에서 반드시 존재해야 하는 필수 항목 셋업 섹션을 그립니다.
        /// </summary>
        private void DrawRequiredSection()
        {
            HelperEditorUI.OnGUITitle("필수 항목");
            EditorGUILayout.HelpBox("* SceneLoadingAffect 오브젝트\n", MessageType.Info);

            if (GUILayout.Button("필수 항목 셋팅하기"))
            {
                SetupRequiredObjects();
            }
        }

        /// <summary>
        /// 로딩 씬의 필수 오브젝트/컴포넌트를 생성 또는 추가합니다.
        /// </summary>
        /// <param name="ctx">
        /// 배치/자동화 실행 컨텍스트입니다. null이면 기본 로그/동작을 사용합니다.
        /// </param>
        /// <remarks>
        /// 동작 개요:
        /// - 패키지(Core)에서 SceneLoading 오브젝트를 찾습니다.
        /// - 루트 패키지 오브젝트를 확보(없으면 생성)합니다.
        /// - 루트 아래에 GGemCo2DAffect.SceneLoadingAffect 컴포넌트를 생성/추가합니다.
        /// - SceneLoading 객체를 Dirty 처리하여 씬 저장 대상에 포함되도록 합니다.
        /// </remarks>
        public void SetupRequiredObjects(EditorSetupContext ctx = null)
        {
            string sceneName = nameof(SceneLoading);

            // Core 패키지에서 SceneLoading 루트 오브젝트를 찾습니다.
            GGemCo2DCore.SceneLoading scene =
                CreateUIComponent.Find(sceneName, ConfigPackageInfo.PackageType.Core)?.GetComponent<SceneLoading>();

            if (scene == null)
            {
                HelperLog.Error(
                    $"[{nameof(SceneEditorLoadingAffect)}] {sceneName} 이 없습니다.\n" +
                    "GGemCoTool > 설정하기 > 로딩 씬 셋팅하기에서 필수 항목 셋팅하기를 실행해주세요.",
                    ctx);
                return;
            }

            // Affect 구성 요소를 배치할 루트 오브젝트를 확보합니다.
            _objGGemCoCore = GetOrCreateRootPackageGameObject();

            // GGemCo2DAffect.SceneLoadingAffect GameObject/Component를 생성 또는 추가합니다.
            // NOTE: CreateOrAddComponent 구현에 따라 동일 이름 오브젝트가 있으면 재사용될 수 있습니다.
            GGemCo2DAffect.SceneLoadingAffect sceneLoading =
                CreateOrAddComponent<GGemCo2DAffect.SceneLoadingAffect>(nameof(GGemCo2DAffect.SceneLoadingAffect));

            HelperLog.Info($"[{nameof(SceneEditorLoadingAffect)}] 로딩 씬 필수 셋업 완료", ctx);

            // 반드시 SetDirty 처리해야 저장됨
            EditorUtility.SetDirty(scene);
        }
    }
}
