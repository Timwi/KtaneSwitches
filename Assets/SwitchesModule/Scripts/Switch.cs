using UnityEngine;
using System.Collections;

public class Switch : MonoBehaviour
{
    public MeshRenderer TopIndicator;
    public MeshRenderer BottomIndicator;
    public bool Up
    {
        get
        {
            return GetComponent<Animator>().GetBool("Up");
        }
        set
        {
            GetComponent<Animator>().SetBool("Up", value);
        }
    }

    public void SetGoal(bool up)
    {
        if(up)
        {
            TopIndicator.material.color = Color.green;
            BottomIndicator.material.color = Color.black;
        }
        else
        {
            TopIndicator.material.color = Color.black;
            BottomIndicator.material.color = Color.green;
        }
    }
}
