using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* CheckpointsBehaviour: clase encargada de darle un Id a todos los
 * checkpoints del circuito.
 */ 
public class CheckpointsBehaviour : MonoBehaviour
{
    #region Variables

    [SerializeField] private int id;

    #endregion

    #region Getter & Setter

    public int ID { get; set; }

    #endregion

    #region Unity Callbaks

    /* Awake: establece el valor del ID en función del valor elegido en 
     * el editor de Unity.
     */ 
    void Awake()
    {
        ID = id;
    }

    #endregion
}
