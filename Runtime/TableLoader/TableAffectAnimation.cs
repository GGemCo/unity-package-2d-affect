using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// affect_animation 테이블 Row입니다.
    /// Affect 1건에 연결되는 Start/Loop/End 애니메이션 정의를 표현합니다.
    /// </summary>
    public sealed class StruckTableAffectAnimation : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }

        public string Memo;
        public int AffectUid;
        public bool StopCharacterOnApply;
        public string StartAnimationName;
        public string LoopAnimationName;
        public string EndAnimationName;
        public int Priority;
        public bool RestoreWaitOnEnd;
    }

    /// <summary>
    /// affect_animation 서브테이블입니다.
    /// </summary>
    public sealed class TableAffectAnimation : DefaultTable<StruckTableAffectAnimation>
    {
        public override string Key => ConfigAddressableTableAffect.AffectAnimation;

        private readonly Dictionary<int, StruckTableAffectAnimation> _byAffectUid = new();

        protected override void PreLoad()
        {
            base.PreLoad();
            _byAffectUid.Clear();
        }

        protected override StruckTableAffectAnimation BuildRow(Dictionary<string, string> data)
        {
            int uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid"));
            int affectUid = MathHelper.ParseInt(data.GetValueOrDefault("AffectUid"));
            string memo = data.GetValueOrDefault("Memo");
            string name = data.GetValueOrDefault("Name");
            if (string.IsNullOrWhiteSpace(name))
                name = string.IsNullOrWhiteSpace(memo) ? $"AffectAnimation_{uid}" : memo;

            return new StruckTableAffectAnimation
            {
                Uid = uid,
                Name = name,
                Memo = memo,
                AffectUid = affectUid,
                StopCharacterOnApply = ConvertBoolean(data.GetValueOrDefault("StopCharacterOnApply")),
                StartAnimationName = data.GetValueOrDefault("StartAnimationName"),
                LoopAnimationName = data.GetValueOrDefault("LoopAnimationName"),
                EndAnimationName = data.GetValueOrDefault("EndAnimationName"),
                Priority = MathHelper.ParseInt(data.GetValueOrDefault("Priority")),
                RestoreWaitOnEnd = ConvertBoolean(data.GetValueOrDefault("RestoreWaitOnEnd")),
            };
        }

        protected override void OnLoadedData(StruckTableAffectAnimation row)
        {
            base.OnLoadedData(row);
            if (row == null || row.AffectUid <= 0)
                return;

            _byAffectUid[row.AffectUid] = row;
        }

        /// <summary>
        /// 지정한 Affect UID에 연결된 애니메이션 정의를 반환합니다.
        /// </summary>
        public StruckTableAffectAnimation GetDataByAffectUid(int affectUid)
        {
            _byAffectUid.TryGetValue(affectUid, out var row);
            return row;
        }
    }
}
