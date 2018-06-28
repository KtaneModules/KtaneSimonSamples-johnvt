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
    private bool _isSolved;
    private int _currentStage = 0;
    private int _lastStage = 2;
    private List<string> _calls = new List<string>();
    private List<string> _expectedResponses = new List<string>();
    private int _cursor;
    private List<List<string>> _possibleCalls = new List<List<string>>()
    {
        // Stage 1
        new List<string>()
        {
            "0212",
            "0213",
            "0012",
            "0112"
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

        // Determine calls and expected responses
        for (var stage = 0; stage < 3; stage++)
        {
            // Start with previous stage
            string call = "";
            if (stage > 0) call = _calls[stage - 1];

            // Glue together two random (different) parts
            /*int part1 = Rnd.Range(0, _possibleCalls[0].Count), part2;
            do part2 = Rnd.Range(0, _possibleCalls[0].Count);
            while (part1 == part2);
            call += _possibleCalls[0][part1] + _possibleCalls[0][part2];*/
            call += _possibleCalls[0][Rnd.Range(0, _possibleCalls[0].Count)];
            _calls.Add(call);
            Debug.Log(call);

            // For now, no rules, just respond with the same as the call
            _expectedResponses.Add(call);
        }
    }

    private void PressPlay()
    {
        GetComponent<KMSelectable>().AddInteractionPunch(.1f);

        if (_isPlaying || _isRecording) return;

        _isPlaying = true;
        SetLed(PlayButton, true);
        StartCoroutine(PlayTones(_calls[_currentStage]));
    }

    private void PressRecord()
    {
        GetComponent<KMSelectable>().AddInteractionPunch(.1f);

        if (_isPlaying || _isRecording) return;

        _isRecording = true;
        _cursor = 0;
        SetLed(RecordButton, true);
    }

    private IEnumerator PlayTones(string tones)
    {
        var startTime = Bomb.GetTime();
        while ((int)Bomb.GetTime() == (int)startTime) yield return null;
        startTime = Bomb.GetTime();
        var tonesPerTick = 2;

        var nextToneTime = startTime;
        while (tones.Length > 0)
        {
            PlayTone(int.Parse(tones[0].ToString()));
            tones = tones.Remove(0, 1);
            nextToneTime = nextToneTime - (1f / tonesPerTick);
            while (Bomb.GetTime() > nextToneTime) yield return null;
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
            if (i == int.Parse(_expectedResponses[_currentStage][_cursor].ToString()))
            {
                if (_cursor == _expectedResponses[_currentStage].Length - 1)
                {
                    _isRecording = false;
                    SetLed(RecordButton, false);
                    if (_currentStage == _lastStage)
                    {
                        GetComponent<KMBombModule>().HandlePass();
                        _isSolved = true;
                    }
                    _currentStage++;
                }
                _cursor++;
            }
            else
            {
                GetComponent<KMBombModule>().HandleStrike();
                _isRecording = false;
                SetLed(RecordButton, false);
            }
        }
    }

    private void SetLed(KMSelectable selectable, bool on)
    {
        selectable.transform.Find("LedOn").gameObject.SetActive(on);
        selectable.transform.Find("LedOff").gameObject.SetActive(!on);
    }
}
