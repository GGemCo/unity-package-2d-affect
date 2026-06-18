using System;
using System.Collections.Generic;
using GGemCo2DAffect;
using GGemCo2DCore;
using GGemCo2DCoreEditor;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DAffectEditor
{
    /// <summary>
    /// 씬의 특정 캐릭터에게 Affect를 적용해보는 커스텀 툴.
    /// </summary>
    /// <remarks>
    /// - Play Mode에서만 적용 가능.
    /// - Hierarchy 선택 또는 씬 내 캐릭터 목록에서 대상 지정.
    /// - Duration Override 지원(0 이하: 테이블 기본).
    /// - Affect/AffectModifier/Stat/State/DamageType 테이블 재로딩 지원.
    /// - 대상에 Affect 관련 컴포넌트가 없으면 자동 부착(옵션) 가능.
    /// - 선택한 Affect의 Modifier 목록을 표시한다.
    /// </remarks>
    public sealed class UseAffect : DefaultEditorWindowAffect
    {
        private const string Title = "Affect 사용하기";

        // Tables
        private TableAffect _tableAffect;
        private TableAffectModifier _tableAffectModifier;
        private Dictionary<int, StruckTableAffect> _dictionary;

        // Dropdown data
        private readonly List<SearchableDropdownUtility.Option<StruckTableAffect>> _dropDownOptions = new();
        private StruckTableAffect _selectedData;

        // Apply params
        private float _valueMultiplier = 1f;
        private float _durationOverride = 0f;

        // Options
        private bool _autoAttachComponents = true;

        // UI
        private Vector2 _modifierScroll;
        private string _lastReloadMessage = string.Empty;
        private Vector2 _scroll;

        [MenuItem(ConfigEditorAffect.NameToolUseAffect, false, (int)ConfigEditorAffect.ToolOrdering.UseAffect)]
        public static void ShowWindow()
        {
            GetWindow<UseAffect>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _selectedData = null;
            selectedCharacterIndex = 0;
            selectedCharacter = null;

            ReloadAllTables();
            RefreshSceneCharacters();
        }

        protected override void OnSelectedCharacterChanged(CharacterBase character)
        {
            Repaint();
        }

        private void OnGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;
                EditorGUILayout.Space(6);

                DrawPlayModeGate();
                EditorGUILayout.Space(6);

                DrawTargetSection();
                EditorGUILayout.Space(8);

                DrawAffectSection();
                EditorGUILayout.Space(8);

                DrawApplySection();
                EditorGUILayout.Space(8);

                DrawModifierSection();
                EditorGUILayout.Space(8);

                DrawReloadSection();
                EditorGUILayout.Space(20);
            }
        }

        #region GUI
        private void DrawTargetSection()
        {
            DrawCharacterSelectionSection(Title);

            if (selectedCharacter == null)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Affect 대상 상태", EditorStyles.boldLabel);

                var affectComp = selectedCharacter.GetComponent<AffectComponent>() ?? selectedCharacter.GetComponentInChildren<AffectComponent>();
                bool hasTarget = selectedCharacter.GetComponent<IAffectTarget>() != null || selectedCharacter.GetComponentInChildren<IAffectTarget>() != null;

                EditorGUILayout.LabelField("AffectComponent", affectComp != null ? "OK" : "없음");
                EditorGUILayout.LabelField("IAffectTarget", hasTarget ? "OK" : "없음");
            }
        }

        private void DrawAffectSection()
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
                EditorGUILayout.LabelField("적용 파라미터", EditorStyles.boldLabel);

                _valueMultiplier = EditorGUILayout.FloatField("Value Multiplier", _valueMultiplier);
                _durationOverride = EditorGUILayout.FloatField("Duration Override (<=0: 테이블 기본)", _durationOverride);

                EditorGUILayout.Space(4);
                _autoAttachComponents = EditorGUILayout.ToggleLeft("대상에 Affect 컴포넌트가 없으면 자동 부착", _autoAttachComponents);

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("어펙트 적용", GUILayout.Height(26)))
                        ApplySelectedAffect();
                }
            }
        }

        private void DrawModifierSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Modifier 목록", EditorStyles.boldLabel);

                if (_tableAffectModifier == null)
                {
                    EditorGUILayout.HelpBox("AffectModifier 테이블이 로드되지 않았습니다.", MessageType.Info);
                    return;
                }

                if (_selectedData == null)
                {
                    EditorGUILayout.HelpBox("Affect가 없습니다.", MessageType.Info);
                    return;
                }

                var modifiers = _tableAffectModifier.GetModifiers(_selectedData.Uid);

                if (modifiers == null || modifiers.Count == 0)
                {
                    EditorGUILayout.HelpBox("연결된 Modifier가 없습니다.", MessageType.Info);
                    return;
                }

                EditorGUILayout.LabelField("Count", modifiers.Count.ToString());

                _modifierScroll = EditorGUILayout.BeginScrollView(_modifierScroll, GUILayout.MinHeight(140));
                try
                {
                    foreach (var m in modifiers)
                    {
                        DrawModifierRow(m);
                        EditorGUILayout.Space(3);
                    }
                }
                finally
                {
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void DrawModifierRow(AffectModifierDefinition m)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"ModifierId: {m.modifierId}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Phase", m.phase.ToString());
                EditorGUILayout.LabelField("Kind", m.kind.ToString());

                switch (m.kind)
                {
                    case ModifierKind.Stat:
                        EditorGUILayout.LabelField("StatId", string.IsNullOrEmpty(m.statId) ? "(None)" : m.statId);
                        EditorGUILayout.LabelField("StatValue", m.statValue.ToString("0.###"));
                        EditorGUILayout.LabelField("ValueType", m.statValueType.ToString());
                        EditorGUILayout.LabelField("Operation", m.statOperation.ToString());
                        break;

                    case ModifierKind.Damage:
                        EditorGUILayout.LabelField("DamageTypeId", string.IsNullOrEmpty(m.damageTypeId) ? "(None)" : m.damageTypeId);
                        EditorGUILayout.LabelField("BaseValue", m.damageBaseValue.ToString("0.###"));
                        EditorGUILayout.LabelField("ScalingStatId", string.IsNullOrEmpty(m.scalingStatId) ? "(None)" : m.scalingStatId);
                        EditorGUILayout.LabelField("ScalingCoeff", m.scalingCoefficient.ToString("0.###"));
                        EditorGUILayout.LabelField("CanCrit", m.canCrit ? "Y" : "N");
                        EditorGUILayout.LabelField("IsDot", m.isDot ? "Y" : "N");
                        EditorGUILayout.LabelField("SuppressDamageReaction", m.suppressDamageReaction ? "Y" : "N");
                        EditorGUILayout.LabelField("ShowHitEffect", m.showHitEffect ? "Y" : "N");
                        break;

                    case ModifierKind.State:
                        EditorGUILayout.LabelField("StateId", string.IsNullOrEmpty(m.stateId) ? "(None)" : m.stateId);
                        EditorGUILayout.LabelField("Chance", m.stateChance.ToString("0.###"));
                        EditorGUILayout.LabelField("DurationOverride", m.stateDurationOverride.ToString("0.###"));
                        break;

                    case ModifierKind.FormulaVariable:
                        EditorGUILayout.LabelField("FormulaVariableId", string.IsNullOrEmpty(m.formulaVariableId) ? "(None)" : m.formulaVariableId);
                        EditorGUILayout.LabelField("FormulaVariableValue", m.formulaVariableValue.ToString("0.###"));
                        EditorGUILayout.LabelField("ValueType", m.formulaVariableValueType.ToString());
                        EditorGUILayout.LabelField("Operation", m.formulaVariableOperation.ToString());
                        break;
                }

                var statusRepo = AffectRuntime.StatusRepository;
                if (statusRepo != null)
                {
                    if (!string.IsNullOrEmpty(m.statId))
                        EditorGUILayout.LabelField("Stat Valid", statusRepo.IsValidStat(m.statId) ? "true" : "false");

                    if (!string.IsNullOrEmpty(m.damageTypeId))
                        EditorGUILayout.LabelField("DamageType Valid", statusRepo.IsValidDamageType(m.damageTypeId) ? "true" : "false");

                    if (!string.IsNullOrEmpty(m.stateId))
                        EditorGUILayout.LabelField("State Valid", statusRepo.IsValidState(m.stateId) ? "true" : "false");
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

        private void ApplySelectedAffect()
        {
            if (!Application.isPlaying || !SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            if (selectedCharacter == null)
            {
                EditorUtility.DisplayDialog(Title, "대상 캐릭터를 지정해주세요.", "OK");
                return;
            }

            if (_selectedData == null)
            {
                EditorUtility.DisplayDialog(Title, "적용할 Affect가 없습니다. 테이블 로딩을 확인해주세요.", "OK");
                return;
            }

            int affectUid = _selectedData.Uid;

            var affectComp = selectedCharacter.GetComponent<AffectComponent>() ??
                             selectedCharacter.GetComponentInChildren<AffectComponent>();
            if (affectComp == null && _autoAttachComponents)
            {
                affectComp = EnsureAffectComponents(selectedCharacter);
            }

            if (affectComp == null)
            {
                EditorUtility.DisplayDialog(
                    Title,
                    "대상 캐릭터에서 AffectComponent를 찾지 못했습니다." +
                    "자동 부착 옵션을 켜거나, 런타임 부트스트랩에서 AffectComponent가 부착되는지 확인해주세요.",
                    "OK");
                return;
            }

            var ctx = new AffectApplyContext
            {
                Source = nameof(UseAffect),
                SkillLevel = 1,
                DurationOverride = _durationOverride,
                ValueMultiplier = Mathf.Max(0f, _valueMultiplier),
            };

            affectComp.ApplyAffect(affectUid, ctx);
            Repaint();
        }

        /// <summary>
        /// 대상 캐릭터에 AffectComponent/IAffectTarget 어댑터가 없으면 안전하게 부착한다.
        /// </summary>
        private static AffectComponent EnsureAffectComponents(CharacterBase target)
        {
            if (target == null)
                return null;

            var hasTarget = target.GetComponent<IAffectTarget>() != null || target.GetComponentInChildren<IAffectTarget>() != null;
            if (!hasTarget)
            {
                if (target.GetComponent<CoreAffectTargetAdapter>() == null)
                    target.gameObject.AddComponent<CoreAffectTargetAdapter>();
            }

            var comp = target.GetComponent<AffectComponent>();
            if (comp == null)
                comp = target.gameObject.AddComponent<AffectComponent>();

            return comp;
        }
    }
}
