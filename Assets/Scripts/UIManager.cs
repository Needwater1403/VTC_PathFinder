using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI txt_guide;

    [Header("Panel")] 
    [SerializeField] private GameObject pnl_menu;
    
    [Header("Button")] 
    [SerializeField] private Button btn_start;
    [SerializeField] private Button btn_quit;
    [SerializeField] private Button btn_return;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        btn_start.onClick.AddListener(OnClickStart);
        btn_quit.onClick.AddListener(OnClickQuit);
        btn_return.onClick.AddListener(OnClickReturn);
    }

    public void SetText(string content)
    {
        txt_guide.SetText(content);
    }

    private void OnClickStart()
    {
        pnl_menu.SetActive(false);
        AstarPath.Instance.maze.StartGame();
        UIManager.Instance.SetText("Press <color=red>A</color> to randomize start & end position");
    }
    
    private static void OnClickReturn()
    {
        SceneManager.LoadScene(sceneBuildIndex: 0);
    }

    private void OnClickQuit()
    {
        Application.Quit();
    }
}
