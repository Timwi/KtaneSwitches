using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class SwitchModule : MonoBehaviour
{
    public Switch[] Switches;
    public KMRuleSeedable RuleSeedable;

    private int _goalConfiguration;
    private int[] _forbiddenConfigurations; // least significant bit = switch on the far LEFT
    private KMSelectable[] _switches;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _isSolved;

    const int _numSwitches = 5;
    const int _numForbiddenConfigurations = 10;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        _switches = new KMSelectable[Switches.Length];
        for (int i = 0; i < Switches.Length; i++)
        {
            _switches[i] = Switches[i].GetComponent<KMSelectable>();
            _switches[i].OnInteract += GetToggleHandler(i);
        }

        // RULE SEED
        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat(@"[Switches #{0}] Using rule seed: {1}", _moduleId, rnd.Seed);
        _forbiddenConfigurations = rnd.Seed == 1 ? new[] { 4, 26, 30, 9, 25, 29, 3, 11, 7, 15 } : GetForbiddenConfigurations(rnd).ToArray();
        Debug.LogFormat(@"[Switches #{0}] Forbidden configurations are:", _moduleId, rnd.Seed);
        for (var i = 0; i < _forbiddenConfigurations.Length; i++)
            Debug.LogFormat(@"[Switches #{0}] • {1}", _moduleId, DescribeConfiguration(_forbiddenConfigurations[i]));
        // END OF RULE SEED

        var safeConfigurations = Enumerable.Range(0, 1 << _numSwitches).Except(_forbiddenConfigurations).ToList();

        // Pick an intended solution
        var goalIx = Rnd.Range(0, safeConfigurations.Count);
        _goalConfiguration = safeConfigurations[goalIx];
        safeConfigurations.RemoveAt(goalIx);
        Debug.LogFormat(@"[Switches #{0}] Intended solution: {1}", _moduleId, DescribeConfiguration(_goalConfiguration));

        // Make sure the initial switch state won’t just be one switch flip away
        foreach (var adj in GetAdjacentConfigurations(_goalConfiguration))
            safeConfigurations.Remove(adj);

        // Pick an initial switch state
        var initialIx = Rnd.Range(0, safeConfigurations.Count);
        var initialConfiguration = safeConfigurations[initialIx];
        SetSwitches(initialConfiguration);
        Debug.LogFormat(@"[Switches #{0}] Initial configuration: {1}", _moduleId, DescribeConfiguration(initialConfiguration));

        GetComponent<KMBombModule>().OnActivate += SetGoalIndicators;
    }

    IEnumerable<int> GetForbiddenConfigurations(MonoRandom rnd)
    {
        var startPoint = rnd.Next(0, 1 << _numSwitches);
        var visitedConfigurations = VisitConfigurations(startPoint, rnd);

        for (var i = 0; i < (1 << _numSwitches); i++)
            if (!visitedConfigurations.Contains(i))
                yield return i;
    }

    HashSet<int> VisitConfigurations(int startConfiguration, MonoRandom rnd)
    {
        var visitedConfigurations = new HashSet<int>();
        var visitStack = new Stack<int>();
        visitStack.Push(startConfiguration);

        while (true)
        {
            if (visitedConfigurations.Count >= (1 << _numSwitches) - _numForbiddenConfigurations)
                return visitedConfigurations;

            int configToVisit = visitStack.Pop();
            if (!visitedConfigurations.Add(configToVisit))
                continue;

            var adjacentConfigurations = GetAdjacentConfigurations(configToVisit).ToList();
            while (adjacentConfigurations.Count > 0)
            {
                var ix = rnd.Next(0, adjacentConfigurations.Count);
                visitStack.Push(adjacentConfigurations[ix]);
                adjacentConfigurations.RemoveAt(ix);
            }
        }
    }

    IEnumerable<int> GetAdjacentConfigurations(int configuration)
    {
        for (int i = 0; i < _numSwitches; i++)
            yield return configuration ^ (1 << i);
    }

    protected KMSelectable.OnInteractHandler GetToggleHandler(int index)
    {
        return delegate
        {
            Switches[index].Up = !Switches[index].Up;
            GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
            if (_isSolved)
                return false;

            var config = GetCurrentConfiguration();
            if (config == _goalConfiguration)
            {
                Debug.LogFormat(@"[Switches #{0}] Module solved.", _moduleId);
                GetComponent<KMBombModule>().HandlePass();
                _isSolved = true;
            }
            else if (_forbiddenConfigurations.Contains(config))
            {
                Debug.LogFormat(@"[Switches #{0}] You tried to flip switch #{1}, yielding {2}, which is forbidden. Strike!", _moduleId, index + 1, DescribeConfiguration(config));
                GetComponent<KMBombModule>().HandleStrike();
                Switches[index].Up = !Switches[index].Up;
            }
            else
            {
                Debug.LogFormat(@"[Switches #{0}] You flipped switch #{1}, yielding {2}.", _moduleId, index + 1, DescribeConfiguration(config));
            }
            return false;
        };
    }

    private static string DescribeConfiguration(int config)
    {
        return Enumerable.Range(0, _numSwitches).Select(i => (config & (1 << i)) != 0 ? "up" : "down").Join("/");
    }

    protected int GetCurrentConfiguration()
    {
        var config = 0;
        for (int i = 0; i < Switches.Length; i++)
            if (Switches[i].Up)
                config |= 1 << i;
        return config;
    }

    protected void SetSwitches(int config)
    {
        for (int i = 0; i < _numSwitches; i++)
            Switches[i].Up = (config & (1 << i)) != 0;
    }

    protected void SetGoalIndicators()
    {
        for (int i = 0; i < _numSwitches; i++)
            Switches[i].SetGoal((_goalConfiguration & (1 << i)) != 0);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} flip 1 5 3 2 [flips the switches in that order; numbered left to right]";
#pragma warning restore 414

    IEnumerable<KMSelectable> ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*(?:(?:flip|toggle|switch)\s+)?([\d ,;]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            return null;
        var lst = new List<KMSelectable>();
        foreach (var ch in m.Groups[1].Value)
            if (ch >= '1' && ch <= '5')
                lst.Add(_switches[ch - '1']);
        return lst;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        var currentConfiguration = GetCurrentConfiguration();

        // Breadth-first search
        var visited = new HashSet<int>();
        var parents = new Dictionary<int, int>();
        var q = new Queue<int>();
        q.Enqueue(currentConfiguration);

        while (q.Count > 0)
        {
            var cnf = q.Dequeue();
            if (!visited.Add(cnf))
                continue;
            if (cnf == _goalConfiguration)
                goto found;

            foreach (var neighbour in GetAdjacentConfigurations(cnf))
            {
                if (_forbiddenConfigurations.Contains(neighbour) || visited.Contains(neighbour))
                    continue;
                q.Enqueue(neighbour);
                parents[neighbour] = cnf;
            }
        }

        throw new Exception("There is a bug in this module’s auto-solve handler. Please contact Timwi about this.");

        found:
        var switchStateChanges = new[] { 1, 2, 4, 8, 16 };
        var path = new List<int>();
        var state = _goalConfiguration;
        while (state != currentConfiguration)
        {
            path.Add(Array.IndexOf(switchStateChanges, state ^ parents[state]));
            state = parents[state];
        }

        for (var i = path.Count - 1; i >= 0; i--)
        {
            _switches[path[i]].OnInteract();
            yield return new WaitForSeconds(.4f);
        }
    }
}
