using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointsBehaviour : MonoBehaviour
{
    [SerializeField] private int id;

    public int ID { get; set; }

    void Awake()
    {
        ID = id;
    }
}
