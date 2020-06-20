using Mirror;
using UnityEngine;


[AddComponentMenu("")]
public class MyNetworkManager : NetworkManager
{
    #region Variables

    // Variables públicas
    public SetupPlayer m_SetUpPlayer;
    public int[] positionsIDs = new int[4];

    // Variables privadas
    private bool[] takenPositions = new bool[4];

    // Variables SerializeField
    [SerializeField] private GameObject[] startingPoints;


    #endregion

    #region Methods

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        int pos = firstPosAvailable();
        Transform startPos = startingPoints[pos].transform;
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

    #endregion

}

