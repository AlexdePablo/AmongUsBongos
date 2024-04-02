using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static PrimerGame;

[CreateAssetMenu(fileName = "MisionesHechas", menuName = "ScriptableObject/MisionesHechas")]
public class MisionesHechas : ScriptableObject
{
    public Misioncitas[] m_MisionesHechas;

    public void ReiniciarMisiones()
    {
        for (int i = 0; i < m_MisionesHechas.Length; i++)
        {
            m_MisionesHechas[i].misionHecha = false;
        }
    }

    public void CheckMision(int id)
    {
        for (int i = 0; i < m_MisionesHechas.Length; i++)
        {
            if(id == i)
                m_MisionesHechas[i].misionHecha = true;
        }
    }

    [Serializable]
    public struct Misioncitas : INetworkSerializable
    {
        public int idMision;
        public bool misionHecha;

        public Misioncitas(int _idMision, bool _misionHecha)
        {
            idMision = _idMision;
            misionHecha = _misionHecha;
        }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref idMision);
            serializer.SerializeValue(ref misionHecha);
        }
    }
}
