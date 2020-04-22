using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class CustomFunctions : MonoBehaviour
{
    public static CustomFunctions instance;
    static GameObject soundHolder;

    private void Awake()
    {
        instance = this;
    }

    public void ScreenShake()
    {
        GetComponent<CinemachineImpulseSource>().GenerateImpulse();
    }

    public static void HitPause(bool includeScreenShake)
    {
        if (Time.timeScale == 1f)
            instance.StartCoroutine(instance.HitPauseEffect(includeScreenShake));
    }

    public IEnumerator HitPauseEffect(bool includeSreenShake)
    {
        if (includeSreenShake)
            ScreenShake();
        Time.timeScale = 0.1f;
        yield return new WaitForSecondsRealtime(0.1f);
        Time.timeScale = 1f;
    }

    public static void PlaySound(AudioClip soundToPlay, float volume = 0.5f)
    {
        if (soundHolder == null)
        {
            soundHolder = GameObject.Instantiate(new GameObject());
            soundHolder.name = "Sound Holder";
        }

        AudioSource audio;
        audio = soundHolder.AddComponent<AudioSource>();
        audio.volume = volume;
        audio.clip = soundToPlay;
        audio.Play();
        Destroy(audio, soundToPlay.length + 0.2f);
    }

    public static void VibrateController()
    {
        if (Gamepad.current != null)
            instance.StartCoroutine(instance.Vibration());
    }

    public IEnumerator Vibration()
    {
        Gamepad.current.SetMotorSpeeds(.4f, .8f);
        yield return new WaitForSecondsRealtime(0.1f);
        Gamepad.current.SetMotorSpeeds(0f, 0f);
    }

}
