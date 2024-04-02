using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PrimerMinijuego : NetworkBehaviour
{
    [SerializeField] private GameObject m_MiniJuegoPanel;
    [SerializeField] private Button m_RotarDerechaButton;
    [SerializeField] private Button m_RotarIzquierdaButton;
    [SerializeField] private Image m_Image;
    [SerializeField] private int idMinijuego;
    private SpriteRenderer m_SpriteRenderer;
    private LevleManager m_LevelManager;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        if (m_RotarDerechaButton) m_RotarDerechaButton.onClick.AddListener(RotarDerechaImagen);
        if (m_RotarIzquierdaButton) m_RotarIzquierdaButton.onClick.AddListener(RotarIzquierdaImagen);
        m_LevelManager = GameObject.Find("LevelManager").GetComponent<LevleManager>();
        m_MiniJuegoPanel.SetActive(false);
        m_SpriteRenderer.color = m_LevelManager.EstaMiMisionEstaHecha(idMinijuego);
    }
    private void RotarDerechaImagen()
    {
        m_Image.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, 1 * -10));
        ComprobarImagen();
    }

    private void ComprobarImagen()
    {
        if ((int)m_Image.GetComponent<RectTransform>().localEulerAngles.z <= 5 || (int)m_Image.GetComponent<RectTransform>().localEulerAngles.z >= 355)
        {
            m_Image.color = Color.green;
            StartCoroutine(AcabarMision());
        }
    }

    private IEnumerator AcabarMision()
    {
        yield return new WaitForSeconds(2);
        m_LevelManager.MissionCompletada(idMinijuego, NetworkManager.Singleton.LocalClientId);
        m_MiniJuegoPanel.SetActive(false);
        m_SpriteRenderer.color = Color.green;
        m_Image.color = Color.white;
    }

    private void RotarIzquierdaImagen()
    {
        m_Image.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, 1 * 10));
        ComprobarImagen();
    }



    public void AbrirPanel()
    {
        m_MiniJuegoPanel.SetActive(true);
        DarRotacionInicialImagen();
    }


    private void DarRotacionInicialImagen()
    {
        int num = UnityEngine.Random.Range(-18, 18);
        if (num == 0)
        {
            DarRotacionInicialImagen();
        }
        else
        {
            m_Image.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, num * 10));
        }
    }

    public void SetColor(Color color)
    {
        m_SpriteRenderer.color = color;
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
