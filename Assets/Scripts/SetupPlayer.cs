﻿using System;
using System.Threading;
using Mirror;
using UnityEngine;
using Random = System.Random;
using System.Diagnostics;

/*
	Documentation: https://mirror-networking.com/docs/Guides/NetworkBehaviour.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
*/

public class SetupPlayer : NetworkBehaviour
{
    [SyncVar] private int m_ID;
    [SyncVar (hook = nameof(setName))] private string m_Name;
    [SyncVar (hook = nameof(setColour))] private int m_Colour;
    private UIManager m_UIManager;
    private NetworkManager m_NetworkManager;
    public PlayerController m_PlayerController;
    public PlayerInfo m_PlayerInfo;
    public PolePositionManager m_PolePositionManager;

    #region Start & Stop Callbacks

    /// <summary>
    /// This is invoked for NetworkBehaviour objects when they become active on the server.
    /// <para>This could be triggered by NetworkServer.Listen() for objects in the scene, or by NetworkServer.Spawn() for objects that are dynamically created.</para>
    /// <para>This will be called for objects on a "host" as well as for object on a dedicated server.</para>
    /// </summary>
    public override void OnStartServer()
    {
        base.OnStartServer();
        if (isServerOnly)
        {
            if (!m_PolePositionManager.started && !m_PolePositionManager.full)
            {
                m_PolePositionManager.AddPlayer(m_PlayerInfo);
            }
            else
            {
                print("f");
            }

        }
        else
        {
            m_ID = connectionToClient.connectionId;
        }
        /*if (!m_PolePositionManager.CheckSpace())
        {
            if (isServerOnly)
            {
                m_PolePositionManager.AddPlayer(m_PlayerInfo);
            }
            else
            {
                m_ID = connectionToClient.connectionId;
            }
        }*/
    }

   

    /// <summary>
    /// Called on every NetworkBehaviour when it is activated on a client.
    /// <para>Objects on the host have this function called, as there is a local client on the host. The values of SyncVars on object are guaranteed to be initialized correctly with the latest state from the server when this function is called on the client.</para>
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
        print("Entre jugador");
        m_PlayerInfo.ID = m_PolePositionManager.m_Players.Count;
        m_PlayerInfo.CurrentLap = -1;
        m_PlayerInfo.LastPoint = -1;
        print("Started " + m_PolePositionManager.started);
        if (!m_PolePositionManager.started && !m_PolePositionManager.full)
        {
            print("f");
            m_PolePositionManager.AddPlayer(m_PlayerInfo);
        }
        else
        {
            print("f");
            if (isLocalPlayer)
            {
                m_UIManager.ActivateFullGameHUD();
                m_NetworkManager.StopClient();

            }
        }
        //}
        
        
    }

    /// <summary>
    /// Called when the local player object has been set up.
    /// <para>This happens after OnStartClient(), as it is triggered by an ownership message from the server. This is an appropriate place to activate components or functionality that should only be active for the local player, such as cameras and input.</para>
    /// </summary>
    public override void OnStartLocalPlayer()
    {
        
        m_PolePositionManager.m_SetUpPlayer = this;
        if (!m_PolePositionManager.started)
        {
            CmdSelectName((m_UIManager.playerName == "") ? ("Player" + (m_PolePositionManager.m_Players.Count - 1)) : (m_UIManager.playerName));
            CmdSelectColor(m_UIManager.colorNumber);
            CmdSelectIdLap(m_PlayerInfo.ID);
            m_PlayerInfo.IsReady = false;

        }
    }

    #endregion

    private void Awake()
    {
        print("Awake");
        m_PlayerInfo = GetComponent<PlayerInfo>();
        m_PlayerController = GetComponent<PlayerController>();
        m_NetworkManager = FindObjectOfType<NetworkManager>();
        m_PolePositionManager = FindObjectOfType<PolePositionManager>();
        m_UIManager = FindObjectOfType<UIManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (isLocalPlayer)
        {
            m_PlayerController.enabled = true;
            m_PlayerController.OnSpeedChangeEvent += OnSpeedChangeEventHandler;
            m_PlayerController.OnTotalTimeChangeEvent += OnTotalChangeEventHandler;
            m_PlayerController.OnLapTimeChangeEvent += OnLapChangeEventHandler;
            ConfigureCamera();
        }
    }

    void OnSpeedChangeEventHandler(float speed)
    {
        m_UIManager.UpdateSpeed((int) speed * 5); // 5 for visualization purpose (km/h)
    }

    void OnTotalChangeEventHandler(Stopwatch tt)
    {
        m_UIManager.UpdateTotalTime(m_PlayerController.TimeToString(tt));
    }
    void OnLapChangeEventHandler(Stopwatch lt)
    {
        m_UIManager.UpdateLapTime(m_PlayerController.TimeToString(lt));
    }

    void ConfigureCamera()
    {
        if (Camera.main != null) Camera.main.gameObject.GetComponent<CameraController>().m_Focus = this.gameObject;
    }

    public void UnfocusCamera(Vector3 cameraPos, Vector3 targetPos)
    {
        Camera.main.gameObject.GetComponent<CameraController>().m_Focus = null;
        Camera.main.gameObject.transform.position = cameraPos;
        Camera.main.gameObject.transform.LookAt(targetPos);
    }
    void setName(string old, string newName)
    {
        m_PlayerInfo.Name = newName;
    }
    


    void setColour(int old, int newColour)
    {
        m_PlayerInfo.ColourID = newColour;

        switch (newColour)
        {
            case 0:
                transform.Find("raceCar").Find("body_red").gameObject.SetActive(true);
                break;
            case 1:
                transform.Find("raceCar").Find("body_red").gameObject.SetActive(false);
                transform.Find("raceCar").Find("body_green").gameObject.SetActive(true);

                break;
            case 2:
                transform.Find("raceCar").Find("body_red").gameObject.SetActive(false);
                transform.Find("raceCar").Find("body_orange").gameObject.SetActive(true);
                break;
            case 3:
                transform.Find("raceCar").Find("body_red").gameObject.SetActive(false);
                transform.Find("raceCar").Find("body_white").gameObject.SetActive(true);
                break;
        }
    }

    [Command]
    void CmdSelectName(string name)
    {
        if (isServerOnly)
        {
            m_PolePositionManager.m_Players[m_PolePositionManager.m_Players.Count-1].Name = name;
            m_PolePositionManager.m_Players[m_PolePositionManager.m_Players.Count - 1].ID = m_ID;
        }
        m_Name = name;
    }
    [Command]
    void CmdSelectColor(int colour)
    {
        if (isServerOnly)
        {
            m_PolePositionManager.m_Players[m_PolePositionManager.m_Players.Count - 1].ColourID = colour;
        }
        m_Colour = colour;
    }
    [Command]
    void CmdSelectIdLap(int id)
    {
        if (isServerOnly)
        {
            m_PolePositionManager.m_Players[m_PolePositionManager.m_Players.Count - 1].ID = id;
            m_PolePositionManager.m_Players[m_PolePositionManager.m_Players.Count - 1].CurrentLap = -1;
            m_PolePositionManager.m_Players[m_PolePositionManager.m_Players.Count - 1].LastPoint = -1;
        }
    }

    [Command]
    public void CmdAddNumPlayer()
    {
        m_PolePositionManager.numPlayers += 1;
    }

    [Command]
    public void CmdUpdateOrdenRanking()
    {
        m_PolePositionManager.ordenRanking++;
    }
    [Command]
    public void CmdUpdateNamesRanking()
    {
        m_PolePositionManager.namesRanking += m_PlayerInfo.Name + "\n";
    }

    [Command]
    public void CmdStarted()
    {
        m_PolePositionManager.started = true;
    }

}