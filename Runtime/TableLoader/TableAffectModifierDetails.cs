using System;
using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect Modifier 상세 테이블 Row가 공통으로 제공해야 하는 연결 키 계약입니다.
    /// </summary>
    /// <remarks>
    /// affect_modifier 공통 메타 테이블의 AffectUid + ModifierId 조합으로
    /// Kind별 상세 테이블을 연결합니다. 이후 공통 Modifier UID가 추가되더라도 이 계약만 교체하면 됩니다.
    /// </remarks>
    public interface IAffectModifierDetailRow : IUidName
    {
        /// <summary>상세 값이 연결될 Affect UID입니다.</summary>
        int AffectUid { get; }

        /// <summary>Affect 내부 Modifier 식별자입니다.</summary>
        int ModifierId { get; }
    }

    /// <summary>
    /// Kind별 Affect Modifier 상세 테이블의 공통 베이스입니다.
    /// </summary>
    /// <typeparam name="TRow">상세 테이블 Row 타입입니다.</typeparam>
    /// <remarks>
    /// - 각 상세 테이블은 Uid를 독립 Row 식별자로 사용합니다.
    /// - 실제 런타임 연결은 기존 affect_modifier의 AffectUid + ModifierId 조합으로 수행합니다.
    /// - 상세 테이블 값은 실행 Payload의 기준 데이터로 사용됩니다.
    /// </remarks>
    public abstract class TableAffectModifierDetailBase<TRow> : DefaultTable<TRow>
        where TRow : class, IAffectModifierDetailRow
    {
        /// <summary>
        /// AffectUid + ModifierId 조합으로 상세 Row를 빠르게 찾기 위한 캐시입니다.
        /// </summary>
        private readonly Dictionary<long, TRow> _byModifierKey = new();

        /// <summary>
        /// 테이블 로드 전 상세 Row 인덱스를 초기화합니다.
        /// </summary>
        protected override void PreLoad()
        {
            base.PreLoad();
            _byModifierKey.Clear();
        }

        /// <summary>
        /// 상세 Row를 AffectUid + ModifierId 기준 인덱스에 등록합니다.
        /// </summary>
        /// <param name="row">방금 로드된 상세 Row입니다.</param>
        protected override void OnLoadedData(TRow row)
        {
            base.OnLoadedData(row);
            if (row == null || row.AffectUid <= 0 || row.ModifierId <= 0)
                return;

            _byModifierKey[BuildKey(row.AffectUid, row.ModifierId)] = row;
        }

        /// <summary>
        /// 상세 Row를 Payload로 변환해 반환합니다.
        /// </summary>
        /// <param name="affectUid">연결 대상 Affect UID입니다.</param>
        /// <param name="modifierId">연결 대상 Modifier ID입니다.</param>
        /// <param name="ownerKind">공통 affect_modifier 행에 정의된 Kind입니다.</param>
        /// <param name="payload">상세 테이블에서 생성한 Payload입니다.</param>
        /// <returns>상세 Row가 존재하고 Payload 생성에 성공하면 true입니다.</returns>
        public bool TryCreatePayload(int affectUid, int modifierId, ModifierKind ownerKind, out IAffectModifierPayload payload)
        {
            payload = null;
            if (!_byModifierKey.TryGetValue(BuildKey(affectUid, modifierId), out TRow row) || row == null)
                return false;

            payload = CreatePayload(row, ownerKind);
            return payload != null;
        }

        /// <summary>
        /// 상세 Row를 Kind별 Payload DTO로 변환합니다.
        /// </summary>
        /// <param name="row">상세 테이블 Row입니다.</param>
        /// <param name="ownerKind">공통 affect_modifier 행에 정의된 Kind입니다.</param>
        /// <returns>생성된 Payload입니다.</returns>
        protected abstract IAffectModifierPayload CreatePayload(TRow row, ModifierKind ownerKind);

        /// <summary>
        /// AffectUid와 ModifierId를 Dictionary 키로 압축합니다.
        /// </summary>
        /// <param name="affectUid">Affect UID입니다.</param>
        /// <param name="modifierId">Modifier ID입니다.</param>
        /// <returns>두 정수 조합을 나타내는 64비트 키입니다.</returns>
        private static long BuildKey(int affectUid, int modifierId)
        {
            return ((long)affectUid << 32) | (uint)modifierId;
        }

        /// <summary>
        /// 공통 상세 Row 필드를 읽습니다.
        /// </summary>
        /// <param name="reader">테이블 행 리더입니다.</param>
        /// <param name="uid">Row UID입니다.</param>
        /// <param name="name">표시 이름입니다.</param>
        /// <param name="memo">메모입니다.</param>
        /// <param name="affectUid">Affect UID입니다.</param>
        /// <param name="modifierId">Modifier ID입니다.</param>
        protected static void ReadCommon(
            TableRowReader reader,
            out int uid,
            out string name,
            out string memo,
            out int affectUid,
            out int modifierId)
        {
            uid = reader.Int("Uid");
            memo = reader.String("Memo");
            name = reader.String("Name");
            if (string.IsNullOrWhiteSpace(name))
                name = string.IsNullOrWhiteSpace(memo) ? $"AffectModifierDetail_{uid}" : memo;

            affectUid = reader.Int("AffectUid");
            modifierId = reader.Int("ModifierId");
        }
    }

    /// <summary>
    /// affect_modifier_stat 테이블 Row입니다.
    /// </summary>
    public sealed class StruckTableAffectModifierStat : IAffectModifierDetailRow
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public string Memo;
        public int AffectUid { get; set; }
        public int ModifierId { get; set; }
        public string StatId;
        public float Value;
        public StatValueType ValueType;
        public StatOperation Operation;
    }

    /// <summary>
    /// affect_modifier_stat 상세 테이블입니다.
    /// </summary>
    public sealed class TableAffectModifierStat : TableAffectModifierDetailBase<StruckTableAffectModifierStat>
    {
        public override string Key => ConfigAddressableTableAffect.AffectModifierStat;

        /// <summary>
        /// 테이블 Row를 스탯 Modifier 상세 Row로 변환합니다.
        /// </summary>
        protected override StruckTableAffectModifierStat BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            ReadCommon(reader, out int uid, out string name, out string memo, out int affectUid, out int modifierId);

            return new StruckTableAffectModifierStat
            {
                Uid = uid,
                Name = name,
                Memo = memo,
                AffectUid = affectUid,
                ModifierId = modifierId,
                StatId = reader.String("StatId"),
                Value = reader.Float("Value"),
                ValueType = reader.Enum<StatValueType>("ValueType"),
                Operation = reader.Enum<StatOperation>("Operation"),
            };
        }

        /// <summary>
        /// 스탯 상세 Row를 Payload로 변환합니다.
        /// </summary>
        protected override IAffectModifierPayload CreatePayload(StruckTableAffectModifierStat row, ModifierKind ownerKind)
        {
            return new AffectStatModifierPayload
            {
                statId = row.StatId,
                value = row.Value,
                valueType = row.ValueType,
                operation = row.Operation,
            };
        }
    }

    /// <summary>
    /// affect_modifier_damage 테이블 Row입니다.
    /// Damage Kind가 함께 사용합니다.
    /// </summary>
    public sealed class StruckTableAffectModifierDamage : IAffectModifierDetailRow
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public string Memo;
        public int AffectUid { get; set; }
        public int ModifierId { get; set; }
        public string DamageTypeId;
        public float BaseValue;
        public string ScalingStatId;
        public float ScalingCoefficient;
        public bool CanCrit;
        public bool IsDot;
        public bool SuppressDamageReaction;
        public bool ShowHitEffect;
    }

    /// <summary>
    /// affect_modifier_damage 상세 테이블입니다.
    /// </summary>
    public sealed class TableAffectModifierDamage : TableAffectModifierDetailBase<StruckTableAffectModifierDamage>
    {
        public override string Key => ConfigAddressableTableAffect.AffectModifierDamage;

        /// <summary>
        /// 테이블 Row를 피해 Modifier 상세 Row로 변환합니다.
        /// </summary>
        protected override StruckTableAffectModifierDamage BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            ReadCommon(reader, out int uid, out string name, out string memo, out int affectUid, out int modifierId);

            return new StruckTableAffectModifierDamage
            {
                Uid = uid,
                Name = name,
                Memo = memo,
                AffectUid = affectUid,
                ModifierId = modifierId,
                DamageTypeId = reader.String("DamageTypeId"),
                BaseValue = reader.Float("BaseValue"),
                ScalingStatId = reader.String("ScalingStatId"),
                ScalingCoefficient = reader.Float("ScalingCoefficient"),
                CanCrit = reader.BoolYN("CanCrit"),
                IsDot = reader.BoolYN("IsDot"),
                SuppressDamageReaction = reader.BoolYN("SuppressDamageReaction"),
                ShowHitEffect = reader.BoolYN("ShowHitEffect", true),
            };
        }

        /// <summary>
        /// 피해 상세 Row를 Damage Payload로 변환합니다.
        /// </summary>
        protected override IAffectModifierPayload CreatePayload(StruckTableAffectModifierDamage row, ModifierKind ownerKind)
        {
            return new AffectDamageModifierPayload(ownerKind)
            {
                damageTypeId = row.DamageTypeId,
                baseValue = row.BaseValue,
                scalingStatId = row.ScalingStatId,
                scalingCoefficient = row.ScalingCoefficient,
                canCrit = row.CanCrit,
                isDot = row.IsDot,
                suppressDamageReaction = row.SuppressDamageReaction,
                showHitEffect = row.ShowHitEffect,
            };
        }
    }

    /// <summary>
    /// affect_modifier_element_gauge 테이블 Row입니다.
    /// </summary>
    public sealed class StruckTableAffectModifierElementGauge : IAffectModifierDetailRow
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public string Memo;
        public int AffectUid { get; set; }
        public int ModifierId { get; set; }
        public string ElementTypeId;
        public float GaugeValue;
        public ElementGaugeTargetPolicy TargetPolicy;
        public bool UseStackMultiplier;
        public bool UseContextMultiplier;
        public bool RequireAliveTarget;
    }

    /// <summary>
    /// affect_modifier_element_gauge 상세 테이블입니다.
    /// </summary>
    public sealed class TableAffectModifierElementGauge : TableAffectModifierDetailBase<StruckTableAffectModifierElementGauge>
    {
        public override string Key => ConfigAddressableTableAffect.AffectModifierElementGauge;

        /// <summary>
        /// 테이블 Row를 속성 게이지 Modifier 상세 Row로 변환합니다.
        /// </summary>
        /// <param name="data">컬럼 이름과 값으로 구성된 원본 Row입니다.</param>
        /// <returns>속성 게이지 Modifier 상세 Row입니다.</returns>
        protected override StruckTableAffectModifierElementGauge BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            ReadCommon(reader, out int uid, out string name, out string memo, out int affectUid, out int modifierId);

            return new StruckTableAffectModifierElementGauge
            {
                Uid = uid,
                Name = name,
                Memo = memo,
                AffectUid = affectUid,
                ModifierId = modifierId,
                ElementTypeId = ReadElementTypeId(reader),
                GaugeValue = reader.Float("GaugeValue"),
                TargetPolicy = reader.Enum("TargetPolicy", ElementGaugeTargetPolicy.HitTarget),
                UseStackMultiplier = reader.BoolYN("UseStackMultiplier", true),
                UseContextMultiplier = reader.BoolYN("UseContextMultiplier", true),
                RequireAliveTarget = reader.BoolYN("RequireAliveTarget", true),
            };
        }

        /// <summary>
        /// 속성 게이지 상세 Row를 Payload로 변환합니다.
        /// </summary>
        /// <param name="row">상세 테이블 Row입니다.</param>
        /// <param name="ownerKind">공통 affect_modifier 행에 정의된 Kind입니다.</param>
        /// <returns>속성 게이지 Payload입니다.</returns>
        protected override IAffectModifierPayload CreatePayload(StruckTableAffectModifierElementGauge row, ModifierKind ownerKind)
        {
            return new AffectElementGaugeModifierPayload
            {
                elementTypeId = row.ElementTypeId,
                gaugeValue = row.GaugeValue,
                targetPolicy = row.TargetPolicy,
                useStackMultiplier = row.UseStackMultiplier,
                useContextMultiplier = row.UseContextMultiplier,
                requireAliveTarget = row.RequireAliveTarget,
            };
        }

        /// <summary>
        /// 속성 타입 컬럼을 읽습니다.
        /// </summary>
        /// <param name="reader">테이블 행 리더입니다.</param>
        /// <returns>ElementTypeId 값입니다. 이전 컬럼명으로 작성된 데이터가 있으면 DamageTypeId도 허용합니다.</returns>
        private static string ReadElementTypeId(TableRowReader reader)
        {
            string elementTypeId = reader.String("ElementTypeId");
            return string.IsNullOrWhiteSpace(elementTypeId)
                ? reader.String("DamageTypeId")
                : elementTypeId;
        }
    }

    /// <summary>
    /// affect_modifier_heal 테이블 Row입니다.
    /// </summary>
    public sealed class StruckTableAffectModifierHeal : IAffectModifierDetailRow
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public string Memo;
        public int AffectUid { get; set; }
        public int ModifierId { get; set; }
        public float BaseValue;
        public string ScalingStatId;
        public float ScalingCoefficient;
    }

    /// <summary>
    /// affect_modifier_heal 상세 테이블입니다.
    /// </summary>
    public sealed class TableAffectModifierHeal : TableAffectModifierDetailBase<StruckTableAffectModifierHeal>
    {
        public override string Key => ConfigAddressableTableAffect.AffectModifierHeal;

        /// <summary>
        /// 테이블 Row를 회복 Modifier 상세 Row로 변환합니다.
        /// </summary>
        protected override StruckTableAffectModifierHeal BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            ReadCommon(reader, out int uid, out string name, out string memo, out int affectUid, out int modifierId);

            return new StruckTableAffectModifierHeal
            {
                Uid = uid,
                Name = name,
                Memo = memo,
                AffectUid = affectUid,
                ModifierId = modifierId,
                BaseValue = reader.Float("BaseValue"),
                ScalingStatId = reader.String("ScalingStatId"),
                ScalingCoefficient = reader.Float("ScalingCoefficient"),
            };
        }

        /// <summary>
        /// 회복 상세 Row를 Payload로 변환합니다.
        /// </summary>
        protected override IAffectModifierPayload CreatePayload(StruckTableAffectModifierHeal row, ModifierKind ownerKind)
        {
            return new AffectHealModifierPayload
            {
                baseValue = row.BaseValue,
                scalingStatId = row.ScalingStatId,
                scalingCoefficient = row.ScalingCoefficient,
            };
        }
    }

    /// <summary>
    /// affect_modifier_state 테이블 Row입니다.
    /// </summary>
    public sealed class StruckTableAffectModifierState : IAffectModifierDetailRow
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public string Memo;
        public int AffectUid { get; set; }
        public int ModifierId { get; set; }
        public string StateId;
        public float Chance;
        public float DurationOverride;
    }

    /// <summary>
    /// affect_modifier_state 상세 테이블입니다.
    /// </summary>
    public sealed class TableAffectModifierState : TableAffectModifierDetailBase<StruckTableAffectModifierState>
    {
        public override string Key => ConfigAddressableTableAffect.AffectModifierState;

        /// <summary>
        /// 테이블 Row를 상태 Modifier 상세 Row로 변환합니다.
        /// </summary>
        protected override StruckTableAffectModifierState BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            ReadCommon(reader, out int uid, out string name, out string memo, out int affectUid, out int modifierId);

            return new StruckTableAffectModifierState
            {
                Uid = uid,
                Name = name,
                Memo = memo,
                AffectUid = affectUid,
                ModifierId = modifierId,
                StateId = reader.String("StateId"),
                Chance = reader.Float("Chance"),
                DurationOverride = reader.Float("DurationOverride"),
            };
        }

        /// <summary>
        /// 상태 상세 Row를 Payload로 변환합니다.
        /// </summary>
        protected override IAffectModifierPayload CreatePayload(StruckTableAffectModifierState row, ModifierKind ownerKind)
        {
            return new AffectStateModifierPayload
            {
                stateId = row.StateId,
                chance = row.Chance,
                durationOverride = row.DurationOverride,
            };
        }
    }

    /// <summary>
    /// affect_modifier_crowd_control 테이블 Row입니다.
    /// </summary>
    public sealed class StruckTableAffectModifierCrowdControl : IAffectModifierDetailRow
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public string Memo;
        public int AffectUid { get; set; }
        public int ModifierId { get; set; }
        public int CrowdControlUid;
    }

    /// <summary>
    /// affect_modifier_crowd_control 상세 테이블입니다.
    /// </summary>
    public sealed class TableAffectModifierCrowdControl : TableAffectModifierDetailBase<StruckTableAffectModifierCrowdControl>
    {
        public override string Key => ConfigAddressableTableAffect.AffectModifierCrowdControl;

        /// <summary>
        /// 테이블 Row를 CrowdControl Modifier 상세 Row로 변환합니다.
        /// </summary>
        protected override StruckTableAffectModifierCrowdControl BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            ReadCommon(reader, out int uid, out string name, out string memo, out int affectUid, out int modifierId);

            return new StruckTableAffectModifierCrowdControl
            {
                Uid = uid,
                Name = name,
                Memo = memo,
                AffectUid = affectUid,
                ModifierId = modifierId,
                CrowdControlUid = reader.Int("CrowdControlUid"),
            };
        }

        /// <summary>
        /// CrowdControl 상세 Row를 Payload로 변환합니다.
        /// </summary>
        protected override IAffectModifierPayload CreatePayload(StruckTableAffectModifierCrowdControl row, ModifierKind ownerKind)
        {
            return new AffectCrowdControlModifierPayload
            {
                crowdControlUid = row.CrowdControlUid,
            };
        }
    }

    /// <summary>
    /// affect_modifier_apply_affect 테이블 Row입니다.
    /// </summary>
    public sealed class StruckTableAffectModifierApplyAffect : IAffectModifierDetailRow
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public string Memo;
        public int AffectUid { get; set; }
        public int ModifierId { get; set; }
        public int ApplyAffectUid;
        public float Chance;
        public float DurationOverride;
        public bool ConsumeOnProc;
    }

    /// <summary>
    /// affect_modifier_apply_affect 상세 테이블입니다.
    /// </summary>
    public sealed class TableAffectModifierApplyAffect : TableAffectModifierDetailBase<StruckTableAffectModifierApplyAffect>
    {
        public override string Key => ConfigAddressableTableAffect.AffectModifierApplyAffect;

        /// <summary>
        /// 테이블 Row를 ApplyAffect Modifier 상세 Row로 변환합니다.
        /// </summary>
        protected override StruckTableAffectModifierApplyAffect BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            ReadCommon(reader, out int uid, out string name, out string memo, out int affectUid, out int modifierId);

            return new StruckTableAffectModifierApplyAffect
            {
                Uid = uid,
                Name = name,
                Memo = memo,
                AffectUid = affectUid,
                ModifierId = modifierId,
                ApplyAffectUid = reader.Int("ApplyAffectUid"),
                Chance = reader.Float("Chance"),
                DurationOverride = reader.Float("DurationOverride"),
                ConsumeOnProc = reader.BoolYN("ConsumeOnProc"),
            };
        }

        /// <summary>
        /// ApplyAffect 상세 Row를 Payload로 변환합니다.
        /// </summary>
        protected override IAffectModifierPayload CreatePayload(StruckTableAffectModifierApplyAffect row, ModifierKind ownerKind)
        {
            return new AffectApplyAffectModifierPayload
            {
                applyAffectUid = row.ApplyAffectUid,
                chance = row.Chance,
                durationOverride = row.DurationOverride,
                consumeOnProc = row.ConsumeOnProc,
            };
        }
    }

    /// <summary>
    /// affect_modifier_formula_variable 테이블 Row입니다.
    /// </summary>
    public sealed class StruckTableAffectModifierFormulaVariable : IAffectModifierDetailRow
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public string Memo;
        public int AffectUid { get; set; }
        public int ModifierId { get; set; }
        public string FormulaVariableId;
        public float Value;
        public StatValueType ValueType;
        public StatOperation Operation;
    }

    /// <summary>
    /// affect_modifier_formula_variable 상세 테이블입니다.
    /// </summary>
    public sealed class TableAffectModifierFormulaVariable : TableAffectModifierDetailBase<StruckTableAffectModifierFormulaVariable>
    {
        public override string Key => ConfigAddressableTableAffect.AffectModifierFormulaVariable;

        /// <summary>
        /// 테이블 Row를 공식 변수 Modifier 상세 Row로 변환합니다.
        /// </summary>
        protected override StruckTableAffectModifierFormulaVariable BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            ReadCommon(reader, out int uid, out string name, out string memo, out int affectUid, out int modifierId);

            return new StruckTableAffectModifierFormulaVariable
            {
                Uid = uid,
                Name = name,
                Memo = memo,
                AffectUid = affectUid,
                ModifierId = modifierId,
                FormulaVariableId = reader.String("FormulaVariableId"),
                Value = reader.Float("Value"),
                ValueType = reader.Enum<StatValueType>("ValueType"),
                Operation = reader.Enum<StatOperation>("Operation"),
            };
        }

        /// <summary>
        /// 공식 변수 상세 Row를 Payload로 변환합니다.
        /// </summary>
        protected override IAffectModifierPayload CreatePayload(StruckTableAffectModifierFormulaVariable row, ModifierKind ownerKind)
        {
            return new AffectFormulaVariableModifierPayload
            {
                variableId = row.FormulaVariableId,
                value = row.Value,
                valueType = row.ValueType,
                operation = row.Operation,
            };
        }
    }
}
