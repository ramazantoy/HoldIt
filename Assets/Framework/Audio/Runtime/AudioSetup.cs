using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Audio;

namespace AF.Audio
{
    /// <summary>The mixer as the audio services use it, checked against the options when the root scope is configured.</summary>
    internal sealed class AudioSetup
    {
        public AudioSetup(AudioOptions options, AudioMixerGroup musicGroup, Action<string, float> setParameter)
        {
            Options = options;
            MusicGroup = musicGroup;
            SetParameter = setParameter;
        }

        public AudioOptions Options { get; }
        public AudioMixerGroup MusicGroup { get; }

        /// <summary>Sets an exposed mixer parameter.</summary>
        public Action<string, float> SetParameter { get; }

        public static AudioSetup FromMixer(AudioMixer mixer, AudioOptions options)
        {
            var groups = mixer.FindMatchingGroups(string.Empty);
            var problems = FindProblems(options, groups.Select(group => group.name).ToList(), parameter => mixer.GetFloat(parameter, out _));
            if (problems.Count > 0)
                throw new InvalidOperationException($"The audio mixer {mixer.name} does not match the audio options:\n- {string.Join("\n- ", problems)}");

            var musicGroup = groups.Single(group => group.name == options.Music.Group);
            return new AudioSetup(options, musicGroup, (parameter, value) => mixer.SetFloat(parameter, value));
        }

        /// <summary>What keeps the options from working with a mixer of these groups and exposed parameters.</summary>
        internal static List<string> FindProblems(AudioOptions options, IReadOnlyList<string> groups, Func<string, bool> isExposed)
        {
            var problems = new List<string>();
            if (options.Music == null)
                problems.Add("No music channel: set AudioOptions.Music.");
            else if (!options.Channels.Contains(options.Music))
                problems.Add($"The music channel {options.Music} is not among the channels; Add it.");

            foreach (var channel in options.Channels)
            {
                var matches = groups.Count(group => group == channel.Group);
                if (matches == 0)
                    problems.Add($"No mixer group is named '{channel.Group}'. Groups: {string.Join(", ", groups)}.");
                else if (matches > 1)
                    problems.Add($"{matches} mixer groups are named '{channel.Group}'; channel groups need unique names.");
                if (!isExposed(channel.VolumeParameter))
                    problems.Add($"'{channel.VolumeParameter}' (volume of {channel.Group}) is not an exposed parameter of the mixer.");
            }
            return problems;
        }
    }
}
