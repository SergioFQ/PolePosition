using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LapManager : MonoBehaviour
{
    [SerializeField] private GameObject posCamera;
    [SerializeField] private GameObject targetCamera;
      
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>().isLocalPlayer)
        {
            if(other.GetComponent<PlayerController>().m_CurrentLap == -1)
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
                        other.GetComponent<SetupPlayer>().m_PolePositionManager.SetNamesRanking();
                    other.GetComponent<PlayerController>().SetInactive();

                }
                other.GetComponent<PlayerController>().IncreaseLap(other.GetComponent<PlayerInfo>().ID);
            }
        }
        
    }
}
