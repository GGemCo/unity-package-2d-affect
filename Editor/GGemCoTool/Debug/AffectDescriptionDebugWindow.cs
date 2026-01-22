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

        /// <summary>
        /// Affect 테이블(원본 데이터) 참조입니다.
        /// </summary>
        private TableAffect _tableAffect;

        /// <summary>
        /// 팝업에서 선택된 항목의 인덱스입니다.
        /// </summary>
        private int _selectedIndex;

        /// <summary>
        /// Affect UID를 키로 하는 테이블 데이터 사전입니다.
        /// </summary>
        private Dictionary<int, StruckTableAffect> _dictionary;

        /// <summary>
        /// 팝업에 표시할 "Uid - Name" 문자열 목록입니다.
        /// </summary>
        private readonly List<string> _names = new List<string>();

        /// <summary>
        /// 팝업 인덱스와 1:1로 매핑되는 Affect UID 목록입니다.
        /// </summary>
        private readonly List<int> _uids = new List<int>();

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
            _selectedIndex = 0;

            _tableAffect = TableLoaderManagerAffect.LoadAffectTable();
            _dictionary = _tableAffect.GetDatas();

            LoadInfoData();
        }

        /// <summary>
        /// 에디터 윈도우 UI를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            if (_selectedIndex >= _names.Count)
            {
                _selectedIndex = 0;
            }

            EditorGUILayout.LabelField(Title, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("게임을 실행 해야 합니다.");
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("`선택 Affect 설명 확인하기`를 실행하면 콘솔 로그 창에 출력됩니다.");
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("`모든 Affect 설명 txt로 내보내기`를 실행하면, 선택한 폴더에 txt 파일로 생성됩니다.");
                EditorGUILayout.Space(4);
            }

            _selectedIndex = EditorGUILayout.Popup("Affect 선택", _selectedIndex, _names.ToArray());

            if (GUILayout.Button("선택 Affect 설명 확인하기")) CheckAffectDescription();
            if (GUILayout.Button("모든 Affect 설명 txt로 내보내기")) ExportAllAffectDescription();
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

            int affectUid = _uids[_selectedIndex];
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

        /// <summary>
        /// 테이블 데이터를 기반으로 팝업(이름/UID) 목록을 다시 구성합니다.
        /// </summary>
        private void LoadInfoData()
        {
            _names.Clear();
            _uids.Clear();

            foreach (var kvp in _dictionary)
            {
                var info = kvp.Value;
                if (info.Uid <= 0) continue;

                _names.Add($"{info.Uid} - {info.Name}");
                _uids.Add(info.Uid);
            }

            // 목록 재구성 시 기본 선택값을 0으로 초기화합니다.
            _selectedIndex = 0;
        }
    }
}
