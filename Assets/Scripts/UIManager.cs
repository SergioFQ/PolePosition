using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/* UIManager: esta clase se encarga de administrar todo lo relacionado con
 * los HUDs e información por pantalla que verán los jugadores.
 */ 
public class UIManager : MonoBehaviour
{
    #region Variables
    // Variables privadas
    private NetworkManager m_NetworkManager;
    private int laps = 3;

    // Variables públicas
    public bool showGUI = true;
    public PolePositionManager m_polePositionManager;
    public int colorNumber = 0;
    public string playerName = "";
    public bool ready = false;

    // Variables SerializeField
    [Header("Main Menu")] [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonServer;
    [SerializeField] private InputField inputFieldIP;
    [SerializeField] private Button buttonColor;
    [SerializeField] private InputField inputFieldName;
    [SerializeField] private Button buttonMainMenuQuit;
    [SerializeField] private Text textColorButton;

    [Header("In-Game HUD")]
    [SerializeField] private GameObject inGameHUD;
    [SerializeField] private Text textSpeed;
    [SerializeField] public Text textLaps;
    [SerializeField] private Text textPosition;
    [SerializeField] private Text textColor;
    [SerializeField] private Text textLapTime;
    [SerializeField] private Text textTotalTime;
    [SerializeField] private RawImage wrongWay;
    [SerializeField] private Button buttonQuitGame;
    [SerializeField] private Button buttonInGameMainMenu;

    [Header("Ready Menu")] [SerializeField] private GameObject readyMenu;
    [SerializeField] private Button buttonReady;
    [SerializeField] private Button buttonColorLobby;
    [SerializeField] private Text textColorButtonLobby;
    [SerializeField] public Button buttonLaps;

    [Header("Game Over Menu")] [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private Text finalPositions;
    [SerializeField] private Text finalTotalTime;
    [SerializeField] private Text finalBestLap;
    [SerializeField] private Button buttonRestart;

    [Header("Server HUD")] [SerializeField] private GameObject serverHUD;
    [SerializeField] private Button buttonExitServer;
    [SerializeField] private Button buttonMainMenuServer;
    [SerializeField] public Button buttonLapsSer;

    [Header("Abandonmet Victory")] [SerializeField] private GameObject abandonmetVictory;
    [SerializeField] private Button buttonExitAbandonment;
    [SerializeField] private Button buttonMainMenuAbandonment;

    [Header("Full or started race")] [SerializeField] private GameObject fullGame;
    [SerializeField] private Button buttonExitFullGame;
    [SerializeField] private Button buttonMainMenuFullGame;

    [Header("Server Disconnected")] [SerializeField] private GameObject serverDisconnected;
    [SerializeField] private Button buttonExitServerOut;
    [SerializeField] private Button buttonMainMenuServerOut;

    [Header("End Server")] [SerializeField] private GameObject gameEnd;
    [SerializeField] private Button buttonEndExit;
    [SerializeField] private Button buttonEndRestart;
    #endregion

    #region Unity Callbacks
    /* Awake: se encarga de inicializar la referencia del NetworkManager
     */ 
    private void Awake()
    {
        m_NetworkManager = FindObjectOfType<NetworkManager>();
    }

    /* Start: establece el evento que realizará cada botón al ser pulsado 
     * y activará el HUD del menú principal
     */ 
    private void Start()
    {
        buttonHost.onClick.AddListener(() => StartHost());
        buttonClient.onClick.AddListener(() => StartClient());
        buttonServer.onClick.AddListener(() => StartServer());
        buttonColor.onClick.AddListener(() => SelectColor());
        buttonReady.onClick.AddListener(() => StartRace());
        buttonRestart.onClick.AddListener(() => RestartGame());
        buttonInGameMainMenu.onClick.AddListener(() => RestartGame());
        buttonQuitGame.onClick.AddListener(() => ExitGame());
        buttonMainMenuQuit.onClick.AddListener(() => ExitGameMainMenu());
        buttonExitServer.onClick.AddListener(() => ExitGame());
        buttonMainMenuServer.onClick.AddListener(() => RestartGame());
        buttonExitAbandonment.onClick.AddListener(() => ExitGame());
        buttonMainMenuAbandonment.onClick.AddListener(() => RestartGame());
        buttonExitFullGame.onClick.AddListener(() => ExitGame());
        buttonMainMenuFullGame.onClick.AddListener(() => RestartGame());
        buttonExitServerOut.onClick.AddListener(() => ExitGame());
        buttonMainMenuServerOut.onClick.AddListener(() => RestartGame());
        buttonColorLobby.onClick.AddListener(() => SelectColorLobby());
        buttonEndExit.onClick.AddListener(() => ExitGame());
        buttonEndRestart.onClick.AddListener(() => RestartGame());
        buttonLaps.onClick.AddListener(() => SetLaps());
        buttonLapsSer.onClick.AddListener(() => SetLapsServer());
        ActivateMainMenu();

    }

    #endregion

    #region Methods

    /* ExitGame: método que para el cliente o host (dependiendo de que quien lo llame),
     * detruye el objeto NetworkManager y cierra la aplicación
     */ 
    public void ExitGame()
    {
        m_polePositionManager.EndGame();
        Destroy(m_NetworkManager.gameObject);
        Application.Quit();
    }

    /* ExitGameMainMenu: método que cierra la aplicación. Se diferencia del método ExitGame en que 
     * este no ha generado aún un NetworkManager ni se ha conectado de ninguna forma, por tanto,
     * no puede parar la conexión ni destruir el NetworkManager.
     */ 
    private void ExitGameMainMenu()
    {
        Application.Quit();
    }

    /* RestartGame: método que reiniciará la escena volviendo así al menú principal, haciendo 
     * previamente las mismas llamadas y acciones que el método ExitGame.
     */ 
    private void RestartGame()
    {
        m_polePositionManager.EndGame();
        Destroy(m_NetworkManager.gameObject);
        SceneManager.LoadScene("Game");
    }

    /* StartRace: cambia el color de Ready a verde cuando es pulsado por el usuario y llama al método 
     * StartRace del PolePositionManager que tramitará cuantos usuarios han dado a Ready.
     */ 
    private void StartRace()
    {
        buttonReady.GetComponent<Image>().color = Color.green;
        buttonReady.onClick.RemoveAllListeners();
        m_polePositionManager.StartRace();
    }

    /* UpdateSpeed: actualiza por pantalla la velocidad del jugador.
     */ 
    public void UpdateSpeed(int speed)
    {
        textSpeed.text = "Speed " + speed + " Km/h";
    }

    /* UpdateLap: actualiza por pantalla la vuelta actual del jugador.
     */ 
    public void UpdateLap(int lap)
    {
        textLaps.text = "Lap " + lap + "/" + m_polePositionManager.m_SetUpPlayer.m_PlayerController.numVueltas;
    }

    /* UpdateNames: actualiza el nombre de los jugadores en carrera según su posición.
     */ 
    public void UpdateNames(string name)
    {
        textPosition.text = name;
    }

    /* UpdateRanking: actualiza los nombres de los jugadores del HUD de fin de carrera según
     * su posición final.
     */ 
    public void UpdateRanking(string name)
    {
        finalPositions.text = name;
    }

    /* UpdateLapTimeRanking: actualiza el texto del tiempo de la mejor vuelta de todos los jugadores que se
     * mostrará en la ventana de fin de carrera
     */ 
    public void UpdateLapTimeRanking(string name)
    {
        finalBestLap.text += name + "\n";
    }

    /* UpdateTotalTimeRanking: actualiza el texto del tiempo total de todos los jugadores que se
     * mostrará en la ventana de fin de carrera
     */
    public void UpdateTotalTimeRanking(string name)
    {
        finalTotalTime.text += name + "\n";
    }

    /* UpdateTotalTime: actualiza el texto del tiempo total del jugador durante la carrera.
     */ 
    public void UpdateTotalTime(string name)
    {
        textTotalTime.text = "Total time: " + name;
    }

    /* UpdateLapTime: actualliza el texto del tiempo de la vuetla actual del jugador durante la carrera.
     */ 
    public void UpdateLapTime(string name)
    {
        textLapTime.text = "Current lap: " + name;
    }

    /* SetWrongWay: activa una imagen en el HUD durante la carrera si el jugador esta realizando un mal recorrido.
     */ 
    public void SetWrongWay(bool state)
    {
        wrongWay.gameObject.SetActive(state);
    }

    /* ActivateMainMenu: activa el HUD del menú principal desactivando el resto de HUDs
     * 
     */ 
    private void ActivateMainMenu()
    {
        mainMenu.SetActive(true);
        inGameHUD.SetActive(false);
        readyMenu.SetActive(false);
        serverHUD.SetActive(false);
        serverDisconnected.SetActive(false);
        fullGame.SetActive(false);
        abandonmetVictory.SetActive(false);
        gameEnd.SetActive(false);
    }

    /* ActivateInGameHUD: activca el HUD que aparecerá durante toda la carrera y el HUD donde los
     * jugadores deben seleccionar que están listos antes de empezar la carrera. También desactiva el HUD
     * del menu principal.
     */ 
    private void ActivateInGameHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(true);
        readyMenu.SetActive(true);
    }

    /* ActivateGameOver: desactiva todos los posibles HUDs activos y activa el HUD de fin de carrera una vez el 
     * jugador haya finalizado todas la vueltas.
     */ 
    public void ActivateGameOver()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(false);
        readyMenu.SetActive(false);
        gameOverMenu.SetActive(true);
    }

    /* ActivateFullGame: desactiva todos los posibles HUDs activos y activa un HUD que avisa al jugador que no puede
     * conectarse a la partida porque esta ya ha empezado o hay ya 4 jugadores en dicha partida.
     */
    public void ActivateFullGameHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(false);
        readyMenu.SetActive(false);
        fullGame.SetActive(true);
    }

    /* ActivateServerOutHUD: desactiva todos los posibles HUDs activos y muestra un HUD que avisa a los jugadores que el 
     * server se ha desconectado.
     */
    public void ActivateServerOutHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(false);
        readyMenu.SetActive(false);
        fullGame.SetActive(false);
        abandonmetVictory.SetActive(false);
        serverDisconnected.SetActive(true);
    }

    /* StartHost: una vez pulsado el botón de host, el jugador abrirá una partida con el como servidor y jugador y se llamará
     * al método que activa el HUD de la carrera. Además guarda el nombre introducido en el cuadro de texto encargado de ello.
     * Al ser Host también podrá elegir el número de vueltas.
     */ 
    private void StartHost()
    {
        m_NetworkManager.StartHost();
        ActivateInGameHUD();
        buttonLaps.gameObject.SetActive(true);
        playerName = inputFieldName.text;
    }

    /* StartClient: una vez pulsado el botón de Client, el jugador entrará en localhost si no ha introducido ninguna Ip en el 
     * cuadro de texto encargado de ello. Si ha introducido alguna Ip se conectará a esa partida guardando también el nombre
     * introducido en el cuadro de texto del nombre. AL ser solo un cliente no podrá elegir el número de vueltas.
     */ 
    private void StartClient()
    {
        buttonClient.onClick.RemoveAllListeners();
        m_NetworkManager.networkAddress = (inputFieldIP.text != "") ? inputFieldIP.text : "localhost";
        m_NetworkManager.StartClient();
        playerName = inputFieldName.text;
        ActivateInGameHUD();
        buttonLaps.gameObject.SetActive(false);
    }

    /* StartServer: Abrirá un servidor al que podrán unirse los jugadores y activará también un HUD diferente a los demás donde
     * elegirá el número de vueltas y podrá desconectarse.
     */ 
    private void StartServer()
    {
        m_NetworkManager.StartServer();
        ActivateServerHUD();
    }

    /* ActivateServerHUD: activa el HUD del server.
     */ 
    private void ActivateServerHUD()
    {
        mainMenu.SetActive(false);
        serverHUD.SetActive(true);
    }

    /* deactivateReadyMenu: desactiva el HUD en el que los jugadores deben pulsar a Ready una vez hayan ya pulsado todos los conectados
     * (siempre siendo más de 2 jugadores conectados para que empiece la carrera).
     * 
     */ 
    public void deactivateReadyMenu()
    {
        readyMenu.SetActive(false);
    }

    /* ActivateEndingByAbandonment: desactiva los posibles HUDs activos y activa un HUD que avisa al jugador que ha ganado por abandono
     * simpre y cuando no se haya quedado solo después de haber ya terminado la carrera.
     */ 
    public void ActivateEndingByAbandonment()
    {
        inGameHUD.SetActive(false);
        readyMenu.SetActive(false);
        if (!gameOverMenu.activeSelf)
        {
            abandonmetVictory.SetActive(true);
        }
    }

    /* ActivateEndServer: mostrará un HUD en el Servidor que le avisará que la carrera ha finalizado tras haberse ido todos los jugadores durante 
     * o después de haber finalizado esta. Desconectará también al servidor y deberá volver a reiniciarse yendo al menú principal si quieres abrir
     * otra partida.
     */ 
    public void ActiveEndServer()
    {
        serverHUD.SetActive(false);
        m_polePositionManager.EndGame();
        gameEnd.SetActive(true);

    }

    /* SelectColor: mostrará en el botón del menú principal el color elegido del usuario y cambiará cada vez que el jugador 
     * pulse avisandole así del color elegido. Guardará el valor elegido en la variable colorNumber.
     */ 
    private void SelectColor()
    {
        colorNumber = (colorNumber + 1) % 4;
        switch (colorNumber)
        {
            case 0:
                textColorButton.text = "COLOR: RED";
                break;
            case 1:
                textColorButton.text = "COLOR: GREEN";
                break;
            case 2:
                textColorButton.text = "COLOR: ORANGE";
                break;
            case 3:
                textColorButton.text = "COLOR: WHITE";
                break;
        }
        textColorButtonLobby.text = textColorButton.text;
    }

    /* SelectColorLobby: mostrará el color elegido en el botón del HUD donde los jugadores dan a Ready y chatean con los demás jugadores. 
     * Tendría el mismo comportamiento que el botón del menú principal. Es añadido aquí por si el usuario decide cambiar el color elegido tras
     * chatear con el resto de corredores.
     */
    private void SelectColorLobby()
    {
        colorNumber = (colorNumber + 1) % 4;
        switch (colorNumber)
        {
            case 0:
                textColorButtonLobby.text = "COLOR: RED";
                break;
            case 1:
                textColorButtonLobby.text = "COLOR: GREEN";
                break;
            case 2:
                textColorButtonLobby.text = "COLOR: ORANGE";
                break;
            case 3:
                textColorButtonLobby.text = "COLOR: WHITE";
                break;
        }
        m_polePositionManager.m_SetUpPlayer.CmdSelectColor(colorNumber);

    }

    /* InitLaps: inicializa el valor del texto del botón de vueltas (visualizado solo por el server o host) y del panel que informa 
     * de las vueltas
     */ 
    public void InitLaps()
    {
        buttonLaps.GetComponentInChildren<Text>().text = "LAPS: 3";
        textLaps.text = "Lap 0/" + m_polePositionManager.numVueltas;
    }

    /* SetLaps: método que cambia el número de vueltas de la partida elegido por el Host. Posteriormente enviará esa información
     * al resto de clientes.
     */ 
    void SetLaps()
    {
        laps = (laps + 1) % 3;
        switch (laps)
        {
            case 0:
                buttonLaps.GetComponentInChildren<Text>().text = "LAPS: 3";
                m_polePositionManager.m_SetUpPlayer.m_PlayerController.numVueltas = 3;
                m_polePositionManager.numVueltas = 3;
                textLaps.text = "Lap 0/3";
                break;
            case 1:
                buttonLaps.GetComponentInChildren<Text>().text = "LAPS: 4";
                m_polePositionManager.m_SetUpPlayer.m_PlayerController.numVueltas = 4;
                m_polePositionManager.numVueltas = 4;
                textLaps.text = "Lap 0/4";
                break;
            case 2:
                buttonLaps.GetComponentInChildren<Text>().text = "LAPS: 5";
                m_polePositionManager.m_SetUpPlayer.m_PlayerController.numVueltas = 5;
                m_polePositionManager.numVueltas = 5;
                textLaps.text = "Lap 0/5";
                break;

        }
        m_polePositionManager.SetNumLaps(laps);

    }

    /* SetLapsServer: método que cambia el número de vueltas de la partida elegido por el Server Only. Posteriormente enviará esa información
     * al resto de clientes.
     */
    void SetLapsServer()
    {
        if (m_polePositionManager.started)
        {
            buttonLapsSer.onClick.RemoveAllListeners();
        }
        else
        {
            laps = (laps + 1) % 3;
            switch (laps)
            {
                case 0:
                    buttonLapsSer.GetComponentInChildren<Text>().text = "LAPS: 3";
                    break;
                case 1:
                    buttonLapsSer.GetComponentInChildren<Text>().text = "LAPS: 4";
                    break;
                case 2:
                    buttonLapsSer.GetComponentInChildren<Text>().text = "LAPS: 5";
                    break;

            }
            m_polePositionManager.SetNumLaps(laps);
        }
    }

    #endregion

}