using GGemCo2DAffect;
using GGemCo2DCore;
using GGemCo2DCoreEditor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DAffectEditor
{
    /// <summary>
    /// Affect 관련 데이터 테이블을 Addressables의 Table 그룹에 등록하는 에디터 설정 클래스입니다.
    /// </summary>
    /// <remarks>
    /// ConfigAddressableTableAffect.All 목록을 순회하며 각 테이블 에셋을 키/경로로 등록하고,
    /// 공통 라벨(ConfigAddressableLabel.Table)을 적용합니다.
    /// </remarks>
    public class SettingTableAffect : DefaultAddressable
    {
        /// <summary>
        /// 버튼에 표시되는 텍스트(기능 실행 제목)입니다.
        /// </summary>
        private const string Title = "Affect 테이블 추가하기";

        /// <summary>
        /// UI 레이아웃(버튼 폭/높이 등) 정보를 제공하는 부모 에디터 윈도우 참조입니다.
        /// </summary>
        private readonly AddressableEditorAffect _addressableEditor;

        /// <summary>
        /// Affect 테이블 Addressables 등록 UI를 초기화합니다.
        /// </summary>
        /// <param name="addressableEditorWindow">버튼 크기 등 UI 정보를 참조할 에디터 윈도우입니다.</param>
        public SettingTableAffect(AddressableEditorAffect addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;

            // NOTE: 테이블 자산은 Table 그룹으로 등록합니다.
            targetGroupName = ConfigAddressableGroupName.Table;
        }

        /// <summary>
        /// 에디터 윈도우에 표시될 UI를 그립니다.
        /// </summary>
        /// <remarks>
        /// 버튼 클릭 시 Setup을 호출하여 테이블 Addressables 등록을 수행합니다.
        /// </remarks>
        public void OnGUI()
        {
            // Common.OnGUITitle(Title);

            if (GUILayout.Button(Title, GUILayout.Width(_addressableEditor.buttonWidth), GUILayout.Height(_addressableEditor.buttonHeight)))
            {
                try
                {
                    Setup();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    EditorUtility.DisplayDialog(
                        Title,
                        "데이터 테이블 Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.",
                        "OK");
                }
            }
        }

        /// <summary>
        /// Affect 관련 테이블들을 Addressables에 등록하고 저장합니다.
        /// </summary>
        /// <param name="ctx">
        /// 배치/자동화 실행 컨텍스트입니다. null이면 완료 시 AssetDatabase 저장 및 다이얼로그를 표시합니다.
        /// </param>
        /// <remarks>
        /// 동작 개요:
        /// - AddressableAssetSettings가 없으면 생성합니다.
        /// - Table 그룹을 가져오거나 생성합니다.
        /// - ConfigAddressableTableAffect.All에 정의된 테이블을 키/경로로 등록하고 공통 라벨을 부여합니다.
        /// </remarks>
        public void Setup(EditorSetupContext ctx = null)
        {
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = CreateAddressableSettings();
            }

            // Table 그룹 가져오기 또는 생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, targetGroupName);
            if (!group)
            {
                HelperLog.Error($"'{targetGroupName}' 그룹을 설정할 수 없습니다.", ctx);
                return;
            }

            // Affect 테이블 목록을 Addressables 엔트리로 등록
            foreach (var addressableAssetInfo in ConfigAddressableTableAffect.All)
            {
                Add(settings, group, addressableAssetInfo.Key, addressableAssetInfo.Path, ConfigAddressableLabel.Table);
                // Debug.Log($"Addressable 키 값 설정: {keyName}");
            }

            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);

            if (ctx != null)
            {
                HelperLog.Info("Addressable 설정 완료", ctx);
            }
            else
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");
            }
        }
    }
}
