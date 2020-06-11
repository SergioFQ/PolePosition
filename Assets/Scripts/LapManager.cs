using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LapManager : MonoBehaviour
{

    //private PlayerInfo m_PlayerInfo;

    void Awake()
    {
        /*m_PlayerInfo = FindObjectOfType<PlayerInfo>();
        Debug.Log(m_PlayerInfo.Name);*/
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
            other.GetComponent<PlayerInfo>().LastPoint = 0;
            other.GetComponent<PlayerController>().m_CurrentLap++;
        }
        /*else
        {
            Debug.Log("Tramposo");
        }*/
        //other.gameObject.GetComponentInChildren<PlayerController>().m_CurrentLap++;
        //Debug.Log(other.gameObject.GetComponentInChildren<PlayerController>().m_CurrentLap);
    }
}
