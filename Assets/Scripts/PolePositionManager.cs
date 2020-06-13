using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;

public class PolePositionManager : NetworkBehaviour
{
    public int numPlayersReady;
    public NetworkManager networkManager;
    public Vector3[] posSphere; //vector publico que guardaría la posición de las esferas
    private readonly List<PlayerInfo> m_Players = new List<PlayerInfo>(4);
    private List<PlayerInfo> ordenP = new List<PlayerInfo>(4);
    private CircuitController m_CircuitController;
    private PlayerController m_playerController;
    private GameObject[] m_DebuggingSpheres;
    [SyncVar(hook = nameof(SetRaceOrder))] private string myRaceOrder = "";
    private UIManager m_UIManager;
    private float[] arcLengths;
    public GameObject[] checkpoints;
    private PlayerInfo m_PlayerInfo; 
    public SetupPlayer m_SetUpPlayer;

    private void Awake()
    {
        if (networkManager == null) networkManager = FindObjectOfType<NetworkManager>();
        if (m_CircuitController == null) m_CircuitController = FindObjectOfType<CircuitController>();
        posSphere = new Vector3[4];
        m_playerController = FindObjectOfType<PlayerController>();
        m_DebuggingSpheres = new GameObject[networkManager.maxConnections];
        for (int i = 0; i < networkManager.maxConnections; ++i)
        {
            m_DebuggingSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_DebuggingSpheres[i].GetComponent<SphereCollider>().enabled = false;
            posSphere[i] = this.m_DebuggingSpheres[i].transform.position; // Se inicializa a la primera posición de la esfera en el circuito y se actualiza más abajo
        }
        m_UIManager = FindObjectOfType<UIManager>();
        //m_SetUpPlayer = FindObjectOfType<SetupPlayer>();
        m_PlayerInfo = GetComponent<PlayerInfo>();
    }

    private void Update()
    {
        if (m_Players.Count == 0)
            return;
        UpdateRaceProgress();
    }

    public void AddPlayer(PlayerInfo player)
    {
        m_Players.Add(player);
        arcLengths = new float[m_Players.Count];

    }

    private class PlayerInfoComparer : Comparer<PlayerInfo>
    {
        float[] m_ArcLengths;

        public PlayerInfoComparer(float[] orden)
        {
            m_ArcLengths = orden;
        }

        public override int Compare(PlayerInfo x, PlayerInfo y)
        {
            if (m_ArcLengths[x.ID] > m_ArcLengths[y.ID] || x.CurrentLap > y.CurrentLap)
            {
                if (x.CurrentLap == y.CurrentLap && (x.LastPoint == -1 && y.LastPoint != -1))
                {
                    return 1;
                }


                return -1;
            }
            else {


                return 1;
            }
        }
    }

    public void UpdateRaceProgress()
    {
        ordenP = new List<PlayerInfo>();
        float[] orden = new float[4];
        if (isServer)
        {
            //Debug.Log("------------------------------------------");
            for (int i = 0; i < m_Players.Count; i++)
            {
                orden[i] = arcLengths[i];
                ordenP.Add(m_Players[i]);
            }

            foreach (var _player in m_Players)
            {
                RpcComputeCarArcLength(_player.ID);

            }

            for (int i = 0; i < arcLengths.Length; i++)
            {
                //Debug.Log("arclegths " + i + " ID " +m_Players[i].ID + "  Nombre "+ m_Players[i].Name + " "+ arcLengths[i]);
                orden[i] = arcLengths[i];
            }

            ordenP.Sort(new PlayerInfoComparer(orden));

            myRaceOrder = " ";
            foreach (var _player in ordenP)
            {
                myRaceOrder += _player.Name + /*" " + arcLengths[_player.ID] +*/ "\n";

            }
            for (int i = 0; i < m_Players.Count; i++)
            {
                //Debug.Log("I" + i + " ID " + m_Players[i].ID + "  Nombre " + m_Players[i].Name);
            }

            for (int i = 0; i < arcLengths.Length; i++)
            {
                //Debug.Log("arclegths " + i + " ID " + m_Players[i].ID + "  Nombre " + m_Players[i].Name + " " + arcLengths[i] + " Vuelta: " + m_Players[i].CurrentLap);
            }
            SetRaceOrder("", myRaceOrder);


        }
    }
    public void StartRaceCall()
    {
        if (isServer)
        {
            RpcUpdateNumPlayersReady();
        }
        else
        {
            m_SetUpPlayer.CmdStartRace();
        }
    }

    [ClientRpc]
    public void RpcUpdateNumPlayersReady()
    {
        numPlayersReady++;
        StartRace(0,0);
    }
    


    //Una vez los jugadores seleccionen "Ready" se llamará a este método y empezará la carrera
    //[ClientRpc]
    public void StartRace(int old, int newV)
    {
        /*bool activado = false;
        foreach (var player in m_Players)
        {
            if(!player.GetComponent<PlayerInfo>().isReady && !activado)
            {
                player.GetComponent<PlayerInfo>().isReady = true;
            }
        }
        
        int aux = 0;
        foreach (var player in m_Players)
        {
            if (player.GetComponent<UIManager>().ready)
                aux++;
        }*/
        Debug.Log(numPlayersReady);
        if (numPlayersReady >= m_Players.Count)
        {
            //activamos todos los coches
            foreach (var player in m_Players)
            {
                player.GetComponent<PlayerController>().isReady = true;
                m_UIManager.deactivateReadyMenu();
            }
        }
    }

    [ClientRpc]
    void RpcComputeCarArcLength(int ID)
    {
        // Compute the projection of the car position to the closest circuit 
        // path segment and accumulate the arc-length along of the car along
        // the circuit.
        Vector3 carPos = this.m_Players[ID].transform.position;
        int segIdx;
        float carDist;
        Vector3 carProj;

        float minArcL =
            this.m_CircuitController.ComputeClosestPointArcLength(carPos, out segIdx, out carProj, out carDist);

        this.m_DebuggingSpheres[ID].transform.position = carProj;
        posSphere[ID] = this.m_DebuggingSpheres[ID].transform.position; //actualización de la posición de la esfera por ID, esto sí funciona
        if (this.m_Players[ID].CurrentLap == 0)
        {
            minArcL -= m_CircuitController.CircuitLength;
        }
        else
        {
            minArcL += m_CircuitController.CircuitLength *
                       (m_Players[ID].CurrentLap - 1);
        }
        arcLengths[ID] = minArcL;
    }
    void SetRaceOrder(string old, string newOrder)
    {
        
        m_UIManager.UpdateNames(newOrder);
    }

}