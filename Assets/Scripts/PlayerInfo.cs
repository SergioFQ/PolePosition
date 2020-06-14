using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public string Name { get; set; }

    public int ID { get; set; }

    public int CurrentPosition { get; set; }

    public int CurrentLap { get; set; }

    public int ColourID { get; set; }

    public int LastPoint { get; set; }

    public override string ToString()
    {
        return Name;
    }

    public bool IsReady { get; set; }
}