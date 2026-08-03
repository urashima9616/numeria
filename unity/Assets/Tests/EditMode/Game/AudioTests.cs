using System;
using System.Collections.Generic;
using NUnit.Framework;
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
    }
}
