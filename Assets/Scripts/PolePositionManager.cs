using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;

public class PolePositionManager : NetworkBehaviour
{
    public int numPlayers;
    public NetworkManager networkManager;
    public Vector3[] posSphere; //vector publico que guardaría la posición de las esferas aunque en teoría solo se necesitaría la propia
    private readonly List<PlayerInfo> m_Players = new List<PlayerInfo>(4);
    private List<PlayerInfo> ordenP = new List<PlayerInfo>(4);
    private CircuitController m_CircuitController;
    private GameObject[] m_DebuggingSpheres;
    [SyncVar(hook = nameof(RpcSetRaceOrder))] private string myRaceOrder = "";
    private UIManager m_UIManager;
    float[] arcLengths;

    private void Awake()
    {
        if (networkManager == null) networkManager = FindObjectOfType<NetworkManager>();
        if (m_CircuitController == null) m_CircuitController = FindObjectOfType<CircuitController>();
        posSphere = new Vector3[4];
        m_DebuggingSpheres = new GameObject[networkManager.maxConnections]; 
        for (int i = 0; i < networkManager.maxConnections; ++i)
        {
            m_DebuggingSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_DebuggingSpheres[i].GetComponent<SphereCollider>().enabled = false;
            posSphere[i] = this.m_DebuggingSpheres[i].transform.position; // Se inicializa a la primera posición de la esfera en el circuito y se actualiza más abajo
        }
        m_UIManager = FindObjectOfType<UIManager>();
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
            //Debug.Log(" X " + x.ID + " " + m_ArcLengths[x.ID]);
            //Debug.Log(" Y " + y.ID + " " + m_ArcLengths[y.ID]);

            if (m_ArcLengths[x.ID] > m_ArcLengths[y.ID] || x.CurrentLap > y.CurrentLap)
            {
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
                ordenP.Add( m_Players[i]);
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
                myRaceOrder += _player.Name + " " + arcLengths[_player.ID] + "\n";
            }
            for (int i = 0; i < arcLengths.Length; i++)
            {
                Debug.Log("arclegths " + i + " ID " + m_Players[i].ID + "  Nombre " + m_Players[i].Name + " " + arcLengths[i] + " Vuelta: " + m_Players[i].CurrentLap);
            }
            RpcSetRaceOrder("", myRaceOrder);

            
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
        if (this.m_Players[ID].CurrentLap == 0 || this.m_Players[ID].CurrentLap == -1)
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
    [ClientRpc]
    void RpcSetRaceOrder(string old, string newOrder)
    {
        m_UIManager.UpdateNames(newOrder);
    }
}