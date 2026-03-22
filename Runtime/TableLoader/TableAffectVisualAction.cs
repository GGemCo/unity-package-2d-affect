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
            int uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid"));
            int affectUid = MathHelper.ParseInt(data.GetValueOrDefault("AffectUid"));
            int order = MathHelper.ParseInt(data.GetValueOrDefault("Order"));
            string memo = data.GetValueOrDefault("Memo");
            string name = data.GetValueOrDefault("Name");
            if (string.IsNullOrWhiteSpace(name))
                name = string.IsNullOrWhiteSpace(memo) ? $"AffectVisualAction_{uid}" : memo;

            return new StruckTableAffectVisualAction
            {
                Uid = uid,
                Name = name,
                Memo = memo,
                AffectUid = affectUid,
                Order = order,
                Phase = EnumHelper.ConvertEnum<AffectPhase>(data.GetValueOrDefault("Phase")),
                VfxUid = MathHelper.ParseInt(data.GetValueOrDefault("VfxUid")),
                VfxPlayMode = EnumHelper.ConvertEnum<AffectVfxPlayMode>(data.GetValueOrDefault("VfxPlayMode")),
                VfxScale = MathHelper.ParseFloat(data.GetValueOrDefault("VfxScale")),
                VfxOffsetY = MathHelper.ParseFloat(data.GetValueOrDefault("VfxOffsetY")),
                VfxPositionType = EnumHelper.ConvertEnum<AffectVfxPositionType>(data.GetValueOrDefault("VfxPositionType")),
                VfxFollowType = EnumHelper.ConvertEnum<AffectVfxFollowType>(data.GetValueOrDefault("VfxFollowType")),
                VfxSortingLayerKey = EnumHelper.ConvertEnum<ConfigSortingLayer.Keys>(data.GetValueOrDefault("VfxSortingLayerKey")),
                DurationOverride = MathHelper.ParseFloat(data.GetValueOrDefault("DurationOverride")),
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
