using Mirror;
using UnityEngine;


[AddComponentMenu("")]
public class MyNetworkManager : NetworkManager
{
    /*public string PlayerName { get; set; }


    public class CreatePlayerMessage : MessageBase
    {
        public string name;
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

        // tell the server to create a player with this name
        conn.Send(new CreatePlayerMessage { name = PlayerName });
    }

    */
    public SetupPlayer m_SetUpPlayer;
    [SerializeField] private GameObject[] startingPoints;
    public int[] positionsIDs = new int[4];
    private bool[] takenPositions = new bool[4];

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        //base.OnServerAddPlayer(conn);
        int pos = firstPosAvailable();
        Transform startPos = startingPoints[pos].transform; //GetStartPosition();
        GameObject player = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab);

        positionsIDs[pos] = m_SetUpPlayer.m_PlayerInfo.ID;
        NetworkServer.AddPlayerForConnection(conn, player);
    }
    public int firstPosAvailable()
    {

        for (int i = 0; i < takenPositions.Length; i++)
        {
            if(positionsIDs[i] == -1)
            {
                takenPositions[i] = false;
            }
        }


        for (int i = 0; i < takenPositions.Length; i++)
        {
            
            if (!takenPositions[i])
            {
                takenPositions[i] = true;
                return i;
            }
        }
        return -1; // Returneamos -1 porque ya comprobamos que esté llena la partida por tanto no va a llegar a pasar
    }


}

