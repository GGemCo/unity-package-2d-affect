using System.Collections.Generic;

namespace GGemCo2DAffect
{
    public sealed class AffectInstance
    {
        public readonly AffectDefinition Definition;
        public readonly AffectApplyContext Context;

        public int Stacks { get; private set; } = 1;
        public float RemainingTime { get; private set; }
        public float TotalDuration { get; }
        public float TickElapsed { get; private set; }

        private readonly List<object> _statTokens = new();
        private readonly List<object> _stateTokens = new();

        public AffectInstance(AffectDefinition definition, AffectApplyContext context, float duration)
        {
            Definition = definition;
            Context = context;
            RemainingTime = duration;
            TotalDuration = duration;
            TickElapsed = 0f;
        }

        public void AddStack(int maxStacks)
        {
            if (maxStacks <= 0) maxStacks = 1;
            Stacks = System.Math.Min(maxStacks, Stacks + 1);
        }

        public void Refresh(float duration)
        {
            RemainingTime = duration;
        }

        public void UpdateTime(float dt)
        {
            if (RemainingTime <= 0f) return;
            RemainingTime -= dt;
            if (RemainingTime < 0f) RemainingTime = 0f;
        }

        public bool IsExpired => RemainingTime <= 0f;

        public void AccumulateTick(float dt)
        {
            TickElapsed += dt;
            if (TickElapsed < 0f) TickElapsed = 0f;
        }

        public bool TryConsumeTick(float interval)
        {
            if (interval <= 0f) return false;
            if (TickElapsed < interval) return false;
            TickElapsed -= interval;
            return true;
        }

        public void AddStatToken(object token)
        {
            if (token != null) _statTokens.Add(token);
        }

        public void AddStateToken(object token)
        {
            if (token != null) _stateTokens.Add(token);
        }

        public IReadOnlyList<object> StatTokens => _statTokens;
        public IReadOnlyList<object> StateTokens => _stateTokens;
    }
}
