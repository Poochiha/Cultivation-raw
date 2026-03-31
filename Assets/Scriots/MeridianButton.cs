using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MeridianButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Refs")]
    public Game game;
    public int index; 

    public GameObject infoPanel;

    [Header("Panel Texts")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI multText;
    public TextMeshProUGUI costText;

    [Header("Long press")]
    public float holdToShowSeconds = 0.25f;

    private bool isDown;
    private float downTime;
    private bool panelShown;
    private bool pointerExited;

    private void Awake()
    {
        if (infoPanel != null) infoPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isDown || panelShown || pointerExited) return;

        if (Time.unscaledTime - downTime >= holdToShowSeconds)
            ShowPanel();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDown = true;
        pointerExited = false;
        panelShown = false;
        downTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDown) return;

        if (panelShown) HidePanel();
        else TapAction();

        isDown = false;
        pointerExited = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerExited = true;
        if (panelShown) HidePanel();
        isDown = false;
    }

    private void TapAction()
    {
        if (game == null) return;

        if (!game.opened[index])
            game.TryOpenMeridian(index);
        else
            game.TryUpgradeMeridian(index);
    }

    private void ShowPanel()
    {
        if (infoPanel == null || game == null) return;

        panelShown = true;

        bool isOpened = game.opened[index];
        int lvl = game.levels[index];
        int cap = game.GetCurrentLevelCap();
        double mult = game.GetMeridianMultiplier(index);

        if (titleText != null) titleText.text = $"Meridian {index + 1}";
        if (levelText != null) levelText.text = isOpened ? $"Level: {lvl}/{cap}" : "Level: -";
        if (multText != null) multText.text = isOpened ? $"Multiplier: x{mult:0.###}" : "Multiplier: -";

        if (costText != null)
        {
            if (!isOpened)
                costText.text = $"Open: {game.Format(game.GetOpenCost(index))} Qi";
            else if (lvl >= cap)
                costText.text = $"MAX ({cap})";
            else
                costText.text = $"Upgrade: {game.Format(game.GetUpgradeCost(index))} Qi";
        }

        infoPanel.SetActive(true);
    }

    private void HidePanel()
    {
        if (infoPanel != null) infoPanel.SetActive(false);
        panelShown = false;
    }
}
