using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class ScrabbleScrambleScript : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBossModule BossModule;

    public KMSelectable submitButton;
    public KMSelectable[] tileSelectables;
    public GameObject[] tiles;
    public GameObject inputDisp;
    public GameObject tilesParent;
    public TextMesh timerText;

    private Vector3[] tilePositions = new Vector3[6];
    private Vector3[] inputPositions = new Vector3[] { new Vector3(-0.625f, 0, 0), new Vector3(0.625f, 0, 0) };

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    bool isAnimating;
    bool lightsOn;

    float startingTime;
    int modCount;
    public static string[] ignoredModules = null;
    float threshold = 0.50f;

    int activations = 0;
    float timeElapsed = 0f;
    private Coroutine timerTick;
    bool goingIntoSolve = false;

    States state = States.Initial;
    bool satisfied = true;
    bool boardClear = true;

    List<string> words = new List<string> { "AA", "AB", "AD", "AE", "AG", "AH", "AI", "AL", "AM", "AN", "AR", "AS", "AT", "AW", "AX", "AY", "BA", "BE", "BI", "BO", "BY", "DA", "DE", "DO", "ED", "EF", "EH", "EL", "EM", "EN", "ER", "ES", "ET", "EW", "EX", "FA", "FE", "GI", "GO", "HA", "HE", "HI", "HM", "HO", "ID", "IF", "IN", "IS", "IT", "JO", "KA", "KI", "LA", "LI", "LO", "MA", "ME", "MI", "MM", "MO", "MU", "MY", "NA", "NE", "NO", "NU", "OD", "OE", "OF", "OH", "OI", "OK", "OM", "ON", "OP", "OR", "OS", "OW", "OX", "OY", "PA", "PE", "PI", "PO", "QI", "RE", "SH", "SI", "SO", "TA", "TE", "TI", "TO", "UH", "UM", "UN", "UP", "US", "UT", "WE", "WO", "XI", "XU", "YA", "YE", "YO", "ZA", "CH", "DI", "EA", "EE", "FY", "GU", "IO", "JA", "KO", "KY", "NY", "OB", "OO", "OU", "ST", "UG", "UR", "YU", "ZE", "ZO" };
    int[] letterValues = new int[] { 1, 3, 3, 2, 1, 4, 2, 4, 1, 8, 5, 1, 3, 1, 1, 3, 10, 1, 1, 1, 1, 4, 4, 8, 4, 10 };
    int currentScore = 0;
    List<string> submittedWords = new List<string>();

    char[] input = new char[] { '-', '-' };
    int[] selectedTiles = new int[] { -1, -1 };
    char[] availableTiles = new char[6];

    bool showingBigs = false;
    int submission = 0;

    char[][] numberSets = new char[][] { "01234↓".ToCharArray(), "56789↑".ToCharArray() };

    void Awake()
    {
        moduleId = moduleIdCounter++;
        for (int i = 0; i < 6; i++)
        {
            int x = i;
            tileSelectables[x].OnInteract += delegate () { TilePress(x); return false; };
            tilePositions[x] = tileSelectables[x].transform.localPosition;
            tiles[x].SetActive(false);
        }
        submitButton.OnInteract += delegate () { Submit(); return false; };
        ignoredModules = ignoredModules ?? BossModule.GetIgnoredModules("Scrabble Scramble", new string[] { "14", "42", "501", "A>N<D", "Bamboozling Time Keeper", "Black Arrows", "Brainf---", "Busy Beaver", "Don't Touch Anything", "Floor Lights", "Forget Any Color", "Forget Enigma", "Forget Everything", "Forget Infinity", "Forget It Not", "Forget Maze Not", "Forget Me Later", "Forget Me Not", "Forget Perspective", "Forget The Colors", "Forget Them All", "Forget This", "Forget Us Not", "Iconic", "Keypad Directionality", "Kugelblitz", "Multitask", "OmegaDestroyer", "OmegaForest", "Organization", "Password Destroyer", "Purgatory", "RPS Judging", "Security Council", "Shoddy Chess", "Simon Forgets", "Simon's Stages", "Souvenir", "Tallordered Keys", "The Time Keeper", "Timing is Everything", "The Troll", "Turn The Key", "The Twin", "Übermodule", "Ultimate Custom Night", "The Very Annoying Button", "Whiteout" });
        GetComponent<KMBombModule>().OnActivate += delegate () { lightsOn = true; timerTick = StartCoroutine(Timer()); };
    }

    void Start()
    {
        startingTime = Bomb.GetTime();
        modCount = Bomb.GetSolvableModuleNames().Count(x => !ignoredModules.Contains(x));
    }

    void Update()
    {
        if (Bomb.GetTime() <= startingTime * (1 - threshold) || Bomb.GetSolvedModuleNames().Count() >= modCount * threshold)
            goingIntoSolve = true;
        if (lightsOn && state < States.Submission)
            timeElapsed += Time.deltaTime;
    }

    void TilePress(int pos)
    {
        Audio.PlaySoundAtTransform("tile", tiles[pos].transform);
        if (state == States.Activated)
            PlaceTile(pos);
        else if (state == States.Submission)
            HandleNumberInput(pos);
    }

    void PlaceTile(int pos)
    {
        if (selectedTiles.Contains(pos))
        {
            tiles[pos].transform.SetParent(tilesParent.transform);
            tiles[pos].transform.localPosition = tilePositions[pos];
            input[Array.IndexOf(selectedTiles, pos)] = '-';
            selectedTiles[Array.IndexOf(selectedTiles, pos)] = -1;
        }
        else
        {
            if (input.Count(x => x != '-') == 2)
            {
                Debug.Log("attepmted to place a tile in a full input box");
                return;
            }
            int boxPosition = (input[0] == '-') ? 0 : 1;
            tiles[pos].transform.SetParent(inputDisp.transform);
            tiles[pos].transform.localPosition = inputPositions[boxPosition];
            selectedTiles[boxPosition] = pos;
            input[boxPosition] = availableTiles[pos];
        }
    }

    void HandleNumberInput(int pos)
    {
        if (state != States.Submission)
            throw new NotImplementedException();
        if (pos == 5)
        {
            for (int i = 0; i < 6; i++)
                tiles[i].GetComponentInChildren<TextMesh>().text = "" + numberSets[showingBigs ? 0 : 1][i];
            showingBigs = !showingBigs;
        }
        else
        {
            submission = submission * 10 + pos;
            if (showingBigs) submission += 5;
            timerText.text = (submission % 100).ToString().PadLeft(2, '0');
        }
    }
    void HandleNumberSubmit()
    {
        if (submission == currentScore)
        {
            Debug.LogFormat("[Scrabble Scramble #{0}] You submitted {1}, which is correct. Module solved!", moduleId, submission);
            state = States.Solved;
            moduleSolved = true;
            GetComponent<KMBombModule>().HandlePass();
            timerText.text = "!!";
        }
        else
        {
            Debug.LogFormat("[Scrabble Scramble #{0}] You submitted {1}, which is incorrect. Displaying stages...", moduleId, submission);
            GetComponent<KMBombModule>().HandleStrike();
            submission = 0;
            timerText.text = "00";
            StartCoroutine(StageRecall());
        }
    }

    void Submit()
    {
        submitButton.AddInteractionPunch(1);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submitButton.transform);
        if (isAnimating || moduleSolved || activations == 0)
            return;
        CheckAnswer();
    }

    void CheckAnswer()
    {
        if (state == States.Submission)
        {
            HandleNumberSubmit();
            return;
        }
        if (satisfied)
            return;
        satisfied = true;
        string inputted = string.Empty + input[0] + input[1]; //Just adding two chars results in an int.
        if (words.Contains(inputted))
        {
            Debug.LogFormat("[Scrabble Scramble #{0}] Entered the word {1} which is in the word list. Added {2} points to the score.", moduleId, inputted, GetScrabbleScore(inputted));
        }
        else
        {
            Debug.LogFormat("[Scrabble Scramble #{0}] Enterred the word {1} which is not in the word list. Strike incurred, but the module deactivated anyway :) :) :) :) :) :) :) :) :)", moduleId, inputted);
            GetComponent<KMBombModule>().HandleStrike();
        }
        StartCoroutine(Clear());
        currentScore += GetScrabbleScore(inputted);
        submittedWords.Add(inputted);
    }

    IEnumerator GoIntoSolve()
    {
        StopCoroutine(timerTick);
        Debug.LogFormat("[Scrabble Scramble #{0}] The module has gone into its submission phase. The correct answer is {1}.", moduleId, currentScore);
        StartCoroutine(DisplayTiles(numberSets[0]));
        yield return null;
        tiles[5].GetComponentInChildren<TextMesh>().transform.localPosition = new Vector3(0, 0.51f, 0.125f);
        state = States.Submission;
        timerText.text = "00";

    }

    IEnumerator StageRecall()
    {
        state = States.Recalling;
        if (!boardClear)
        {
            StartCoroutine(Clear());
            yield return new WaitForSeconds(1.7f);
        }
        foreach (string word in submittedWords)
        {
            timerText.text = word;
            yield return new WaitForSeconds(1);
        }
        showingBigs = false;
        StartCoroutine(DisplayTiles(numberSets[0]));
        timerText.text = "00";
        state = States.Submission;

    }

    IEnumerator Timer()
    {
        while (state != States.Submission && state != States.Solved)
        {
            int timerNumber;
            switch (state)
            {
                case States.Deactivated: timerNumber = 30; break;
                case States.Initial: timerNumber = 60; break;
                case States.Activated: timerNumber = 90; break;
                default: throw new ArgumentException();
            }
            if (timeElapsed > timerNumber)
            {
                timeElapsed = Time.deltaTime;
                if (state == States.Activated)
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    Debug.LogFormat("[Scrabble Scramble #{0}] Time ran out. Strike incurred.", moduleId);
                    StartCoroutine(Clear());
                    state = States.Deactivated;
                }
                else
                {
                    StartCoroutine(GenerateStage());
                    state = States.Activated;
                }
            }
            if (state == States.Activated && !satisfied)
                timerText.text = (timerNumber - Mathf.FloorToInt(timeElapsed)).ToString().PadLeft(2, '0');
            else timerText.text = string.Empty;
            yield return null;

        }
    }

    IEnumerator Clear()
    {
        boardClear = true;
        for (int i = 0; i < 6; i++)
        {
            Audio.PlaySoundAtTransform("tile", tiles[i].transform);
            tiles[i].SetActive(false);
            yield return new WaitForSeconds(0.2f);
        }
        input = new char[] { '-', '-' };
        selectedTiles = new int[] { -1, -1 };
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator GenerateStage()
    {
        isAnimating = true;
        satisfied = false;
        if (!boardClear)
        {
            StartCoroutine(Clear());
            yield return new WaitForSeconds(1.7f);
        }
        boardClear = false;
        if (goingIntoSolve)
        {
            StartCoroutine(GoIntoSolve());
        }
        else
        {
            activations++;
            string chosenWord = words.PickRandom();
            availableTiles[0] = chosenWord[0];
            availableTiles[1] = chosenWord[1];
            for (int i = 0; i < 4; i++)
                availableTiles[i + 2] = (char)UnityEngine.Random.Range('A', 'Z' + 1);
            availableTiles.Shuffle();
            Debug.LogFormat("[Scrabble Scramble #{0}] The displayed tiles are {1}. One word you can spell is {2}.", moduleId, availableTiles.Join(" "), chosenWord);
            StartCoroutine(DisplayTiles(availableTiles));
        }
        yield return null;
    }

    IEnumerator DisplayTiles(char[] input)
    {
        for (int i = 0; i < 6; i++)
        {
            Audio.PlaySoundAtTransform("tile", tiles[i].transform);
            tiles[i].transform.SetParent(tilesParent.transform);
            tiles[i].transform.localPosition = tilePositions[i];
            tiles[i].GetComponentInChildren<TextMesh>().text = input[i].ToString();
            tiles[i].SetActive(true);
            yield return new WaitForSeconds(0.5f);
        }
        isAnimating = false;
    }

    int GetScrabbleScore(string input)
    {
        int score = 0;
        foreach (char letter in input)
            score += letterValues[letter - 'A'];
        return score;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use [!{0} submit AB] to enter that word into the module. If the module is in submission phase, use [!{0} submit 12] to enter that number.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string input)
    {
        string command = input.Trim().ToUpperInvariant();
        List<string> parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (Regex.IsMatch(command, @"^SUBMIT\s+[A-Z][A-Z]$") && state == States.Activated)
        {
            int[] selects = new int[] { -1, -1 };
            for (int i = 0; i < 6; i++)
                if (availableTiles[i] == parameters.Last()[0])
                    selects[0] = i;
            for (int i = 0; i < 6; i++)
                if (availableTiles[i] == parameters.Last()[1] && i != selects[0])
                    selects[1] = i;
            if (selects.Any(x => x == -1))
            {
                yield return "sendtochaterror Attempted to enter an impossible word.";
                yield break;
            }
            yield return null;
            while (isAnimating) yield return null;
            tileSelectables[selects[0]].OnInteract();
            yield return new WaitForSeconds(0.3f);
            tileSelectables[selects[1]].OnInteract();
            yield return new WaitForSeconds(0.3f);
            submitButton.OnInteract();
        }
        else if (Regex.IsMatch(command, @"^SUBMIT\s+[0-9]+$") && state == States.Submission)
        {
            yield return null;
            while (isAnimating) yield return null;
            foreach (int number in parameters.Last().Select(x => x - '0'))
            {
                if (number > 4 ^ showingBigs)
                {
                    tileSelectables[5].OnInteract();
                    yield return new WaitForSeconds(0.3f);
                }
                tileSelectables[number % 5].OnInteract();
                yield return new WaitForSeconds(0.3f);
            }
            submitButton.OnInteract();
        }
    }
    //We are not having an autosolver for the pseudoneedy. Fuck you exish!
}
