using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LapManager : MonoBehaviour
{

    //private UIManager m_UIManager;

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
            other.GetComponent<PlayerController>().m_CurrentLap++;
            if (other.GetComponent<PlayerController>().m_CurrentLap == 6)
            {
                other.GetComponent<PlayerController>().m_UIManager.ActivateGameOver();
                other.GetComponent<PlayerController>().SetInactive();
            }

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
