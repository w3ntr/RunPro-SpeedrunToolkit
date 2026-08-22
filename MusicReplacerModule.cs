using MelonLoader;
using MelonLoader.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SpeedrunToolkitMod
{
    public class MusicReplacerModule
    {
        public bool IsEnabled = true;

        private string musicFolder;
        private string listFilePath;
        private Dictionary<string, AudioClip> customClips = new Dictionary<string, AudioClip>();
        private List<string> customClipNames = new List<string>();
        private HashSet<string> discoveredTracks = new HashSet<string>();

        private MelonPreferences_Category configCategory;
        private MelonPreferences_Entry<KeyCode> configMenuKey;
        private MelonPreferences_Entry<float> configMasterVolume;

        private float timer = 0f;
        private bool showMenu = false;
        private bool isRebinding = false;
        private float masterVolume = 1.0f;
        private string lastReplacedTrack = "None";

        private Vector2 scrollPosition = Vector2.zero;
        private Rect windowRect = new Rect(30, 30, 320, 390);

        public void Init()
        {
            configCategory = MelonPreferences.CreateCategory("CustomMusicMod", "Custom Music Settings");
            configMenuKey = configCategory.CreateEntry("MenuKey", KeyCode.F7, "Toggle Menu Key");
            configMasterVolume = configCategory.CreateEntry("MasterVolume", 1.0f, "Master Music Volume");

            masterVolume = configMasterVolume.Value;

            musicFolder = Path.Combine(MelonEnvironment.UserDataDirectory, "CustomMusic");
            if (!Directory.Exists(musicFolder))
            {
                Directory.CreateDirectory(musicFolder);
            }

            listFilePath = Path.Combine(musicFolder, "music_tracks_list.txt");
            MelonLogger.Msg($"[CustomMusic] Mod initialized. Press {configMenuKey.Value} to open menu.");

            LoadAllCustomWavs();
        }

        public void LoadAllCustomWavs()
        {
            if (!Directory.Exists(musicFolder)) return;

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
                        if (!customClipNames.Contains(nameWithoutExt))
                        {
                            customClipNames.Add(nameWithoutExt);
                        }
                        MelonLogger.Msg($"[CustomMusic] Loaded track: {nameWithoutExt}");
                    }
                }
            }
        }

        public void Update()
        {
            if (!IsEnabled) return;

            if (isRebinding)
            {
                foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(key) && key != KeyCode.None)
                    {
                        configMenuKey.Value = key;
                        configCategory.SaveToFile();
                        isRebinding = false;
                        MelonLogger.Msg($"[CustomMusic] New menu key assigned: {key}");
                        break;
                    }
                }
                return;
            }

            if (Input.GetKeyDown(configMenuKey.Value))
            {
                showMenu = !showMenu;
            }

            if (showMenu)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            // Проверяем музыку 1 раз в секунду, чтобы не забивать шину и FPS
            timer += Time.deltaTime;
            if (timer >= 1.0f)
            {
                timer = 0f;
                DumpMusicTracks();
                EnforceCustomMusic();
            }
        }

        public void OnGUI()
        {
            if (!IsEnabled || !showMenu) return;

            GUI.Box(windowRect, "Custom Music Replacer (F7)");

            float startX = windowRect.x + 15;
            float startY = windowRect.y + 30;
            float width = windowRect.width - 30;

            GUI.Label(new Rect(startX, startY, width, 20), $"Volume: {(int)(masterVolume * 100)}%");
            float newVolume = GUI.HorizontalSlider(new Rect(startX, startY + 22, width, 20), masterVolume, 0.0f, 1.0f);

            if (Math.Abs(newVolume - masterVolume) > 0.01f)
            {
                masterVolume = newVolume;
                configMasterVolume.Value = masterVolume;
                configCategory.SaveToFile();
                UpdateLiveVolume();
            }

            GUI.Label(new Rect(startX, startY + 45, width, 20), $"Loaded WAVs: {customClips.Count}");
            GUI.Label(new Rect(startX, startY + 65, width, 20), $"Playing: {lastReplacedTrack}");

            GUI.Box(new Rect(startX, startY + 90, width, 155), "Available Custom Songs");

            Rect scrollOuterRect = new Rect(startX + 5, startY + 110, width - 10, 128);
            float contentHeight = Mathf.Max(128, customClipNames.Count * 26);
            Rect scrollContentRect = new Rect(0, 0, width - 28, contentHeight);

            scrollPosition = GUI.BeginScrollView(scrollOuterRect, scrollPosition, scrollContentRect);

            for (int i = 0; i < customClipNames.Count; i++)
            {
                string songName = customClipNames[i];
                string displayName = songName.Length > 18 ? songName.Substring(0, 15) + "..." : songName;

                float itemY = i * 26;
                GUI.Label(new Rect(5, itemY + 2, 170, 22), displayName);

                if (GUI.Button(new Rect(180, itemY, 65, 22), "Play"))
                {
                    ForcePlaySong(songName);
                }
            }

            GUI.EndScrollView();

            string keyBtnText = isRebinding ? "Press any key..." : $"Menu Key: [{configMenuKey.Value}]";
            if (GUI.Button(new Rect(startX, startY + 255, width, 28), keyBtnText))
            {
                isRebinding = true;
            }

            if (GUI.Button(new Rect(startX, startY + 290, width, 28), "Rescan Folder"))
            {
                LoadAllCustomWavs();
            }
        }

        private void ForcePlaySong(string songName)
        {
            if (!customClips.TryGetValue(songName, out AudioClip customClip)) return;

            var sources = Resources.FindObjectsOfTypeAll<AudioSource>();
            foreach (var source in sources)
            {
                if (source == null || source.clip == null) continue;

                if (source.clip.length >= 10.0f || IsCustomClip(source.clip))
                {
                    source.Stop();
                    source.clip = customClip;
                    source.volume = masterVolume;
                    source.Play();
                    lastReplacedTrack = songName;
                    MelonLogger.Msg($"[CustomMusic] Forced playback: '{songName}'");
                    break;
                }
            }
        }

        private bool IsCustomClip(AudioClip clip)
        {
            if (clip == null) return false;
            foreach (var custom in customClips.Values)
            {
                if (custom != null && custom.name == clip.name) return true;
            }
            return false;
        }

        private void UpdateLiveVolume()
        {
            var sources = Resources.FindObjectsOfTypeAll<AudioSource>();
            foreach (var source in sources)
            {
                if (source == null || source.clip == null) continue;
                if (IsCustomClip(source.clip))
                {
                    source.volume = masterVolume;
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

                if (clip.length >= 15.0f && !discoveredTracks.Contains(clip.name) && !IsCustomClip(clip))
                {
                    discoveredTracks.Add(clip.name);
                    newFound = true;

                    int minutes = (int)(clip.length / 60);
                    int seconds = (int)(clip.length % 60);
                    MelonLogger.Msg($"[CustomMusic] Discovered track: '{clip.name}' ({minutes}:{seconds:D2})");
                }
            }

            if (newFound)
            {
                List<string> lines = new List<string>
                {
                    "=== DISCOVERED GAME MUSIC TRACKS ===",
                    "Name your .wav files exactly as listed below:",
                    "------------------------------------------------"
                };

                foreach (var track in discoveredTracks)
                {
                    lines.Add(track);
                }

                File.WriteAllLines(listFilePath, lines.ToArray());
            }
        }

        private void EnforceCustomMusic()
        {
            if (customClips.Count == 0) return;

            var sources = Resources.FindObjectsOfTypeAll<AudioSource>();
            foreach (var source in sources)
            {
                if (source == null || source.clip == null) continue;

                string currentClipName = source.clip.name;

                if (IsCustomClip(source.clip))
                {
                    source.volume = masterVolume;
                    continue;
                }

                if (customClips.TryGetValue(currentClipName, out AudioClip customClip))
                {
                    bool wasPlaying = source.isPlaying;
                    source.Stop();
                    source.clip = customClip;
                    source.volume = masterVolume;
                    if (wasPlaying)
                    {
                        source.Play();
                    }
                    lastReplacedTrack = currentClipName;
                    MelonLogger.Msg($"[CustomMusic] Track replaced: '{currentClipName}'");
                }
            }
        }

        private AudioClip LoadWavFromFile(string filePath, string clipName)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);

                int channels = BitConverter.ToInt16(fileBytes, 22);
                int frequency = BitConverter.ToInt32(fileBytes, 24);
                int bitsPerSample = BitConverter.ToInt16(fileBytes, 34);

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

                int dataSize = BitConverter.ToInt32(fileBytes, pos);
                pos += 4;

                int totalSamples = dataSize / (bitsPerSample / 8);
                int samplesPerChannel = totalSamples / channels;

                float[] sampleData = new float[totalSamples];

                if (bitsPerSample == 16)
                {
                    for (int i = 0; i < totalSamples; i++)
                    {
                        short sample = BitConverter.ToInt16(fileBytes, pos + i * 2);
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
                        sampleData[i] = BitConverter.ToSingle(fileBytes, pos + i * 4);
                    }
                }

                AudioClip audioClip = AudioClip.Create(clipName, samplesPerChannel, channels, frequency, false);
                audioClip.SetData(sampleData, 0);
                return audioClip;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[CustomMusic] WAV load error ({clipName}): {ex.Message}");
                return null;
            }
        }
    }
}