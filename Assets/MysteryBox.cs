using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class MysteryBox : MonoBehaviour
{
    [SerializeField] private KMBombInfo Bomb;
    [SerializeField] private KMAudio Audio;
    [SerializeField] private KMColorblindMode Colourblind;

    [SerializeField] KMSelectable Box;
    [SerializeField] AudioClip OpenSound;
    [SerializeField] GameObject Hinge;
    [SerializeField] Light BoxLight;
    [SerializeField] TextMesh ColourblindText;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    Color[] lightColours = new Color[10] { Color.red, Color.blue, new Color(1, 1, 0), new Color(0.5f, 0, 1), new Color(1, 0.5f, 0), Color.green, Color.cyan, Color.magenta, Color.black, Color.white };
    string[] colourNames = new string[10] { "Red", "Blue", "Yellow", "Purple", "Orange", "Green", "Cyan", "Magenta", "Black", "White" };
    int[,] xTable = new int[10, 10]
    {
        { -1, 1, 83, 63, 52, 78, 27, 17, 58, 44 },        { 64, -1, 47, 71, 21, 12, 28, 85, 7, 25 },        { 91, 57, -1, 76, 34, 62, 3, 38, 15, 66 },        { 26, 92, 72, -1, 33, 84, 65, 79, 32, 39 },        { 95, 22, 43, 93, -1, 41, 53, 23, 5, 9 },        { 6, 96, 14, 73, 31, -1, 42, 86, 35, 59 },        { 97, 4, 68, 36, 67, 29, -1, 51, 18, 69 },        { 48, 49, 45, 19, 54, 89, 74, -1, 81, 13 },        { 37, 99, 11, 94, 55, 16, 87, 56, -1, 46 },        { 75, 24, 98, 61, 2, 88, 83, 8, 82, -1 },
    };

    int x;
    int boxColour;

    bool openedBox;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        Box.OnInteract += delegate () { OnBoxInteract(); return false; };
        /*
        foreach (KMSelectable object in keypad) {
            object.OnInteract += delegate () { keypadPress(object); return false; };
            }
        */

        //button.OnInteract += delegate () { buttonPress(); return false; };
    }

    void OnBoxInteract()
    {
        if (!openedBox)
        {
            Audio.PlaySoundAtTransform(OpenSound.name, transform);
            StartCoroutine("OpenBox");
            openedBox = true;
        }
    }

    void Start()
    {
        BoxLight.gameObject.SetActive(false);
        boxColour = Rnd.Range(0, 10);
        ColourblindText.text = colourNames[boxColour];
        Log(String.Format("Generated Mystery Box colour is {0}.", colourNames[boxColour]));
        CalculateX();
    }

    void CalculateX()
    {
        int batteries = Bomb.GetBatteryCount();
        int ports = Bomb.GetPortCount();
        int bHolders = Bomb.GetBatteryHolderCount();
        int pPlates = Bomb.GetPortPlateCount();
        int litInds = Bomb.GetOnIndicators().Count();
        int unlitInds = Bomb.GetOffIndicators().Count();
        bool lastEven = Bomb.GetSerialNumberNumbers().Last() % 2 == 0;

        int r = 0;
        int c = 0;
        string rowUsing = "";
        string colUsing = "";
        bool flag = false;

        if (batteries != ports)
        {
            r = !lastEven ? batteries : ports;
            c = !lastEven ? ports : batteries;
            x = xTable[r, c];
            rowUsing = !lastEven ? "Batteries" : "Ports";
            colUsing = !lastEven ? "Ports" : "Batteries";
        }
        else if (bHolders != pPlates)
        {
            r = !lastEven ? bHolders : pPlates;
            c = !lastEven ? pPlates : bHolders;
            x = xTable[r, c];
            rowUsing = !lastEven ? "Battery Holders" : "Port Plates";
            colUsing = !lastEven ? "Port Plates" : "Battery Holders";
        }
        else if (litInds != unlitInds)
        {
            r = !lastEven ? litInds : unlitInds;
            c = !lastEven ? unlitInds : litInds;
            x = xTable[r, c];
            rowUsing = !lastEven ? "Lit Indicators" : "Unlit Indicators";
            colUsing = !lastEven ? "Unlit Indicators" : "Lit Indicators";
        }
        else
        {
            flag = true;
            x = xTable[6, 9];
        }

        if (!flag) Log(String.Format("Using {0} as the row and {1} as the column: row {2}, column {3} results in X = {4}.", rowUsing, colUsing, r, c, x));
        else Log(String.Format("All edgework combinations are equal, row 6, column 9, results in X = {0}.", x));
    }

    void Log(string arg)
    {
        Debug.Log($"[Mystery Box #{ModuleId}] {arg}");
    }

    IEnumerator OpenBox()
    {
        BoxLight.gameObject.SetActive(true);
        BoxLight.color = lightColours[boxColour];
        ColourblindText.gameObject.SetActive(Colourblind.ColorblindModeActive);
        for (int i = 0; i < 35; i++)
        {
            Hinge.transform.Rotate(4, 0, 0);
            yield return null;
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
    }
}
