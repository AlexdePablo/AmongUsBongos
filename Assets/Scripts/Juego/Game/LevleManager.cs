using LobbyPckg;
using m17;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevleManager : NetworkBehaviour
{
    [Header("Sus GUI")]
    [SerializeField] private GameObject m_SusPanel;
    [SerializeField] private Button m_MatarButton;
    [Header("Tripulante GUI")]
    [SerializeField] private GameObject m_TripulantePanel;
    [SerializeField] private Button m_MisionButton;
    [Header("Global GUI")]
    [SerializeField] private Button m_ReportarButton;
    [SerializeField] private TextMeshProUGUI m_MisionesHechasTexto;
    [Header("Misiones")]
    [SerializeField] private MisionesHechas m_MisionesHechas;

    private PlayerBehaviour m_Jugadorcin;

    private NetworkVariable<int> m_MisionesActuales = new NetworkVariable<int>(0);
    private int misionesTotales;
    public override void OnNetworkSpawn()
    {
        m_Jugadorcin = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<PlayerBehaviour>();

        if (NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<PlayerBehaviour>().GetSus())
        {
            m_SusPanel.SetActive(true);
            m_TripulantePanel.SetActive(false);
            if (m_MatarButton) m_MatarButton.onClick.AddListener(MatarJugador);
        }
        else
        {
            m_SusPanel.SetActive(false);
            m_TripulantePanel.SetActive(true);
            if (m_MisionButton) m_MisionButton.onClick.AddListener(AbrirMision);
        }
        if (m_ReportarButton) m_ReportarButton.onClick.AddListener(ReportarCadaver);
        m_MisionesActuales.OnValueChanged += MisionHecha;
        if (NetworkManager.Singleton.IsHost)
            MisionesTotalesRpc();
    }

    private void MatarJugador()
    {
        MatarJugadorRpc(m_Jugadorcin.KillPlayer());
    }
    [Rpc(SendTo.Server)]
    private void MatarJugadorRpc(ulong id)
    {
        foreach (NetworkClient Player in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (Player.ClientId == id)
            {
                Player.PlayerObject.GetComponent<PlayerBehaviour>().SetVivo(false);
            }
        }
    }

    private void AbrirMision()
    {
        m_Jugadorcin.HacerMision();
    }

    private void ReportarCadaver()
    {
        m_Jugadorcin.ReportarCadaver();
    }

    public void SetMatarButtonInteract(bool interact)
    {
        m_MatarButton.interactable = interact;
    }
    public void SetReportarButtonInteract(bool interact)
    {
        m_ReportarButton.interactable = interact;
    }
    public void SetMisionButtonInteract(bool interact)
    {
        m_MisionButton.interactable = interact;
    }

    [Rpc(SendTo.Server)]
    private void MisionesTotalesRpc()
    {
        int num = NetworkManager.Singleton.ConnectedClients.Count * 3;
        num -= 3;


        SendClientMisionesTotalesClientRpc(num,
                       new ClientRpcParams
                       {
                           Send = new ClientRpcSendParams
                           {
                               TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds
                           }
                       });
    }

    [Rpc(SendTo.Server)]
    public void RestarMisionesRpc(int numMisiones)
    {
        int num = 0;
        if (NetworkManager.Singleton)
        {
            num = NetworkManager.Singleton.ConnectedClients.Count * 3;
            num -= 6;
        }

        m_MisionesActuales.Value -= numMisiones;

        SendClientMisionesTotalesClientRpc(num,
                       new ClientRpcParams
                       {
                           Send = new ClientRpcSendParams
                           {
                               TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds
                           }
                       });
    }
    [Rpc(SendTo.Server)]
    public void SumarMisionesRpc(int numMisiones)
    {
        m_MisionesActuales.Value += numMisiones;

    }
    [ClientRpc]
    private void SendClientMisionesTotalesClientRpc(int num, ClientRpcParams clientRpcParams = default)
    {
        misionesTotales = num;
        m_MisionesHechasTexto.text = m_MisionesActuales.Value + "/" + misionesTotales;
    }

    private void MisionHecha(int previousValue, int newValue)
    {
        m_MisionesHechasTexto.text = m_MisionesActuales.Value + "/" + misionesTotales;
        if (m_MisionesActuales.Value == misionesTotales && NetworkManager.Singleton.IsHost)
            NetworkManager.Singleton.SceneManager.LoadScene("LobbyIp", LoadSceneMode.Single);
    }

    public void ComprobarMisionesFinales()
    {
        if (m_MisionesActuales.Value == misionesTotales && NetworkManager.Singleton.IsHost)
            NetworkManager.Singleton.SceneManager.LoadScene("LobbyIp", LoadSceneMode.Single);
    }
    public void MissionCompletada(int idMinijuego, ulong idPlayer)
    {
        GameManager.Singleton.SubirMisionRpc(idPlayer);
        MissionCompletadaRpc();
        CompletarMision(idMinijuego);
    }

    [Rpc(SendTo.Server)]
    private void MissionCompletadaRpc()
    {
        m_MisionesActuales.Value++;
    }


    private void CompletarMision(int id)
    {
        for (int i = 0; i < m_MisionesHechas.m_MisionesHechas.Length; i++)
        {
            if (id == m_MisionesHechas.m_MisionesHechas[i].idMision)
            {
                m_MisionesHechas.m_MisionesHechas[i].misionHecha = true;
            }
        }
    }

    public Color EstaMiMisionEstaHecha(int id)
    {
        Color colorin = Color.white;
        for(int i = 0; i < m_MisionesHechas.m_MisionesHechas.Length; i++)
        {
            if (id == m_MisionesHechas.m_MisionesHechas[i].idMision) 
            {
                if (m_MisionesHechas.m_MisionesHechas[i].misionHecha)
                    colorin = Color.green;
                else
                    colorin = Color.red;
            }
        }
        return colorin;
    }
}
