using System;
using System.Collections.Generic;
using System.Linq;
using WordSearch;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Word Search
/// Created by Timwi
/// </summary>
public class WordSearchModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;

    public KMSelectable MainSelectable;
    public Transform Screen;
    public Material MatText;
    public Font Font;
    public Mesh Highlight;

    private bool _isActive;

    void Start()
    {
        Debug.Log("[Word Search] Started");
        _isActive = false;

        for (int i = 0; i < MainSelectable.Children.Length; i++)
        {
            var j = i;
            MainSelectable.Children[i].OnInteract = delegate
            {
                HandleLetter(j);
                return false;
            };
        }

        Module.OnActivate = ActivateModule;

        // ** Code to generate objects

        //var text = "..A.....U.....THE...H.....OF....RZ..";
        //var selectables = new List<KMSelectable>();
        //for (int y = 0; y < 6; y++)
        //    for (int x = 0; x < 6; x++)
        //    {
        //        var t = new GameObject { name = string.Format("Text {0}, {1}", x, y) };
        //        t.transform.parent = Screen;
        //        t.transform.localPosition = new Vector3(.32f * (x - 2.5f), .001f, .32f * (2.5f - y));
        //        t.transform.localEulerAngles = new Vector3(90, 0, 0);
        //        t.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        //        var tm = t.AddComponent<TextMesh>();
        //        var ch = text[y * 6 + x];
        //        tm.text = (ch == '.' ? (char) Rnd.Range('A', 'A' + 26) : ch).ToString();
        //        tm.anchor = TextAnchor.MiddleCenter;
        //        tm.font = Font;
        //        tm.fontSize = 56;
        //        var mr = t.GetComponent<MeshRenderer>();
        //        mr.material = MatText;

        //        var h = new GameObject { name = string.Format("Highlight {0}, {1}", x, y) };
        //        h.transform.parent = t.transform;
        //        h.transform.localPosition = new Vector3(0, 0, 0);
        //        h.transform.localEulerAngles = new Vector3(-90, 0, 0);
        //        h.transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);
        //        var mf = h.AddComponent<MeshFilter>();
        //        mf.mesh = Highlight;
        //        var kmh = h.AddComponent<KMHighlightable>();
        //        kmh.HighlightScale = new Vector3(1, 1, 1);

        //        var sel = t.AddComponent<KMSelectable>();
        //        sel.Highlight = kmh;
        //        sel.Parent = MainSelectable;

        //        selectables.Add(sel);
        //    }

        //MainSelectable.Children = selectables.ToArray();
        //MainSelectable.UpdateChildren();
    }

    private void HandleLetter(int j)
    {
        if (_isActive)
            Debug.LogFormat("[Word Search] Pushed button #{0}.", j);
    }

    void ActivateModule()
    {
        _isActive = true;
        Debug.Log("[Word Search] Activated");
    }
}
