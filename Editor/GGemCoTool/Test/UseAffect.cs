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
    public sealed class UseAffect : DefaultEditorWindow
    {
        private const string Title = "Affect 사용하기";

        // Tables
        private TableAffect _tableAffect;
        private TableAffectModifier _tableAffectModifier;
        private Dictionary<int, StruckTableAffect> _affectDict;

        // Dropdown data
        private readonly List<string> _affectNames = new();
        private readonly List<int> _affectUids = new();
        private int _selectedAffectIndex;

        // Target
        private CharacterBase _targetCharacter;
        private readonly List<CharacterBase> _sceneCharacters = new();
        private readonly List<string> _sceneCharacterNames = new();
        private int _selectedCharacterIndex;

        // Apply params
        private float _valueMultiplier = 1f;
        private float _durationOverride = 0f;

        // Options
        private bool _autoAttachComponents = true;

        // UI
        private Vector2 _modifierScroll;
        private string _lastReloadMessage = string.Empty;

        [MenuItem(ConfigEditorAffect.NameToolUseAffect, false, (int)ConfigEditorAffect.ToolOrdering.UseAffect)]
        public static void ShowWindow()
        {
            GetWindow<UseAffect>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _selectedAffectIndex = 0;
            _selectedCharacterIndex = 0;

            ReloadAllTables();
            RefreshSceneCharacters();
        }

        private void OnGUI()
        {
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
        }

        private void DrawPlayModeGate()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("실행 조건", EditorStyles.boldLabel);

                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Play Mode에서만 동작합니다.", MessageType.Warning);
                    return;
                }

                if (!SceneGame.Instance)
                {
                    EditorGUILayout.HelpBox("SceneGame.Instance를 찾지 못했습니다. 게임 씬이 로드되어 있는지 확인해주세요.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("Play Mode에서 동작 중입니다.", MessageType.Info);
                }
            }
        }

        private void DrawTargetSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("대상 캐릭터", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("현재 선택 오브젝트로 지정", GUILayout.Height(22)))
                            TryAssignFromSelection();

                        if (GUILayout.Button("씬 캐릭터 목록 새로고침", GUILayout.Height(22)))
                            RefreshSceneCharacters();
                    }

                    if (_sceneCharacterNames.Count == 0)
                    {
                        EditorGUILayout.HelpBox("씬에서 CharacterBase를 찾지 못했습니다. (비활성 오브젝트는 제외됩니다)", MessageType.Info);
                    }
                    else
                    {
                        _selectedCharacterIndex = Mathf.Clamp(_selectedCharacterIndex, 0, _sceneCharacterNames.Count - 1);
                        int newIndex = EditorGUILayout.Popup("캐릭터 목록", _selectedCharacterIndex, _sceneCharacterNames.ToArray());
                        if (newIndex != _selectedCharacterIndex)
                        {
                            _selectedCharacterIndex = newIndex;
                            _targetCharacter = _sceneCharacters[_selectedCharacterIndex];
                        }
                    }

                    _targetCharacter = (CharacterBase)EditorGUILayout.ObjectField("대상(직접 지정)", _targetCharacter, typeof(CharacterBase), true);

                    if (_targetCharacter != null)
                    {
                        EditorGUILayout.LabelField("대상 이름", _targetCharacter.name);

                        // 상태 표시
                        var affectComp = _targetCharacter.GetComponent<AffectComponent>() ?? _targetCharacter.GetComponentInChildren<AffectComponent>();
                        bool hasTarget = _targetCharacter.GetComponent<IAffectTarget>() != null || _targetCharacter.GetComponentInChildren<IAffectTarget>() != null;

                        EditorGUILayout.LabelField("AffectComponent", affectComp != null ? "OK" : "없음");
                        EditorGUILayout.LabelField("IAffectTarget", hasTarget ? "OK" : "없음");
                    }
                }
            }
        }

        private void DrawAffectSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Affect 선택", EditorStyles.boldLabel);

                if (_affectNames.Count == 0)
                {
                    EditorGUILayout.HelpBox("Affect 테이블이 비어있습니다. 테이블 로딩/Addressables 설정을 확인해주세요.", MessageType.Warning);
                    return;
                }

                _selectedAffectIndex = Mathf.Clamp(_selectedAffectIndex, 0, _affectNames.Count - 1);
                _selectedAffectIndex = EditorGUILayout.Popup("Affect", _selectedAffectIndex, _affectNames.ToArray());

                int uid = _affectUids[_selectedAffectIndex];
                if (_affectDict != null && _affectDict.TryGetValue(uid, out var row))
                {
                    EditorGUILayout.LabelField("UID", uid.ToString());
                    EditorGUILayout.LabelField("Name", row.Name);
                    EditorGUILayout.LabelField("GroupId", string.IsNullOrEmpty(row.GroupId) ? "(None)" : row.GroupId);
                    EditorGUILayout.LabelField("BaseDuration", row.BaseDuration.ToString("0.###"));
                    EditorGUILayout.LabelField("TickInterval", row.TickInterval.ToString("0.###"));
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

                if (_affectUids.Count == 0)
                {
                    EditorGUILayout.HelpBox("Affect가 없습니다.", MessageType.Info);
                    return;
                }

                int affectUid = _affectUids[Mathf.Clamp(_selectedAffectIndex, 0, _affectUids.Count - 1)];
                var modifiers = _tableAffectModifier.GetModifiers(affectUid);

                if (modifiers == null || modifiers.Count == 0)
                {
                    EditorGUILayout.HelpBox("연결된 Modifier가 없습니다.", MessageType.Info);
                    return;
                }

                // 간단한 요약
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

                // kind별 핵심 필드만 출력 (테이블 해석 확인용)
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
                        EditorGUILayout.LabelField("CanCrit", m.canCrit ? "true" : "false");
                        EditorGUILayout.LabelField("IsDot", m.isDot ? "true" : "false");
                        break;

                    case ModifierKind.State:
                        EditorGUILayout.LabelField("StateId", string.IsNullOrEmpty(m.stateId) ? "(None)" : m.stateId);
                        EditorGUILayout.LabelField("Chance", m.stateChance.ToString("0.###"));
                        EditorGUILayout.LabelField("DurationOverride", m.stateDurationOverride.ToString("0.###"));
                        break;

                    default:
                        // 신규 Kind가 늘어날 수 있으므로 기본 정보만 유지
                        break;
                }

                // (선택) 현재 런타임 StatusRepo에 등록된 ID인지 빠르게 점검
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
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("테이블 재로딩", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("affect / affect_modifier / stat / state / damage_type 재로딩", GUILayout.Height(24)))
                    {
                        ReloadAllTables();
                        RefreshSceneCharacters();
                    }
                }

                if (!string.IsNullOrEmpty(_lastReloadMessage))
                    EditorGUILayout.HelpBox(_lastReloadMessage, MessageType.Info);
            }
        }

        private void TryAssignFromSelection()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                EditorUtility.DisplayDialog(Title, "Hierarchy에서 대상 오브젝트를 선택해주세요.", "OK");
                return;
            }

            if (!go.TryGetComponent<CharacterBase>(out var character))
                character = go.GetComponentInParent<CharacterBase>();

            if (character == null)
            {
                EditorUtility.DisplayDialog(Title, "선택한 오브젝트에서 CharacterBase를 찾지 못했습니다.", "OK");
                return;
            }

            _targetCharacter = character;
            _selectedCharacterIndex = Mathf.Max(0, _sceneCharacters.IndexOf(character));
            Repaint();
        }

        private void RefreshSceneCharacters()
        {
            _sceneCharacters.Clear();
            _sceneCharacterNames.Clear();

            if (!Application.isPlaying)
                return;

#if UNITY_2023_1_OR_NEWER
            var characters = UnityEngine.Object.FindObjectsByType<CharacterBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            var characters = UnityEngine.Object.FindObjectsOfType<CharacterBase>();
#endif
            foreach (var c in characters)
            {
                if (c == null) continue;
                _sceneCharacters.Add(c);
                _sceneCharacterNames.Add($"{c.name} (id:{c.GetInstanceID()})");
            }

            _selectedCharacterIndex = Mathf.Clamp(_selectedCharacterIndex, 0, Mathf.Max(0, _sceneCharacters.Count - 1));
            if (_sceneCharacters.Count > 0 && _targetCharacter == null)
                _targetCharacter = _sceneCharacters[_selectedCharacterIndex];
        }

        private void ReloadAllTables()
        {
            try
            {
                _tableAffect = TableLoaderManagerAffect.LoadAffectTable();
                _tableAffectModifier = TableLoaderManagerAffect.LoadAffectModifierTable();

                _affectDict = _tableAffect?.GetDatas();
                RebuildAffectDropdown();

                // Core 테이블(툴에서 검증/표시 및 런타임 Repo 초기화 지원)
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

        private void RebuildAffectDropdown()
        {
            _affectNames.Clear();
            _affectUids.Clear();

            if (_affectDict == null) return;

            foreach (var kvp in _affectDict)
            {
                var row = kvp.Value;
                if (row == null || row.Uid <= 0) continue;

                _affectNames.Add($"{row.Uid} - {row.Name}");
                _affectUids.Add(row.Uid);
            }

            _selectedAffectIndex = Mathf.Clamp(_selectedAffectIndex, 0, Mathf.Max(0, _affectUids.Count - 1));
        }

        private void ApplySelectedAffect()
        {
            if (!Application.isPlaying || !SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            if (_targetCharacter == null)
            {
                EditorUtility.DisplayDialog(Title, "대상 캐릭터를 지정해주세요.", "OK");
                return;
            }

            if (_affectUids.Count == 0)
            {
                EditorUtility.DisplayDialog(Title, "적용할 Affect가 없습니다. 테이블 로딩을 확인해주세요.", "OK");
                return;
            }

            int affectUid = _affectUids[Mathf.Clamp(_selectedAffectIndex, 0, _affectUids.Count - 1)];

            // AffectComponent 확보 (+ 옵션에 따라 자동 부착)
            var affectComp = _targetCharacter.GetComponent<AffectComponent>() ?? _targetCharacter.GetComponentInChildren<AffectComponent>();
            if (affectComp == null && _autoAttachComponents)
            {
                affectComp = EnsureAffectComponents(_targetCharacter);
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
            if (target == null) return null;

            // 1) IAffectTarget 확보 (CoreAffectTargetAdapter)
            var hasTarget = target.GetComponent<IAffectTarget>() != null || target.GetComponentInChildren<IAffectTarget>() != null;
            if (!hasTarget)
            {
                // CharacterBase가 있는 GO에 어댑터를 붙이는 것이 가장 안전(동일 Transform 기준)
                if (target.GetComponent<CoreAffectTargetAdapter>() == null)
                    target.gameObject.AddComponent<CoreAffectTargetAdapter>();
            }

            // 2) AffectComponent 확보
            var comp = target.GetComponent<AffectComponent>();
            if (comp == null)
                comp = target.gameObject.AddComponent<AffectComponent>();

            return comp;
        }
    }
}
