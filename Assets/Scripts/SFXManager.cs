using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("可拖入多个碰撞声，随机播放")]
    public AudioClip[] crashClips;

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioSource = GetComponent<AudioSource>();
    }

    public void PlayCrashSound()
    {
        if (crashClips.Length == 0 || audioSource == null) return;

        // 随机选一个声音，避免重复感
        AudioClip clip = crashClips[Random.Range(0, crashClips.Length)];
        audioSource.PlayOneShot(clip);
    }
}
