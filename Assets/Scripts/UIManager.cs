using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public bool showGUI = true;
    private PolePositionManager m_polePositionManager;
    private NetworkManager m_NetworkManager;

    [Header("Main Menu")] [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonServer;
    [SerializeField] private InputField inputFieldIP;
    [SerializeField] private Button buttonColor;
    [SerializeField] private InputField inputFieldName;
    public int colorNumber = 0;
    public string playerName = "";

    [Header("In-Game HUD")] [SerializeField]
    private GameObject inGameHUD;

    [SerializeField] private Text textSpeed;
    [SerializeField] private Text textLaps;
    [SerializeField] private Text textPosition;
    [SerializeField] private Text textColor;
    [SerializeField] public RawImage wrongWay;

    [Header("Ready Menu")] [SerializeField] private GameObject readyMenu; //facilitamos la visualización del inspector poniendo este header
    [SerializeField] private Button buttonReady;

    
    private void Awake()
    {
        m_NetworkManager = FindObjectOfType<NetworkManager>();
        m_polePositionManager = FindObjectOfType<PolePositionManager>();
    }

    private void Start()
    {
        buttonHost.onClick.AddListener(() => StartHost());
        buttonClient.onClick.AddListener(() => StartClient());
        buttonServer.onClick.AddListener(() => StartServer());
        buttonColor.onClick.AddListener(() => SelectColor());
        buttonReady.onClick.AddListener(() => StartRace());
        ActivateMainMenu();
        textLaps.text = "Lap 0/5";
    }

    private void StartRace()
    {

        buttonReady.GetComponent<Image>().color = Color.green;
        m_polePositionManager.StartRace();
    }

    public void UpdateSpeed(int speed)
    {
        textSpeed.text = "Speed " + speed + " Km/h";
    }

    public void UpdateLap(int lap)
    {
        textLaps.text = "Lap " + lap + "/5";
    }
    public void UpdateNames(string name)
    {
        textPosition.text = name;
    }

    private void ActivateMainMenu()
    {
        mainMenu.SetActive(true);
        inGameHUD.SetActive(false);
        readyMenu.SetActive(false);
    }

    private void ActivateInGameHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(true);
        readyMenu.SetActive(true);
    }

    private void StartHost()
    {
        m_NetworkManager.StartHost();
        ActivateInGameHUD();
        playerName = inputFieldName.text;
    }

    private void StartClient()
    {
        m_NetworkManager.StartClient();
        m_NetworkManager.networkAddress = inputFieldIP.text;
        playerName = inputFieldName.text;
        ActivateInGameHUD();
    }

    private void StartServer()
    {
        m_NetworkManager.StartServer();
        ActivateInGameHUD();
    }

    public void deactivateReadyMenu()
    {
        readyMenu.SetActive(false);
    }

    //cambiamos el color del coche dando click al botón de color las veces que sean necesarias hasta ver que aparece el texto del color deseado. Los colores son (ROJO, VERDE, AMARILLO Y BLANCO)
    private void SelectColor()
    {
        Text text = buttonColor.GetComponentInChildren<Text>();
        colorNumber++;
        if (colorNumber >= 4) { colorNumber = 0; }
        switch (colorNumber)
        {
            case 0:
                text.text = "COLOR: RED";
                break;
            case 1:
                text.text = "COLOR: GREEN";
                break;
            case 2:
                text.text = "COLOR: ORANGE";
                break;
            case 3:
                text.text = "COLOR: WHITE";
                break;
        }
    }
}