using System;
using System.Collections.Generic;
using System.Linq;
using WordSearch;
using UnityEngine;
using System.Collections;

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

    private const int _w = 6;
    private const int _h = 6;

    private Color _dark = new Color(0xC7 / 255f, 0xFD / 255f, 0xCA / 255f, 0f);
    private Color _light = new Color(0xC7 / 255f, 0xFD / 255f, 0xCA / 255f, 1f);
    private Color _highlight = new Color(1, 1, 1, 1);

    private bool _isActive;
    private TextMesh[] _textMeshes;
    private LetterState[] _states;
    private int? _selectedLetter;
    private bool _isSolved;

    private string _solution;
    private char[] _field;

    void Start()
    {
        Debug.Log("[Word Search] Started");

        _isActive = false;
        _textMeshes = new TextMesh[_w * _h];
        _states = new LetterState[_w * _h];
        _selectedLetter = null;
        _isSolved = false;

        for (int i = 0; i < MainSelectable.Children.Length; i++)
        {
            _textMeshes[i] = MainSelectable.Children[i].GetComponent<TextMesh>();
            _textMeshes[i].text = "";
            _textMeshes[i].color = _dark;
            _states[i] = LetterState.Invisible;

            var j = i;
            MainSelectable.Children[i].OnInteract = delegate
            {
                Debug.LogFormat("[Word Search] Pushed button #{0}. _isActive={1}, _isSolved={2}", j, _isActive, _isSolved);
                MainSelectable.Children[j].AddInteractionPunch();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, MainSelectable.Children[j].transform);
                if (_isActive && !_isSolved)
                    HandleLetter(j);
                return false;
            };
        }

        Module.OnActivate = ActivateModule;
    }

    void ActivateModule()
    {
        _isActive = true;
        Debug.Log("[Word Search] Activated");

        _field = new char[_w * _h];
        //_field[0] = (char) ('A' + Rnd.Range(0, 26));
        //_field[_w - 1] = (char) ('A' + Rnd.Range(0, 26));
        //_field[_w * (_h - 1)] = (char) ('A' + Rnd.Range(0, 26));
        //_field[_w * _h - 1] = (char) ('A' + Rnd.Range(0, 26));
        for (int i = 0; i < _w * _h; i++)
        {
            _field[i] = (char) ('A' + Rnd.Range(0, 26));
            _textMeshes[i].text = _field[i].ToString();
            TransitionLetter(i, LetterState.Visible, 25, Rnd.Range(.5f, 1.5f));
        }

        _solution = "ABC";
    }

    private void TransitionLetter(int ix, LetterState newState, int steps, float? delay = null)
    {
        if (_states[ix] == newState)
            return;
        StartCoroutine(TransitionLetter(ix, _states[ix], newState, delay, steps));
        _states[ix] = newState;
    }

    private Color ColorFromLetterState(LetterState state)
    {
        switch (state)
        {
            case LetterState.Visible: return _light;
            case LetterState.Highlighted: return _highlight;
        }
        return _dark;
    }

    private IEnumerator TransitionLetter(int ix, LetterState prevState, LetterState newState, float? delay, int steps)
    {
        if (delay != null)
            yield return new WaitForSeconds(delay.Value);

        for (int i = 0; i <= steps; i++)
        {
            _textMeshes[ix].color = Color.Lerp(ColorFromLetterState(prevState), ColorFromLetterState(newState), Math.Max(0, Math.Min(1, i / (float) steps + Rnd.Range(-.25f, .25f))));
            yield return null;
        }
        _textMeshes[ix].color = ColorFromLetterState(newState);
    }

    private void HandleLetter(int ix)
    {
        if (_selectedLetter == ix)
        {
            _selectedLetter = null;
            TransitionLetter(ix, LetterState.Visible, 5);
            Audio.PlaySoundAtTransform("Off1", MainSelectable.transform);
        }
        else
        {
            if (_selectedLetter != null)
            {
                string logMessage = null;
                int[] ixs;
                try
                {
                    if (isCorrect(_selectedLetter.Value, ix, out logMessage, out ixs))
                    {
                        Audio.PlaySoundAtTransform("On2", MainSelectable.transform);
                        Module.HandlePass();
                        _isSolved = true;
                        foreach (var i in ixs)
                            TransitionLetter(i, LetterState.Highlighted, 5);
                    }
                    else
                    {
                        Audio.PlaySoundAtTransform("Off2", MainSelectable.transform);
                        Module.HandleStrike();
                        TransitionLetter(_selectedLetter.Value, LetterState.Visible, 5);
                    }
                }
                finally
                {
                    if (logMessage != null)
                        Debug.Log("[Word Search] " + logMessage);
                }
                _selectedLetter = null;
            }
            else
            {
                _selectedLetter = ix;
                TransitionLetter(ix, LetterState.Highlighted, 10);
                Audio.PlaySoundAtTransform("On1", MainSelectable.transform);
            }
        }
    }

    private bool isCorrect(int startLetter, int endLetter, out string logMessage, out int[] indexes)
    {
        indexes = null;

        var startX = startLetter % _w;
        var startY = startLetter / _w;
        var endX = endLetter % _w;
        var endY = endLetter / _w;

        logMessage = string.Format("Coordinates clicked: {0},{1} → {2},{3}.", startX, startY, endX, endY);
        if (Math.Abs(startX - endX) != 0 && Math.Abs(startY - endY) != 0 && Math.Abs(startX - endX) != Math.Abs(startY - endY))
        {
            logMessage += " Not horizontal, vertical or diagonal.";
            return false;
        }
        var dx = Math.Sign(endX - startX);
        var dy = Math.Sign(endY - startY);

        indexes = new int[Math.Max(Math.Abs(endX - startX) + 1, Math.Abs(endY - startY) + 1)];
        var word = "";
        int x = startX, y = startY, i = 0;
        for (; x != endX || y != endY; x += dx, y += dy)
        {
            word += _field[y * _w + x];
            indexes[i++] = y * _w + x;
        }
        word += _field[y * _w + x];
        indexes[i] = y * _w + x;

        logMessage += " Word selected: " + word;

        if (word.Length == 3)
        {
            logMessage += " Correct.";
            return true;
        }

        logMessage += " Wrong.";
        return false;
    }
}
