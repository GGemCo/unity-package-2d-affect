using System;
using System.Collections.Generic;
using System.Text;
using GGemCo2DCore;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 현재 활성 Affect 를 Debug HUD 에 요약 출력합니다.
    /// 몬스터별로 남은 시간을 빠르게 확인하는 용도입니다.
    /// </summary>
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

        private sealed class MonsterSnapshot
        {
            public string Name;
            public readonly List<DisplayEntry> Entries = new();
        }

        private readonly List<AffectInstance> _instancesBuffer = new(64);
        private readonly List<MonsterSnapshot> _monsterSnapshots = new(16);
        private readonly Dictionary<int, DisplayEntry> _aggregateByUid = new(32);
        private readonly StringBuilder _builder = new(1024);

        public bool IsEnabled(GGemCoSettings settings)
        {
            GGemCoAffectSettings affectSettings = GGemCoAffectSettingsRuntime.GetOrLoad();
            return settings != null
                   && settings.EnableDebugHud
                   && affectSettings != null
                   && affectSettings.EnableAffectDebugHud;
        }

        public float GetUpdateInterval(GGemCoSettings settings)
        {
            GGemCoAffectSettings affectSettings = GGemCoAffectSettingsRuntime.GetOrLoad();
            return affectSettings != null ? Mathf.Max(0.05f, affectSettings.refreshInterval) : 0.10f;
        }

        public void Reset()
        {
            ClearSnapshots();
        }

        public void Tick(float elapsedSeconds)
        {
            GGemCoAffectSettings affectSettings = GGemCoAffectSettingsRuntime.GetOrLoad();
            if (affectSettings == null)
            {
                ClearSnapshots();
                return;
            }

            ClearSnapshots();

            AffectComponent[] components = Object.FindObjectsByType<AffectComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (components == null || components.Length <= 0)
                return;

            for (int i = 0; i < components.Length; i++)
            {
                AffectComponent affectComponent = components[i];
                if (affectComponent == null || !affectComponent.isActiveAndEnabled)
                    continue;

                CharacterBase character = affectComponent.GetComponent<CharacterBase>();
                if (affectSettings.showOnlyMonsters)
                {
                    if (character == null || !character.IsMonster())
                        continue;
                }

                _instancesBuffer.Clear();
                affectComponent.CollectActiveInstances(_instancesBuffer);
                if (_instancesBuffer.Count <= 0)
                    continue;

                MonsterSnapshot snapshot = new MonsterSnapshot
                {
                    Name = character != null ? character.name : affectComponent.gameObject.name
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
                    continue;

                SortEntries(snapshot.Entries, affectSettings.sortMode);
                _monsterSnapshots.Add(snapshot);
            }

            _monsterSnapshots.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            int maxVisibleMonsters = Mathf.Max(1, affectSettings.maxVisibleMonsters);
            if (_monsterSnapshots.Count > maxVisibleMonsters)
            {
                for (int i = _monsterSnapshots.Count - 1; i >= maxVisibleMonsters; i--)
                {
                    _monsterSnapshots.RemoveAt(i);
                }
            }
        }

        public bool TryBuildContent(StringBuilder builder)
        {
            if (_monsterSnapshots.Count <= 0)
                return false;

            GGemCoAffectSettings affectSettings = GGemCoAffectSettingsRuntime.Current;
            int maxVisibleLines = affectSettings != null ? Mathf.Max(1, affectSettings.maxVisibleLinesPerMonster) : 4;

            _builder.Clear();
            _builder.Append("[Affect]");
            _builder.Append(" Active Monsters: ").Append(_monsterSnapshots.Count);

            for (int i = 0; i < _monsterSnapshots.Count; i++)
            {
                MonsterSnapshot snapshot = _monsterSnapshots[i];
                _builder.AppendLine();
                _builder.Append("- ").Append(snapshot.Name).Append(" (").Append(snapshot.Entries.Count).Append(')');

                int lineCount = Mathf.Min(maxVisibleLines, snapshot.Entries.Count);
                for (int j = 0; j < lineCount; j++)
                {
                    DisplayEntry entry = snapshot.Entries[j];
                    _builder.AppendLine();
                    _builder.Append("  • ").Append(entry.Name);

                    if (affectSettings != null && affectSettings.showAffectUid)
                    {
                        _builder.Append(" [").Append(entry.AffectUid).Append(']');
                    }

                    if (affectSettings == null || affectSettings.showStacks)
                    {
                        if (entry.Stacks > 1)
                            _builder.Append(" x").Append(entry.Stacks);
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

        private void BuildAggregatedEntries(List<DisplayEntry> target, GGemCoAffectSettings settings)
        {
            _aggregateByUid.Clear();

            for (int i = 0; i < _instancesBuffer.Count; i++)
            {
                AffectInstance instance = _instancesBuffer[i];
                if (!TryCreateEntry(instance, i, settings, out DisplayEntry entry))
                    continue;

                if (_aggregateByUid.TryGetValue(entry.AffectUid, out DisplayEntry existing))
                {
                    existing.Stacks += Mathf.Max(1, entry.Stacks);
                    if (entry.RemainingTime > existing.RemainingTime)
                        existing.RemainingTime = entry.RemainingTime;
                    if (entry.TotalDuration > existing.TotalDuration)
                        existing.TotalDuration = entry.TotalDuration;
                    if (entry.ApplyOrder < existing.ApplyOrder)
                        existing.ApplyOrder = entry.ApplyOrder;
                    if (string.IsNullOrWhiteSpace(existing.GroupId))
                        existing.GroupId = entry.GroupId;
                    if (string.IsNullOrWhiteSpace(existing.Name))
                        existing.Name = entry.Name;

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

        private void BuildInstanceEntries(List<DisplayEntry> target, GGemCoAffectSettings settings)
        {
            for (int i = 0; i < _instancesBuffer.Count; i++)
            {
                if (TryCreateEntry(_instancesBuffer[i], i, settings, out DisplayEntry entry))
                    target.Add(entry);
            }
        }

        private static bool TryCreateEntry(AffectInstance instance, int applyOrder, GGemCoAffectSettings settings, out DisplayEntry entry)
        {
            entry = default;
            if (instance == null || instance.Definition == null)
                return false;

            if (!settings.includeExpiredEntries && instance.RemainingTime <= 0f)
                return false;

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

        private static string ResolveDisplayName(AffectDefinition definition)
        {
            if (definition == null)
                return "Unknown";

            try
            {
                LocalizationManagerAffect localization = LocalizationManagerAffect.Instance;
                if (localization != null)
                {
                    string localized = localization.GetAffectNameByKey(definition.uid.ToString());
                    if (!string.IsNullOrWhiteSpace(localized))
                        return localized;
                }
            }
            catch
            {
                // 로컬라이제이션 초기화 전일 수 있으므로 디버그 HUD에서는 조용히 폴백한다.
            }

            if (!string.IsNullOrWhiteSpace(definition.nameKey))
                return definition.nameKey;

            return $"Affect {definition.uid}";
        }

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

        private void ClearSnapshots()
        {
            for (int i = 0; i < _monsterSnapshots.Count; i++)
            {
                _monsterSnapshots[i].Entries.Clear();
            }
            _monsterSnapshots.Clear();
            _instancesBuffer.Clear();
            _aggregateByUid.Clear();
        }
    }
}
