using System;
using System.Collections.Generic;
using NUnit.Framework;
using Numeria.Core;
using UnityEngine;

namespace Numeria.Game.Tests
{
    public class AudioTests
    {
        [Test]
        public void EverySfxCue_HasAnImportedResource()
        {
            foreach (SfxCue cue in Enum.GetValues(typeof(SfxCue)))
            {
                string path = Sfx.ResourcePath(cue);
                Assert.IsNotEmpty(path, cue.ToString());
                Assert.IsNotNull(Resources.Load<AudioClip>(path), $"Missing resource for {cue}: {path}");
            }
        }

        [Test]
        public void MusicMoods_HaveUniqueLocalStorePaths()
        {
            var paths = new HashSet<string>();
            foreach (MusicMood mood in Enum.GetValues(typeof(MusicMood)))
            {
                if (mood == MusicMood.None) continue;
                string path = Music.ResourcePath(mood);
                StringAssert.StartsWith("Music/Jukebox/", path);
                Assert.True(paths.Add(path), $"Duplicate music path: {path}");
            }
        }

        [Test]
        public void EveryMusicMood_HasAnImportedNonEmptyClip()
        {
            foreach (MusicMood mood in Enum.GetValues(typeof(MusicMood)))
            {
                if (mood == MusicMood.None) continue;
                var clip = Resources.Load<AudioClip>(Music.ResourcePath(mood));
                Assert.IsNotNull(clip, $"Missing music for {mood}: {Music.ResourcePath(mood)}");
                Assert.Greater(clip.length, 60f, $"Music for {mood} is unexpectedly short");
                Assert.GreaterOrEqual(clip.channels, 1, mood.ToString());
            }
        }

        [Test]
        public void ExpansionRosterAndDesertLinesHaveOfflineNarration()
        {
            foreach (SpeciesDef species in GameData.Roster)
            {
                AssertVoice($"A wild {species.Name} appeared!");
                AssertVoice($"Gotcha! {species.Name} wants to travel with you!");
                AssertVoice($"{species.Name} is getting stronger!");
            }

            AssertVoice("Thirty-nine plus what makes forty?");
            AssertVoice("Forty take away what leaves thirty-nine?");
            AssertVoice("You caught a stronger friend. Keep it, or turn the catch into experience.");
            AssertVoice("Your stronger friend is ready for adventure!");
            AssertVoice("Welcome to Fever Desert!");
            AssertVoice("The four Digit Crystals sing together. The gate home is awake!");
            AssertVoice("What tile comes next in the pattern?");
            AssertVoice("Which tile fills the pattern gap?");
            AssertVoice("Find the exact tile pattern!");
            AssertVoice("Find the tiles in mirror order!");
            AssertVoice("Match the shapes. Colors can change!");
            AssertVoice("Match the colors. Shapes can change!");
            AssertVoice("Balance both sides. What number is missing?");
            AssertVoice("Which number fills the path?");
            AssertVoice("Double damage!");
        }

        private static void AssertVoice(string line)
        {
            string path = "Voice/" + VoiceKeys.Sanitize(line);
            Assert.IsNotNull(Resources.Load<AudioClip>(path), $"Missing narration for '{line}' at {path}");
        }
    }
}
