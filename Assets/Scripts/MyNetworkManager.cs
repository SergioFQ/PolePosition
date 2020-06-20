using Mirror;
using UnityEngine;

/* MyNetworkManager: clase que hereda de NetworkManager para hacer Override del método que se
 * encarga de hacer aparecer a los jugadores en escena.
 */ 
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

    /* OnServerAddPlayer: metodo override de Network Manager para cambiar 
     * la forma en la que los players aparecen en escena, evitando asi que en algún
     * momento pueda aparecer uno encima de otro. Con este método controlamos qué
     * posición inicial está libre y ponemos al nuevo player en dicha posición.
     */
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        int pos = FirstPosAvailable();
        Transform startPos = startingPoints[pos].transform;
        GameObject player = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab);

        positionsIDs[pos] = m_SetUpPlayer.m_PlayerInfo.ID;
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    /* FirstPosAvailable: método que recorre el array de posiciones iniciales 
     * para conocer la primera posición libre en la que puede aparecer el nuevo
     * jugador.
     */
    public int FirstPosAvailable()
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
        return -1; // Return -1 porque ya comprobamos en otra clase que esté llena la partida por tanto no va a llegar a pasar
    }

    #endregion

}

