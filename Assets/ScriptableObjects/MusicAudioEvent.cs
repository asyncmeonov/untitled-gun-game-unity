using UnityEngine;

[CreateAssetMenu(fileName = "sfx_newMusicEvent", menuName = "Audio/New Music Event")]
public class MusicAudioEvent : AudioEvent
{
    #region config
    public AudioClip clip;

    public float lowPassFreq;
    public float highPassFreq;

    private AudioHighPassFilter _highPass;

    private AudioLowPassFilter _lowPass;
    #endregion

    public override GameObject Play(AudioSource audioSourceParam = null)
    {
        if (clip == null)
        {
            Debug.Log("Missing sound clips for " + this);
            return null;
        }

        var source = audioSourceParam;
        if (source == null)
        {
            var _obj = new GameObject("Music SoundSource", typeof(AudioSource));
            source = _obj.GetComponent<AudioSource>();
        }


        if (source.gameObject.GetComponent<AudioHighPassFilter>() == null)
        {
            source.gameObject.AddComponent<AudioHighPassFilter>();
        }

        if (source.gameObject.GetComponent<AudioLowPassFilter>() == null)
        {
            source.gameObject.AddComponent<AudioLowPassFilter>();
        }

        _highPass = source.gameObject.GetComponent<AudioHighPassFilter>();
        _lowPass = source.gameObject.GetComponent<AudioLowPassFilter>();


        _lowPass.cutoffFrequency = lowPassFreq;
        _highPass.cutoffFrequency = highPassFreq;
        source.clip = clip;
        source.loop = loop;
        source.outputAudioMixerGroup = mixGroup;
        source.volume = Random.Range(volume.x, volume.y);
        source.pitch = Random.Range(pitch.x, pitch.y);

        source.Play();

        //return configurations if we want to modify them externally
        return source.gameObject;
    }

    public void Stop(AudioSource audioSourceParam)
    {
        //Destroy after playing
        DestroyImmediate(audioSourceParam.gameObject);
    }
}
