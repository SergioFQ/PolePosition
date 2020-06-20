using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* LapManager: clase encargada de controlar cuando el jugador pasa por meta y aumenta su número de vueltas
 * actuales.
 */ 
public class LapManager : MonoBehaviour
{
    #region Variables

    // SerializeField
    [SerializeField] private GameObject posCamera;
    [SerializeField] private GameObject targetCamera;

    #endregion Variables

    #region Unity Callbacks

    /* OnTriggerEnter: detecta si el jugador ha pasado por meta y si ha recorrido completamente el circuito sin
     * haber hecho trampas (si ha pasado TODOS los checkpoints repartidos por el circuito).
     */ 
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>().isLocalPlayer)
        {
            if (other.GetComponent<PlayerController>().m_CurrentLap == -1)
            {
                other.GetComponent<PlayerInfo>().LastPoint = -1;
                other.GetComponent<PlayerController>().IncreaseLap(other.GetComponent<PlayerInfo>().ID);
            }

            if (other.GetComponent<PlayerInfo>().LastPoint == 12)
            {
                other.GetComponent<PlayerInfo>().LastPoint = -1;
                if (other.GetComponent<PlayerInfo>().CurrentLap == (other.GetComponent<PlayerController>().numVueltas - 1))
                {
                    other.GetComponent<SetupPlayer>().UnfocusCamera(posCamera.transform.position, targetCamera.transform.position);
                    other.GetComponent<PlayerController>().m_UIManager.ActivateGameOver();

                    if (other.GetComponent<PlayerController>().isReady)
                    {
                        other.GetComponent<SetupPlayer>().m_PolePositionManager.SetNamesRanking();
                    }
                    other.GetComponent<PlayerController>().SetInactive();
                }
                other.GetComponent<PlayerController>().IncreaseLap(other.GetComponent<PlayerInfo>().ID);
            }
        }
    }
    #endregion Unity Callbacks
}
