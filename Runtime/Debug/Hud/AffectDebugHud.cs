using System;
using System.Collections.Generic;
using System.Text;
using GGemCo2DCore;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 현재 활성 어펙트를 Debug HUD에 요약 출력합니다.
    /// </summary>
    /// <remarks>
    /// 기본 용도는 몬스터 어펙트 확인이지만, <see cref="GGemCoAffectSettings.showOnlyMonsters"/>가 꺼져 있으면
    /// 플레이어와 기타 대상도 함께 수집합니다. 이때 플레이어는 표시 제한에 의해 잘리지 않도록 우선 정렬합니다.
    /// </remarks>
    [DebugHudProvider(500)]
    public sealed class AffectDebugHud : IDebugHudProvider
    {
        private struct DisplayEntry
        {
            public int AffectUid;
            public string Name;
            public int Stacks;
            public float RemainingTime;
            public float TotalDuration;
            public int ApplyOrder;
            public string GroupId;
        }

        private sealed class TargetSnapshot
        {
            public string Name;
            public CharacterConstants.Type CharacterType;
            public readonly List<DisplayEntry> Entries = new();
        }

        private readonly List<AffectInstance> _instancesBuffer = new(64);
        private readonly List<TargetSnapshot> _targetSnapshots = new(16);
        private readonly Dictionary<int, DisplayEntry> _aggregateByUid = new(32);
        private readonly StringBuilder _builder = new(1024);

        /// <summary>
        /// Affect Debug HUD를 표시할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="settings">Core Debug HUD 설정입니다.</param>
        /// <returns>Core Debug HUD와 Affect Debug HUD가 모두 활성화되어 있으면 true입니다.</returns>
        public bool IsEnabled(GGemCoSettings settings)
        {
            GGemCoAffectSettings affectSettings = GGemCoAffectSettingsRuntime.GetOrLoad();
            return settings != null
                   && settings.EnableDebugHud
                   && affectSettings != null
                   && affectSettings.EnableAffectDebugHud;
        }

        /// <summary>
        /// Affect Debug HUD 갱신 주기를 반환합니다.
        /// </summary>
        /// <param name="settings">Core Debug HUD 설정입니다.</param>
        /// <returns>초 단위 갱신 주기입니다.</returns>
        public float GetUpdateInterval(GGemCoSettings settings)
        {
            GGemCoAffectSettings affectSettings = GGemCoAffectSettingsRuntime.GetOrLoad();
            return affectSettings != null ? Mathf.Max(0.05f, affectSettings.refreshInterval) : 0.10f;
        }

        /// <summary>
        /// 캐시된 HUD 스냅샷을 초기화합니다.
        /// </summary>
        public void Reset()
        {
            ClearSnapshots();
        }

        /// <summary>
        /// 현재 씬의 AffectComponent를 수집하고 표시용 스냅샷을 갱신합니다.
        /// </summary>
        /// <param name="elapsedSeconds">이전 갱신 이후 경과 시간입니다.</param>
        public void Tick(float elapsedSeconds)
        {
            GGemCoAffectSettings affectSettings = GGemCoAffectSettingsRuntime.GetOrLoad();
            if (affectSettings == null)
            {
                ClearSnapshots();
                return;
            }

            ClearSnapshots();

            AffectComponent[] components = Object.FindObjectsByType<AffectComponent>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            if (components == null || components.Length <= 0)
            {
                return;
            }

            for (int i = 0; i < components.Length; i++)
            {
                AffectComponent affectComponent = components[i];
                if (affectComponent == null || !affectComponent.isActiveAndEnabled)
                {
                    continue;
                }

                CharacterBase character = affectComponent.GetComponent<CharacterBase>();
                if (affectSettings.showOnlyMonsters && (character == null || !character.IsMonster()))
                {
                    continue;
                }

                _instancesBuffer.Clear();
                affectComponent.CollectActiveInstances(_instancesBuffer);
                if (_instancesBuffer.Count <= 0)
                {
                    continue;
                }

                TargetSnapshot snapshot = new TargetSnapshot
                {
                    Name = ResolveTargetName(affectComponent, character),
                    CharacterType = character != null ? character.type : CharacterConstants.Type.None
                };

                if (affectSettings.aggregateSameAffectUid)
                {
                    BuildAggregatedEntries(snapshot.Entries, affectSettings);
                }
                else
                {
                    BuildInstanceEntries(snapshot.Entries, affectSettings);
                }

                if (snapshot.Entries.Count <= 0)
                {
                    continue;
                }

                SortEntries(snapshot.Entries, affectSettings.sortMode);
                _targetSnapshots.Add(snapshot);
            }

            SortTargets(_targetSnapshots, affectSettings.showOnlyMonsters);
            TrimTargets(_targetSnapshots, affectSettings);
        }

        /// <summary>
        /// Debug HUD에 표시할 문자열을 구성합니다.
        /// </summary>
        /// <param name="builder">HUD 전체 문자열을 누적할 StringBuilder입니다.</param>
        /// <returns>표시할 내용이 있으면 true입니다.</returns>
        public bool TryBuildContent(StringBuilder builder)
        {
            if (_targetSnapshots.Count <= 0)
            {
                return false;
            }

            GGemCoAffectSettings affectSettings = GGemCoAffectSettingsRuntime.Current;
            int maxVisibleLines = affectSettings != null ? Mathf.Max(1, affectSettings.maxVisibleLinesPerMonster) : 4;
            bool monsterOnly = affectSettings != null && affectSettings.showOnlyMonsters;

            _builder.Clear();
            _builder.Append("[Affect]");
            _builder.Append(monsterOnly ? " Active Monsters: " : " Active Targets: ");
            _builder.Append(_targetSnapshots.Count);

            for (int i = 0; i < _targetSnapshots.Count; i++)
            {
                TargetSnapshot snapshot = _targetSnapshots[i];
                _builder.AppendLine();
                _builder.Append("- ").Append(snapshot.Name).Append(" (").Append(snapshot.Entries.Count).Append(')');

                int lineCount = Mathf.Min(maxVisibleLines, snapshot.Entries.Count);
                for (int j = 0; j < lineCount; j++)
                {
                    DisplayEntry entry = snapshot.Entries[j];
                    _builder.AppendLine();
                    _builder.Append("  - ").Append(entry.Name);

                    if (affectSettings != null && affectSettings.showAffectUid)
                    {
                        _builder.Append(" [").Append(entry.AffectUid).Append(']');
                    }

                    if (affectSettings == null || affectSettings.showStacks)
                    {
                        if (entry.Stacks > 1)
                        {
                            _builder.Append(" x").Append(entry.Stacks);
                        }
                    }

                    _builder.Append(' ').Append(entry.RemainingTime.ToString("0.0")).Append('s');

                    if (affectSettings != null && affectSettings.showGroupId && !string.IsNullOrWhiteSpace(entry.GroupId))
                    {
                        _builder.Append(" {group:").Append(entry.GroupId).Append('}');
                    }
                }

                if (snapshot.Entries.Count > lineCount)
                {
                    _builder.AppendLine();
                    _builder.Append("  +").Append(snapshot.Entries.Count - lineCount).Append(" more");
                }
            }

            builder.Append(_builder);
            return _builder.Length > 0;
        }

        /// <summary>
        /// 동일 Affect UID를 하나의 표시 항목으로 합산합니다.
        /// </summary>
        /// <param name="target">표시 항목을 추가할 목록입니다.</param>
        /// <param name="settings">Affect Debug HUD 설정입니다.</param>
        private void BuildAggregatedEntries(List<DisplayEntry> target, GGemCoAffectSettings settings)
        {
            _aggregateByUid.Clear();

            for (int i = 0; i < _instancesBuffer.Count; i++)
            {
                AffectInstance instance = _instancesBuffer[i];
                if (!TryCreateEntry(instance, i, settings, out DisplayEntry entry))
                {
                    continue;
                }

                if (_aggregateByUid.TryGetValue(entry.AffectUid, out DisplayEntry existing))
                {
                    existing.Stacks += Mathf.Max(1, entry.Stacks);
                    if (entry.RemainingTime > existing.RemainingTime)
                    {
                        existing.RemainingTime = entry.RemainingTime;
                    }

                    if (entry.TotalDuration > existing.TotalDuration)
                    {
                        existing.TotalDuration = entry.TotalDuration;
                    }

                    if (entry.ApplyOrder < existing.ApplyOrder)
                    {
                        existing.ApplyOrder = entry.ApplyOrder;
                    }

                    if (string.IsNullOrWhiteSpace(existing.GroupId))
                    {
                        existing.GroupId = entry.GroupId;
                    }

                    if (string.IsNullOrWhiteSpace(existing.Name))
                    {
                        existing.Name = entry.Name;
                    }

                    _aggregateByUid[entry.AffectUid] = existing;
                    continue;
                }

                _aggregateByUid.Add(entry.AffectUid, entry);
            }

            foreach (KeyValuePair<int, DisplayEntry> pair in _aggregateByUid)
            {
                target.Add(pair.Value);
            }
        }

        /// <summary>
        /// 개별 AffectInstance를 각각 표시 항목으로 변환합니다.
        /// </summary>
        /// <param name="target">표시 항목을 추가할 목록입니다.</param>
        /// <param name="settings">Affect Debug HUD 설정입니다.</param>
        private void BuildInstanceEntries(List<DisplayEntry> target, GGemCoAffectSettings settings)
        {
            for (int i = 0; i < _instancesBuffer.Count; i++)
            {
                if (TryCreateEntry(_instancesBuffer[i], i, settings, out DisplayEntry entry))
                {
                    target.Add(entry);
                }
            }
        }

        /// <summary>
        /// 런타임 AffectInstance를 HUD 표시 항목으로 변환합니다.
        /// </summary>
        /// <param name="instance">변환할 어펙트 인스턴스입니다.</param>
        /// <param name="applyOrder">현재 대상 안에서의 적용 순서입니다.</param>
        /// <param name="settings">Affect Debug HUD 설정입니다.</param>
        /// <param name="entry">생성된 표시 항목입니다.</param>
        /// <returns>표시 가능한 항목이면 true입니다.</returns>
        private static bool TryCreateEntry(
            AffectInstance instance,
            int applyOrder,
            GGemCoAffectSettings settings,
            out DisplayEntry entry)
        {
            entry = default;
            if (instance == null || instance.Definition == null)
            {
                return false;
            }

            if (!settings.includeExpiredEntries && instance.RemainingTime <= 0f)
            {
                return false;
            }

            AffectDefinition definition = instance.Definition;
            entry = new DisplayEntry
            {
                AffectUid = definition.uid,
                Name = ResolveDisplayName(definition),
                Stacks = Mathf.Max(1, instance.Stacks),
                RemainingTime = Mathf.Max(0f, instance.RemainingTime),
                TotalDuration = Mathf.Max(0f, instance.TotalDuration),
                ApplyOrder = applyOrder,
                GroupId = definition.groupId
            };
            return true;
        }

        /// <summary>
        /// HUD 대상 이름을 캐릭터 타입과 함께 구성합니다.
        /// </summary>
        /// <param name="affectComponent">어펙트 컴포넌트입니다.</param>
        /// <param name="character">연결된 Core 캐릭터입니다.</param>
        /// <returns>Debug HUD에 표시할 대상 이름입니다.</returns>
        private static string ResolveTargetName(AffectComponent affectComponent, CharacterBase character)
        {
            string targetName = character != null ? character.name : affectComponent.gameObject.name;
            if (character == null)
            {
                return targetName;
            }

            return character.type switch
            {
                CharacterConstants.Type.Player => $"Player: {targetName}",
                CharacterConstants.Type.Monster => $"Monster: {targetName}",
                CharacterConstants.Type.Npc => $"Npc: {targetName}",
                _ => targetName
            };
        }

        /// <summary>
        /// 어펙트 정의에서 표시 이름을 해석합니다.
        /// </summary>
        /// <param name="definition">표시 이름을 해석할 어펙트 정의입니다.</param>
        /// <returns>Localization 이름, nameKey, fallback 이름 순서로 해석한 표시 이름입니다.</returns>
        private static string ResolveDisplayName(AffectDefinition definition)
        {
            if (definition == null)
            {
                return "Unknown";
            }

            try
            {
                LocalizationManagerAffect localization = LocalizationManagerAffect.Instance;
                if (localization != null)
                {
                    string localized = localization.GetAffectNameByKey(definition.uid.ToString());
                    if (!string.IsNullOrWhiteSpace(localized))
                    {
                        return localized;
                    }
                }
            }
            catch
            {
                // Localization 초기화 전에도 Debug HUD는 조용히 fallback 이름을 사용합니다.
            }

            if (!string.IsNullOrWhiteSpace(definition.nameKey))
            {
                return definition.nameKey;
            }

            return $"Affect {definition.uid}";
        }

        /// <summary>
        /// 대상 스냅샷을 HUD 표시 순서로 정렬합니다.
        /// </summary>
        /// <param name="targets">정렬할 대상 목록입니다.</param>
        /// <param name="monsterOnly">몬스터만 표시하는 모드인지 여부입니다.</param>
        private static void SortTargets(List<TargetSnapshot> targets, bool monsterOnly)
        {
            if (monsterOnly)
            {
                targets.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
                return;
            }

            // 전체 대상 표시 모드에서는 플레이어가 표시 제한에 잘리지 않도록 항상 가장 앞에 둡니다.
            targets.Sort((a, b) =>
            {
                int priorityCompare = GetTargetPriority(a).CompareTo(GetTargetPriority(b));
                return priorityCompare != 0
                    ? priorityCompare
                    : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });
        }

        /// <summary>
        /// HUD 대상 표시 우선순위를 반환합니다.
        /// </summary>
        /// <param name="target">우선순위를 확인할 대상입니다.</param>
        /// <returns>낮을수록 먼저 표시됩니다.</returns>
        private static int GetTargetPriority(TargetSnapshot target)
        {
            if (target == null)
            {
                return 99;
            }

            return target.CharacterType switch
            {
                CharacterConstants.Type.Player => 0,
                CharacterConstants.Type.Monster => 1,
                CharacterConstants.Type.Npc => 2,
                _ => 3
            };
        }

        /// <summary>
        /// 설정된 최대 표시 대상 수에 맞춰 스냅샷 목록을 잘라냅니다.
        /// </summary>
        /// <param name="targets">표시 대상 스냅샷 목록입니다.</param>
        /// <param name="settings">Affect Debug HUD 설정입니다.</param>
        private static void TrimTargets(List<TargetSnapshot> targets, GGemCoAffectSettings settings)
        {
            int maxVisibleTargets = settings.MaxVisibleDebugTargets;
            if (targets.Count <= maxVisibleTargets)
            {
                return;
            }

            for (int i = targets.Count - 1; i >= maxVisibleTargets; i--)
            {
                targets.RemoveAt(i);
            }
        }

        /// <summary>
        /// 표시 항목을 설정된 정렬 방식에 따라 정렬합니다.
        /// </summary>
        /// <param name="entries">정렬할 표시 항목 목록입니다.</param>
        /// <param name="sortMode">정렬 기준입니다.</param>
        private static void SortEntries(List<DisplayEntry> entries, AffectDebugSortMode sortMode)
        {
            switch (sortMode)
            {
                case AffectDebugSortMode.RemainingTimeDesc:
                    entries.Sort((a, b) => b.RemainingTime.CompareTo(a.RemainingTime));
                    break;
                case AffectDebugSortMode.Name:
                    entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
                    break;
                case AffectDebugSortMode.ApplyOrder:
                    entries.Sort((a, b) => a.ApplyOrder.CompareTo(b.ApplyOrder));
                    break;
                case AffectDebugSortMode.RemainingTimeAsc:
                default:
                    entries.Sort((a, b) => a.RemainingTime.CompareTo(b.RemainingTime));
                    break;
            }
        }

        /// <summary>
        /// 내부 스냅샷 버퍼와 집계 캐시를 초기화합니다.
        /// </summary>
        private void ClearSnapshots()
        {
            for (int i = 0; i < _targetSnapshots.Count; i++)
            {
                _targetSnapshots[i].Entries.Clear();
            }

            _targetSnapshots.Clear();
            _instancesBuffer.Clear();
            _aggregateByUid.Clear();
        }
    }
}
