using Il2Cpp;
using Il2CppInterop.Runtime;
using MelonLoader;
using Harmony;
using MelonLoader.Utils;
using NLayer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public class MusicReplacerModule
    {
        public enum ReplaceMode
        {
            ByName = 0,
            SelectedTrack = 1,
            Shuffle = 2
        }

        public bool EnableMusicReplacer = true;
        public ReplaceMode Mode = ReplaceMode.SelectedTrack;
        public static MusicReplacerModule Instance;
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
        private float dumpTimer = 0f;
        private string lastReplacedTrack = "None";
        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 outerScrollPosition = Vector2.zero;

        public void Init()
        {
            configCategory = MelonPreferences.CreateCategory("MusicReplacerMod", "Custom Music Settings");
            configEnable = configCategory.CreateEntry("EnableMusicReplacer", true, "Enable Custom Music");
            configMode = configCategory.CreateEntry("ReplaceMode", (int)ReplaceMode.SelectedTrack, "Replace Mode");
            configVolume = configCategory.CreateEntry("MasterVolume", 1.0f, "Music Volume");
            configSelectedTrack = configCategory.CreateEntry("SelectedTrackName", "", "Selected Track Name");
            configPitch = configCategory.CreateEntry("PitchMultiplier", 1.0f, "Music Speed & Pitch");

            EnableMusicReplacer = configEnable.Value;
            Mode = (ReplaceMode)configMode.Value;
            MasterVolume = configVolume.Value;
            PitchMultiplier = configPitch.Value;
            SelectedTrackName = configSelectedTrack.Value;
            Instance = this;

            musicFolder = Path.Combine(MelonEnvironment.UserDataDirectory, "CustomMusic");
            if (!Directory.Exists(musicFolder))
            {
                Directory.CreateDirectory(musicFolder);
            }

            listFilePath = Path.Combine(musicFolder, "music_tracks_list.txt");

            // Загружаем все треки при старте
            LoadAllCustomMusic();

            MelonLogger.Msg("[MusicReplacerModule] Initialized successfully with MP3 support!");
        }

        public void OnUpdate()
        {
            if (!EnableMusicReplacer) return;

            float dt = Time.deltaTime;

            checkTimer += dt;
            if (checkTimer >= 0.25f)
            {
                checkTimer = 0f;
                EnforceCustomMusic();
            }

            dumpTimer += dt;
            if (dumpTimer >= 3.0f)
            {
                dumpTimer = 0f;
                DumpMusicTracks();
            }
        }
        public void Update()
        {
            if (!EnableMusicReplacer || string.IsNullOrEmpty(lastReplacedTrack)) return;

            foreach (var radioNowPlaying in UnityEngine.Object.FindObjectsOfType<RadioNowPlaying>())
            {
                if (radioNowPlaying != null && radioNowPlaying.gameObject.activeInHierarchy && radioNowPlaying.musicName != null)
                {
                    if (radioNowPlaying.musicName.text != lastReplacedTrack)
                    {
                        radioNowPlaying.musicName.text = lastReplacedTrack;
                        radioNowPlaying.musicName.resizeTextForBestFit = true;
                        radioNowPlaying.musicName.resizeTextMinSize = 10;
                        radioNowPlaying.musicName.resizeTextMaxSize = 24;
                    }
                }
            }
        }

        public void LoadAllCustomMusic()
        {
            string[] mp3Files = Directory.GetFiles(musicFolder, "*.mp3");
            string[] wavFiles = Directory.GetFiles(musicFolder, "*.wav");

            foreach (var filePath in mp3Files)
            {
                string clipName = Path.GetFileNameWithoutExtension(filePath);
                if (!customClips.ContainsKey(clipName))
                {
                    AudioClip clip = LoadMp3File(filePath, clipName);
                    if (clip != null)
                    {
                        customClips[clipName] = clip;
                        if (!customClipNames.Contains(clipName)) customClipNames.Add(clipName);
                        MelonLogger.Msg($"[MusicReplacer] Loaded MP3: {clipName}");
                    }
                }
            }

            foreach (var filePath in wavFiles)
            {
                string clipName = Path.GetFileNameWithoutExtension(filePath);
                if (!customClips.ContainsKey(clipName))
                {
                    AudioClip clip = LoadWavFile(filePath, clipName);
                    if (clip != null)
                    {
                        customClips[clipName] = clip;
                        if (!customClipNames.Contains(clipName)) customClipNames.Add(clipName);
                        MelonLogger.Msg($"[MusicReplacer] Loaded WAV: {clipName}");
                    }
                }
            }

            if (string.IsNullOrEmpty(SelectedTrackName) && customClipNames.Count > 0)
            {
                SelectedTrackName = customClipNames[0];
            }
        }

        // Декодер MP3 через NLayer в PCM-сэмплы Unity
        private AudioClip LoadMp3File(string filePath, string clipName)
        {
            try
            {
                using (var mpegFile = new MpegFile(filePath))
                {
                    int sampleRate = mpegFile.SampleRate;
                    int channels = mpegFile.Channels;

                    List<float> sampleList = new List<float>();
                    float[] readBuffer = new float[1024 * 16];
                    int readCount;

                    while ((readCount = mpegFile.ReadSamples(readBuffer, 0, readBuffer.Length)) > 0)
                    {
                        for (int i = 0; i < readCount; i++)
                        {
                            sampleList.Add(readBuffer[i]);
                        }
                    }

                    if (sampleList.Count == 0) return null;

                    float[] samples = sampleList.ToArray();
                    int totalFrames = samples.Length / channels;

                    AudioClip clip = AudioClip.Create(clipName, totalFrames, channels, sampleRate, false);
                    clip.SetData(samples, 0);
                    clip.hideFlags = HideFlags.DontUnloadUnusedAsset;

                    return clip;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MusicReplacer] Error reading MP3 '{clipName}': {ex.Message}");
                return null;
            }
        }

        // Простой считыватель WAV 16-bit
        private AudioClip LoadWavFile(string filePath, string clipName)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                int channels = fileBytes[22];
                int sampleRate = BitConverter.ToInt32(fileBytes, 24);
                int pos = 12;

                while (pos < fileBytes.Length - 8)
                {
                    if (fileBytes[pos] == 'd' && fileBytes[pos + 1] == 'a' && fileBytes[pos + 2] == 't' && fileBytes[pos + 3] == 'a')
                    {
                        pos += 4;
                        break;
                    }
                    pos++;
                }

                int dataSize = BitConverter.ToInt32(fileBytes, pos);
                pos += 4;

                int sampleCount = dataSize / 2;
                float[] samples = new float[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    short sample16 = BitConverter.ToInt16(fileBytes, pos + i * 2);
                    samples[i] = sample16 / 32768.0f;
                }

                int totalFrames = sampleCount / channels;
                AudioClip clip = AudioClip.Create(clipName, totalFrames, channels, sampleRate, false);
                clip.SetData(samples, 0);
                clip.hideFlags = HideFlags.DontUnloadUnusedAsset;

                return clip;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MusicReplacer] Error reading WAV '{clipName}': {ex.Message}");
                return null;
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

                        // Вызываем обновление плашки сразу при автоматической замене трека
                        UpdateInGameHUDTrackName(targetClip.name);
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
                int randomIndex = UnityEngine.Random.Range(0, customClipNames.Count);
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

                    // Перенесли вызов внутрь условия успешного воспроизведения
                    UpdateInGameHUDTrackName(songName);
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
                    "Name your .mp3 / .wav files as listed below (if using ByName mode. THATS OLD CODE, DON'T CARE ABOUT THAT. Name your .mp3 how u want, it's doesn't matter!):",
                    "------------------------------------------------"
                };

                foreach (var track in discoveredTracks)
                {
                    lines.Add(track);
                }

                File.WriteAllLines(listFilePath, lines.ToArray());
            }
        }

        private void UpdateInGameHUDTrackName(string newSongName)
        {
            try
            {
                // 1. Меняем название прямо в памяти игры (чтобы игра сама использовала твое имя)
                if (RadioSystem.i != null && RadioSystem.i.listSounds != null)
                {
                    int activeId = RadioSystem.i.activeMusicID;
                    if (activeId >= 0 && activeId < RadioSystem.i.listSounds.Length)
                    {
                        RadioSystem.i.listSounds[activeId].soundName = newSongName;
                    }
                }

                // 2. Ищем саму плашку "Now Playing" по скрипту RadioNowPlaying
                foreach (var radioNowPlaying in UnityEngine.Object.FindObjectsOfType<RadioNowPlaying>())
                {
                    if (radioNowPlaying != null && radioNowPlaying.musicName != null)
                    {
                        radioNowPlaying.musicName.text = newSongName;
                        radioNowPlaying.musicName.resizeTextForBestFit = true;
                        radioNowPlaying.musicName.resizeTextMinSize = 10;
                        radioNowPlaying.musicName.resizeTextMaxSize = 24;
                    }
                }

                // 3. Ищем радио-холст RadioCanvas и форсируем обновление
                foreach (var radioCanvas in UnityEngine.Object.FindObjectsOfType<RadioCanvas>())
                {
                    if (radioCanvas != null)
                    {
                        if (radioCanvas.nowPlayingText != null)
                        {
                            radioCanvas.nowPlayingText.text = newSongName;
                            radioCanvas.nowPlayingText.resizeTextForBestFit = true;
                            radioCanvas.nowPlayingText.resizeTextMinSize = 10;
                            radioCanvas.nowPlayingText.resizeTextMaxSize = 24;
                        }
                        // Просим саму игру перезапустить логику вывода имени
                        radioCanvas.NowPlayingName();
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"[MusicReplacer] Could not update RadioNowPlaying text: {ex.Message}");
            }
        }



        public float DrawUI(float startX, float startY, float width)
        {
            // Фиксируем общую высоту окна на 310f, чтобы не залезать на нижнюю панель
            outerScrollPosition = GUI.BeginScrollView(
                new Rect(startX, startY, width, 310f),
                outerScrollPosition,
                new Rect(0, 0, width - 25f, 520f)
            );

            float y = 5f;
            float contentWidth = width - 25f;

            GUI.Label(new Rect(0, y, contentWidth, 20), "<b>Custom Music Replacer Settings:</b>");
            y += 24f;

            bool newEnable = GUI.Toggle(new Rect(0, y, contentWidth, 20), EnableMusicReplacer, " Enable Music Replacer");
            if (newEnable != EnableMusicReplacer)
            {
                EnableMusicReplacer = newEnable;
                configEnable.Value = EnableMusicReplacer;
                configCategory.SaveToFile();
            }
            y += 25f;

            if (!EnableMusicReplacer)
            {
                GUI.EndScrollView();
                return 310f;
            }

            GUI.Label(new Rect(0, y, contentWidth, 18), $"Volume: {(int)(MasterVolume * 100)}%");
            y += 20f;
            float newVol = GUI.HorizontalSlider(new Rect(0, y, contentWidth, 16), MasterVolume, 0f, 1f);
            if (Mathf.Abs(newVol - MasterVolume) > 0.01f)
            {
                MasterVolume = newVol;
                configVolume.Value = MasterVolume;
                configCategory.SaveToFile();
            }
            y += 24f;

            GUI.Label(new Rect(0, y, contentWidth, 18), $"Speed & Pitch: {PitchMultiplier:F2}x");
            y += 20f;
            float newPitch = GUI.HorizontalSlider(new Rect(0, y, contentWidth, 16), PitchMultiplier, 0.5f, 2.0f);
            if (Mathf.Abs(newPitch - PitchMultiplier) > 0.01f)
            {
                PitchMultiplier = newPitch;
                configPitch.Value = PitchMultiplier;
                configCategory.SaveToFile();
                UpdateAllSourcesPitch();
            }
            y += 26f;

            GUI.Label(new Rect(0, y, contentWidth, 18), $"Mode: <b>{Mode}</b>");
            y += 20f;
            float btnWidth = (contentWidth - 10f) / 3f;
            if (GUI.Button(new Rect(0, y, btnWidth, 22), "Selected")) { Mode = ReplaceMode.SelectedTrack; configMode.Value = (int)Mode; configCategory.SaveToFile(); }
            if (GUI.Button(new Rect(btnWidth + 5f, y, btnWidth, 22), "By Name")) { Mode = ReplaceMode.ByName; configMode.Value = (int)Mode; configCategory.SaveToFile(); }
            if (GUI.Button(new Rect((btnWidth + 5f) * 2f, y, btnWidth, 22), "Shuffle")) { Mode = ReplaceMode.Shuffle; configMode.Value = (int)Mode; configCategory.SaveToFile(); }
            y += 30f;

            GUI.Label(new Rect(0, y, contentWidth, 18), $"Playing: <i>{lastReplacedTrack}</i> | Loaded Tracks: {customClipNames.Count}");
            y += 24f;

            // Внутренний скролл-бокс для списка файлов
            float boxHeight = 180f;
            GUI.Box(new Rect(0, y, contentWidth, boxHeight), "Available Custom Songs (.mp3 / .wav)");
            Rect scrollOuterRect = new Rect(5, y + 22, contentWidth - 10, boxHeight - 28);
            float innerHeight = Mathf.Max(boxHeight - 28, customClipNames.Count * 24);
            Rect scrollContentRect = new Rect(0, 0, contentWidth - 28, innerHeight);

            scrollPosition = GUI.BeginScrollView(scrollOuterRect, scrollPosition, scrollContentRect);

            for (int i = 0; i < customClipNames.Count; i++)
            {
                string songName = customClipNames[i];
                int maxChars = Mathf.Max(12, (int)((contentWidth - 130) / 7.5f));
                string displayName = songName.Length > maxChars ? songName.Substring(0, maxChars - 3) + "..." : songName;

                float itemY = i * 24;
                bool isSelected = (songName == SelectedTrackName);

                GUI.Label(new Rect(5, itemY + 2, contentWidth - 130, 20), isSelected ? $"<b>> {displayName}</b>" : displayName);

                if (GUI.Button(new Rect(contentWidth - 115, itemY, 50, 20), "Select"))
                {
                    SelectedTrackName = songName;
                    configSelectedTrack.Value = SelectedTrackName;
                    configCategory.SaveToFile();
                }

                if (GUI.Button(new Rect(contentWidth - 60, itemY, 45, 20), "Play"))
                {
                    SelectedTrackName = songName;
                    configSelectedTrack.Value = SelectedTrackName;
                    configCategory.SaveToFile();
                    ForcePlaySong(songName);
                }
            }

            GUI.EndScrollView();
            y += boxHeight + 10f;

            if (GUI.Button(new Rect(0, y, contentWidth, 24), "Rescan CustomMusic Folder"))
            {
                LoadAllCustomMusic();
            }
            y += 30f;

            GUI.EndScrollView();
            return 310f;
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