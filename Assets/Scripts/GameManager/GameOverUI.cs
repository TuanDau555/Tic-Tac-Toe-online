using TMPro;
using UnityEngine;

public class GameOverUI : Singleton<GameOverUI>
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject gameNotify;

    void Start()
    {
        Hide();
    }

    public void ShowGameOver(string gameOverText, Color textColor)
    {
        resultText.text = gameOverText;
        resultText.color = textColor;
        Show();
    }

    private void Show()
    {
        gameNotify.SetActive(true);
    }
    public void Hide()
    {
        gameNotify.SetActive(false);
    }
}
