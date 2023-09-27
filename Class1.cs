using System.Collections;
using System.Collections.Generic;
using System.IO;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace MusicReplacer
{
    public class MusicSettings
    {
        public string explanation { get; set; }
        public Dictionary<string, string> waveIDs { get; set; }
        public Dictionary<string, Vector2> loopTimes { get; set; }
    }
    public class MusicScript : ThunderScript
    {
        public Dictionary<string, string> waveIDs = new Dictionary<string, string>();
        public Dictionary<string, Vector2> loopTimes = new Dictionary<string, Vector2>();
        List<WaveSpawner> spawners = new List<WaveSpawner>();
        [ModOption(name: "Enable/Disable", tooltip: "Enables/disables the music replacer", valueSourceName: nameof(booleanValues), defaultValueIndex = 0, order = 0)]
        public static bool EnableMusic = true;
        public static ModOptionBool[] booleanValues =
        {
            new ModOptionBool("Enabled", true),
            new ModOptionBool("Disabled", false)
        };
        public override void ScriptEnable()
        {
            base.ScriptEnable();
            foreach (ModManager.ModData data in ModManager.loadedMods)
            {
                if (File.Exists(data.fullPath + "/MusicReplacerSettings.json"))
                {
                    MusicSettings settings = JsonConvert.DeserializeObject<MusicSettings>(File.ReadAllText(data.fullPath + "/MusicReplacerSettings.json"), Catalog.jsonSerializerSettings);
                    if (!settings.waveIDs.IsNullOrEmpty())
                        foreach (string key in settings.waveIDs.Keys)
                        {
                            if (!waveIDs.ContainsKey(key)) waveIDs.Add(key, settings.waveIDs[key]);
                        }
                    if (!settings.loopTimes.IsNullOrEmpty())
                        foreach (string loop in settings.loopTimes.Keys)
                        {
                            if (!loopTimes.ContainsKey(loop)) loopTimes.Add(loop, settings.loopTimes[loop]);
                        }
                }
            }
        }
        public override void ScriptUpdate()
        {
            base.ScriptUpdate();
            foreach (WaveSpawner spawner in WaveSpawner.instances)
            {
                if (spawner != null && !spawners.Contains(spawner))
                {
                    spawner.gameObject.AddComponent<MusicComponent>().Setup(waveIDs, loopTimes, ModData);
                    spawners.Add(spawner);
                }
            }
        }
    }
    public class MusicComponent : MonoBehaviour
    {
        WaveSpawner spawner;
        AudioSource audio;
        AudioSource step;
        Dictionary<string, string> waveIDs = new Dictionary<string, string>();
        Dictionary<string, Vector2> loopTimes = new Dictionary<string, Vector2>();
        Dictionary<AudioClip, Vector2> pairedTimes = new Dictionary<AudioClip, Vector2>();
        Dictionary<string, AudioClip> musicAddresses = new Dictionary<string, AudioClip>();
        ModManager.ModData modData;
        public void Start()
        {
            spawner = GetComponent<WaveSpawner>();
            spawner.OnWaveBeginEvent.AddListener(Replace);
            spawner.OnWaveAnyEndEvent.AddListener(Return);
            foreach (string address in waveIDs.Keys)
            {
                Catalog.LoadAssetAsync<AudioClip>(waveIDs[address], value =>
                {
                    if (value != null)
                    {
                        if (!musicAddresses.ContainsKey(address))
                            musicAddresses.Add(address, value);
                        if (!pairedTimes.ContainsKey(value) && loopTimes.ContainsKey(waveIDs[address]))
                            pairedTimes.Add(value, loopTimes[waveIDs[address]]);
                    }
                }, "MusicReplacer");
            }
            audio = spawner.gameObject.AddComponent<AudioSource>();
            audio.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Music);
            audio.loop = true;
            step = spawner.gameObject.AddComponent<AudioSource>();
            step.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.UI);
            step.loop = false;
            StartCoroutine(GetAudioClips());
        }
        public IEnumerator GetAudioClips()
        {
            foreach (WaveData data in Catalog.GetDataList(Category.Wave))
            {
                string address = data.id;
                using (UnityWebRequest clip = UnityWebRequestMultimedia.GetAudioClip(("file:///" + modData.fullPath + "/Songs/" + address + ".mp3").Replace(" ", "%20"), AudioType.MPEG))
                {
                    yield return clip.SendWebRequest();
                    if (clip.error == null)
                    {
                        AudioClip value = DownloadHandlerAudioClip.GetContent(clip);
                        if (!musicAddresses.ContainsKey(address))
                            musicAddresses.Add(address, value);
                        if (!pairedTimes.ContainsKey(value) && loopTimes.ContainsKey(address))
                            pairedTimes.Add(value, loopTimes[address]);
                    }
                }
            }
            using (UnityWebRequest clip = UnityWebRequestMultimedia.GetAudioClip(("file:///" + modData.fullPath + "/Songs/global.mp3").Replace(" ", "%20"), AudioType.MPEG))
            {
                yield return clip.SendWebRequest();
                if (clip.error == null)
                {
                    AudioClip value = DownloadHandlerAudioClip.GetContent(clip);
                    if (!musicAddresses.ContainsKey("global"))
                        musicAddresses.Add("global", value);
                    if (!pairedTimes.ContainsKey(value) && loopTimes.ContainsKey("global"))
                        pairedTimes.Add(value, loopTimes["global"]);
                }
            }
        }
        public void Setup(Dictionary<string, string> waves = null, Dictionary<string, Vector2> times = null, ModManager.ModData data = null)
        {
            waveIDs = waves;
            loopTimes = times;
            modData = data;
        }
        public void Update()
        {
            if (audio?.clip != null && audio.isPlaying && pairedTimes.ContainsKey(audio.clip) && audio.time >= pairedTimes[audio.clip].y)
            {
                audio.time = pairedTimes[audio.clip].x;
            }
        }
        public void Replace()
        {
            ThunderBehaviourSingleton<MusicManager>.Instance.Volume = 1;
            if (musicAddresses.ContainsKey("global") && MusicScript.EnableMusic)
            {
                ThunderBehaviourSingleton<MusicManager>.Instance.Volume = 0;
                audio.clip = musicAddresses["global"];
                audio.time = 0;
                audio.Play();
            }
            else if (musicAddresses.ContainsKey(spawner.waveData.id) && MusicScript.EnableMusic)
            {
                ThunderBehaviourSingleton<MusicManager>.Instance.Volume = 0;
                audio.clip = musicAddresses[spawner.waveData.id];
                audio.time = 0;
                audio.Play();
            }
        }
        public void Return()
        {
            if (audio?.clip && audio.isPlaying)
                StartCoroutine(Utils.FadeOut(audio, 2));
            if (musicAddresses.ContainsKey("globalstep") && audio.isPlaying)
            {
                step.clip = musicAddresses["globalstep"];
                step.time = 0;
                step.Play();
            }
            if (musicAddresses.ContainsKey(spawner.waveData.id + "step") && audio.isPlaying)
            {
                step.clip = musicAddresses[spawner.waveData.id + "step"];
                step.time = 0;
                step.Play();
            }
        }
    }
}
