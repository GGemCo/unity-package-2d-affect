using System;
using System.Collections.Generic;
using System.Linq;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// affect_visual_action 테이블 Row
    /// - 1행 = 1개 비주얼 액션
    /// - AffectUid를 통해 affect(Uid)와 연결
    /// </summary>
    public sealed class StruckTableAffectVisualAction : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }

        public string Memo;
        public int AffectUid;
        public int Order;
        public AffectPhase Phase;
        public int VfxUid;
        public AffectVfxPlayMode VfxPlayMode;
        public float VfxScale;
        public float VfxOffsetY;
        public AffectVfxPositionType VfxPositionType;
        public AffectVfxFollowType VfxFollowType;
        public ConfigSortingLayer.Keys VfxSortingLayerKey;
        public float DurationOverride;
    }

    /// <summary>
    /// affect_visual_action 테이블
    /// </summary>
    public sealed class TableAffectVisualAction : DefaultTable<StruckTableAffectVisualAction>
    {
        public override string Key => ConfigAddressableTableAffect.AffectVisualAction;

        private readonly Dictionary<int, List<StruckTableAffectVisualAction>> _byAffectUid = new();

        protected override void PreLoad()
        {
            base.PreLoad();
            _byAffectUid.Clear();
        }

        protected override StruckTableAffectVisualAction BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            int uid = reader.Int("Uid");
            int affectUid = reader.Int("AffectUid");
            int order = reader.Int("Order");
            string memo = reader.String("Memo");
            string name = reader.String("Name");
            if (string.IsNullOrWhiteSpace(name))
                name = string.IsNullOrWhiteSpace(memo) ? $"AffectVisualAction_{uid}" : memo;

            return new StruckTableAffectVisualAction
            {
                Uid = uid,
                Name = name,
                Memo = memo,
                AffectUid = affectUid,
                Order = order,
                Phase = reader.Enum<AffectPhase>("Phase"),
                VfxUid = reader.Int("VfxUid"),
                VfxPlayMode = reader.Enum<AffectVfxPlayMode>("VfxPlayMode"),
                VfxScale = reader.Float("VfxScale"),
                VfxOffsetY = reader.Float("VfxOffsetY"),
                VfxPositionType = reader.Enum<AffectVfxPositionType>("VfxPositionType"),
                VfxFollowType = reader.Enum<AffectVfxFollowType>("VfxFollowType"),
                VfxSortingLayerKey = reader.Enum<ConfigSortingLayer.Keys>("VfxSortingLayerKey"),
                DurationOverride = reader.Float("DurationOverride"),
            };
        }

        protected override void OnLoadedData(StruckTableAffectVisualAction row)
        {
            base.OnLoadedData(row);
            if (row == null || row.AffectUid <= 0)
                return;

            if (!_byAffectUid.TryGetValue(row.AffectUid, out var list))
            {
                list = new List<StruckTableAffectVisualAction>();
                _byAffectUid[row.AffectUid] = list;
            }

            list.Add(row);
        }

        public IReadOnlyList<StruckTableAffectVisualAction> GetActions(int affectUid)
        {
            if (!_byAffectUid.TryGetValue(affectUid, out var list) || list == null || list.Count == 0)
                return Array.Empty<StruckTableAffectVisualAction>();

            return list.OrderBy(x => x.Order).ThenBy(x => x.Uid).ToList();
        }
    }
}
