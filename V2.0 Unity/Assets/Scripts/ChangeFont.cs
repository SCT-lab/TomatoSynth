using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChangeFont : MonoBehaviour
{
    public bool defaultFont = true;
    public TMP_FontAsset[] newFont;

    public void Start()
    {
        //dropDown.onValueChanged.AddListener(FontChanger);
    }

    public void FontChanger()
    {
        if (defaultFont) { defaultFont = false;}
        else { defaultFont = true;}

        if(!defaultFont)
        {
            int num = 1;
            TMP_FontAsset selectedFont = newFont[num];

            TextMeshProUGUI[] allText = FindObjectsOfType<TextMeshProUGUI>(true);
            Debug.Log(allText.Length);

            for(int i = 0; i < allText.Length; i++)
            {
                allText[i].font = newFont[num];
                allText[i].fontMaterial = selectedFont.material;
            }
        }
        else
        {
            int num = 0;
            TMP_FontAsset selectedFont = newFont[num];

            TextMeshProUGUI[] allText = FindObjectsOfType<TextMeshProUGUI>(true);
            Debug.Log(allText.Length);

            for(int i = 0; i < allText.Length; i++)
            {
                allText[i].font = newFont[num];
                allText[i].fontMaterial = selectedFont.material;
            }
        }
    }
}
