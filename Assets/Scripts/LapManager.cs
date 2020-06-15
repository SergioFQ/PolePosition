using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LapManager : MonoBehaviour
{
    [SerializeField] private GameObject posCamera;
    [SerializeField] private GameObject targetCamera;

    void Awake()
    {
        /*m_PlayerInfo = FindObjectOfType<PlayerInfo>();
        Debug.Log(m_PlayerInfo.Name);*/
        //m_UIManager = FindObjectOfType<UIManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerInfo>().LastPoint == 12)
        {
            Debug.Log("Bien");
            other.GetComponent<PlayerInfo>().LastPoint = - 1;
            if (other.GetComponent<PlayerInfo>().CurrentLap == 1)
            {
                other.GetComponent<SetupPlayer>().UnfocusCamera(posCamera.transform.position, targetCamera.transform.position);
                other.GetComponent<PlayerController>().m_UIManager.ActivateGameOver();
                if (other.GetComponent<PlayerController>().isReady)
                    other.GetComponent<SetupPlayer>().m_PolePositionManager.SetNamesRanking();
                other.GetComponent<PlayerController>().SetInactive();

            }

            other.GetComponent<PlayerController>().IncreaseLap();
            //m_UIManager.UpdateLap(other.GetComponent<PlayerController>().m_CurrentLap);
        }
        /*else
        {
            Debug.Log("Tramposo");
        }*/
        //other.gameObject.GetComponentInChildren<PlayerController>().m_CurrentLap++;
        //Debug.Log(other.gameObject.GetComponentInChildren<PlayerController>().m_CurrentLap);
    }
}
