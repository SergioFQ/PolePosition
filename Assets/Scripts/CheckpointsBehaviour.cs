using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointsBehaviour : MonoBehaviour
{
    #region Variables

    [SerializeField] private int id;

    #endregion

    #region Getter & Setter

    public int ID { get; set; }

    #endregion

    #region Unity Callbaks

    void Awake()
    {
        ID = id;
    }

    #endregion
}
