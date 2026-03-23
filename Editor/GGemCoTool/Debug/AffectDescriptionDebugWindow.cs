using System;
using System.Collections.Generic;
using GGemCo2DAffect;
using GGemCo2DCore;
using GGemCo2DCoreEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace GGemCo2DAffectEditor
{
    /// <summary>
    /// Affect 설명(로컬라이징 문자열)을 선택/검증하고, 전체 목록을 TSV로 내보내는 디버그용 에디터 윈도우입니다.
    /// </summary>
    /// <remarks>
    /// - 실행 중(SceneGame.Instance 존재)일 때 AffectDescriptionService를 통해 실제 표시될 설명을 조회합니다.
    /// - en/ko Locale을 번갈아 적용해 각 언어별 설명을 추출한 뒤 TSV(Uid/Name/En/Ko) 형태로 저장합니다.
    /// </remarks>
    public sealed class AffectDescriptionDebugWindow : DefaultEditorWindow
    {
        /// <summary>
        /// 에디터 윈도우의 표시 제목입니다.
        /// </summary>
        private const string Title = "Affect 설명 체크기";

        // Tables
        private TableAffect _tableAffect;
        private TableAffectModifier _tableAffectModifier;
        private Dictionary<int, StruckTableAffect> _dictionary;

        // Dropdown data
        private readonly List<SearchableDropdownUtility.Option<StruckTableAffect>> _dropDownOptions = new();
        private StruckTableAffect _selectedData;
        
        private string _lastReloadMessage = string.Empty;
        private Vector2 _scroll;
        
        /// <summary>
        /// Unity 메뉴에서 호출되어 디버그 윈도우를 엽니다.
        /// </summary>
        [MenuItem(ConfigEditorAffect.NameToolDebugAffectDescription, false, (int)ConfigEditorAffect.ToolOrdering.DebugAffectDescription)]
        private static void Open()
        {
            GetWindow<AffectDescriptionDebugWindow>(Title);
        }

        /// <summary>
        /// 윈도우가 활성화될 때 테이블을 로드하고 팝업 목록을 구성합니다.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            _selectedData = null;
            selectedCharacterIndex = 0;
            selectedCharacter = null;

            ReloadAllTables();
            RefreshSceneCharacters();
        }
        
        /// <summary>
        /// 에디터 윈도우 UI를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;
                EditorGUILayout.Space(6);

                DrawPlayModeGate();
                EditorGUILayout.Space(6);

                DrawSection();
                EditorGUILayout.Space(8);

                DrawApplySection();
                EditorGUILayout.Space(8);

                DrawReloadSection();
                EditorGUILayout.Space(20);
            }
        }

        #region GUI
        
        private void DrawSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Affect");

                    if (_dropDownOptions.Count == 0)
                    {
                        EditorGUILayout.HelpBox("Affect 테이블이 비어있습니다. 테이블 로딩/Addressables 설정을 확인해주세요.", MessageType.Warning);
                        return;
                    }

                    string currentText = _selectedData != null ? _selectedData.Name : "선택...";
                    int selectIndex = _selectedData?.Uid ?? 0;

                    SearchableDropdownUtility.DrawButtonAndShow(
                        buttonText: currentText,
                        options: _dropDownOptions,
                        selectedIndex: selectIndex,
                        onSelected: (idx, opt) =>
                        {
                            _selectedData = opt.Data;
                            Repaint();
                        },
                        defaultSearchMode: SearchableDropdownUtility.SearchMode.Both);
                }

                if (_selectedData != null)
                {
                    EditorGUILayout.LabelField("UID", _selectedData.Uid.ToString());
                    EditorGUILayout.LabelField("Name", _selectedData.Name);
                    EditorGUILayout.LabelField("GroupId", string.IsNullOrEmpty(_selectedData.GroupId) ? "(None)" : _selectedData.GroupId);
                    EditorGUILayout.LabelField("BaseDuration", _selectedData.BaseDuration.ToString("0.###"));
                    EditorGUILayout.LabelField("TickInterval", _selectedData.TickInterval.ToString("0.###"));
                }
            }
        }
        
        private void DrawApplySection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUI.DisabledScope(!Application.isPlaying || !SceneGame.Instance))
                {
                    if (GUILayout.Button("선택 Affect 설명 확인하기")) CheckAffectDescription();
                    if (GUILayout.Button("모든 Affect 설명 txt로 내보내기")) ExportAllAffectDescription();
                }
            }
        }
        
        private void DrawReloadSection()
        {
            DrawTableReloadSection(
                _lastReloadMessage,
                "affect / affect_modifier / stat / state / damage_type 재로딩",
                ReloadAllTables);
        }

        #endregion
        
        
        private void ReloadAllTables()
        {
            try
            {
                _tableAffect = TableLoaderManagerAffect.LoadAffectTable();
                _tableAffectModifier = TableLoaderManagerAffect.LoadAffectModifierTable();

                _dictionary = _tableAffect?.GetDatas();
                RebuildDropdown();

                TableLoaderManagerAffect.LoadCoreTable<TableStat>("stat");
                TableLoaderManagerAffect.LoadCoreTable<TableState>("state");
                TableLoaderManagerAffect.LoadCoreTable<TableDamageType>("damage_type");
                
                _lastReloadMessage = $"테이블 재로딩 완료: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _lastReloadMessage = $"테이블 재로딩 실패: {e.GetType().Name} - {e.Message}";
            }

            Repaint();
        }

        private void RebuildDropdown()
        {
            RebuildDropdownOptions(
                source: _dictionary?.Values,
                targetOptions: _dropDownOptions,
                isValidRow: row => row.Uid > 0,
                keySelector: row => row.Uid.ToString(),
                valueSelector: row => row.Name,
                assignSelected: row => _selectedData = row);
        }
        
        /// <summary>
        /// 현재 선택된 Affect의 설명을 로컬라이징 결과로 조회하여 콘솔에 출력합니다.
        /// </summary>
        /// <remarks>
        /// 런타임 서비스(AffectDescriptionService, SceneGame.Instance)에 의존하므로 게임이 실행 중이어야 합니다.
        /// </remarks>
        private void CheckAffectDescription()
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            int affectUid = _selectedData.Uid;
            if (affectUid <= 0)
            {
                EditorUtility.DisplayDialog(Title, "확인할 Affect를 선택해주세요.", "OK");
                return;
            }

            var desc = AffectDescriptionService.Instance.GetDescription(affectUid);
            Debug.Log(desc);
        }

        /// <summary>
        /// 모든 Affect의 설명을 en/ko 두 언어로 조회하여 TSV 파일로 내보냅니다.
        /// </summary>
        /// <remarks>
        /// 처리 과정:
        /// - 저장 경로를 사용자가 선택합니다.
        /// - 현재 선택된 Locale을 백업한 뒤 en/ko로 번갈아 설정합니다.
        /// - 캐시를 비우고(AffectDescriptionService.ClearCache) 각 UID의 설명을 가져옵니다.
        /// - TSV 규칙에 맞게 탭/개행/따옴표를 정리한 후 파일로 저장합니다.
        /// </remarks>
        private void ExportAllAffectDescription()
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanel(
                "Affect Description TSV Export",
                Application.dataPath,
                "affect_description",
                "csv");

            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                // 현재 Locale 백업
                var originalLocale = LocalizationSettings.SelectedLocale;

                // en / ko Locale 확보
                var enLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
                var koLocale = LocalizationSettings.AvailableLocales.GetLocale("ko");

                if (enLocale == null || koLocale == null)
                {
                    EditorUtility.DisplayDialog(
                        Title,
                        "en 또는 ko Locale이 Localization Settings에 존재하지 않습니다.",
                        "OK");
                    return;
                }

                var sb = new System.Text.StringBuilder(4096);

                // TSV Header
                sb.AppendLine("Uid\tName\tEn\tKo");

                foreach (var kvp in _dictionary)
                {
                    var info = kvp.Value;
                    if (info.Uid <= 0)
                        continue;

                    // EN
                    LocalizationSettings.SelectedLocale = enLocale;
                    AffectDescriptionService.ClearCache();
                    var enDesc = AffectDescriptionService.Instance.GetDescription(info.Uid);

                    // KO
                    LocalizationSettings.SelectedLocale = koLocale;
                    AffectDescriptionService.ClearCache();
                    var koDesc = AffectDescriptionService.Instance.GetDescription(info.Uid);

                    // TSV Escape (개행/탭/따옴표 처리)
                    enDesc = SanitizeTsv(enDesc);
                    koDesc = SanitizeTsv(koDesc);

                    sb.Append(info.Uid).Append('\t')
                        .Append(info.Name).Append('\t')
                        .Append(enDesc).Append('\t')
                        .Append(koDesc).AppendLine();
                }

                // Locale 복구
                LocalizationSettings.SelectedLocale = originalLocale;
                AffectDescriptionService.ClearCache();

                System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);

                EditorUtility.DisplayDialog(
                    Title,
                    $"TSV 파일 생성 완료\n\n{path}",
                    "OK");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog(
                    Title,
                    "TSV 생성 중 오류가 발생했습니다.\n콘솔 로그를 확인해주세요.",
                    "OK");
            }
        }

        /// <summary>
        /// TSV 컬럼에 안전하게 기록할 수 있도록 문자열을 정리합니다.
        /// </summary>
        /// <param name="value">정리할 원본 문자열입니다.</param>
        /// <returns>탭/개행/따옴표 규칙을 반영해 TSV에 안전한 문자열을 반환합니다.</returns>
        /// <remarks>
        /// TSV 처리 규칙:
        /// - 탭은 컬럼을 깨뜨리므로 공백으로 치환합니다.
        /// - 큰따옴표는 ""로 escape 합니다.
        /// - 개행이 포함된 경우 값 전체를 큰따옴표로 감쌉니다.
        /// </remarks>
        private static string SanitizeTsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            bool hasNewLine = value.Contains("\n") || value.Contains("\r");

            value = value
                .Replace("\r\n", "\n")   // 개행 통일
                .Replace("\r", "\n")
                .Replace("\t", " ")      // 탭 제거
                .Replace("\"", "\"\"");  // quote escape

            if (hasNewLine)
                return $"\"{value}\"";

            return value;
        }
    }
}
