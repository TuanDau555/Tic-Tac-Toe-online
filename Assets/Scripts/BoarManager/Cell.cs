using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This script is attach to each Button
/// </summary> <summary>
public class Cell : MonoBehaviour
{
    public int row;
    public int column;
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void OnEnable()
    {
        button.onClick.AddListener(OnClick);
    }

    void OnDisable()
    {
        button.onClick.RemoveListener(OnClick);
    }

    public void OnClick()
    {         
        if (BoardManager.Instance != null && GameManager.Instance.IsMyTurn())
        {
            BoardManager.Instance.SetPlayerSpace(row, column);
            Debug.Log($"Cell clicked at row: {row}, column: {column}");
        }
    }

    public void SetSprite(Sprite sprite)
    {
        button.image.sprite = sprite;
        button.image.enabled = true;
        button.interactable = false; // Disable the button after setting the sprite
    }

    public void ResetCell()
    {
        button.interactable = true;
        button.image.sprite = null;
    }
}