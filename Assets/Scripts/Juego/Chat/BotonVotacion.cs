using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class BotonVotacion : MonoBehaviour
{
    private ulong m_PlayerID;
    public ulong PlayerID => m_PlayerID;
    private ChatManager m_ChatManager;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(SumarVotacion);
        m_ChatManager = GameObject.Find("ChatManager").GetComponent<ChatManager>();
    }

    private void SumarVotacion()
    {
        m_ChatManager.AumentarVotacionPersonajeRpc(m_PlayerID, NetworkManager.Singleton.LocalClientId);
    }

    internal void setId(ulong _PlayerId)
    {
        m_PlayerID = _PlayerId;
    }
}
