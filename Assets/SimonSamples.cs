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
    public KMSelectable PlayButton;
    public KMSelectable RecordButton;
    public KMSelectable[] Pads;

    private int _moduleId;
    private static int _moduleIdCounter = 1;

    private string[] _sounds = { "Kick", "Snare", "HiHat", "OpenHiHat" };
    private KMAudio.KMAudioRef _audioRef;
    private bool _isPlaying;
    private bool _isRecording;
    private Queue<int> _objective = new Queue<int>();
    private List<List<List<int>>> _possibleObjectives = new List<List<List<int>>>() {

        // First stage
        new List<List<int>>()
        {
            new List<int>("0212".Select(s => (int)(s)).ToArray()),
            new List<int>("0213".Select(s => (int)(s)).ToArray()),
            new List<int>("0012".Select(s => (int)(s)).ToArray()),
            new List<int>("0112".Select(s => (int)(s)).ToArray()),
        }
    };

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        PlayButton.OnInteract += delegate () { PressPlay(); return false; };
        RecordButton.OnInteract += delegate () { PressRecord(); return false; };

        for (int i = 0; i < Pads.Length; i++)
        {
            var j = i;
            Pads[i].OnInteract += delegate () { HitPad(j); return false; };
        }

        _objective = new Queue<int>(new[] { 0, 2, 1, 2, 0, 3, 1, 2 });
    }

    private void PressPlay()
    {
        GetComponent<KMSelectable>().AddInteractionPunch(.1f);

        if (_isPlaying || _isRecording) return;

        _isPlaying = true;
        SetLed(PlayButton, true);
        StartCoroutine(PlayRhythm(new Queue<int>(new [] { 0, 2, 1, 2, 0, 3, 1, 2 })));
    }

    private void PressRecord()
    {
        GetComponent<KMSelectable>().AddInteractionPunch(.1f);

        if (_isPlaying || _isRecording) return;

        _isRecording = true;
        SetLed(RecordButton, true);
    }

    private IEnumerator PlayRhythm(Queue<int> tones)
    {
        var startTime = Bomb.GetTime();
        while ((int)Bomb.GetTime() == (int)startTime) yield return null;
        startTime = Bomb.GetTime();
        var tonesPerTick = 2;

        var nextToneTime = startTime;
        while (tones.Count > 0)
        {
            PlayTone(tones.Dequeue());
            nextToneTime = nextToneTime - (1f / tonesPerTick);
            while (Bomb.GetTime() > nextToneTime)
            {
                yield return null;
            }
        }

        SetLed(PlayButton, false);
        _isPlaying = false;
        yield return null;
    }

    private void PlayTone(int i)
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

    private void HitPad(int i)
    {
        GetComponent<KMSelectable>().AddInteractionPunch();
        PlayTone(i);

        if (_isRecording)
        {
            if (i == _objective.Peek())
            {
                _objective.Dequeue();
                if (_objective.Count == 0)
                {
                    _isRecording = false;
                    SetLed(RecordButton, false);
                }
            }
            else
            {
                GetComponent<KMBombModule>().HandleStrike();
                _isRecording = false;
                SetLed(RecordButton, false);
                // to do: reset objective
            }
        }
    }

    private void SetLed(KMSelectable selectable, bool on)
    {
        selectable.transform.Find("LedOn").gameObject.SetActive(on);
        selectable.transform.Find("LedOff").gameObject.SetActive(!on);
    }
}
