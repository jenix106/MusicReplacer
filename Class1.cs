using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace MusicReplacer
{
    public class MusicModule : LevelModule
    {
        public Dictionary<string, string> waveIDs = new Dictionary<string, string>();
        List<WaveSpawner> spawners = new List<WaveSpawner>();
        public override void Update()
        {
            base.Update();
            foreach (WaveSpawner spawner in WaveSpawner.instances)
            {
                if (spawner != null && !spawners.Contains(spawner))
                {
                    spawner.gameObject.AddComponent<MusicComponent>().Setup(waveIDs);
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
                    if(!musicAddresses.ContainsKey(address))
                    musicAddresses.Add(address, value);
                }, "MusicReplacer");
            }
        }
        public void Setup(Dictionary<string, string> waves = null)
        {
            waveIDs = waves;
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
