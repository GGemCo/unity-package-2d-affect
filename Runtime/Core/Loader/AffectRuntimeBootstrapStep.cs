using System.Collections;
using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 테이블 로드 이후, com.ggemco.2d.affect 런타임 저장소(<see cref="AffectRuntime"/>)를 Core/Affect 테이블로부터 초기화한다.
    /// </summary>
    /// <remarks>
    /// - 스킬 시스템(별도 패키지)에서 <see cref="AffectComponent"/>를 사용하려면 런타임 저장소가 준비되어야 하며,
    ///   본 스텝은 그 초기화를 담당한다.
    /// - 초기화 결과는 전역 정적 진입점(<see cref="AffectRuntime.AffectRepository"/>, <see cref="AffectRuntime.StatusRepository"/>)에 주입된다.
    /// - 이 스텝은 일반적으로 게임 로딩 파이프라인에서 1회 실행되는 것을 전제로 한다.
    /// </remarks>
    public sealed class AffectRuntimeBootstrapStep : GameLoadStepBase
    {
        /// <summary>
        /// Affect 런타임 부트스트랩 스텝을 생성한다.
        /// </summary>
        /// <param name="id">로딩 스텝 식별자.</param>
        /// <param name="order">로딩 스텝 실행 순서.</param>
        /// <param name="localizedKey">UI 표시용 로컬라이즈 키.</param>
        public AffectRuntimeBootstrapStep(string id, int order, string localizedKey)
            : base(id, order, localizedKey)
        {
        }

        /// <summary>
        /// Core 테이블 및 Affect 테이블을 읽어 런타임 저장소를 구성한다.
        /// </summary>
        /// <returns>코루틴 이터레이터.</returns>
        /// <remarks>
        /// - <see cref="TableLoaderManager"/> 및 <see cref="TableLoaderManagerAffect"/> 싱글톤이 존재해야 한다.
        /// - 진행률(<c>progress</c>)은 성공/실패와 무관하게 종료 시 1로 설정된다.
        /// </remarks>
        public override IEnumerator Run()
        {
            progress = 0f;

            var tableLoaderManager = TableLoaderManager.Instance;
            if (tableLoaderManager == null)
            {
                progress = 1f;
                GcLogger.LogError($"{nameof(TableLoaderManager)} 싱글톤이 없습니다.");
                yield break;
            }

            var tableLoaderManagerAffect = TableLoaderManagerAffect.Instance;
            if (tableLoaderManagerAffect == null)
            {
                progress = 1f;
                GcLogger.LogError($"{nameof(TableLoaderManagerAffect)} 싱글톤이 없습니다.");
                yield break;
            }

            // 런타임에서 참조할 인메모리 저장소를 구성한다.
            var affectRepo = new InMemoryAffectRepository();
            var statusRepo = new InMemoryStatusRepository();

            // Status: Stat/DamageType/State
            // Affect 시스템이 참조할 수 있도록 Core 테이블의 ID들을 미리 등록한다.
            foreach (var kv in tableLoaderManager.TableStat.GetAll())
                statusRepo.RegisterStat(kv.Value.ID);

            foreach (var kv in tableLoaderManager.TableDamageType.GetAll())
                statusRepo.RegisterDamageType(kv.Value.ID);

            foreach (var kv in tableLoaderManager.TableState.GetAll())
                statusRepo.RegisterState(kv.Value.ID);

            // Affect + Modifiers
            // Affect 테이블 Row를 AffectDefinition으로 변환하고, UID 기준으로 modifier들을 함께 등록한다.
            foreach (var kv in tableLoaderManagerAffect.TableAffect.GetAll())
            {
                var row = kv.Value;

                // Core 테이블 Row → AffectDefinition
                var def = new AffectDefinition
                {
                    uid = row.Uid,
                    iconKey = row.IconKey,
                    groupId = row.GroupId,
                    baseDuration = row.BaseDuration,
                    tickInterval = row.TickInterval,
                    stackPolicy = row.StackPolicy,
                    maxStacks = row.MaxStacks,
                    refreshPolicy = row.RefreshPolicy,
                    dispelType = row.DispelType,
                    effectUid = row.EffectUid,
                    effectScale = row.EffectScale,
                    effectOffsetY = row.EffectOffsetY,
                    effectPositionType = row.EffectPositionType,
                    effectFollowType = row.EffectFollowType,
                    effectSortingLayerKey = row.EffectSortingLayerKey,
                    applyChance = row.ApplyChance
                };

                // Tags: "a,b c" 형태를 콤마/공백 기준으로 분리하여 정규화한다.
                def.tags.Clear();
                if (!string.IsNullOrWhiteSpace(row.Tags))
                {
                    var parts = row.Tags.Split(',', ' ');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        var t = parts[i]?.Trim();
                        if (string.IsNullOrWhiteSpace(t)) continue;
                        def.tags.Add(t);
                    }
                }

                // UID에 연결된 modifier 정의를 함께 등록한다.
                var mods = tableLoaderManagerAffect.TableAffectModifier.GetModifiers(row.Uid);
                affectRepo.Register(def, new List<AffectModifierDefinition>(mods));
            }

            // 전역 런타임 진입점에 주입한다.
            AffectRuntime.AffectRepository = affectRepo;
            AffectRuntime.StatusRepository = statusRepo;

            progress = 1f;
            yield break;
        }
    }
}
