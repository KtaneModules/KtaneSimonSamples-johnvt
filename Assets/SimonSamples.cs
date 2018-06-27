using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KmHelper;
using Rnd = UnityEngine.Random;

public class SimonSamples : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMSelectable Module;
    public KMAudio Audio;
    public KMSelectable[] Pads;
    public KMSelectable PlayButton;

    private int _moduleId;
    private static int _moduleIdCounter = 1;

    private string[] _sounds = { "HiHat", "Kick", "OpenHiHat", "Snare" };
    private KMAudio.KMAudioRef _audioRef;
    private bool _isPlaying;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        for (int i = 0; i < Pads.Length; i++)
        {
            var j = i;
            Pads[i].OnInteract += delegate () { PressButton(j); return false; };
        }

        PlayButton.OnInteract += delegate () { PressPlayButton(); return false; };

    }

    private void PressPlayButton()
    {
        StartCoroutine(PlayRhythm());
    }

    private IEnumerator PlayRhythm()
    {
        if (_isPlaying) yield break;
        _isPlaying = true;

        var startTime = (int)Bomb.GetTime();
        var time = startTime;
        while (time == startTime)
        {
            yield return null;
            time = (int)Bomb.GetTime();
        }
        PlaySound(0);
        _isPlaying = false;
    }

    private void PlaySound(int i)
    {
        if (_audioRef != null)
        {
            _audioRef.StopSound();
            _audioRef = null;
        }

        if (_sounds[i] == "OpenHiHat" && Audio.HandlePlaySoundAtTransformWithRef != null)
        {
            _audioRef = Audio.HandlePlaySoundAtTransformWithRef(_sounds[i], transform, false);
        }
        else
        {
            Audio.PlaySoundAtTransform(_sounds[i], transform);
        }
    }

    private void PressButton(int i)
    {
        PlaySound(i);
    }
}
