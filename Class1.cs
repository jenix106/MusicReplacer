using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace MusicReplacer
{
    public class MusicModule : LevelModule
    {
        public string explanation = null;
        public Dictionary<string, string> waveIDs = new Dictionary<string, string>();
        public Dictionary<string, Vector2> loopTimes = new Dictionary<string, Vector2>();
        List<WaveSpawner> spawners = new List<WaveSpawner>();
        public override void Update()
        {
            base.Update();
            foreach (WaveSpawner spawner in WaveSpawner.instances)
            {
                if (spawner != null && !spawners.Contains(spawner))
                {
                    spawner.gameObject.AddComponent<MusicComponent>().Setup(waveIDs, loopTimes);
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
        AudioClip defaultClip;
        AudioClip defaultStep;
        Dictionary<string, string> waveIDs = new Dictionary<string, string>();
        Dictionary<string, Vector2> loopTimes = new Dictionary<string, Vector2>();
        Dictionary<AudioClip, Vector2> pairedTimes = new Dictionary<AudioClip, Vector2>();
        Dictionary<string, AudioClip> musicAddresses = new Dictionary<string, AudioClip>();
        public void Start()
        {
            spawner = GetComponent<WaveSpawner>();
            spawner.OnWaveBeginEvent.AddListener(Replace);
            spawner.OnWaveAnyEndEvent.AddListener(Return);
            foreach(string address in waveIDs.Keys)
            {
                Catalog.LoadAssetAsync<AudioClip>(waveIDs[address], value =>
                {
                    if (!musicAddresses.ContainsKey(address))
                        musicAddresses.Add(address, value);
                    if (!pairedTimes.ContainsKey(value) && loopTimes.ContainsKey(waveIDs[address]))
                        pairedTimes.Add(value, loopTimes[waveIDs[address]]);
                }, "MusicReplacer");
            }
        }
        public void Setup(Dictionary<string, string> waves = null, Dictionary<string, Vector2> times = null)
        {
            waveIDs = waves;
            loopTimes = times;
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
            foreach (AudioSource source in spawner.gameObject.GetComponents<AudioSource>())
            {
                if (source != null && source.outputAudioMixerGroup != null && source.outputAudioMixerGroup == GameManager.GetAudioMixerGroup(AudioMixerName.Music))
                {
                    audio = source;
                    defaultClip = source.clip;
                }
            }
            if(musicAddresses.ContainsKey("global"))
            {
                audio.clip = musicAddresses["global"];
            }
            if (musicAddresses.ContainsKey(spawner.waveData.id))
            {
                audio.clip = musicAddresses[spawner.waveData.id];
            }
            if(!musicAddresses.ContainsKey("global") && !musicAddresses.ContainsKey(spawner.waveData.id))
            {
                audio.clip = defaultClip;
            }
            audio.time = 0;
            audio.Play();
        }
        public void Return()
        {
            foreach (AudioSource source in spawner.gameObject.GetComponents<AudioSource>())
            {
                if (source != null && source.outputAudioMixerGroup != null && source.outputAudioMixerGroup == GameManager.GetAudioMixerGroup(AudioMixerName.UI))
                {
                    step = source;
                    defaultStep = source.clip;
                }
            }
            if (musicAddresses.ContainsKey("globalstep"))
            {
                step.clip = musicAddresses["globalstep"];
            }
            if (musicAddresses.ContainsKey(spawner.waveData.id + "step"))
            {
                step.clip = musicAddresses[spawner.waveData.id + "step"];
            }
            if (!musicAddresses.ContainsKey("globalstep") && !musicAddresses.ContainsKey(spawner.waveData.id + "step"))
            {
                step.clip = defaultStep;
            }
            step.Play();
        }
    }
}
