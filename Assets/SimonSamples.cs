using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KmHelper;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

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
    private string[] _soundLetters = { "K", "S", "H", "O" };
    private List<int> _pads = new List<int>() { 0, 1, 2, 3 };
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
            "0012",
            "0112",
            "0212",
            "0213",
        },

        // Stage 2
        new List<string>()
        {
            "0011",
            "0211",
            "0312",
            "0313",
        },

        // Stage 3
        new List<string>()
        {
            "0011",
            "1010",
            "1221",
            "0232",
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

        // Random pad location
        _pads.Shuffle();

        // Determine calls and expected responses
        for (var stage = 0; stage < 3; stage++)
        {
            // Start with previous stage
            string call = "", newPart = "", response = "";
            if (stage > 0)
            {
                call = _calls[stage - 1];
                response = _expectedResponses[stage - 1];
            }

            // Add new part
            newPart = _possibleCalls[stage][Rnd.Range(0, _possibleCalls[0].Count)];
            call += newPart;
            response += ApplyRules(call, newPart, stage);
            _calls.Add(call);
            _expectedResponses.Add(response);

            Debug.LogFormat("[Simon Samples #{0}] Stage {1}. Call:{2}. Expected response:{3}.",
                _moduleId,
                (stage + 1).ToString(),
                Regex.Replace(String.Join("", call.Select(x => _soundLetters[int.Parse(x.ToString())]).ToArray()), ".{4}", " $0"),
                Regex.Replace(String.Join("", response.Select(x => _soundLetters[int.Parse(x.ToString())]).ToArray()), ".{4}", " $0")
            );
        }
    }

    /**
     * X: Add up all digits in the serial number and modulo 10.
     * 0 Kick
     * 1 Snare
     * 2 Hi-Hat
     * 3 Open Hi-Hat
     */
    private string ApplyRules(string call, string newPart, int stage)
    {
        var c = call.Select(i => int.Parse(i.ToString())).ToList();
        var p = newPart.Select(i => int.Parse(i.ToString())).ToList();
        int x = Bomb.GetSerialNumberNumbers().Sum() % 10;

        // Stage 1
        if (stage == 0)
        {
            // If x is smaller than 5, make the second sound S, or O if it already is. Otherwise, replace H's with O's and vice versa.
            if (x < 5)
                if (c[1] != 1) p[1] = 1; else p[1] = 3;
            else
                p = p
                    .Select(s => (s == 2 ? -1 : s))
                    .Select(s => (s == 3 ? 2 : s))
                    .Select(s => (s == -1 ? 3 : s))
                    .ToList();
        }

        // Stage 2
        else if (stage == 1)
        {
            // If there are one or more O's, swap the first two sounds with the second two. Otherwise, reverse the order of the sounds.
            if (c.Count(s => s == 3) > 0)
                p = new List<int>() { p[2], p[3], p[0], p[1] };
            else
                p = new List<int>() { p[3], p[2], p[1], p[0] };
        }

        // Stage 3
        else if (stage == 2)
        {
            // If the number of H's is 3 or more, make the first sound a O. Otherwise, replace K's with S's and vice versa.
            if (c.Count(s => s == 2) >= 3)
                p[0] = 3;
            else
                p = p
                    .Select(s => (s == 0 ? -1 : s))
                    .Select(s => (s == 1 ? 0 : s))
                    .Select(s => (s == -1 ? 1 : s))
                    .ToList();
        }

        return String.Join("", p.Select(i => i.ToString()).ToArray());
    }

    private void PressPlay()
    {
        GetComponent<KMSelectable>().AddInteractionPunch(.2f);

        if (_isPlaying || _isRecording || _isSolved) return;

        StartCoroutine(PlaySounds(_calls[_currentStage]));
    }

    private void PressRecord()
    {
        GetComponent<KMSelectable>().AddInteractionPunch(.2f);

        if (_isPlaying || _isRecording || _isSolved) return;

        _isRecording = true;
        _cursor = 0;
        SetLed(RecordButton, true);
    }

    private IEnumerator PlaySounds(string sounds, float wait = 0f)
    {
        _isPlaying = true;
        SetLed(PlayButton, true);

        yield return new WaitForSeconds(wait);

        // Sync with bomb timer
        var startTime = Bomb.GetTime();
        var numSolved = Bomb.GetSolvedModuleNames().Count;
        while (true)
        {
            // Compare the number of seconds left
            while ((int)Bomb.GetTime() == (int)startTime) yield return null;

            // Same number of solved? We're synced
            if (numSolved == Bomb.GetSolvedModuleNames().Count) break;

            // Something got solved in between! Keep waiting
            startTime = Bomb.GetTime();
            numSolved = Bomb.GetSolvedModuleNames().Count;
        }

        var soundsPerTick = 2f;
        while (sounds.Length > 0)
        {
            PlaySound(int.Parse(sounds[0].ToString()));
            sounds = sounds.Remove(0, 1);
            var speed = 1f;
            int strikes = Bomb.GetStrikes();

            // Keep (almost) synced with bomb timer, which depends on number of strikes
            if (strikes == 1) speed /= 1.25f;
            else if (strikes == 2) speed /= 1.5f;
            else if (strikes == 3) speed /= 1.75f;
            else if (strikes > 3) speed /= 2f;

            yield return new WaitForSeconds(1f / soundsPerTick * speed);
        }

        SetLed(PlayButton, false);
        _isPlaying = false;
        yield return null;
    }

    private void PlaySound(int i)
    {
        if (_audioRef != null && _sounds[i] == "HiHat")
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
        if (_isSolved) return;

        GetComponent<KMSelectable>().AddInteractionPunch(.5f);
        PlaySound(_pads[i]);

        if (_isRecording)
        {
            if (_pads[i] == int.Parse(_expectedResponses[_currentStage][_cursor].ToString()))
            {
                if (_cursor == _expectedResponses[_currentStage].Length - 1)
                {
                    _isRecording = false;
                    SetLed(RecordButton, false);
                    if (_currentStage == _lastStage)
                    {
                        StartCoroutine(BaDumTss());
                        GetComponent<KMBombModule>().HandlePass();
                        _isSolved = true;
                        return;
                    }
                    _currentStage++;
                    StartCoroutine(PlaySounds(_calls[_currentStage], 1f));
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

    private IEnumerator BaDumTss()
    {

        yield return new WaitForSeconds(.5f);
        PlaySound(1);
        yield return new WaitForSeconds(.1f);
        PlaySound(0);
        yield return new WaitForSeconds(.3f);
        PlaySound(3);
        yield return null;
    }

#pragma warning disable 414
    private string TwitchHelpMessage = @"Use '!{0} 1 2 3 4' to hit the pads in reading order. Use '!{0} play' to play the sequence. Use '!{0} record' to start recording.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        var parts = command.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.All(part => part.Length == 1 && "1234".Contains(part)))
        {
            yield return null;
            for (int i = 0; i < parts.Length; i++)
            {
                HitPad(Int32.Parse(parts[i]) - 1);
                yield return new WaitForSeconds(.5f);
            }
        }
        else if (parts.Length == 1 && parts[0] == "play")
        {
            yield return null;
            PressPlay();
        }
        else if (parts.Length == 1 && parts[0] == "record")
        {
            yield return null;
            PressRecord();
        }
    }
}
static class MyExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Rnd.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static IList<T> Swap<T>(this IList<T> list, int indexA, int indexB)
    {
        T tmp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = tmp;
        return list;
    }
}
 
