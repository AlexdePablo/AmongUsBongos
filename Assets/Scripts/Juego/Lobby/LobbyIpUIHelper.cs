using LobbyPckg;
using m17;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;



public class LobbyIpUIHelper : NetworkBehaviour
{
    [Header("Scriptables")]
    [SerializeField]
    private MisionesHechas misionesHechas;

    [Header("UI")]
    [SerializeField] private Button m_AnteriorColorButton;
    [SerializeField] private Button m_PosteriorColorButton;
    [SerializeField] private Image m_Color;
    [SerializeField] private Button m_CambiarColorButton;
    [SerializeField] private Button _StartGameButton;
    [SerializeField] private PrimerGame m_PrimerGame;

    private int m_NumerinActualColor;
    private int m_Numerin;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        misionesHechas.ReiniciarMisiones();
        if (NetworkManager.Singleton && NetworkManager.Singleton.IsHost)
        {
            if (_StartGameButton) StartCoroutine(MirarJugadores());
            _StartGameButton.onClick.AddListener(StartGame);
            NetworkManager.Singleton.OnClientConnectedCallback += BotonStartGameInteractivo;
            NetworkManager.Singleton.OnClientDisconnectCallback += BotonStartGameInteractivo;
            
        }
        else
        {
            if (_StartGameButton) _StartGameButton.gameObject.SetActive(false);
        }
        if (m_AnteriorColorButton) m_AnteriorColorButton.onClick.AddListener(AnteriorColor);
        if (m_PosteriorColorButton) m_PosteriorColorButton.onClick.AddListener(SiguienteColor);
        if (m_CambiarColorButton) m_CambiarColorButton.onClick.AddListener(CambiarColorPersonaje);
        if (m_PrimerGame.primerGeim)
        {
            m_PrimerGame.primerGeim = false;
            GameManager.Singleton.DarColorinInicialRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    public void SetPlayerColor(Color nuevoColorPlayer, NetworkClient player, int numeroAnterior, int numeroActual)
    {
        GameManager.Singleton.ChangeRpc(numeroAnterior, numeroActual);
        player.PlayerObject.GetComponent<PlayerBehaviour>().CambiarColorPersonaje(nuevoColorPlayer);
    }
    

    private void BotonStartGameInteractivo(ulong obj)
    {
        StartCoroutine(MirarJugadores());
    }

    private IEnumerator MirarJugadores()
    {
        yield return new WaitForEndOfFrame();
        if (NetworkManager.Singleton.ConnectedClientsList.Count > 3)
            _StartGameButton.interactable = true;
        else
            _StartGameButton.interactable = false;
    }

    void StartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("GameIp", UnityEngine.SceneManagement.LoadSceneMode.Single);
        int num = UnityEngine.Random.Range(0, NetworkManager.Singleton.ConnectedClientsList.Count);
        int numa = 0;
        foreach (var player in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (player.PlayerObject != null)
            {
                if (numa == num)
                {
                    player.PlayerObject.GetComponentInChildren<PlayerBehaviour>().SetSus(true);
                }
                else
                {
                    player.PlayerObject.GetComponentInChildren<PlayerBehaviour>().SetSus(false);
                }
                numa++;
            }

        }
    }

    private void CambiarColorPersonaje()
    {
        SetPlayerColor(m_Color.color, NetworkManager.Singleton.LocalClient, m_Numerin, m_NumerinActualColor);
        m_CambiarColorButton.interactable = false;
        m_Numerin = m_NumerinActualColor;
    }

    private void AnteriorColor()
    {
        m_NumerinActualColor--;
        if (m_NumerinActualColor < 0)
            m_NumerinActualColor = 9;

        GameManager.Singleton.DarColorinRpc(NetworkManager.Singleton.LocalClientId, m_NumerinActualColor);
    }
    private void SiguienteColor()
    {
        m_NumerinActualColor++;
        if (m_NumerinActualColor > 9)
            m_NumerinActualColor = 0;

        GameManager.Singleton.DarColorinRpc(NetworkManager.Singleton.LocalClientId, m_NumerinActualColor);
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.Singleton.OnClientDisconnectCallback -= BotonStartGameInteractivo;
        NetworkManager.Singleton.OnClientConnectedCallback -= BotonStartGameInteractivo;
    }

    internal void SendColor(Color m_ColorPersonaje, int numerin)
    {
        m_Color.color = m_ColorPersonaje;
        m_NumerinActualColor = numerin;
        m_Numerin = m_NumerinActualColor;
        CambiarColorPersonaje();
    }

    internal void SendColor(Color m_ColorPersonaje, bool colorDsiponible)
    {
        m_Color.color = m_ColorPersonaje;
        m_CambiarColorButton.interactable = colorDsiponible;
    }
}
