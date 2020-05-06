using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WordSearch;

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
    public Mesh PlaneMesh;
    public Texture[] Images;

    private const int _w = 6;
    private const int _h = 6;

    private Color _dark = new Color(0 / 255f, 0xFF / 255f, 0 / 255f, 0f);
    private Color _light = new Color(0 / 255f, 0xFF / 255f, 0 / 255f, 1f);
    private Color _highlight = new Color(1, 1, 1, 1);

    private bool _isActive;
    private TextMesh[] _textMeshes;
    private LetterState[] _states;
    private int? _selectedLetter;
    private bool _isSolved;

    private string _solution;
    private int _solutionStart; // Used by TP autosolver only
    private int _solutionEnd; // Used by TP autosolver only
    private char[] _field;

    private string[][] _chartWords =
        "/;HOTEL/DONE;SEARCH/QUEBEC;ADD/CHECK;SIERRA/FIND;FINISH/EAST;/;PORT/COLOR;BOOM/SUBMIT;LINE/BLUE;KABOOM/ECHO;PANIC/FALSE;MANUAL/ALARM;DECOY/CALL;SEE/TWENTY;INDIA/NORTH;NUMBER/LOOK;ZULU/GREEN;VICTOR/XRAY;DELTA/YES;HELP/LOCATE;ROMEO/BEEP;TRUE/EXPERT;MIKE/EDGE;FOUND/RED;BOMBS/WORD;WORK/UNIQUE;TEST/JINX;GOLF/LETTER;TALK/SIX;BRAVO/SERIAL;SEVEN/TIMER;MODULE/SPELL;LIST/TANGO;YANKEE/SOLVE;/;CHART/OSCAR;MATH/NEXT;READ/LISTEN;LIMA/FOUR;COUNT/OFFICE;/"
            .Split(';')
            .Select(pairStr => pairStr.Split('/'))
            .ToArray();

    // Italian version; currently unused
    private string[][] _chartWords_IT =
        "/;IVREA/FATTO;CERCA/PISA;MOLTO/VEDI;SIERRA/TROVA;FINITO/EST;/;PORTA/COLORE;AFA/INVIA;LINEA/BLU;ACQUA/ECO;PANICO/FALSO;MANO/IOTA;ESCA/CHIAMA;FESTA/VENTI;LASSO/NORD;PLACCA/ARPA;ZULU/ERESIA;MAREA/GAS;ORCO/OGNI;AIUTO/PIANO;ROMEO/PIOTTA;VERO/ASSO;LUCA/BORDO;ICONA/ROSSO;BOMBA/PARLA;ETANO/UNICO;TEST/POLO;GOLF/LETTO;PARA/SEI;BRAVO/SERIE;SETTE/SUOLA;MODO/FANGO;LISTA/TANGO;GRINGO/VIVAI;/;DETTO/OSCAR;MATTO/NESSO;LEGGO/MARIO;LIMA/SOTTO;VERDE/GIALLO;/"
            .Split(';')
            .Select(pairStr => pairStr.Split('/'))
            .ToArray();

    private string _chartLetters = ".VUSZ..PQNXFY.TIMEDA.KBWHJO..RLCG..";

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

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
            MainSelectable.Children[i].OnInteract = delegate { HandleLetter(j); return false; };
        }

        Module.OnActivate = ActivateModule;
    }

    void ActivateModule()
    {
        _isActive = true;
        var serial = Bomb.GetSerialNumber();
        var serialOdd = serial == null ? Rnd.Range(0, 2) == 0 : "13579".Contains(Bomb.GetSerialNumber().Last());
        Debug.LogFormat("[Word Search #{1}] Activated; serial number is {0}", serialOdd ? "odd" : "even", _moduleId);

        tryAgain:

        _field = new char[_w * _h];
        _field[0] = (char) ('A' + Rnd.Range(0, 26));
        _field[_w - 1] = (char) ('A' + Rnd.Range(0, 26));
        _field[_w * (_h - 1)] = (char) ('A' + Rnd.Range(0, 26));
        _field[_w * _h - 1] = (char) ('A' + Rnd.Range(0, 26));

        var wrongWords = Ut.NewArray(
            _chartWords[_chartLetters.IndexOf(_field[0]) + 8][serialOdd ? 1 : 0],
            _chartWords[_chartLetters.IndexOf(_field[_w - 1]) + 7][serialOdd ? 1 : 0],
            _chartWords[_chartLetters.IndexOf(_field[_w * (_h - 1)]) + 1][serialOdd ? 1 : 0],
            _chartWords[_chartLetters.IndexOf(_field[_w * _h - 1])][serialOdd ? 1 : 0]
        ).Distinct().ToList();

        var correctIndex = Rnd.Range(0, wrongWords.Count);
        _solution = wrongWords[correctIndex];
        Debug.LogFormat("[Word Search #{1}] Correct word is {0}", _solution, _moduleId);

        var decoyWords = _chartWords.SelectMany(w => w).Where(w => w.Length > 0).Except(wrongWords).ToList().Shuffle();
        wrongWords.RemoveAt(correctIndex);

        Debug.LogFormat("[Word Search #{1}] Wrong words are {0}", wrongWords.OrderBy(w => w).JoinString(", "), _moduleId);

        var coords = Enumerable.Range(0, _w * _h).ToList();
        var directions = new[] { WordDirection.Down, WordDirection.DownRight, WordDirection.Right, WordDirection.UpRight, WordDirection.Up, WordDirection.UpLeft, WordDirection.Left, WordDirection.DownLeft };
        var indexes = Enumerable.Range(0, _solution.Length).ToList().Shuffle();

        coords.Shuffle();
        foreach (var coord in coords)
            foreach (var dir in directions.Shuffle())
                if (TryPlaceWord(_solution, 0, coord % _w, coord / _w, dir))
                {
                    _solutionStart = coord;
                    var dx = new[] { 0, 1, 1, 1, 0, -1, -1, -1 };
                    var dy = new[] { 1, 1, 0, -1, -1, -1, 0, 1 };
                    _solutionEnd = coord + dx[(int) dir] * (_solution.Length - 1) + 6 * dy[(int) dir] * (_solution.Length - 1);
                    goto initialPlaced;
                }

        Debug.LogFormat("[Word Search #{0}] Fatal: could not place initial word.", _moduleId);
        Module.HandlePass();
        return;

        initialPlaced:;

        while (decoyWords.Count > 0)
        {
            var ix = decoyWords.Count - 1;
            var decoy = decoyWords[ix];
            decoyWords.RemoveAt(ix);

            coords.Shuffle();

            for (int c = 0; c < coords.Count; c++)
            {
                var ch = _field[coords[c]];
                if (ch == '\0')
                    continue;
                indexes.Clear();
                for (int i = 0; i < decoy.Length; i++)
                    if (decoy[i] == ch)
                        indexes.Add(i);
                indexes.Shuffle();

                for (int i = 0; i < indexes.Count; i++)
                {
                    var x = coords[c] % _w;
                    var y = coords[c] / _w;
                    foreach (var dir in directions.Shuffle())
                        if (TryPlaceWord(decoy, i, x, y, dir))
                            goto decoyPlaced;
                }
            }

            decoyPlaced:;
        }

        for (int i = 0; i < _w * _h; i++)
            if (_field[i] == '\0')
                _field[i] = (char) ('A' + Rnd.Range(0, 26));

        // Make sure that the field doesn’t by chance contain one of the wrong words
        foreach (var wrong in wrongWords)
            foreach (var coord in coords)
                foreach (var dir in directions)
                    if (TryPlaceWord(wrong, 0, coord % _w, coord / _w, dir))
                    {
                        Debug.LogFormat("[Word Search #{4}] Wrong word {0} happens to come up in grid at {1},{2},{3}. Restarting.", wrong, coord % _w, coord / _w, dir, _moduleId);
                        goto tryAgain;
                    }

        Debug.LogFormat("[Word Search #{1}] Field:{0}", _field.Select((ch, i) => (i % _w == 0 ? "\n" : null) + (ch == '\0' ? '?' : ch)).JoinString(), _moduleId);

        for (int i = 0; i < _w * _h; i++)
        {
            _textMeshes[i].text = _field[i].ToString();
            TransitionLetter(i, LetterState.Visible, 25, Rnd.Range(.5f, 1.5f));
        }
    }

    private bool TryPlaceWord(string word, int i, int x, int y, WordDirection dir)
    {
        switch (dir)
        {
            case WordDirection.Down:
                if (y - i >= 0 && y + (word.Length - 1) - i < _h)
                {
                    for (int j = 0; j < word.Length; j++)
                        if (_field[x + _w * (y - i + j)] != '\0' && _field[x + _w * (y - i + j)] != word[j])
                            return false;
                    for (int j = 0; j < word.Length; j++)
                        _field[x + _w * (y - i + j)] = word[j];
                    return true;
                }
                break;

            case WordDirection.DownRight:
                if (y - i >= 0 && y + (word.Length - 1) - i < _h && x - i >= 0 && x + (word.Length - 1) - i < _w)
                {
                    for (int j = 0; j < word.Length; j++)
                        if (_field[(x - i + j) + _w * (y - i + j)] != '\0' && _field[(x - i + j) + _w * (y - i + j)] != word[j])
                            return false;
                    for (int j = 0; j < word.Length; j++)
                        _field[(x - i + j) + _w * (y - i + j)] = word[j];
                    return true;
                }
                break;

            case WordDirection.Right:
                if (x - i >= 0 && x + (word.Length - 1) - i < _w)
                {
                    for (int j = 0; j < word.Length; j++)
                        if (_field[(x - i + j) + _w * y] != '\0' && _field[(x - i + j) + _w * y] != word[j])
                            return false;
                    for (int j = 0; j < word.Length; j++)
                        _field[(x - i + j) + _w * y] = word[j];
                    return true;
                }
                break;

            case WordDirection.UpRight:
                if (y + i < _h && y - (word.Length - 1) + i >= 0 && x - i >= 0 && x + (word.Length - 1) - i < _w)
                {
                    for (int j = 0; j < word.Length; j++)
                        if (_field[(x - i + j) + _w * (y + i - j)] != '\0' && _field[(x - i + j) + _w * (y + i - j)] != word[j])
                            return false;
                    for (int j = 0; j < word.Length; j++)
                        _field[(x - i + j) + _w * (y + i - j)] = word[j];
                    return true;
                }
                break;

            case WordDirection.Up:
                if (y + i < _h && y - (word.Length - 1) + i >= 0)
                {
                    for (int j = 0; j < word.Length; j++)
                        if (_field[x + _w * (y + i - j)] != '\0' && _field[x + _w * (y + i - j)] != word[j])
                            return false;
                    for (int j = 0; j < word.Length; j++)
                        _field[x + _w * (y + i - j)] = word[j];
                    return true;
                }
                break;

            case WordDirection.UpLeft:
                if (y + i < _h && y - (word.Length - 1) + i >= 0 && x + i < _w && x - (word.Length - 1) + i >= 0)
                {
                    for (int j = 0; j < word.Length; j++)
                        if (_field[(x + i - j) + _w * (y + i - j)] != '\0' && _field[(x + i - j) + _w * (y + i - j)] != word[j])
                            return false;
                    for (int j = 0; j < word.Length; j++)
                        _field[(x + i - j) + _w * (y + i - j)] = word[j];
                    return true;
                }
                break;

            case WordDirection.Left:
                if (x + i < _w && x - (word.Length - 1) + i >= 0)
                {
                    for (int j = 0; j < word.Length; j++)
                        if (_field[(x + i - j) + _w * y] != '\0' && _field[(x + i - j) + _w * y] != word[j])
                            return false;
                    for (int j = 0; j < word.Length; j++)
                        _field[(x + i - j) + _w * y] = word[j];
                    return true;
                }
                break;

            case WordDirection.DownLeft:
                if (y - i >= 0 && y + (word.Length - 1) - i < _h && x + i < _w && x - (word.Length - 1) + i >= 0)
                {
                    for (int j = 0; j < word.Length; j++)
                        if (_field[(x + i - j) + _w * (y - i + j)] != '\0' && _field[(x + i - j) + _w * (y - i + j)] != word[j])
                            return false;
                    for (int j = 0; j < word.Length; j++)
                        _field[(x + i - j) + _w * (y - i + j)] = word[j];
                    return true;
                }
                break;
        }
        return false;
    }

    private HashSet<int> _coroutinesActive = new HashSet<int>();

    private void TransitionLetter(int ix, LetterState newState, int steps, float? delay = null)
    {
        if (_states[ix] == newState)
            return;
        if (_coroutinesActive.Add(ix))
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

        restart:

        for (int i = 0; i <= steps; i++)
        {
            _textMeshes[ix].color = Color.Lerp(ColorFromLetterState(prevState), ColorFromLetterState(newState), Math.Max(0, Math.Min(1, i / (float) steps + Rnd.Range(-.25f, .25f))));
            yield return null;
        }
        _textMeshes[ix].color = ColorFromLetterState(newState);

        if (_states[ix] != newState)
        {
            prevState = newState;
            newState = _states[ix];
            goto restart;
        }

        _coroutinesActive.Remove(ix);
    }

    private bool? HandleLetter(int ix)
    {
        MainSelectable.Children[ix].AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, MainSelectable.Children[ix].transform);
        if (!_isActive || _isSolved)
            return null;

        Debug.LogFormat("[Word Search #{0}] Pushed button at ({1}, {2}).", _moduleId, ix % 6 + 1, ix / 6 + 1);

        bool? ret = null;

        if (_selectedLetter == ix)
        {
            _selectedLetter = null;
            TransitionLetter(ix, LetterState.Visible, 1);
            Audio.PlaySoundAtTransform("Off1", MainSelectable.transform);
        }
        else
        {
            if (_selectedLetter != null)
            {
                string logMessage;
                int[] ixs;
                if (isCorrect(_selectedLetter.Value, ix, out logMessage, out ixs))
                {
                    Audio.PlaySoundAtTransform("On2", MainSelectable.transform);
                    _isSolved = true;
                    foreach (var i in ixs)
                        TransitionLetter(i, LetterState.Highlighted, 10);
                    Invoke("doPass", .5f);
                    ret = true;
                }
                else
                {
                    Audio.PlaySoundAtTransform("Off2", MainSelectable.transform);
                    Invoke("giveStrike", .5f);
                    TransitionLetter(_selectedLetter.Value, LetterState.Visible, 10);
                    ret = false;
                }
                Debug.LogFormat("[Word Search #{0}] {1}", _moduleId, logMessage);
                _selectedLetter = null;
            }
            else
            {
                _selectedLetter = ix;
                TransitionLetter(ix, LetterState.Highlighted, 1);
                Audio.PlaySoundAtTransform("On1", MainSelectable.transform);
            }
        }
        return ret;
    }

    private void giveStrike()
    {
        Module.HandleStrike();
    }

    private void doPass()
    {
        Module.HandlePass();
    }

    private bool isCorrect(int startLetter, int endLetter, out string logMessage, out int[] indexes)
    {
        indexes = null;

        var startX = startLetter % _w;
        var startY = startLetter / _w;
        var endX = endLetter % _w;
        var endY = endLetter / _w;

        logMessage = string.Format("Coordinates clicked: ({0}, {1}) → ({2}, {3}).", startX + 1, startY + 1, endX + 1, endY + 1);
        if (Math.Abs(startX - endX) != 0 && Math.Abs(startY - endY) != 0 && Math.Abs(startX - endX) != Math.Abs(startY - endY))
        {
            logMessage += " Not horizontal, vertical or diagonal.";
            return false;
        }
        var dx = Math.Sign(endX - startX);
        var dy = Math.Sign(endY - startY);

        var wordLength = Math.Max(Math.Abs(endX - startX) + 1, Math.Abs(endY - startY) + 1);
        indexes = new int[wordLength];
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

        if (word != _solution)
        {
            logMessage += " Wrong.";
            return false;
        }

        logMessage += " Correct.";
        CreateGraphic("CorrectHighlight", new Vector3((startX + endX) / 2f - 2.5f, 0.002f, -(startY + endY) / 2f + 2.5f) * .32f, (startX == endX || startY == endY ? "str" : "diag") + wordLength,
            length: wordLength * (startX == endX || startY == endY ? 1 : Math.Sqrt(2)),
            rotation: startX == endX ? 90 : startY == endY ? 0 : startX > endX ^ startY > endY ? -45 : 45);
        return true;
    }

    private void CreateGraphic(string name, Vector3 position, string imgName, double length, int rotation)
    {
        Debug.LogFormat("name = {0}, imgName = {1}", name, imgName);
        var go = new GameObject { name = name };
        go.transform.parent = Screen;
        go.transform.localPosition = position;
        go.transform.localEulerAngles = new Vector3(90, rotation, 0);
        go.transform.localScale = new Vector3((float) (.32 * length), .32f, .32f);
        go.AddComponent<MeshFilter>().mesh = PlaneMesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.material.mainTexture = Images.First(im => im.name == imgName);
        mr.material.shader = Shader.Find("Unlit/Transparent");
    }

#pragma warning disable 414
    private string TwitchHelpMessage = @"Select the word starting at column B row 3, and ending at column E row 6, with “!{0} select B3 E6”.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        var pieces = command.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if ((pieces.Length != 3 && pieces.Length != 4 && pieces.Length != 5) || pieces[0] != "select")
            yield break;
        if (pieces.Length == 4 && pieces[2] != "to")
            yield break;
        if (pieces.Length == 5 && (pieces[1] != "from" || pieces[3] != "to"))
            yield break;

        var indexes = new int[2];
        for (int i = 0; i < 2; i++)
        {
            var str = i == 0 ? pieces[pieces.Length == 5 ? 2 : 1] : pieces[pieces.Length - 1];
            if (str.Length == 2 && str[0] >= 'a' && str[0] <= 'f' && str[1] >= '1' && str[1] <= '6')
                indexes[i] = (str[0] - 'a') + 6 * (str[1] - '1');
            else if (str.Length == 3 && str[0] >= '1' && str[0] <= '6' && str[1] == ',' && str[2] >= '1' && str[2] <= '6')
                indexes[i] = (str[0] - '1') + 6 * (str[2] - '1');
            else
                yield break;
        }

        yield return null;

        foreach (var index in indexes)
        {
            // Returns true for “solved”, false for “strike”, and null for neither
            var result = HandleLetter(index);

            if (result == true)
            {
                yield return "solve";
                yield break;
            }
            else if (result == false)
            {
                yield return "strike";
                yield break;
            }
            yield return new WaitForSeconds(.25f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (_selectedLetter != null)
        {
            MainSelectable.Children[_selectedLetter.Value].OnInteract();
            yield return new WaitForSeconds(.5f);
        }

        MainSelectable.Children[_solutionStart].OnInteract();
        yield return new WaitForSeconds(.5f);
        MainSelectable.Children[_solutionEnd].OnInteract();
    }
}
