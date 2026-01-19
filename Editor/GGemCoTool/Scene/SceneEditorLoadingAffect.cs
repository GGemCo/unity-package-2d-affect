using GGemCo2DCore;
using GGemCo2DCoreEditor;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DAffectEditor
{
    /// <summary>
    /// 인트로 씬 설정 툴
    /// </summary>
    public class SceneEditorLoadingAffect : DefaultSceneEditorAffect
    {
        private const string Title = "로딩 씬 셋팅하기";
        private GameObject _objGGemCoCore;
        
        [MenuItem(ConfigEditorAffect.NameToolSettingSceneLoading, false, (int)ConfigEditorAffect.ToolOrdering.SettingSceneLoading)]
        public static void ShowWindow()
        {
            GetWindow<SceneEditorLoadingAffect>(Title);
        }

        private void OnGUI()
        {
            if (!CheckCurrentLoadedScene(ConfigDefine.SceneNameLoading))
            {
                EditorGUILayout.HelpBox($"로딩 씬을 불러와 주세요.", MessageType.Error);
            }
            else
            {
                DrawRequiredSection();
            }
        }
        private void DrawRequiredSection()
        {
            HelperEditorUI.OnGUITitle("필수 항목");
            EditorGUILayout.HelpBox($"* SceneLoadingTcg 오브젝트\n", MessageType.Info);
            if (GUILayout.Button("필수 항목 셋팅하기"))
            {
                SetupRequiredObjects();
            }
        }
        /// <summary>
        /// 필수 항목 셋팅
        /// </summary>
        public void SetupRequiredObjects(EditorSetupContext ctx = null)
        {
            string sceneName = nameof(SceneLoading);
            GGemCo2DCore.SceneLoading scene = CreateUIComponent.Find(sceneName, ConfigPackageInfo.PackageType.Core)?.GetComponent<SceneLoading>();
            if (scene == null) 
            {
                HelperLog.Error($"[{nameof(SceneEditorLoadingAffect)}] {sceneName} 이 없습니다.\nGGemCoTool > 설정하기 > 로딩 씬 셋팅하기에서 필수 항목 셋팅하기를 실행해주세요.", ctx);
                return;
            }
            _objGGemCoCore = GetOrCreateRootPackageGameObject();
            // GGemCo2DCore.SceneLoading GameObject 만들기
            GGemCo2DAffect.SceneLoadingAffect sceneLoading =
                CreateOrAddComponent<GGemCo2DAffect.SceneLoadingAffect>(nameof(GGemCo2DAffect.SceneLoadingAffect));

            HelperLog.Info($"[{nameof(SceneEditorLoadingAffect)}] 로딩 씬 필수 셋업 완료", ctx);
            // 반드시 SetDirty 처리해야 저장됨
            EditorUtility.SetDirty(scene);
        }
    }
}