using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Feedback
{
    public static class FeedbackInstaller
    {
        /// <summary>
        /// Registers <see cref="IFeedback"/> in the root scope, after the audio and the haptics it plays through. It is
        /// scoped: each scene's own gets that scene's sounds, so they stop when the scene does.
        /// </summary>
        public static void UseFeedback(this IContainerBuilder builder, FeedbackSet set)
        {
            builder.RegisterInstance(new FeedbackTable(set.Entries));
            builder.Register<FeedbackPlayer>(Lifetime.Scoped).As<IFeedback>();
        }

        /// <summary>In a gameplay scene scope that has the messages: feedback for what happens on the board and in the run.</summary>
        public static void UseFeedbackEvents(this IContainerBuilder builder) => builder.RegisterEntryPoint<FeedbackEvents>();

        /// <summary>
        /// In a scene scope: plays <paramref name="clip"/> as the music when the scene starts, at <paramref name="level"/>
        /// (0 to 1, under the player's music volume); null leaves the music as it is.
        /// </summary>
        public static void UseSceneMusic(this IContainerBuilder builder, AudioClip clip, float level) =>
            builder.RegisterEntryPoint<SceneMusic>().WithParameter<AudioClip>(clip).WithParameter<float>(level);
    }
}
