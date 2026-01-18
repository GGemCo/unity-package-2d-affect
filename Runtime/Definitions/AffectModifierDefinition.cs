using System;

namespace GGemCo2DAffect
{
    [Serializable]
    public sealed class AffectModifierDefinition
    {
        public int affectUid;
        public int modifierId;

        public AffectPhase phase;
        public ModifierKind kind;

        // --- Kind=Stat ---
        public string statId;
        public float statValue;
        public StatValueType statValueType;
        public StatOperation statOperation;

        // --- Kind=Damage ---
        public string damageTypeId;
        public float damageBaseValue;
        public string scalingStatId;
        public float scalingCoefficient;
        public bool canCrit;
        public bool isDot;

        // --- Kind=State ---
        public string stateId;
        public float stateChance;
        public float stateDurationOverride;

        // --- Common ---
        public string conditionId; // 확장용(예: TargetHasState:STUN 등)

        public override string ToString()
        {
            return $"AffectUid={affectUid}, ModifierId={modifierId}, Phase={phase}, Kind={kind}";
        }
    }
}
