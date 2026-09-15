#if COZY_GIBBERISH_VCONTAINER
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CozyGibberish.Integrations.VContainer
{
    public sealed class GibberishLifetimeScope : LifetimeScope
    {
        [SerializeField] private GibberishVoicePlayer _player;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<GibberishUtterancePlanner>(Lifetime.Singleton)
                .As<IGibberishUtterancePlanner>();
            builder.Register<ProceduralGibberishSynthesizer>(Lifetime.Singleton)
                .As<IGibberishSynthesizer>();
            builder.Register<GibberishEventBus>(Lifetime.Singleton)
                .As<IGibberishEventBus>();
            builder.RegisterComponent(_player)
                .AsSelf()
                .As<IGibberishSpeechService>()
                .As<IAdvancedGibberishSpeechService>();
            builder.RegisterEntryPoint<GibberishCompositionRoot>();
        }
    }

    public sealed class GibberishCompositionRoot : IStartable
    {
        private readonly GibberishVoicePlayer _player;
        private readonly IGibberishUtterancePlanner _planner;
        private readonly IGibberishSynthesizer _synthesizer;
        private readonly IGibberishEventBus _eventBus;

        public GibberishCompositionRoot(
            GibberishVoicePlayer player,
            IGibberishUtterancePlanner planner,
            IGibberishSynthesizer synthesizer,
            IGibberishEventBus eventBus)
        {
            _player = player;
            _planner = planner;
            _synthesizer = synthesizer;
            _eventBus = eventBus;
        }

        public void Start()
        {
            _player.Initialize(_planner, _synthesizer, _eventBus);
        }
    }
}
#endif
