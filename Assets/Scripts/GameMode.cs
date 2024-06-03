using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameMode : MonoBehaviour
{
    [SerializeField] public PlayerController player;
    [SerializeField] public Plane plane;
    [SerializeField] public InputField inputField;
    [SerializeField] public int heuristicIndex = 0;
    [SerializeField] public Text searched;
    private string input;
    private static GameMode instance = null;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public static GameMode Instance
    {
        get
        {
            if (instance == null)
            {
                return null;
            }
            return instance;
        }
    }

    public void OnSummit()
    {
        string input = inputField.text;
        if (0 < int.Parse(input))
        {
            plane.MakeGrid(int.Parse(input));
        }
    }

    public void OnClickMan()
    {
        heuristicIndex = 0;
    }

    public void OnClickEuclid()
    {
        heuristicIndex = 1;
    }

    public void OnClickDijk()
    {
        heuristicIndex = 2;
    }

    public void SetSearched(int count)
    {
        searched.text = "Searched " + count.ToString();
    }

}
