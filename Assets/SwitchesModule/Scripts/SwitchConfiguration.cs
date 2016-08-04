using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class SwitchConfiguration
{
    public static int NUM_SWITCHES = 5;
    public bool[] SwitchStates;
    public static System.Random RuleRandom;

    public SwitchConfiguration(bool[] states)
    {
        SwitchStates = states;
    }

    public SwitchConfiguration(int intValue)
    {
        SwitchStates = new bool[NUM_SWITCHES];
        for(int i = NUM_SWITCHES - 1; i >= 0; i--)
        {
            SwitchStates[i] = (intValue & 1) == 1;
            intValue = intValue >> 1;
        }
    }

    public static SwitchConfiguration GetGoalConfiguration(List<SwitchConfiguration> safeConfigurations)
    {
        return safeConfigurations[UnityEngine.Random.Range(0, safeConfigurations.Count)];
    }

    public static SwitchConfiguration GetInitialConfiguration(List<SwitchConfiguration> safeConfigurations, SwitchConfiguration goalConfiguration)
    {
        List<SwitchConfiguration> safeClone = new List<SwitchConfiguration>(safeConfigurations);
        safeClone.Remove(goalConfiguration);
        foreach(int adjacentInt in GetAdjacentConfigurations(goalConfiguration.GetIntValue()))
        {
            SwitchConfiguration adjacentConfig = new SwitchConfiguration(adjacentInt);
            if(safeClone.Contains(adjacentConfig))
            {
                safeClone.Remove(adjacentConfig);
            }
        }
        return safeClone[UnityEngine.Random.Range(0, safeClone.Count)];
    }

    protected static SwitchConfiguration CreateRuleRandomConfiguration()
    {
        bool[] states = new bool[NUM_SWITCHES];

        for (int i = 0; i < NUM_SWITCHES; i++)
        {
            states[i] = RuleRandom.Next(0, 2) == 1;
        }

        return new SwitchConfiguration(states);
    }

    protected static List<int> GetAdjacentConfigurations(int currentValue)
    {
        List<int> adjacentConfigurations = new List<int>();
        int shift = 1;
        for(int i=0; i<NUM_SWITCHES; i++)
        {
            adjacentConfigurations.Add(currentValue ^ shift);
            shift = shift << 1;
        }

        return adjacentConfigurations;
    }

    public override bool Equals(System.Object obj)
    {
        if (obj == null)
        {
            return false;
        }

        SwitchConfiguration c = obj as SwitchConfiguration;
        if (c == null)
        {
            return false;
        }

        return c.GetHashCode() == GetHashCode();
    }

    public static List<SwitchConfiguration> GetSafeConfigurations(int numBadConfigurations)
    {
        List<SwitchConfiguration> safeConfigurations = new List<SwitchConfiguration>();
        SwitchConfiguration startPoint = CreateRuleRandomConfiguration();

        List<int> visitedConfigurations = VisitConfigurations(startPoint, (int)Mathf.Pow(2, NUM_SWITCHES) - numBadConfigurations);

        foreach(int config in visitedConfigurations)
        {
            safeConfigurations.Add(new SwitchConfiguration(config));
        }

        return safeConfigurations;
    }

    public static List<SwitchConfiguration> GetStrikeConfigurations(List<SwitchConfiguration> safeConfigurations)
    {
        List<SwitchConfiguration> strikeConfigurations = new List<SwitchConfiguration>();

        for (int i = 0; i < (int)Mathf.Pow(2, NUM_SWITCHES); i++)
        {
            SwitchConfiguration config = new SwitchConfiguration(i);
            if (!safeConfigurations.Contains(config))
            {
                strikeConfigurations.Add(config);
            }
        }
        
        return strikeConfigurations;
    }

    protected static List<int> VisitConfigurations(SwitchConfiguration startPoint, int numToVisit)
    {
        List<int> visitedConfigurations = new List<int>();

        Stack<int> visitStack = new Stack<int>();
        visitStack.Push(startPoint.GetIntValue());
        
        int loops = 0;

        while(visitStack.Count > 0)
        {
            if(visitedConfigurations.Count >= numToVisit)
            {
                break;
            }

            int configToVisit = visitStack.Pop();
            if (!visitedConfigurations.Contains(configToVisit))
            {
                visitedConfigurations.Add(configToVisit);
                List<int> adjacentConfigurations = GetAdjacentConfigurations(configToVisit);
                while(adjacentConfigurations.Count > 0)
                {
                    int configToAdd = adjacentConfigurations[RuleRandom.Next(0, adjacentConfigurations.Count)];
                    visitStack.Push(configToAdd);
                    adjacentConfigurations.Remove(configToAdd);
                }                    
            }

            if (loops++ > 5000)
            {
                Debug.LogError("Too many loops when attempting to generate switch module");
                break;
            }
        }

        return visitedConfigurations;
    }

    public override int GetHashCode()
    {
        return GetIntValue();
    }

    protected int GetIntValue()
    {
        int intValue = 0;
        for (int i = 0; i < NUM_SWITCHES; i++)
        {
            intValue += SwitchStates[i] ? 1 : 0;
            intValue = intValue << 1;
        }
        
        return intValue >> 1;
    }

    public override string ToString()
    {
        string switchConfig = "";
        for(int i=0; i < NUM_SWITCHES; i++)
        {
            switchConfig += SwitchStates[i] ? "I" : "O";
            switchConfig += " ";
        }

        return switchConfig;
    }
}
