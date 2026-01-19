using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect(버프/디버프) 설명 문구를 Unity Localization Smart String 기반으로 생성하는 서비스.
    /// </summary>
    /// <remarks>
    /// - 기본은 AffectModifierDefinition 조합을 통해 자동 생성한다.
    /// - 특정 Affect에 대해 문장을 완전히 제어하고 싶다면, String Table에 오버라이드 키를 추가할 수 있다.
    ///   예) Affect_Desc_{AffectUid}
    /// - 결과는 Locale+Uid 기준으로 캐시한다.
    /// </remarks>
    public sealed class AffectDescriptionService
    {
        /// <summary>싱글톤 인스턴스(지연 초기화) 필드입니다.</summary>
        private static AffectDescriptionService _instance;

        /// <summary>
        /// 서비스의 싱글톤 인스턴스를 가져옵니다.
        /// </summary>
        public static AffectDescriptionService Instance => _instance ??= new AffectDescriptionService();

        /// <summary>
        /// (Locale, AffectUid) 조합별로 생성된 설명 문자열을 캐시합니다.
        /// </summary>
        private readonly Dictionary<CacheKey, string> _cache = new(256);

        /// <summary>
        /// 설명 조합 중 임시 문자열 생성을 위한 재사용 버퍼입니다.
        /// </summary>
        /// <remarks>한 인스턴스 내부에서만 재사용하므로 멀티스레드 환경에서는 주의가 필요합니다.</remarks>
        private readonly StringBuilder _sb = new(256);

        /// <summary>
        /// 코어(Localization) 매니저에 대한 지연 초기화 캐시입니다.
        /// </summary>
        private static LocalizationManager _localizationManager;

        /// <summary>
        /// 코어(TableLoader) 매니저에 대한 지연 초기화 캐시입니다.
        /// </summary>
        private static TableLoaderManager _tableLoaderManager;

        /// <summary>
        /// 외부 생성을 막기 위한 private 생성자입니다.
        /// </summary>
        private AffectDescriptionService() { }

        /// <summary>
        /// Locale 변경 등으로 캐시가 무효화되어야 할 때 호출합니다.
        /// </summary>
        /// <remarks>서비스가 생성되지 않은 상태라면 아무 작업도 하지 않습니다.</remarks>
        public static void ClearCache()
        {
            // 서비스가 아직 사용되지 않았다면 인스턴스 생성 없이 종료한다.
            if (_instance == null)
                return;

            _instance._cache.Clear();
        }

        /// <summary>
        /// 지정한 AffectUid에 대한 표시용 설명 문구를 생성합니다.
        /// </summary>
        /// <param name="affectUid">Affect 고유 ID</param>
        /// <returns>Modifier/메타 정보를 포함한 여러 줄 설명 문자열(실패 시 빈 문자열)</returns>
        public string GetDescription(int affectUid)
        {
            if (affectUid <= 0) return string.Empty;

            var loc = LocalizationManagerAffect.Instance;
            if (loc == null) return string.Empty;

            var locale = loc.GetCurrentLanguageCode() ?? string.Empty;
            var key = new CacheKey(locale, affectUid);
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            var tables = TableLoaderManagerAffect.Instance;
            if (tables == null) return string.Empty;

            var affectInfo = tables.TableAffect.GetDataByUid(affectUid);
            if (affectInfo == null) return string.Empty;

            // 1) 개별 Affect 오버라이드(선택)
            string overrideKey = $"Affect_Desc_{affectUid}";
            if (loc.HasAffectDescriptionLocalizationKey(overrideKey))
            {
                var overrideText = loc.GetAffectDescriptionByKey(overrideKey);
                if (!string.IsNullOrWhiteSpace(overrideText))
                {
                    _cache[key] = overrideText;
                    return overrideText;
                }
            }

            // 2) 자동 생성: AffectModifierDefinition들을 순회하며 라인 단위로 조합한다.
            _sb.Clear();

            var modifiers = tables.TableAffectModifier.GetModifiers(affectUid);
            for (int i = 0; i < modifiers.Count; i++)
            {
                var mod = modifiers[i];
                string line = BuildLineFromModifier(tables, loc, affectInfo, mod);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (_sb.Length > 0)
                    _sb.Append('\n');
                _sb.Append(line);
            }

            // 3) 메타 정보(지속/틱/스택 등) 라인을 추가한다.
            AppendMetaLines(loc, affectInfo);

            var result = _sb.ToString();
            _cache[key] = result;
            return result;
        }

        /// <summary>
        /// 스킬/아이템처럼 "발동 확률"이 별도로 존재하는 경우, 확률 접두 문구를 붙여 설명을 반환합니다.
        /// </summary>
        /// <param name="affectUid">Affect 고유 ID</param>
        /// <param name="chancePercent">발동 확률(퍼센트 값, 예: 25 = 25%)</param>
        /// <returns>확률 접두 + Affect 설명(실패 시 빈 문자열)</returns>
        public string GetDescriptionWithChancePrefix(int affectUid, float chancePercent)
        {
            var loc = LocalizationManagerAffect.Instance;
            if (loc == null) return string.Empty;

            var desc = GetDescription(affectUid);
            if (string.IsNullOrWhiteSpace(desc)) return string.Empty;

            var args = new AffectChancePrefixArgs
            {
                ChancePercentText = FormatNumber(chancePercent)
            };

            // UIWindowSkillInfo의 Text_Affect 같은 키로도 처리할 수 있지만,
            // Affect 전용 테이블에서 공용 접두 문구를 관리하면 재사용성이 높다.
            var prefix = loc.GetAffectDescriptionSmart(AffectDescriptionKeys.PrefixChance, args);
            if (string.IsNullOrWhiteSpace(prefix))
                prefix = $"{chancePercent}%";

            return $"{prefix}\n{desc}";
        }

        /// <summary>
        /// 상태/스탯/데미지 타입 등의 표시 이름을 로컬라이즈 키로 조회하고, 실패 시 fallback 또는 id로 대체합니다.
        /// </summary>
        /// <param name="loc">Affect 전용 로컬라이제이션 매니저</param>
        /// <param name="id">조회할 키(보통 테이블의 Id)</param>
        /// <param name="fallback">테이블에 캐시된 Name 등 대체 문자열</param>
        /// <returns>로컬라이즈된 이름 또는 fallback/id</returns>
        private static string ResolveStatusName(LocalizationManagerAffect loc, string id, string fallback)
        {
            if (loc == null)
                return fallback ?? string.Empty;

            if (string.IsNullOrWhiteSpace(id))
                return fallback ?? string.Empty;

            _localizationManager ??= LocalizationManager.Instance;
            var localized = _localizationManager.GetStatusNameByKey(id);
            if (!string.IsNullOrWhiteSpace(localized))
                return localized;

            // 로컬라이즈 테이블에 엔트리가 없으면, 테이블에 캐시된 Name 또는 id 자체를 사용한다.
            return !string.IsNullOrWhiteSpace(fallback) ? fallback : id;
        }

        /// <summary>
        /// Modifier 종류에 따라 Smart String 라인 1개를 생성합니다.
        /// </summary>
        /// <param name="tables">Affect 테이블 로더</param>
        /// <param name="loc">Affect 전용 로컬라이제이션 매니저</param>
        /// <param name="affectInfo">Affect 메타 정보(지속시간/틱/스택 등)</param>
        /// <param name="mod">설명 라인으로 변환할 Modifier 정의</param>
        /// <returns>생성된 라인(생성 불가 시 빈 문자열)</returns>
        private string BuildLineFromModifier(
            TableLoaderManagerAffect tables,
            LocalizationManagerAffect loc,
            StruckTableAffect affectInfo,
            AffectModifierDefinition mod)
        {
            _tableLoaderManager ??= TableLoaderManager.Instance;

            switch (mod.kind)
            {
                case ModifierKind.Stat:
                {
                    var stat = _tableLoaderManager.TableStat.GetDataById(mod.statId);
                    string statName = ResolveStatusName(loc, mod.statId, stat?.Name);

                    var args = new AffectStatLineArgs
                    {
                        StatName = statName,
                        Operation = mod.statOperation.ToString(),
                        ValueType = mod.statValueType.ToString(),
                        ValueText = FormatNumber(mod.statValue)
                    };
                    return loc.GetAffectDescriptionSmart(AffectDescriptionKeys.LineStat, args);
                }

                case ModifierKind.Damage:
                {
                    var dmg = _tableLoaderManager.TableDamageType.GetDataById(mod.damageTypeId);
                    string damageTypeName = ResolveStatusName(loc, mod.damageTypeId, dmg?.Name);

                    var scalingStat = _tableLoaderManager.TableStat.GetDataById(mod.scalingStatId);
                    string scalingStatName = ResolveStatusName(loc, mod.scalingStatId, scalingStat?.Name);

                    var args = new AffectDamageLineArgs
                    {
                        DamageTypeName = damageTypeName,
                        BaseValueText = FormatNumber(mod.damageBaseValue),
                        HasScaling = !string.IsNullOrWhiteSpace(mod.scalingStatId) && Math.Abs(mod.scalingCoefficient) > 0f,
                        ScalingStatName = scalingStatName,
                        ScalingCoefText = FormatNumber(mod.scalingCoefficient),
                        CanCrit = mod.canCrit,
                        IsDot = mod.isDot
                    };
                    return loc.GetAffectDescriptionSmart(AffectDescriptionKeys.LineDamage, args);
                }

                case ModifierKind.State:
                {
                    var state = _tableLoaderManager.TableState.GetDataById(mod.stateId);
                    string stateName = ResolveStatusName(loc, mod.stateId, state?.Name);

                    float duration = mod.stateDurationOverride > 0f ? mod.stateDurationOverride : affectInfo.BaseDuration;

                    var args = new AffectStateLineArgs
                    {
                        StateName = stateName,
                        ChancePercentText = FormatNumber(mod.stateChance),
                        DurationSecondsText = duration > 0f ? FormatNumber(duration) : string.Empty,
                        HasDuration = duration > 0f
                    };
                    return loc.GetAffectDescriptionSmart(AffectDescriptionKeys.LineState, args);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Affect 메타 정보(지속시간/틱/스택)를 설명 문자열에 덧붙입니다.
        /// </summary>
        /// <param name="loc">Affect 전용 로컬라이제이션 매니저</param>
        /// <param name="affectInfo">Affect 메타 정보</param>
        private void AppendMetaLines(LocalizationManagerAffect loc, StruckTableAffect affectInfo)
        {
            // 지속시간
            if (affectInfo.BaseDuration > 0f)
            {
                var args = new AffectDurationArgs { SecondsText = FormatNumber(affectInfo.BaseDuration) };
                AppendLineIfAny(loc.GetAffectDescriptionSmart(AffectDescriptionKeys.InfoDuration, args));
            }

            // Tick
            if (affectInfo.TickInterval > 0f)
            {
                var args = new AffectTickArgs { SecondsText = FormatNumber(affectInfo.TickInterval) };
                AppendLineIfAny(loc.GetAffectDescriptionSmart(AffectDescriptionKeys.InfoTick, args));
            }

            // 스택
            if (affectInfo.StackPolicy != StackPolicy.None)
            {
                var args = new AffectStackArgs
                {
                    StackPolicy = ResolveStackPolicyName(affectInfo.StackPolicy),
                    MaxStacksText = affectInfo.MaxStacks > 0
                        ? affectInfo.MaxStacks.ToString(CultureInfo.InvariantCulture)
                        : string.Empty,
                    HasMaxStacks = affectInfo.MaxStacks > 0
                };
                AppendLineIfAny(loc.GetAffectDescriptionSmart(AffectDescriptionKeys.InfoStack, args));
            }
        }

        /// <summary>
        /// 비어있지 않은 라인만 줄바꿈 규칙에 맞춰 StringBuilder에 추가합니다.
        /// </summary>
        /// <param name="line">추가할 라인(공백/빈 문자열이면 무시)</param>
        private void AppendLineIfAny(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            if (_sb.Length > 0) _sb.Append('\n');
            _sb.Append(line);
        }

        /// <summary>
        /// 숫자 표시용 문자열을 invariant 문화권으로 포맷합니다.
        /// </summary>
        /// <param name="value">표시할 값</param>
        /// <returns>소수점 둘째 자리까지 표현한 문자열</returns>
        private static string FormatNumber(float value)
        {
            // 표시 텍스트는 Culture 영향을 받지 않도록 invariant로 고정한다.
            // 필요하면 프로젝트 규칙(소수점 자리수)로 조정 가능.
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 설명 캐시의 키로 사용되는 (Locale, AffectUid) 값 객체입니다.
        /// </summary>
        private readonly struct CacheKey : IEquatable<CacheKey>
        {
            private readonly string _locale;
            private readonly int _uid;

            /// <summary>
            /// 캐시 키를 생성합니다.
            /// </summary>
            /// <param name="locale">언어 코드(Null이면 빈 문자열로 대체)</param>
            /// <param name="uid">Affect 고유 ID</param>
            public CacheKey(string locale, int uid)
            {
                _locale = locale ?? string.Empty;
                _uid = uid;
            }

            /// <inheritdoc />
            public bool Equals(CacheKey other)
                => _uid == other._uid && string.Equals(_locale, other._locale, StringComparison.Ordinal);

            /// <inheritdoc />
            public override bool Equals(object obj)
                => obj is CacheKey other && Equals(other);

            /// <inheritdoc />
            public override int GetHashCode()
                => HashCode.Combine(_locale, _uid);
        }

        /// <summary>
        /// 스택 정책(StackPolicy)의 표시 이름을 로컬라이즈 테이블에서 조회하고, 실패 시 enum 문자열로 대체합니다.
        /// </summary>
        /// <param name="policy">스택 정책</param>
        /// <returns>로컬라이즈된 이름 또는 enum 문자열</returns>
        private static string ResolveStackPolicyName(StackPolicy policy)
        {
            var loc = LocalizationManagerAffect.Instance;
            if (loc == null)
                return policy.ToString();

            var localized = loc.GetStackPolicyName(policy);
            if (!string.IsNullOrEmpty(localized))
                return localized;

            return policy.ToString(); // fallback
        }
    }
}
