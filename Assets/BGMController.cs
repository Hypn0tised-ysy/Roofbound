using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMController : MonoBehaviour
{
    public AudioSource audioSource;

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    /// <summary>停止当前 BGM</summary>
    public void StopBGM()
    {
        if (audioSource != null)
            audioSource.Stop();
    }

    /// <summary>立即切换为指定的新 Clip，并继续循环播放</summary>
    public void SwitchClip(AudioClip newClip)
    {
        if (audioSource == null || newClip == null)
            return;
        audioSource.clip = newClip;
        audioSource.Play();
    }

    /// <summary>播放一次指定 Clip，不循环</summary>
    public void PlayOnce(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;
        audioSource.clip = clip;
        audioSource.loop = false;
        audioSource.Play();
    }
}