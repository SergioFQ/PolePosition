using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Ready Menu")] [SerializeField] private GameObject readyMenu; //facilitamos la visualización del inspector poniendo este header
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

    private void Awake()
    {
        m_NetworkManager = FindObjectOfType<NetworkManager>();
    }

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
    public void ExitGame()
    {
        m_polePositionManager.EndGame();
        Destroy(m_NetworkManager.gameObject);
        Application.Quit();
    }

    private void ExitGameMainMenu()
    {
        Application.Quit();
    }

    private void RestartGame()
    {
        m_polePositionManager.EndGame();
        Destroy(m_NetworkManager.gameObject);
        SceneManager.LoadScene("Game");
    }
    private void StartRace()
    {
        buttonReady.GetComponent<Image>().color = Color.green;
        buttonReady.onClick.RemoveAllListeners();
        m_polePositionManager.startRace();
    }

    public void UpdateSpeed(int speed)
    {
        textSpeed.text = "Speed " + speed + " Km/h";
    }

    public void UpdateLap(int lap)
    {
        textLaps.text = "Lap " + lap + "/" + m_polePositionManager.m_SetUpPlayer.m_PlayerController.numVueltas;
    }
    public void UpdateNames(string name)
    {
        textPosition.text = name;
    }
    public void UpdateRanking(string name)
    {
        finalPositions.text = name;
    }
    public void UpdateLapTimeRanking(string name)
    {
        finalBestLap.text += name + "\n";
    }
    public void UpdateTotalTimeRanking(string name)
    {
        finalTotalTime.text += name + "\n";
    }

    public void UpdateTotalTime(string name)
    {
        textTotalTime.text = "Total time: " + name;
    }
    public void UpdateLapTime(string name)
    {
        textLapTime.text = "Current lap: " + name;
    }
    public void SetWrongWay(bool state)
    {
        wrongWay.gameObject.SetActive(state);
    }

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

    private void ActivateInGameHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(true);
        readyMenu.SetActive(true);
    }

    public void ActivateGameOver()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(false);
        readyMenu.SetActive(false);
        gameOverMenu.SetActive(true);
        Debug.Log("GameOver");
    }

    public void ActivateFullGameHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(false);
        readyMenu.SetActive(false);
        fullGame.SetActive(true);
        Debug.Log("GameOver");
    }

    public void ActivateServerOutHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(false);
        readyMenu.SetActive(false);
        fullGame.SetActive(false);
        serverDisconnected.SetActive(true);
        Debug.Log("GameOver");
    }

    private void StartHost()
    {
        m_NetworkManager.StartHost();
        ActivateInGameHUD();
        playerName = inputFieldName.text;
    }

    private void StartClient()
    {
        buttonClient.onClick.RemoveAllListeners();
        m_NetworkManager.networkAddress = (inputFieldIP.text != "") ? inputFieldIP.text : "localhost";
        m_NetworkManager.StartClient();
        playerName = inputFieldName.text;
        ActivateInGameHUD();
    }

    private void StartServer()
    {
        m_NetworkManager.StartServer();
        ActivateServerHUD();
    }

    private void ActivateServerHUD()
    {
        mainMenu.SetActive(false);
        serverHUD.SetActive(true);
    }

    public void deactivateReadyMenu()
    {
        readyMenu.SetActive(false);
    }
    public void ActivateEndingByAbandonment()
    {
        inGameHUD.SetActive(false);
        readyMenu.SetActive(false);
        if (!gameOverMenu.activeSelf)
        {
            abandonmetVictory.SetActive(true);
        }
    }

    public void ActiveEndServer()
    {
        serverHUD.SetActive(false);
        m_polePositionManager.EndGame();
        gameEnd.SetActive(true);

    }
    //cambiamos el color del coche dando click al botón de color las veces que sean necesarias hasta ver que aparece el texto del color deseado. Los colores son (ROJO, VERDE, AMARILLO Y BLANCO)
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

    public void InitLaps()
    {
        buttonLaps.GetComponentInChildren<Text>().text = "LAPS: 3";
        textLaps.text = "Lap 0/" + m_polePositionManager.numVueltas;
    }

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

    void SetLapsServer()
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

    #endregion

}