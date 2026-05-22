using UnityEngine;
using UnityEngine.UI;

public sealed class HeartUI : MonoBehaviour
{
    [SerializeField] private Text heartText;
    [SerializeField] private Text gameOverText;
    [SerializeField] private string fullHeart = "<3";
    [SerializeField] private string emptyHeart = "--";

    public static HeartUI CreateRuntime()
    {
        GameObject canvasObject = new GameObject("Runtime_HeartCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject heartsObject = new GameObject("HeartText");
        heartsObject.transform.SetParent(canvasObject.transform, false);
        Text hearts = heartsObject.AddComponent<Text>();
        hearts.font = GetBuiltinFont();
        hearts.fontSize = 42;
        hearts.fontStyle = FontStyle.Bold;
        hearts.color = new Color(1f, 0.12f, 0.12f);
        hearts.alignment = TextAnchor.UpperLeft;

        RectTransform heartsRect = hearts.GetComponent<RectTransform>();
        heartsRect.anchorMin = new Vector2(0f, 1f);
        heartsRect.anchorMax = new Vector2(0f, 1f);
        heartsRect.pivot = new Vector2(0f, 1f);
        heartsRect.anchoredPosition = new Vector2(24f, -20f);
        heartsRect.sizeDelta = new Vector2(360f, 70f);

        GameObject gameOverObject = new GameObject("GameOverText");
        gameOverObject.transform.SetParent(canvasObject.transform, false);
        Text gameOver = gameOverObject.AddComponent<Text>();
        gameOver.font = GetBuiltinFont();
        gameOver.fontSize = 72;
        gameOver.fontStyle = FontStyle.Bold;
        gameOver.color = Color.white;
        gameOver.alignment = TextAnchor.MiddleCenter;
        gameOver.text = "GAME OVER";
        gameOver.enabled = false;

        RectTransform gameOverRect = gameOver.GetComponent<RectTransform>();
        gameOverRect.anchorMin = new Vector2(0.5f, 0.5f);
        gameOverRect.anchorMax = new Vector2(0.5f, 0.5f);
        gameOverRect.pivot = new Vector2(0.5f, 0.5f);
        gameOverRect.anchoredPosition = Vector2.zero;
        gameOverRect.sizeDelta = new Vector2(600f, 140f);

        HeartUI ui = canvasObject.AddComponent<HeartUI>();
        ui.heartText = hearts;
        ui.gameOverText = gameOver;
        return ui;
    }

    public void SetHearts(int current, int max)
    {
        if (heartText == null)
        {
            return;
        }

        string value = string.Empty;
        for (int i = 0; i < max; i++)
        {
            value += i < current ? fullHeart : emptyHeart;
            if (i < max - 1)
            {
                value += " ";
            }
        }

        heartText.text = value;
    }

    public void ShowGameOver()
    {
        if (gameOverText != null)
        {
            gameOverText.enabled = true;
        }
    }

    private static Font GetBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
