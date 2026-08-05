# Numeria Music Attribution

Numeria currently uses selections from **The 8-bit Jukebox Lite**, composed and produced by
**Andrea Baroni / Cyberleaf Studio**.

The package readme says author credit is optional but greatly appreciated. Recommended in-game or
store-page credit:

> Music: The 8-bit Jukebox by Andrea Baroni / Cyberleaf Studio · andreabaroni.com · cyberleafstudio.com

## Runtime mapping

| Numeria mood | Jukebox track | Length | Package loop note | Reason |
|---|---|---:|---|---|
| Mystic Forest | Capt Chip Pants | 1:34 | Seamless | Bright, playful exploration at 128 BPM |
| Silent Peaks | Little Haunted Mansion | 1:52 | Seamless | Mysterious mountain/cave atmosphere at 120 BPM |
| Azure Sky City | Don't Fall Off The Clouds | 1:31 | Seamless | Airy, calm sky theme at 74 BPM |
| Fever Desert | Pyramids Pyramids | 1:33 | Seamless | Bright desert-adventure pulse at 105 BPM |
| Dark Mines | Trial Of Spikes | 1:18 | Full song | Tense mechanical descent with a crisp exploratory pulse |
| Underground Tunnels | Deep In The Caves Below | 2:27 | Full song | Deep, mysterious final-region atmosphere |
| Normal battle | Of Gods And Philosophers | 3:16 | Seamless | Energetic but readable combat pulse at 120 BPM |
| Boss battle | Waking The Demons | 2:45 | Full song | Stronger dramatic escalation at 118 BPM |
| Evolution | Victory At Last | 1:33 | Full song | Celebratory transformation cue at 136 BPM |

The map and normal-battle selections are full tracks explicitly marked as seamlessly loopable in the
package readme. Boss and evolution states use long full songs; normal gameplay should leave those moods
before the tracks reach their endings.

## Local installation

The licensed source package and synchronized `Resources/Music/Jukebox` runtime WAV files are intentionally excluded from Git.
After importing the package into `unity/Assets`, install the selected mapping with:

```bash
zsh tools/install-jukebox-music.sh
```

To copy Numeria's earlier Dynamic Music selections into the same Jukebox runtime slots:

```bash
zsh tools/install-jukebox-music.sh --restore-dynamic
```

`Music.cs` loads stable `Music/Jukebox/{forest,mountains,sky,desert,dark_mines,underground,battle,boss,evolution}` resource names, so
crossfades, settings persistence, and voice ducking remain unchanged. The old `LocalStore` files stay intact;
reverting the feature commit therefore restores the previous soundtrack immediately.
