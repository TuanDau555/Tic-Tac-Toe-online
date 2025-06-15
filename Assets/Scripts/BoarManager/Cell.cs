using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// This script is attach to each Button
/// </summary> <summary>

public class Cell : MonoBehaviour
{
    [SerializeField] private int row;
    [SerializeField] private int column;
    [SerializeField] private Button button;

    public void OnClick()
    {
        BoardManager.Instance.SetPlayerSpace(button, row, column); 
    }

}