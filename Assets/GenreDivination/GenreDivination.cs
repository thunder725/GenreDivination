using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;


public class GenreDivination : MonoBehaviour {

    /// <summary> All of the possible Prefixes, including ones only accessible by Ruleseed. </summary>
    readonly string[] possiblePrefixes = new string[30] { "Cyber", "Hyper-", "Hard", "Deep", "Bit-", "Hi-", "Drum", "Flash", "Auto", "Über",
        "Neo", "Off-", "Drop", "Lo-", "99-", "Speed", "Break", "Jump", "Acid", "Retro", "Sun-", "Ink-", "Micro", "Out", "Mix-", "Big",
        "Tera", "Self-", "Oily", "Primal" };

    /// <summary> All of the possible Adjectives, including ones only accessible by Ruleseed. </summary>
    readonly string[] possibleAdjectives = new string[30] { "Electro", "Rush", "Colour", "Pop", "Funk", "Sonic", "Show", "Synk", "Delta", "Simon",
        "Cortex", "30k", "Heart", "Liquid", "Progressive", "Duster", "Wave", "Sound", "Soul", "Concrete", "Room", "Urban", "Indie", "Tune",
        "Blast", "Screen", "Chaos", "Thunder", "Frost", "Plasma" };

    /// <summary> All of the possible Styles, including ones only accessible by Ruleseed. </summary>
    readonly string[] possibleStyles = new string[30] { "Bass", "Trance", "House", "Dance", "Fusion", "Symptom", "Spin", "& Crash", "Pulse", "Cipher",
        "Stack", "Dash", "Whiplash", "Murder", "Craftstep", "Trap", "Pride", "Beats", "Swing", "Pop", "Key", "Slab", "Burst", "Synchro", "Shot",
        "Forest", "Burn", "Cry", "Slap", "Drift" };


    [SerializeField] KMAudio moduleAudio;
    [SerializeField] KMBombInfo bombInfo;
    [SerializeField] KMBombModule module;
    [SerializeField] KMRuleSeedable ruleseedManager;


    [SerializeField] KMSelectable SubmitButton;
    [SerializeField] KMSelectable PlayButton;
    [SerializeField] KMSelectable PrefixLeftButton, PrefixRightButton;
    [SerializeField] KMSelectable AdjectiveLeftButton, AdjectiveRightButton;
    [SerializeField] KMSelectable StyleLeftButton, StyleRightButton;
    [SerializeField] KMSelectable PrefixVstBankButton, AdjectiveVstBankButton, StyleVstBankButton;


    // I wish I could use an AudioClip[][] here, but that wouldn't serialize and be editable
    // in the Inspector, so it's the one part of the code that's in separate arrays then!!
    [SerializeField] AudioClip[] DrumsClips;
	[SerializeField] AudioClip[] BassClips;
	[SerializeField] AudioClip[] ChordClips;
    [SerializeField] AudioClip[] LeadClips;

    [SerializeField] AudioClip solveSound, screenTextChangeSound, submitSound;
    [SerializeField] AudioClip[] buttonSounds;


    /// <summary> For all 3 Genre Parts (Prefix, Adjective, Style), contains the index of the 25 selected Genre Parts, in the order of the manual. </summary>
    int[][] RuleseedAllowedGenreParts = new int[3][];
    /// <summary> For all 4 Instruments (Drums, Bass, Chords, Lead), contains the index of the 5 clips found in the manual. </summary>
    int[][] RuleseedAllowedMusicClips = new int[4][];
    /// <summary> Contains the 4 selected Clips for the 4 instruments, as indices into RuleseedAllowedMusicClips. </summary>
    int[] selectedClipIndices = new int[4];


    /// <summary> The Genre Parts that are expected to be submitted in order to solve the module. </summary>
    int[] expectedGenreParts = new int[3];
    /// <summary> The Genre Parts currently being selected and shown on the Module. </summary>
    int[] selectedGenreParts = new int[3];
    /// <summary> The VST Banks currently being selected and shown on the Module. </summary>
    int[] selectedVstBanks = new int[3];

    [SerializeField] TextMesh[] selectedGenrePartTexts;
    [SerializeField] MeshRenderer[] VstBankNotchRenderers;
    /// <summary> The 3 Materials for each currently selected VST Banks, so that we can edit the colour of them independently on the module. </summary>
    Material[] VstBankNotchMaterials = new Material[3];
    /// <summary> The Material to make a copy of, a simple flat colour. </summary>
    [SerializeField] Material VstBankNotchBaseMaterial;

    /// <summary> Target local X position of each notch depending on the selected VstBank. Order has been selected to avoid shuffling and make everything smooth. </summary>
    readonly float[][] VstBankNotchLocations = new float[5][]
    {
        new float[5] { 0, 0, 0, 0, 0 },
        new float[5] { -0.15f, 0.15f, 0, 0, 0 },
        new float[5] { -0.2f, 0.2f, 0, 0, 0 },
        new float[5] { -0.3f, 0.3f, -0.1f, 0.1f, 0 },
        new float[5] { -0.35f, 0.35f, -0.175f, 0.175f, 0 }
    };


    /// <summary> Value that determines whether the Notches are considered in movement or not, to only move them for X seconds after a button press and optimize a bit </summary>
    float notchMovementTimeRemaining = 0f;
    float textScalingTimeRemaining = 0f;
    float textTargetScale = 1f;
    float buttonMovementTimeRemaining = 0f;

    /// <summary> Whether pressing the Play button will actually do something, to avoid overlaying audio tracks since there is no Stop nor Pause. </summary>
    bool AllowMusicPlaying;

    /// <summary> Colours used for the VST Banks, shown on the Notches and on the Text themselves. </summary>
    [SerializeField] Color[] VstBankColours;


    // Logging Data
    static int moduleIdCounter = 1;
    int moduleId;
    bool moduleSolved;


    /// <summary> Buttons gathering and GetComponents </summary>
    void Awake()
	{
        // Initialize Logging
        moduleId = moduleIdCounter++;

        // Submit & Play Button
        SubmitButton.OnInteract += delegate () { SubmitResult(); return false; } ;
        PlayButton.OnInteract += delegate () { PlayAllTracks(); return false; };

        // Text Selection Buttons
        PrefixLeftButton.OnInteract += delegate () { OnGenrePartArrowButtonPressed(false, 0, PrefixLeftButton); return false; };
        PrefixRightButton.OnInteract += delegate () { OnGenrePartArrowButtonPressed(true, 0, PrefixRightButton); return false; };
        AdjectiveLeftButton.OnInteract += delegate () { OnGenrePartArrowButtonPressed(false, 1, AdjectiveLeftButton); return false; };
        AdjectiveRightButton.OnInteract += delegate () { OnGenrePartArrowButtonPressed(true, 1, AdjectiveRightButton); return false; };
        StyleLeftButton.OnInteract += delegate () { OnGenrePartArrowButtonPressed(false, 2, StyleLeftButton); return false; };
        StyleRightButton.OnInteract += delegate () { OnGenrePartArrowButtonPressed(true, 2, StyleRightButton); return false; };

        // VstBank Buttons
        PrefixVstBankButton.OnInteract += delegate () { OnChangeVstBankButtonPressed(0); return false; };
        AdjectiveVstBankButton.OnInteract += delegate () { OnChangeVstBankButtonPressed(1); return false; };
        StyleVstBankButton.OnInteract += delegate () { OnChangeVstBankButtonPressed(2); return false; };


        // Create copies of the materials for each individual Genre Part, so that we can update their colour easily
        for (int i = 0; i < 3; i ++)
        {
            VstBankNotchMaterials[i] = Instantiate(VstBankNotchBaseMaterial);

            for (int j = 0; j < 5; j ++)
            {
                GetNotch(i, j).material = VstBankNotchMaterials[i];
            }
        }
    }

    void Start()
    {
        InitializePuzzle();
    }

    void Update()
    {
        // Smoothly move the VST Bank Notches, but only if they've been updated recently.
        // No need to run the for loop every frame otherwise, that's wasted resources.
        if (notchMovementTimeRemaining > 0)
        {
            MoveVstBankNotches();
            notchMovementTimeRemaining -= Time.deltaTime;
        }

        if (textScalingTimeRemaining > 0)
        {
            ScaleSelectedGenrePartTexts();
            textScalingTimeRemaining -= Time.deltaTime;
        }

        if (buttonMovementTimeRemaining > 0)
        {
            ReturnButtonsToLocations();
            buttonMovementTimeRemaining -= Time.deltaTime;
        }
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Interaction
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

	void PlayAllTracks()
	{
        // Feedback
        PlayButton.AddInteractionPunch();

        PlaySound(submitSound);

        buttonMovementTimeRemaining = 0.3f;
        PlayButton.transform.localPosition += Vector3.down * 0.007f;


        if (AllowMusicPlaying == false) { return; }

        // Prevent music from being played for around 16 seconds.
        StartCoroutine(BlockAudioPlayCoroutine());

        // After the module is solved, the music that gets played is fully randomized!
        if (moduleSolved)
        {
            PlaySound(DrumsClips[UnityEngine.Random.Range(0, 7)]);
            PlaySound(BassClips[UnityEngine.Random.Range(0, 7)]);
            PlaySound(ChordClips[UnityEngine.Random.Range(0, 7)]);
            PlaySound(LeadClips[UnityEngine.Random.Range(0, 7)]);
        }
        else
        {
            // Can't just use DrumClips[SelectedClip[0]] because the order of the clips is dependant on Ruleseed
            PlaySound(DrumsClips[RuleseedAllowedMusicClips[0][selectedClipIndices[0]]]);
            PlaySound(BassClips[RuleseedAllowedMusicClips[1][selectedClipIndices[1]]]);
            PlaySound(ChordClips[RuleseedAllowedMusicClips[2][selectedClipIndices[2]]]);
            PlaySound(LeadClips[RuleseedAllowedMusicClips[3][selectedClipIndices[3]]]);
        }
	}

    IEnumerator BlockAudioPlayCoroutine()
    {
        AllowMusicPlaying = false;

        // The samples usually last slightly less than 16 seconds.
        yield return new WaitForSeconds(16f);
        AllowMusicPlaying = true;
    }

    /// <summary> Simplified method to just play a sound from an AudioClip </summary>
    public void PlaySound(AudioClip soundToPlay)
    {
        moduleAudio.PlaySoundAtTransform(soundToPlay.name, transform);
    }


    void SubmitResult()
    {
        // Button Feedback here
        SubmitButton.AddInteractionPunch();

        PlaySound(submitSound);

        buttonMovementTimeRemaining = 0.3f;
        SubmitButton.transform.localPosition += Vector3.down * 0.005f;

        if (moduleSolved) { return; }

        ModuleLog(true, "Submitted full Genre name {0}.", GetReadableFullGenreName(selectedGenreParts[0], selectedGenreParts[1], selectedGenreParts[2]));

        if (expectedGenreParts[0] == selectedGenreParts[0] && expectedGenreParts[1] == selectedGenreParts[1] && expectedGenreParts[2] == selectedGenreParts[2])
        {
            ModuleLog(true, "That is the correct answer. Solving module!");

            SolveModule();
        }
        else
        {
            ModuleLog(true, "!! STRIKE !! That is incorrect. Expected {0}. !! STRIKE !!",
                GetReadableFullGenreName(expectedGenreParts[0], expectedGenreParts[1], expectedGenreParts[2]));

            StrikeModule();
        }
    }

    /// <summary> Called when one of the arrow buttons next to the Genre Part is pressed. </summary>
    void OnGenrePartArrowButtonPressed(bool shouldIncrement, int genrePartIndex, KMSelectable pressedButton)
    {
        // Feedback
        pressedButton.AddInteractionPunch(0.6f);

        PlaySound(buttonSounds.PickRandom());
        PlaySound(screenTextChangeSound);

        buttonMovementTimeRemaining = 0.3f;
        pressedButton.transform.localPosition += Vector3.down * 0.003f;


        int _currentIndexInVstBank = Array.IndexOf(RuleseedAllowedGenreParts[genrePartIndex], selectedGenreParts[genrePartIndex]) % 5;

        // Update the index inside of the Vst Bank
        // % is not a true modulo; -1 % 5 = -1
        // whereas -1 mod 5 would return 4
        _currentIndexInVstBank = (_currentIndexInVstBank + (shouldIncrement ? 1 : -1)) % 5;
        if (_currentIndexInVstBank < 0) { _currentIndexInVstBank += 5; }

        selectedGenreParts[genrePartIndex] = RuleseedAllowedGenreParts[genrePartIndex][selectedVstBanks[genrePartIndex] * 5 + _currentIndexInVstBank];

        // Update the Text
        UpdateSelectedGenrePartText(genrePartIndex);
    }

    /// <summary> Called when the main screen of a Genre Part is pressed, to switch VST Bank. </summary>
    void OnChangeVstBankButtonPressed(int genrePartIndex)
    {
        // Feedback
        PlaySound(screenTextChangeSound);
        PrefixVstBankButton.AddInteractionPunch(0.6f);

        selectedVstBanks[genrePartIndex] = (selectedVstBanks[genrePartIndex] + 1) % 5;

        // Update the text due to VstBank changement, without offset
        selectedGenreParts[genrePartIndex] = RuleseedAllowedGenreParts[genrePartIndex][selectedVstBanks[genrePartIndex] * 5 +
            (Array.IndexOf(RuleseedAllowedGenreParts[genrePartIndex], selectedGenreParts[genrePartIndex]) % 5)];

        UpdateVstBankVisualFeedback(genrePartIndex);
        UpdateSelectedGenrePartText(genrePartIndex);
    }

    /// <summary> Update the VST Bank visual feedback for a given Genre Part </summary>
    void UpdateVstBankVisualFeedback(int genrePartIndex)
    {
        // Save selected Bank
        int _selectedBank = selectedVstBanks[genrePartIndex];

        // Update Text Colour
        selectedGenrePartTexts[genrePartIndex].color = VstBankColours[_selectedBank];

        // Update Notch Colour
        VstBankNotchMaterials[genrePartIndex].SetColor("_Color", VstBankColours[_selectedBank]);

        // Update Notch Visuals
        for (int i = 0; i < 5; i ++)
        {
            // Show or hide the notch
            GetNotch(genrePartIndex, i).gameObject.SetActive(i <= _selectedBank);
        }

        // Restart Notch sliding movement
        notchMovementTimeRemaining = .3f;
    }

    /// <summary> Called every frame for a bit after a VST Bank has changed, to move the Notches to their expected locations </summary>
    void MoveVstBankNotches()
    {
        // It's a small optimization, but we're gonna run a loop 15 times so let's just use a single memory location
        // instead of creating 15 of each!
        Transform _notch;
        Vector3 _pos;
        float _progress = 15 * Time.deltaTime;

        // For the 3 Genre Parts
        for (int _part = 0; _part < 3; _part ++)
        {
            // For the 5 Notches inside of that Genre Part
            for (int i = 0; i < 5; i++)
            {
                _notch = GetNotch(_part, i).transform;

                // Interp towards
                _pos = _notch.localPosition;
                _pos.x = Mathf.Lerp(_pos.x, VstBankNotchLocations[selectedVstBanks[_part]][i], _progress);
                _notch.localPosition = _pos;
            }
        }
    }

    /// <summary> Refresh the Selected Genre Part Text after a change has been done to it. </summary>
    void UpdateSelectedGenrePartText(int genrePartIndex)
    {
        // Update the Text's value by pulling the new text from the table
        selectedGenrePartTexts[genrePartIndex].text = GetGenrePartArraysByIndex(genrePartIndex)[selectedGenreParts[genrePartIndex]];

        // Scale the text by its number of characters
        // Let's say 5 characters is 160 font size
        // and 10 is 140
        // We cannot get too big even for small words, because of uppercase Ü and lowercase g that quickly get messy
        // Simple Lerp with Inverse Lerp!

        int _worldLength = Mathf.Clamp(selectedGenrePartTexts[genrePartIndex].text.Length, 5, 10);

        selectedGenrePartTexts[genrePartIndex].fontSize = (int)Mathf.Lerp(160, 140, Mathf.InverseLerp(5, 10, _worldLength));


        Vector3 _scale = selectedGenrePartTexts[genrePartIndex].transform.localScale;
        _scale.x = 0;
        selectedGenrePartTexts[genrePartIndex].transform.localScale = _scale;

        textScalingTimeRemaining = 0.3f;
    }

    /// <summary> Called every frame for a bit after a text has changed, to scale the texts back to their original scale </summary>
    void ScaleSelectedGenrePartTexts()
    {
        Vector3 _scale;
        float _progress = 15 * Time.deltaTime;

        // For the 3 Genre Parts
        for (int _part = 0; _part < 3; _part++)
        {
            _scale = selectedGenrePartTexts[_part].transform.localScale;
            _scale.x = Mathf.Lerp(_scale.x, textTargetScale, _progress);
            selectedGenrePartTexts[_part].transform.localScale = _scale;
        }
    }

    /// <summary> Called every frame for a bit after a button has been pressed, to return the buttons to their locations </summary>
    void ReturnButtonsToLocations()
    {
        KMSelectable button;
        Vector3 position;
        float progress = 15f * Time.deltaTime;

        for (int i = 0; i < 8; i ++)
        {
            switch (i)
            {
                default: case 0: button = PrefixLeftButton; break;
                case 1: button = PrefixRightButton; break;
                case 2: button = AdjectiveLeftButton; break;
                case 3: button = AdjectiveRightButton; break;
                case 4: button = StyleLeftButton; break;
                case 5: button = StyleRightButton; break;
                case 6: button = PlayButton; break;
                case 7: button = SubmitButton; break;
            }

            position = button.transform.localPosition;
            position.y = Mathf.Lerp(position.y, 0.0165f, progress);
            button.transform.localPosition = position;
        }
    }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Helper Functions
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    /// <summary> Helper Function to quickly make Logging Strings in for loops </summary>
    string GetInstrumentDisplayNameByIndex(int index)
    {
        switch (index)
        {
            default: case 0: return "Drums";
            case 1: return "Bass";
            case 2: return "Chords";
            case 3: return "Lead";
        }
    }

    /// <summary> Helper Function to quickly make Logging Strings in for loops </summary>
    string GetGenrePartDisplayNameByIndex(int index)
    {
        switch (index)
        {
            default: case 0: return "Prefix";
            case 1: return "Adjective";
            case 2: return "Style";
        }
    }

    /// <summary> Helper Function to quickly access Audio Clips in for loops </summary>
    AudioClip[] GetInstrumentClipsArraysByIndex(int index)
    {
        switch (index)
        {
            default: case 0: return DrumsClips;
            case 1: return BassClips;
            case 2: return ChordClips;
            case 3: return LeadClips;
        }
    }

    /// <summary> Helper Function to quickly access Genre Parts in for loops </summary>
    string[] GetGenrePartArraysByIndex(int index)
    {
        switch (index)
        {
            default: case 0: return possiblePrefixes;
            case 1: return possibleAdjectives;
            case 2: return possibleStyles;
        }
    }

    MeshRenderer GetNotch(int genrePartIndex, int notchIndex)
    {
        return VstBankNotchRenderers[genrePartIndex * 5 + notchIndex];
    }

    string GetReadableFullGenreName(int Prefix, int Adjective, int Style)
    {
        // Prefixes like "Off-" or "Mix-" must be glued to the Adjective, like "Off-Synk" or "Mix-Frost".
        // But otherwise, it must not be glued, like "Drop Delta" or "Primal Screen"
        return possiblePrefixes[Prefix] + (possiblePrefixes[Prefix].Last() == '-' ? "" : " ") + possibleAdjectives[Adjective] + " " + possibleStyles[Style];
    }

    void FisherYatesShuffle<Type>(ref Type[] array, MonoRandom random)
    {
        int _length = array.Length;
        int _index;
        Type _value;
        while (_length > 1)
        {
            // Get an Index from ruleseed, within [0, _i[
            _index = random.Next(0, _length);
            _length--;

            // Get the value from that Index
            _value = array[_index];
            // Replace value at that index by the last value (we're stepping through)
            array[_index] = array[_length];
            // Replace the last value by that Index
            array[_length] = _value;
        }
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    void InitializePuzzle()
    {
        ManageRuleseed();
        SelectMusicSamples();
        ComputeAnswer();
        RandomizeStartingSelectedAnswers();
    }

    void ManageRuleseed()
    {
        MonoRandom Rng = ruleseedManager.GetRNG();

        // Initialize the default values
        if (Rng.Seed == 1)
        {
            RuleseedAllowedMusicClips[0] = new int[5] { 4, 1, 6, 0, 3 };
            RuleseedAllowedMusicClips[1] = new int[5] { 6, 2, 5, 0, 4 };
            RuleseedAllowedMusicClips[2] = new int[5] { 5, 2, 1, 3, 4 };
            RuleseedAllowedMusicClips[3] = new int[5] { 1, 4, 5, 3, 6 };

            RuleseedAllowedGenreParts[0] = new int[25] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
            RuleseedAllowedGenreParts[1] = RuleseedAllowedGenreParts[0];
            RuleseedAllowedGenreParts[2] = RuleseedAllowedGenreParts[0];
            return;
        }

        ModuleLog(true, "Ruleseed {0} Detected! Shuffling available music clips and answers!", Rng.Seed);

        // Shuffle the Audio Clips
        for (int i = 0; i < 4; i ++)
        {
            RuleseedAllowedMusicClips[i] = new int[7] { 0, 1, 2, 3, 4, 5, 6 };
            FisherYatesShuffle(ref RuleseedAllowedMusicClips[i], Rng);

            // We only need the first 5 Audio clips
            RuleseedAllowedMusicClips[i] = RuleseedAllowedMusicClips[i].Take(5).ToArray();

            ModuleLog(false, "Available {0} clips are: {1}", GetInstrumentDisplayNameByIndex(i),
                RuleseedAllowedMusicClips[i].Select(x => GetInstrumentClipsArraysByIndex(i)[x].name).Join(" // "));
        }

        // Shuffle the Genre Parts
        for (int i = 0; i < 3; i++)
        {
            RuleseedAllowedGenreParts[i] = new int[30] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29 };
            FisherYatesShuffle(ref RuleseedAllowedGenreParts[i], Rng);

            // We only need the first 25 words
            RuleseedAllowedGenreParts[i] = RuleseedAllowedGenreParts[i].Take(25).ToArray();

            ModuleLog(false, "Available {0} names are: {1}", GetGenrePartDisplayNameByIndex(i),
                RuleseedAllowedGenreParts[i].Select(x => GetGenrePartArraysByIndex(i)[x]).Join(", "));
        }
    }

    void SelectMusicSamples()
    {
        // One sample for each Instrument (Drums, Bass, Chords, Lead)
        for (int i = 0; i < 4; i ++)
        {
            selectedClipIndices[i] = UnityEngine.Random.Range(0, 5);
            ModuleLog(true, "Selected {0} Clip number {1}; with internal name {2}.", GetInstrumentDisplayNameByIndex(i), selectedClipIndices[i] + 1,
                GetInstrumentClipsArraysByIndex(i)[RuleseedAllowedMusicClips[i][selectedClipIndices[i]]].name);
        }

        // Loading has been finished, we can now play the sounds
        AllowMusicPlaying = true;
    }

    void ComputeAnswer()
    {
        // Get Edgework
        int _batteryCount = bombInfo.GetBatteryCount();
        int _indicatorCount = bombInfo.GetIndicators().Count();
        int _portCount = bombInfo.GetPortCount();

        ModuleLog(true, "Found {0} batteries, {1} indicators and {2} ports.", _batteryCount, _indicatorCount, _portCount);


        // VST Bank is determined by the value of each clip
        int[] VstBankPerGenrePart = new int[3];
        int[] AnswerPerGenrePart = new int[3];


        // Drums number indicates which value inside this VST bank to use
        switch (selectedClipIndices[0])
        {
            case 0:
                AnswerPerGenrePart[0] = Mathf.Clamp(_batteryCount, 1, 5) - 1;
                AnswerPerGenrePart[1] = Mathf.Clamp(_indicatorCount, 1, 5) - 1;
                AnswerPerGenrePart[2] = Mathf.Clamp(_portCount, 1, 5) - 1;
                break;

            case 1:
                AnswerPerGenrePart[0] = Mathf.Clamp(_indicatorCount, 1, 5) - 1;
                AnswerPerGenrePart[1] = Mathf.Clamp(_portCount, 1, 5) - 1;
                AnswerPerGenrePart[2] = Mathf.Clamp(_batteryCount, 1, 5) - 1;
                break;

            case 2:
                AnswerPerGenrePart[0] = Mathf.Clamp(_portCount, 1, 5) - 1;
                AnswerPerGenrePart[1] = Mathf.Clamp(_batteryCount, 1, 5) - 1;
                AnswerPerGenrePart[2] = Mathf.Clamp(_indicatorCount, 1, 5) - 1;
                break;

            case 3:
                AnswerPerGenrePart[0] = Mathf.Clamp(_indicatorCount, 1, 5) - 1;
                AnswerPerGenrePart[1] = Mathf.Clamp(_batteryCount, 1, 5) - 1;
                AnswerPerGenrePart[2] = Mathf.Clamp(_portCount, 1, 5) - 1;
                break;

            case 4:
                AnswerPerGenrePart[0] = Mathf.Clamp(_portCount, 1, 5) - 1;
                AnswerPerGenrePart[1] = Mathf.Clamp(_indicatorCount, 1, 5) - 1;
                AnswerPerGenrePart[2] = Mathf.Clamp(_batteryCount, 1, 5) - 1;
                break;

            default:
                ModuleLogError(true, "Received unknown Drum Clip Number: {0}. Solving module to avoid softlocks. Please report this to thunder725.", selectedClipIndices[0]);
                SolveModule();
                return;
        }

        ModuleLog(true, "Drum Clip is number {0}, so in each respective VST Bank, Prefix will be the number {1}, Adjective the number {2}, and Style the number {3}. Remember that values are clamped between 1 and 5.",
            selectedClipIndices[0] + 1, AnswerPerGenrePart[0] + 1, AnswerPerGenrePart[1] + 1, AnswerPerGenrePart[2] + 1);


        for (int i = 0; i < 3; i ++)
        {
            // VST Banks:
            // Prefix is determined by Bass
            // Adjective is determined by Chords
            // Style is determined by Lead

            VstBankPerGenrePart[i] = selectedClipIndices[i + 1];

            // Determine the expected Genre Part index, in Appendix BEATS (which can be Ruleseeded)
            expectedGenreParts[i] = RuleseedAllowedGenreParts[i][VstBankPerGenrePart[i] * 5 + AnswerPerGenrePart[i]];

            // Log
            ModuleLog(true, "{0} uses VST Bank {1} since {2} Clip is number {3}, so the expected {0} is {4}.", GetGenrePartDisplayNameByIndex(i),
                VstBankPerGenrePart[i] + 1, GetInstrumentDisplayNameByIndex(i + 1), selectedClipIndices[i + 1] + 1, GetGenrePartArraysByIndex(i)[expectedGenreParts[i]]);
        }

        ModuleLog(true, "Submit {0} to solve the module.", GetReadableFullGenreName(expectedGenreParts[0], expectedGenreParts[1], expectedGenreParts[2]));
    }

    void RandomizeStartingSelectedAnswers()
    {
        for (int i = 0; i < 3; i ++)
        {
            selectedVstBanks[i] = UnityEngine.Random.Range(0, 5);
            selectedGenreParts[i] = RuleseedAllowedGenreParts[i][selectedVstBanks[i] * 5 + UnityEngine.Random.Range(0, 5)];

            UpdateSelectedGenrePartText(i);
            UpdateVstBankVisualFeedback(i);
        }
    }

    void SolveModule()
    {
        // Solve Feedback
        PlaySound(solveSound);

        moduleSolved = true;
        module.HandlePass();
    }

    void StrikeModule()
    {
        module.HandleStrike();
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Logging
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    public void ModuleLog(bool LogInLfa, string message, params object[] args)
    {
        if (LogInLfa)
        { Debug.LogFormat("[Genre Divination #{0}] {1}", moduleId, string.Format(message, args)); }
        else
        { Debug.LogFormat("<Genre Divination #{0}> {1}", moduleId, string.Format(message, args)); }
    }

    public void ModuleLogWarning(bool LogInLfa, string message, params object[] args)
    {
        if (LogInLfa)
        { Debug.LogWarningFormat("[Genre Divination #{0}] {1}", moduleId, string.Format(message, args)); }
        else
        { Debug.LogWarningFormat("<Genre Divination #{0}> {1}", moduleId, string.Format(message, args)); }
    }

    public void ModuleLogError(bool LogInLfa, string message, params object[] args)
    {
        if (LogInLfa)
        { Debug.LogErrorFormat("[Genre Divination #{0}] {1}", moduleId, string.Format(message, args)); }
        else
        { Debug.LogErrorFormat("<Genre Divination #{0}> {1}", moduleId, string.Format(message, args)); }
    }

    
    
    
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"“!{0} Play” to play the music sequence. “!{0} Submit Flash Synk Craftstep” to submit an answer. Hyphens and spaces both work (Lo-Pop is treated the same as Lo Pop). ";
#pragma warning restore 414


    IEnumerator ProcessTwitchCommand(string command)
    {
        // Credit to Royal_Flu$h for this line 
        // Also split on hyphens, replace Ü by U, and handle the "& crash" case
        // And while we're at it, take care of "Colour" being spelt "Color" since the submission needs a u
        var commandParts = command.ToLowerInvariant().Replace('ü', 'u').Replace("& crash", "crash").Replace("color", "colour").Split(new[] { ' ', ',', ';', '-' }, StringSplitOptions.RemoveEmptyEntries);

        if (commandParts.Length == 0)
        {
            yield return "sendtochaterror {0} Please submit a non-empty command. Use “!{0} Play” or “!{0} Submit Flash Synk Craftstep”.";
            yield break;
        }

        if (commandParts[0] == "play")
        {
            PlayButton.OnInteract();
            yield return null;
            yield break;
        }

        if (commandParts[0] != "submit" && commandParts[0] != "s")
        {
            yield return "sendtochaterror {0} Please submit a valid command. Use “!{0} Play” or “!{0} Submit Flash Synk Craftstep”.";
            yield break;
        }

        if (commandParts.Length < 4)
        {
            yield return "sendtochaterror {0} Please submit a Genre Name with all three parts, such as “Flash Synk Craftstep” or “Off-Room Whiplash”.";
            yield break;
        }

        if (commandParts.Length > 4)
        {
            yield return "sendtochaterror {0} Received more than 3 Genre Parts: " + commandParts.Join(" ");
            yield break;
        }


        // Now, try to navigate VST Banks and selections (with Ruleseed!!) to select the 3 wished Genre Parts.


        // Determine the target VST Banks and Selections for all of those 3 Genre Parts
        for (int i = 0; i < 3; i ++)
        {
            string _matchTarget = commandParts[i + 1];

            // Create a copy of the possible array, but format it in a similar way:
            //     - Remove Hyphens
            //     - Replace Ü by U
            //     - Put everything to lowercase
            //     - Concatenate "& Crash" to simply "crash"
            string[] allowedStrings = GetGenrePartArraysByIndex(i).Select(s => s.ToLowerInvariant().Replace('ü', 'u').Replace("& crash", "crash").Replace("-", "")).ToArray();


            // In this array, get the index of the match
            int _matchResult = Array.IndexOf(allowedStrings, _matchTarget);
            if (_matchResult == -1)
            {
                yield return "sendtochaterror {0} Unable to find “" + _matchTarget + "” in the " + GetGenrePartDisplayNameByIndex(i) + " table. Submission aborted.";
                yield break;
            }

            // Then, taking Ruleseed into account, find if this is located into Appendix BEAT and where
            int _ruleseededMatchResult = Array.IndexOf(RuleseedAllowedGenreParts[i], _matchResult);
            if (_ruleseededMatchResult == -1)
            {
                yield return "sendtochaterror {0} “" + _matchTarget + "” is a valid " + GetGenrePartDisplayNameByIndex(i) + " but not for this Ruleseed. Submission aborted.";
                yield break;
            }

            // Then, extract VST Bank from that value
            int _targetVstBank = _ruleseededMatchResult / 5;

            int safetyExit = 0;
            while (_targetVstBank != selectedVstBanks[i])
            {
                safetyExit++;
                if (safetyExit > 100)
                {
                    yield return "sendtochaterror Something has gone incredibly wrong, the module is looping indefinitely trying to reach VstBank " + _targetVstBank + ". Please report this to thunder725. Autosolving module.";
                    SolveModule();
                    yield break;
                }

                // Cycle the VST Banks
                (i == 0 ? PrefixVstBankButton : i == 1 ? AdjectiveVstBankButton : StyleVstBankButton).OnInteract();

                yield return new WaitForSeconds(0.15f);
            }

            // Cycle the Possibilities!
            // But this time don't care about the position in Ruleseeded table, as
            // selectedGenreParts uses the actual id of the answer
            while (_matchResult != selectedGenreParts[i])
            {
                safetyExit++;
                if (safetyExit > 100)
                {
                    yield return "sendtochaterror Something has gone incredibly wrong, the module is looping indefinitely trying to reach Genre Part " + _matchResult + ". Please report this to thunder725. Autosolving module.";
                    SolveModule();
                    yield break;
                }

                // Cycle the individual options
                (i == 0 ? PrefixRightButton : i == 1 ? AdjectiveRightButton : StyleRightButton).OnInteract();

                yield return new WaitForSeconds(0.15f);
            }

            // Once we're here, we should have the correct submission shown on the module!
        }

        // Submit
        SubmitButton.OnInteract();

        yield break;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        ModuleLog(true, "Received Twitch ForceSolve!");

        string _submission = GetReadableFullGenreName(expectedGenreParts[0], expectedGenreParts[1], expectedGenreParts[2]);
        ModuleLog(true, "Submitting Twitch Plays Command for {0}", _submission);

        // Simulate receiving a TP command with the correct information
        IEnumerator processedCommand = ProcessTwitchCommand("submit " + _submission);

        while (processedCommand.MoveNext())
        {
            yield return processedCommand.Current;
        }
        yield break;
    }

}
