using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "PrimerGame", menuName = "ScriptableObject/PrimerGame")]
public class PrimerGame : ScriptableObject
{
    public bool primerGeim;
    public ListaColores ListaColorines;

    public void EverythingAvailable()
    {
        primerGeim = true;
        for(int i = 0; i < ListaColorines.m_Colores.Length; i++)
        {
            ListaColorines.m_Colores[i].m_ColorDisponible = true;
        }
    }
    public void SetPrimerGueim(bool game)
    {
        primerGeim = game;
    }

    [Serializable]
    public struct ListaColores : INetworkSerializable
    {
        public Colores[] m_Colores;

        public ListaColores(Colores[] _Colores)
        {
            m_Colores = _Colores;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref m_Colores);
        }
    }

    [Serializable]
    public struct Colores : INetworkSerializable
    {
        public Color m_ColorPersonaje;
        public bool m_ColorDisponible;

        public Colores(Color _ColorPersonaje, bool _ColorDisponible)
        {
            m_ColorPersonaje = _ColorPersonaje;
            m_ColorDisponible = _ColorDisponible;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref m_ColorPersonaje);
            serializer.SerializeValue(ref m_ColorDisponible);
        }
    }

}
