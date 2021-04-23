using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class ScrabbleScrambleScript : MonoBehaviour {

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
    float threshold = 0.5f;

    int activations = 0;
    float timeElapsed = 0f;
    private Coroutine timerTick;
    bool coroutineActive = false;
    bool waiting = false;
    bool goingIntoSolve = false;
    bool satisfied = false;
    List<string> words = new List<string> { "AA", "AB", "AD", "AE", "AG", "AH", "AI", "AL", "AM", "AN", "AR", "AS", "AT", "AW", "AX", "AY", "BA", "BE", "BI", "BO", "BY", "DA", "DE", "DO", "ED", "EF", "EH", "EL", "EM", "EN", "ER", "ES", "ET", "EW", "EX", "FA", "FE", "GI", "GO", "HA", "HE", "HI", "HM", "HO", "ID", "IF", "IN", "IS", "IT", "JO", "KA", "KI", "LA", "LI", "LO", "MA", "ME", "MI", "MM", "MO", "MU", "MY", "NA", "NE", "NO", "NU", "OD", "OE", "OF", "OH", "OI", "OK", "OM", "ON", "OP", "OR", "OS", "OW", "OX", "OY", "PA", "PE", "PI", "PO", "QI", "RE", "SH", "SI", "SO", "TA", "TE", "TI", "TO", "UH", "UM", "UN", "UP", "US", "UT", "WE", "WO", "XI", "XU", "YA", "YE", "YO", "ZA", "CH", "DI", "EA", "EE", "FY", "GU", "IO", "JA", "KO", "KY", "NY", "OB", "OO", "OU", "ST", "UG", "UR", "YU", "ZE", "ZO" };
    int[] letterValues = new int[] { 1, 3, 3, 2, 1, 4, 2, 4, 1, 8, 5, 1, 3, 1, 1, 3, 10, 1, 1, 1, 1, 4, 4, 8, 4, 10 };
    int currentScore = 0;

    char[] input = new char[] { '-' , '-' };
    int[] selectedTiles = new int[] { -1, -1 };
    char[] availableTiles = new char[6];
    char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    void Awake ()
    {
        moduleId = moduleIdCounter++;
        for (int i = 0; i < 6; i++)
        {
            int fuckyou = i;
            tileSelectables[fuckyou].OnInteract += delegate () { TilePress(fuckyou); return false; };
            tilePositions[fuckyou] = tileSelectables[fuckyou].transform.localPosition;
            tiles[fuckyou].SetActive(false);
        }
        submitButton.OnInteract += delegate () { Submit(); return false; };
        if (ignoredModules == null)
            ignoredModules = BossModule.GetIgnoredModules("Scrabble Scramble", new string[] { "14", "42", "501", "A>N<D", "Bamboozling Time Keeper", "Black Arrows", "Brainf---", "Busy Beaver", "Don't Touch Anything", "Floor Lights", "Forget Any Color", "Forget Enigma", "Forget Everything", "Forget Infinity", "Forget It Not", "Forget Maze Not", "Forget Me Later", "Forget Me Not", "Forget Perspective", "Forget The Colors", "Forget Them All", "Forget This", "Forget Us Not", "Iconic", "Keypad Directionality", "Kugelblitz", "Multitask", "OmegaDestroyer", "OmegaForest", "Organization", "Password Destroyer", "Purgatory", "RPS Judging", "Security Council", "Shoddy Chess", "Simon Forgets", "Simon's Stages", "Souvenir", "Tallordered Keys", "The Time Keeper", "Timing is Everything", "The Troll", "Turn The Key", "The Twin", "Übermodule", "Ultimate Custom Night", "The Very Annoying Button", "Whiteout" } );
        GetComponent<KMBombModule>().OnActivate += delegate () { lightsOn = true; };
    }

    void Start ()
    {
        startingTime = Bomb.GetTime();
        modCount = Bomb.GetSolvableModuleNames().Count(x => !ignoredModules.Contains(x));
        StartCoroutine(InitialActivation());
    }

    void Update ()
    {
        if (Bomb.GetTime() < startingTime * threshold || Bomb.GetSolvedModuleNames().Count() > modCount * threshold)
            goingIntoSolve = true;
        if (lightsOn && coroutineActive)
            timeElapsed += 10*Time.deltaTime;
    }

    IEnumerator InitialActivation()
    {
        coroutineActive = true;
        while (timeElapsed < 60)
            yield return null;
        timeElapsed = 0f;
        timerTick = StartCoroutine(Timer());
        StartCoroutine(GenerateStage());
    }

    void TilePress(int pos)
    {
        Debug.Log(pos);
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
    void Submit()
    {
        submitButton.AddInteractionPunch(1);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submitButton.transform);
        if (isAnimating || moduleSolved || satisfied || activations == 0)
            return;
        CheckAnswer();
    }

    void CheckAnswer()
    {
        string inputted = string.Empty + input[0] + input[1]; //Just adding two chars results in an int.
        satisfied = true;
        if (words.Contains(inputted))
        {
            Debug.LogFormat("[Scrabble Scramble #{0}] Entered the word {1} which is in the word list. Added {2} points to the score.", moduleId, inputted, GetScrabbleScore(inputted));
            currentScore += GetScrabbleScore(inputted);
        }
        else
        {
            Debug.LogFormat("[Scrabble Scramble #{0}] Enterred the word {1} which is not in the word list. Strike incurred, but the module deactivated anyway :) :) :) :) :) :) :) :) :)", moduleId, inputted);
            GetComponent<KMBombModule>().HandleStrike();
            StartCoroutine(Clear());
        }
    }

    IEnumerator Timer()
    {
        coroutineActive = true;
        while (!moduleSolved)
        {
            if (!satisfied)
                timerText.text = Mathf.FloorToInt(90 - timeElapsed).ToString().PadLeft(2, '0');
            else timerText.text = string.Empty;
            if (timeElapsed >= 90)
            {
                Debug.LogFormat("[Scrabble Scramble #{0}] Ran out of time, strike!", moduleId);
                GetComponent<KMBombModule>().HandleStrike();
                timeElapsed = 0;
                satisfied = false;
                timerText.text = string.Empty;
                while (timeElapsed <= 30) //Adds a 30 second free period after deactivation. This should give a total of one stage done every 2 minutes.
                    yield return null;
                timeElapsed = 0;
                StartCoroutine(GenerateStage());
            }
            yield return null;
        }
    }

    IEnumerator Clear()
    {
        for (int i = 0; i < 6; i++)
        {
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
        if (activations != 0)
        {
            StartCoroutine(Clear());
            yield return new WaitForSeconds(1.7f);
        }
        if (goingIntoSolve)
        {
            StopCoroutine(timerTick);
        }
        else
        {
            activations++;
            string chosenWord = words.PickRandom();
            availableTiles[0] = chosenWord[0];
            availableTiles[1] = chosenWord[1];
            for (int i = 0; i < 4; i++)
                availableTiles[i + 2] = alphabet.PickRandom();
            availableTiles.Shuffle();
            Debug.LogFormat("[Scrabble Scramble #{0}] The displayed tiles are {1}. One word you can spell is {2}.", moduleId, availableTiles.Join(" "), chosenWord);
            for (int i = 0; i < 6; i++)
            {
                tiles[i].transform.SetParent(tilesParent.transform);
                tiles[i].transform.localPosition = tilePositions[i];
                tiles[i].GetComponentInChildren<TextMesh>().text = availableTiles[i].ToString();
                tiles[i].SetActive(true);
                yield return new WaitForSeconds(0.5f);
            }
        }
        yield return null;
    }

    int GetScrabbleScore(string input)
    {
        int score = 0;
        foreach (char letter in input)
            score += letterValues[Array.IndexOf(alphabet, letter)];
        return score;
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string Command)
    {
        StartCoroutine(GenerateStage());
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve () {
      yield return null;
    }
}
