using UnityEngine;
using System.Collections.Generic;


public class SwitchModule : MonoBehaviour
{
    public Switch[] Switches;
    SwitchConfiguration goalConfiguration;
    List<SwitchConfiguration> safeConfigurations;

    void Awake()
    {
        Init();
    }

    void Init()
    {
        for(int i = 0; i < Switches.Length; i++)
        {
            int j = i;
            Switches[i].GetComponent<KMSelectable>().OnInteract += delegate() { OnToggle(j); return false; };
        }

        SetInitialConfiguration();
        GetComponent<KMBombModule>().OnActivate += OnActivate;
    }

    protected void OnToggle(int index)
    {
        Switches[index].Up = !Switches[index].Up;
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);

        if(GetCurrentConfiguration().Equals(goalConfiguration))
        {
            GetComponent<KMBombModule>().HandlePass();
        }
        else if(!safeConfigurations.Contains(GetCurrentConfiguration()))
        {
            GetComponent<KMBombModule>().HandleStrike();
            Switches[index].Up = !Switches[index].Up;
        }
    }

    protected SwitchConfiguration GetCurrentConfiguration()
    {
        bool[] states = new bool[Switches.Length];

        for(int i=0; i < Switches.Length; i++)
        {
            states[i] = Switches[i].Up;
        }

        return new SwitchConfiguration(states);
    }

    protected void SetInitialConfiguration()
    {
        SwitchConfiguration.RuleRandom = new System.Random(5);
        safeConfigurations = SwitchConfiguration.GetSafeConfigurations(10);
        goalConfiguration = SwitchConfiguration.GetGoalConfiguration(safeConfigurations);
        SwitchConfiguration initialConfiguration = SwitchConfiguration.GetInitialConfiguration(safeConfigurations, goalConfiguration);
        SetSwitches(initialConfiguration);
        SwitchConfiguration.GetStrikeConfigurations(safeConfigurations);
    }

    protected void SetSwitches(SwitchConfiguration config)
    {
        for(int i=0; i < config.SwitchStates.Length; i++)
        {
            Switches[i].Up = config.SwitchStates[i];
        }
    }

    protected void SetGoalIndicators()
    {
        for (int i = 0; i < goalConfiguration.SwitchStates.Length; i++)
        {
            Switches[i].SetGoal(goalConfiguration.SwitchStates[i]);
        }
    }

    [ContextMenu("Log rules")]
    public void LogHTMLRules()
    {
        string html = "<table class='switch-table'>";

        foreach(SwitchConfiguration strikeConfig in SwitchConfiguration.GetStrikeConfigurations(safeConfigurations))
        {
            html += "<tr class='switch-row'>";
            for(int i=0; i < strikeConfig.SwitchStates.Length; i++)
            {
                html += "<td class='switch-column'>";
                html += string.Format("<img src='img/{0}'></img>", strikeConfig.SwitchStates[i] ? "switch-up.svg" : "switch-down.svg");
                html += "</td>";
            }
            html += "</tr>";
        }

        html += "</table>";

        Debug.Log(html);
    }

    public void OnActivate()
    {
        SetGoalIndicators();
    }
}
