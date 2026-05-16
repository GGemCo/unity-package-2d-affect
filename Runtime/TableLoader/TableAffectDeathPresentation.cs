using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// affect_death_presentation 테이블 Row입니다.
    /// 특정 Affect가 사망 원인이 되었을 때 사용할 캐릭터 사망 연출을 표현합니다.
    /// </summary>
    public sealed class StruckTableAffectDeathPresentation : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }

        public string Memo;
        public int AffectUid;
        public int Priority;
        public string DeathAnimationName;
        public int DeathVfxUid;
        public float DeathVfxScale;
        public float DeathVfxOffsetY;
        public AffectVfxPositionType DeathVfxPositionType;
        public AffectVfxFollowType DeathVfxFollowType;
        public ConfigSortingLayer.Keys DeathVfxSortingLayerKey;
        public bool UseDeathVfxSortingLayer;
        public float DeathVfxDurationOverride;
        public int DeathCutsceneUid;
        public bool SuppressDefaultDeathAnimation;
        public bool FreezeLastFrame;
    }

    /// <summary>
    /// affect_death_presentation 서브테이블입니다.
    /// </summary>
    /// <remarks>
    /// Affect UID 기준으로 1개의 사망 연출 정의를 조회할 수 있도록 보조 인덱스를 구성합니다.
    /// 동일 Affect UID가 여러 번 등록되면 Priority가 높은 행을 우선하고, Priority가 같으면 Uid가 큰 행을 사용합니다.
    /// </remarks>
    public sealed class TableAffectDeathPresentation : DefaultTable<StruckTableAffectDeathPresentation>
    {
        public override string Key => ConfigAddressableTableAffect.AffectDeathPresentation;

        private readonly Dictionary<int, StruckTableAffectDeathPresentation> _byAffectUid = new();

        protected override void PreLoad()
        {
            base.PreLoad();
            _byAffectUid.Clear();
        }

        protected override StruckTableAffectDeathPresentation BuildRow(Dictionary<string, string> data)
        {
            int uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid"));
            int affectUid = MathHelper.ParseInt(data.GetValueOrDefault("AffectUid"));
            string memo = data.GetValueOrDefault("Memo");
            string name = data.GetValueOrDefault("Name");
            if (string.IsNullOrWhiteSpace(name))
                name = string.IsNullOrWhiteSpace(memo) ? $"AffectDeathPresentation_{uid}" : memo;

            return new StruckTableAffectDeathPresentation
            {
                Uid = uid,
                Name = name,
                Memo = memo,
                AffectUid = affectUid,
                Priority = MathHelper.ParseInt(data.GetValueOrDefault("Priority")),
                DeathAnimationName = data.GetValueOrDefault("DeathAnimationName"),
                DeathVfxUid = MathHelper.ParseInt(data.GetValueOrDefault("DeathVfxUid")),
                DeathVfxScale = MathHelper.ParseFloat(data.GetValueOrDefault("DeathVfxScale")),
                DeathVfxOffsetY = MathHelper.ParseFloat(data.GetValueOrDefault("DeathVfxOffsetY")),
                DeathVfxPositionType = EnumHelper.ConvertEnum<AffectVfxPositionType>(data.GetValueOrDefault("DeathVfxPositionType")),
                DeathVfxFollowType = EnumHelper.ConvertEnum<AffectVfxFollowType>(data.GetValueOrDefault("DeathVfxFollowType")),
                DeathVfxSortingLayerKey = EnumHelper.ConvertEnum<ConfigSortingLayer.Keys>(data.GetValueOrDefault("DeathVfxSortingLayerKey")),
                UseDeathVfxSortingLayer = ConvertBoolean(data.GetValueOrDefault("UseDeathVfxSortingLayer")),
                DeathVfxDurationOverride = MathHelper.ParseFloat(data.GetValueOrDefault("DeathVfxDurationOverride")),
                DeathCutsceneUid = MathHelper.ParseInt(data.GetValueOrDefault("DeathCutsceneUid")),
                SuppressDefaultDeathAnimation = ConvertBoolean(data.GetValueOrDefault("SuppressDefaultDeathAnimation")),
                FreezeLastFrame = ConvertBoolean(data.GetValueOrDefault("FreezeLastFrame")),
            };
        }

        protected override void OnLoadedData(StruckTableAffectDeathPresentation row)
        {
            base.OnLoadedData(row);
            if (row == null || row.AffectUid <= 0)
                return;

            if (!_byAffectUid.TryGetValue(row.AffectUid, out var current) || ShouldReplace(current, row))
                _byAffectUid[row.AffectUid] = row;
        }

        /// <summary>
        /// 지정한 Affect UID에 연결된 사망 연출 Row를 반환합니다.
        /// </summary>
        /// <param name="affectUid">조회할 Affect UID입니다.</param>
        /// <returns>사망 연출 Row입니다. 없으면 <see langword="null"/>입니다.</returns>
        public StruckTableAffectDeathPresentation GetDataByAffectUid(int affectUid)
        {
            _byAffectUid.TryGetValue(affectUid, out var row);
            return row;
        }

        /// <summary>
        /// 동일 Affect UID 중 어떤 Row를 최종 정의로 사용할지 판단합니다.
        /// </summary>
        /// <param name="current">현재 등록된 Row입니다.</param>
        /// <param name="next">새로 등록할 후보 Row입니다.</param>
        /// <returns>새 Row로 교체해야 하면 <see langword="true"/>입니다.</returns>
        private static bool ShouldReplace(StruckTableAffectDeathPresentation current, StruckTableAffectDeathPresentation next)
        {
            if (current == null)
                return true;
            if (next == null)
                return false;
            if (next.Priority != current.Priority)
                return next.Priority > current.Priority;
            return next.Uid > current.Uid;
        }
    }
}
