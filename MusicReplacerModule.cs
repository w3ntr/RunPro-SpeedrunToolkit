using MelonLoader;
using MelonLoader.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public class MusicReplacerModule
    {
        public enum ReplaceMode
        {
            ByName = 0,         // По совпадению имен файлов
            SelectedTrack = 1,  // Любая музыка игры заменяется на выбранный трек
            Shuffle = 2         // Случайный кастомный трек при смене музыки
        }

        public bool EnableMusicReplacer = true;
        public ReplaceMode Mode = ReplaceMode.SelectedTrack;
        public float MasterVolume = 1.0f;
        public string SelectedTrackName = "";
        public float PitchMultiplier = 1.0f;

        private string musicFolder;
        private string listFilePath;
        private Dictionary<string, AudioClip> customClips = new Dictionary<string, AudioClip>();
        private List<string> customClipNames = new List<string>();
        private HashSet<string> discoveredTracks = new HashSet<string>();

        private MelonPreferences_Category configCategory;
        private MelonPreferences_Entry<bool> configEnable;
        private MelonPreferences_Entry<int> configMode;
        private MelonPreferences_Entry<float> configVolume;
        private MelonPreferences_Entry<string> configSelectedTrack;
        private MelonPreferences_Entry<float> configPitch;

        private float checkTimer = 0f;
        private float dumpTimer = 0f; // Отдельный таймер для редкого сканирования клипов в файл
        private string lastReplacedTrack = "None";
        private Vector2 scrollPosition = Vector2.zero;

        public void Init()
        {
            configCategory = MelonPreferences.CreateCategory("MusicReplacerMod", "Custom Music Settings");
            configEnable = configCategory.CreateEntry("EnableMusicReplacer", true, "Enable Custom Music");
            configMode = configCategory.CreateEntry("ReplaceMode", (int)ReplaceMode.SelectedTrack, "Replace Mode (0=ByName, 1=Selected, 2=Shuffle)");
            configVolume = configCategory.CreateEntry("MasterVolume", 1.0f, "Music Volume");
            configSelectedTrack = configCategory.CreateEntry("SelectedTrackName", "", "Selected Track Name");

            EnableMusicReplacer = configEnable.Value;
            Mode = (ReplaceMode)configMode.Value;
            MasterVolume = configVolume.Value;
            configPitch = configCategory.CreateEntry("PitchMultiplier", 1.0f, "Music Speed & Pitch");
            PitchMultiplier = configPitch.Value;
            SelectedTrackName = configSelectedTrack.Value;

            musicFolder = Path.Combine(MelonEnvironment.UserDataDirectory, "CustomMusic");
            if (!Directory.Exists(musicFolder))
            {
                Directory.CreateDirectory(musicFolder);
            }

            listFilePath = Path.Combine(musicFolder, "music_tracks_list.txt");
            LoadAllCustomWavs();

            MelonLogger.Msg("[MusicReplacerModule] Initialized.");
        }

        public void OnUpdate()
        {
            if (!EnableMusicReplacer) return;

            float dt = Time.deltaTime;

            // Проверка музыки теперь происходит 4 раза в секунду вместо 20 (убирает микрофризы)
            checkTimer += dt;
            if (checkTimer >= 0.25f)
            {
                checkTimer = 0f;
                EnforceCustomMusic();
            }

            // Сканирование треков для текстового файла происходит раз в 3 секунды, не нагружая игру
            dumpTimer += dt;
            if (dumpTimer >= 3.0f)
            {
                dumpTimer = 0f;
                DumpMusicTracks();
            }
        }

        public void LoadAllCustomWavs()
        {
            string[] files = Directory.GetFiles(musicFolder, "*.wav");
            foreach (var file in files)
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(file);
                if (!customClips.ContainsKey(nameWithoutExt))
                {
                    AudioClip clip = LoadWavFromFile(file, nameWithoutExt);
                    if (clip != null)
                    {
                        clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
                        customClips[nameWithoutExt] = clip;
                        customClipNames.Add(nameWithoutExt);
                        MelonLogger.Msg($"[MusicReplacer] Loaded custom track: {nameWithoutExt}");
                    }
                }
            }

            if (string.IsNullOrEmpty(SelectedTrackName) && customClipNames.Count > 0)
            {
                SelectedTrackName = customClipNames[0];
            }
        }

        private void EnforceCustomMusic()
        {
            if (customClips.Count == 0) return;

            var sources = Resources.FindObjectsOfTypeAll<AudioSource>();
            foreach (var source in sources)
            {
                if (source == null || source.clip == null) continue;

                if (customClips.ContainsValue(source.clip))
                {
                    source.volume = MasterVolume;
                    continue;
                }

                if (source.clip.length >= 10.0f)
                {
                    AudioClip targetClip = GetTargetClip(source.clip.name);
                    if (targetClip != null)
                    {
                        bool wasPlaying = source.isPlaying;
                        source.Stop();
                        source.clip = targetClip;
                        source.volume = MasterVolume;
                        source.pitch = PitchMultiplier;
                        if (wasPlaying)
                        {
                            source.Play();
                        }
                        lastReplacedTrack = targetClip.name;
                        MelonLogger.Msg($"[MusicReplacer] Replaced game music with '{targetClip.name}'");
                    }
                }
            }
        }

        private AudioClip GetTargetClip(string gameClipName)
        {
            if (Mode == ReplaceMode.ByName)
            {
                if (customClips.TryGetValue(gameClipName, out AudioClip clip))
                    return clip;
                return null;
            }
            else if (Mode == ReplaceMode.SelectedTrack)
            {
                if (!string.IsNullOrEmpty(SelectedTrackName) && customClips.TryGetValue(SelectedTrackName, out AudioClip clip))
                    return clip;
                if (customClipNames.Count > 0)
                    return customClips[customClipNames[0]];
            }
            else if (Mode == ReplaceMode.Shuffle)
            {
                int randomIndex = Random.Range(0, customClipNames.Count);
                return customClips[customClipNames[randomIndex]];
            }

            return null;
        }

        public void ForcePlaySong(string songName)
        {
            if (!customClips.TryGetValue(songName, out AudioClip customClip)) return;

            var sources = Resources.FindObjectsOfTypeAll<AudioSource>();
            foreach (var source in sources)
            {
                if (source == null || source.clip == null) continue;

                if (source.clip.length >= 10.0f || customClips.ContainsValue(source.clip))
                {
                    source.Stop();
                    source.clip = customClip;
                    source.volume = MasterVolume;
                    source.pitch = PitchMultiplier;
                    source.Play();
                    lastReplacedTrack = songName;
                    MelonLogger.Msg($"[MusicReplacer] Force playing: '{songName}'");
                    break;
                }
            }
        }

        private void DumpMusicTracks()
        {
            var clips = Resources.FindObjectsOfTypeAll<AudioClip>();
            bool newFound = false;

            foreach (var clip in clips)
            {
                if (clip == null || string.IsNullOrEmpty(clip.name)) continue;

                if (clip.length >= 15.0f && !discoveredTracks.Contains(clip.name) && !customClips.ContainsValue(clip))
                {
                    discoveredTracks.Add(clip.name);
                    newFound = true;
                }
            }

            if (newFound)
            {
                List<string> lines = new List<string>
                {
                    "=== DISCOVERED GAME MUSIC TRACKS ===",
                    "Name your .wav files as listed below (if using ByName mode):",
                    "------------------------------------------------"
                };

                foreach (var track in discoveredTracks)
                {
                    lines.Add(track);
                }

                File.WriteAllLines(listFilePath, lines.ToArray());
            }
        }

        public float DrawUI(float startX, float startY, float width)
        {
            float y = startY;

            GUI.Label(new Rect(startX, y, width, 20), "<b>Custom Music Replacer Settings:</b>");
            y += 24f;

            bool newEnable = GUI.Toggle(new Rect(startX, y, width, 20), EnableMusicReplacer, " Enable Music Replacer");
            if (newEnable != EnableMusicReplacer)
            {
                EnableMusicReplacer = newEnable;
                configEnable.Value = EnableMusicReplacer;
                configCategory.SaveToFile();
            }
            y += 25f;

            if (!EnableMusicReplacer) return y;

            // Volume
            GUI.Label(new Rect(startX, y, width, 18), $"Volume: {(int)(MasterVolume * 100)}%");
            y += 20f;
            float newVol = GUI.HorizontalSlider(new Rect(startX, y, width, 16), MasterVolume, 0f, 1f);
            if (Mathf.Abs(newVol - MasterVolume) > 0.01f)
            {
                MasterVolume = newVol;
                configVolume.Value = MasterVolume;
                configCategory.SaveToFile();
            }
            y += 24f;

            // Speed & Pitch (Slowed / Speed Up)
            GUI.Label(new Rect(startX, y, width, 18), $"Speed & Pitch: {PitchMultiplier:F2}x");
            y += 20f;
            float newPitch = GUI.HorizontalSlider(new Rect(startX, y, width, 16), PitchMultiplier, 0.5f, 2.0f);
            if (Mathf.Abs(newPitch - PitchMultiplier) > 0.01f)
            {
                PitchMultiplier = newPitch;
                configPitch.Value = PitchMultiplier;
                configCategory.SaveToFile();
                UpdateAllSourcesPitch();
            }
            y += 26f;

            // Mode Selector
            GUI.Label(new Rect(startX, y, width, 18), $"Mode: <b>{Mode}</b>");
            y += 20f;
            float btnWidth = (width - 10f) / 3f;
            if (GUI.Button(new Rect(startX, y, btnWidth, 22), "Selected")) { Mode = ReplaceMode.SelectedTrack; configMode.Value = (int)Mode; configCategory.SaveToFile(); }
            if (GUI.Button(new Rect(startX + btnWidth + 5f, y, btnWidth, 22), "By Name")) { Mode = ReplaceMode.ByName; configMode.Value = (int)Mode; configCategory.SaveToFile(); }
            if (GUI.Button(new Rect(startX + (btnWidth + 5f) * 2f, y, btnWidth, 22), "Shuffle")) { Mode = ReplaceMode.Shuffle; configMode.Value = (int)Mode; configCategory.SaveToFile(); }
            y += 30f;

            // Статус трека
            GUI.Label(new Rect(startX, y, width, 18), $"Playing: <i>{lastReplacedTrack}</i> | Loaded WAVs: {customClipNames.Count}");
            y += 24f;

            // Блок для списка песен
            float boxHeight = 200f;
            GUI.Box(new Rect(startX, y, width, boxHeight), "Available Custom Songs");
            Rect scrollOuterRect = new Rect(startX + 5, y + 22, width - 10, boxHeight - 28);
            float contentHeight = Mathf.Max(boxHeight - 28, customClipNames.Count * 24);
            Rect scrollContentRect = new Rect(0, 0, width - 28, contentHeight);

            scrollPosition = GUI.BeginScrollView(scrollOuterRect, scrollPosition, scrollContentRect);

            for (int i = 0; i < customClipNames.Count; i++)
            {
                string songName = customClipNames[i];
                int maxChars = Mathf.Max(15, (int)((width - 130) / 7.5f));
                string displayName = songName.Length > maxChars ? songName.Substring(0, maxChars - 3) + "..." : songName;

                float itemY = i * 24;
                bool isSelected = (songName == SelectedTrackName);

                GUI.Label(new Rect(5, itemY + 2, width - 130, 20), isSelected ? $"<b>> {displayName}</b>" : displayName);

                if (GUI.Button(new Rect(width - 115, itemY, 50, 20), "Select"))
                {
                    SelectedTrackName = songName;
                    configSelectedTrack.Value = SelectedTrackName;
                    configCategory.SaveToFile();
                }

                if (GUI.Button(new Rect(width - 60, itemY, 45, 20), "Play"))
                {
                    SelectedTrackName = songName;
                    configSelectedTrack.Value = SelectedTrackName;
                    configCategory.SaveToFile();
                    ForcePlaySong(songName);
                }
            }

            GUI.EndScrollView();
            y += boxHeight + 10f;

            if (GUI.Button(new Rect(startX, y, width, 24), "Rescan CustomMusic Folder"))
            {
                LoadAllCustomWavs();
            }
            y += 28f;

            return y;
        }

        private AudioClip LoadWavFromFile(string filePath, string clipName)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);

                int channels = System.BitConverter.ToInt16(fileBytes, 22);
                int frequency = System.BitConverter.ToInt32(fileBytes, 24);
                int bitsPerSample = System.BitConverter.ToInt16(fileBytes, 34);

                int pos = 12;
                while (pos < fileBytes.Length - 4)
                {
                    if (fileBytes[pos] == 'd' && fileBytes[pos + 1] == 'a' && fileBytes[pos + 2] == 't' && fileBytes[pos + 3] == 'a')
                    {
                        pos += 4;
                        break;
                    }
                    pos++;
                }

                int dataSize = System.BitConverter.ToInt32(fileBytes, pos);
                pos += 4;

                int totalSamples = dataSize / (bitsPerSample / 8);
                int samplesPerChannel = totalSamples / channels;

                float[] sampleData = new float[totalSamples];

                if (bitsPerSample == 16)
                {
                    for (int i = 0; i < totalSamples; i++)
                    {
                        short sample = System.BitConverter.ToInt16(fileBytes, pos + i * 2);
                        sampleData[i] = sample / 32768f;
                    }
                }
                else if (bitsPerSample == 8)
                {
                    for (int i = 0; i < totalSamples; i++)
                    {
                        byte sample = fileBytes[pos + i];
                        sampleData[i] = (sample - 128) / 128f;
                    }
                }
                else if (bitsPerSample == 32)
                {
                    for (int i = 0; i < totalSamples; i++)
                    {
                        sampleData[i] = System.BitConverter.ToSingle(fileBytes, pos + i * 4);
                    }
                }

                AudioClip audioClip = AudioClip.Create(clipName, samplesPerChannel, channels, frequency, false);
                audioClip.SetData(sampleData, 0);
                return audioClip;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[MusicReplacer] WAV load error ({clipName}): {ex.Message}");
                return null;
            }
        }

        private void UpdateAllSourcesPitch()
        {
            var sources = Resources.FindObjectsOfTypeAll<AudioSource>();
            foreach (var source in sources)
            {
                if (source != null && source.clip != null && (customClips.ContainsValue(source.clip) || source.clip.length >= 10.0f))
                {
                    source.pitch = PitchMultiplier;
                }
            }
        }
    }
}
