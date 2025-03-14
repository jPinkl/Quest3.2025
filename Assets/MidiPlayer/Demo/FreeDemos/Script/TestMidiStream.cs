﻿#define MPTK_PRO
//#define DEBUG_MULTI
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MidiPlayerTK;

namespace DemoMPTK
{
    public class TestMidiStream : MonoBehaviour
    {
        // MPTK component able to play a stream of midi events
        // Add a MidiStreamPlayer Prefab to your game object and defined midiStreamPlayer in the inspector with this prefab.
        public MidiStreamPlayer midiStreamPlayer;

        public Vector2 scale = new Vector2(1f, 1f);
        public Vector2 pivotPoint; // =new Vector2(0, 0);

        [Range(0.05f, 10f)]
        public float Frequency = 1;

        [Range(-10f, 100f)]
        public float NoteDuration = 0;

        [Range(0f, 10f)]
        public float NoteDelay = 0;

        public bool RandomPlay = true;
        public bool DrumKit = false;
        public bool ChordPlay = false;
        public int ArpeggioPlayChord = 0;
        public int DelayPlayScale = 200;
        public bool ChordLibPlay = false;
        public bool RangeLibPlay = false;
        public int CurrentChord;

        [Range(0, 127)]
        public int StartNote = 50;

        [Range(0, 127)]
        public int EndNote = 60;

        [Range(0, 127)]
        public int Velocity = 100;

        [Range(0, 16)]
        public int StreamChannel = 0;

        [Range(0, 16)]
        public int DrumChannel = 9; // by convention the channel 10 is used for playing drum (value = 9 because channel start from channel 0 in Maestro)

        [Range(0, 127)]
        public int CurrentNote;

        [Range(0, 127)]
        public int StartPreset = 0;

        [Range(0, 127)]
        public int EndPreset = 127;

        [Range(0, 127)]
        public int CurrentPreset;

        [Range(0, 127)]
        public int CurrentBank;

        [Range(0, 127)]
        public int CurrentPatchDrum;

        [Range(0, 127)]
        public int PanChange = 64;

        [Range(0, 127)]
        public int ModulationChange;

        [Range(0, 1)]
        public float PitchChange = DEFAULT_PITCH;

        [Range(0, 127)]
        public int ExpressionChange = 127; // default value

        [Range(0, 127)]
        public int AttenuationChange = 100; // default value

        const float DEFAULT_PITCH = 0.5f; // 8192;

        [Range(0, 24)]
        public int PitchSensi = 2;

        private float currentVelocityPitch;
        private float LastTimePitchChange;

        public int CountNoteToPlay = 1;
        public int CountNoteChord = 3;
        public int DegreeChord = 1;
        public int CurrentScale = 0;

        /// <summary>@brief
        /// Current note playing
        /// </summary>
        private MPTKEvent NotePlaying;

        private float LastTimeChange;

        /// <summary>@brief
        /// Popup to select an instrument
        /// </summary>
        private PopupListItem PopPatchInstrument;
        private PopupListItem PopBankInstrument;
        private PopupListItem PopPatchDrum;
        private PopupListItem PopBankDrum;

        // Popup to select a realtime generator
        private PopupListItem[] PopGenerator;
        private int[] indexGenerator;
        private string[] labelGenerator;
        private float[] valueGenerator;
        private const int nbrGenerator = 4;

        // Manage skin
        public CustomStyle myStyle;

        private Vector2 scrollerWindow = Vector2.zero;
        private int buttonLargeWidth = 500;
        private int buttonWidth = 250;
        private int buttonSmallWidth = 166;
        private int buttonTinyWidth = 100;
        private float spaceVertical = 0;
        private float widthFirstCol = 100;
        public bool IsplayingLoopNotes;
        public bool IsplayingLoopPresets;

        private void Awake()
        {
            if (midiStreamPlayer != null)
            {
                // Warning: depending on the starting orders of the GameObjects, 
                //          this call may be missed if MidiStreamPlayer is started before TestMidiStream, 
                //          so it is recommended to define these events in the inspector.

                // It's recommended to set calling this method in the prefab MidiStreamPlayer
                // from the Unity editor inspector. See "On Event Synth Awake". 
                // StartLoadingSynth will be called just before the initialization of the synthesizer.
                //midiStreamPlayer.OnEventSynthAwake.AddListener(StartLoadingSynth);

                // It's recommended to set calling this method in the prefab MidiStreamPlayer
                // from the Unity editor inspector. See "On Event Synth Started".
                // EndLoadingSynth will be called when the synthesizer is ready.
                //midiStreamPlayer.OnEventSynthStarted.AddListener(EndLoadingSynth);
            }
            else
                Debug.LogWarning("midiStreamPlayer is not defined. Check in Unity editor inspector of this gameComponent");
        }

        // Use this for initialization
        void Start()
        {
            //Debug.Log(Application.consoleLogPath);

            // Define popup to display to select preset and bank
            PopBankInstrument = new PopupListItem() { Title = "Select A Bank", OnSelect = PopupBankPatchChanged, Tag = "BANK_INST", ColCount = 5, ColWidth = 150, };
            PopPatchInstrument = new PopupListItem() { Title = "Select A Patch", OnSelect = PopupBankPatchChanged, Tag = "PATCH_INST", ColCount = 5, ColWidth = 150, };
            PopBankDrum = new PopupListItem() { Title = "Select A Bank", OnSelect = PopupBankPatchChanged, Tag = "BANK_DRUM", ColCount = 5, ColWidth = 150, };
            PopPatchDrum = new PopupListItem() { Title = "Select A Patch", OnSelect = PopupBankPatchChanged, Tag = "PATCH_DRUM", ColCount = 5, ColWidth = 150, };

            GenModifier.InitListGenerator();
            indexGenerator = new int[nbrGenerator];
            labelGenerator = new string[nbrGenerator];
            valueGenerator = new float[nbrGenerator];
            PopGenerator = new PopupListItem[nbrGenerator];
            for (int i = 0; i < nbrGenerator; i++)
            {
                indexGenerator[i] = GenModifier.RealTimeGenerator[0].Index;
                labelGenerator[i] = GenModifier.RealTimeGenerator[0].Label;
                if (indexGenerator[i] >= 0)
                    valueGenerator[i] = GenModifier.DefaultNormalizedVal((fluid_gen_type)indexGenerator[i]) * 100f;
                PopGenerator[i] = new PopupListItem() { Title = "Select A Generator", OnSelect = PopupGeneratorChanged, Tag = i, ColCount = 3, ColWidth = 250, };
            }
            LastTimeChange = Time.realtimeSinceStartup;
            CurrentNote = StartNote;
            LastTimeChange = -9999999f;
            PitchChange = DEFAULT_PITCH;
            CountNoteToPlay = 1;

        }

        // disabled
        void xxxOnApplicationFocus(bool hasFocus)
        {
            Debug.Log("TestMidiStream OnApplicationFocus " + hasFocus);
            if (!hasFocus)
            {
                midiStreamPlayer.MPTK_StopSynth();
                ///midiStreamPlayer.CoreAudioSource.enabled = false;
                midiStreamPlayer.CoreAudioSource.Stop();
            }
            else
            {
                //midiStreamPlayer.CoreAudioSource.enabled = true;
                midiStreamPlayer.CoreAudioSource.Play();
                midiStreamPlayer.MPTK_InitSynth();
            }
        }

        /// <summary>@brief
        /// The call of this method is defined in MidiPlayerGlobal from the Unity editor inspector. 
        /// The method is called when SoundFont is loaded. We use it only to statistics purpose.
        /// </summary>
        public void EndLoadingSF()
        {
            Debug.Log("End loading SoundFont. Statistics: ");

            //Debug.Log("List of presets available");
            //foreach (MPTKListItem preset in MidiPlayerGlobal.MPTK_ListPreset)
            //    Debug.Log($"   [{preset.Index,3:000}] - {preset.Label}");

            Debug.Log("   Time To Load SoundFont: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadSoundFont.TotalSeconds, 3).ToString() + " second");
            Debug.Log("   Time To Load Samples: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadWave.TotalSeconds, 3).ToString() + " second");
            Debug.Log("   Presets Loaded: " + MidiPlayerGlobal.MPTK_CountPresetLoaded);
            Debug.Log("   Samples Loaded: " + MidiPlayerGlobal.MPTK_CountWaveLoaded);
        }

        public void StartLoadingSynth(string name)
        {
            Debug.LogFormat($"Start loading Synth {name}");
        }

        //! [ExampleOnEventEndLoadingSynth]

        /// <summary>@brief
        /// This methods is run when the synthesizer is ready if you defined OnEventSynthStarted or set event from Inspector in Unity.
        /// It's a good place to set some synth parameter's as defined preset by channel 
        /// </summary>
        /// <param name="name"></param>
        public void EndLoadingSynth(string name)
        {
            Debug.LogFormat($"Synth {name} loaded, now change bank and preset");

            // It's recommended to defined callback method (here EndLoadingSynth) in the prefab MidiStreamPlayer from the Unity editor inspector. 
            // EndLoadingSynth will be called when the synthesizer will be ready.
            // These calls will not work in Unity Awake() or Startup() because Midi synth must be ready when changing preset and/or bank.

            // Mandatory for updating UI list but not for playing sample.
            // The default instrument and drum banks are defined with the popup "SoundFont Setup Alt-F" in the Unity editor.
            // This method can be used by script to change the instrument bank and build presets available for it: MPTK_ListPreset.
            MidiPlayerGlobal.MPTK_SelectBankInstrument(CurrentBank);

            // Don't forget to initialize your MidiStreamPlayer variable, see link below:
            // https://paxstellar.fr/api-mptk-v2/#DefinedVariablePrefab

            // Channel 0: set Piano (if SoundFont is GeneralUser GS v1.471)
            // Define bank with CurrentBank (value defined in inspector to 0).
            midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.BankSelectMsb, Value = CurrentBank, Channel = StreamChannel, });
            Debug.LogFormat($"   Bank '{CurrentBank}' defined on channel {StreamChannel}");

            // Defined preset with CurrentPreset (value defined in inspector to 0).
            midiStreamPlayer.MPTK_ChannelPresetChange(StreamChannel, CurrentPreset);
            Debug.LogFormat($"   Preset '{midiStreamPlayer.MPTK_ChannelPresetGetName(0)}' defined on channel {StreamChannel}");

            // Playing a preset from another bank in the channel 1

            // Channel 1: set Laser Gun (if SoundFont is GeneralUser GS v1.471)
            int channel = 1, bank = 2, preset = 127;
            midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.BankSelectMsb, Value = bank, Channel = channel, });
            midiStreamPlayer.MPTK_ChannelPresetChange(channel, preset);
            // MPTK_GetPatchName mandatory for getting the patch nane when the bank is not the default bank.
            Debug.LogFormat($"   Preset '{MidiPlayerGlobal.MPTK_GetPatchName(bank, preset)}' defined on channel {channel} and bank {bank}");
        }

        //! [ExampleOnEventEndLoadingSynth]

        [Header("Test MPTK_ChannelPresetChange for changing preset")]
        public bool Test_MPTK_ChannelPresetChange = false;

        /// <summary>@brief
        /// Two method are avaliable for changing preset and bank : 
        ///         MPTK_ChannelPresetChange(channel, preset, bank)
        ///     or standard MIDI 
        ///         // change bank
        ///         MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.BankSelectMsb, Value = index, Channel = StreamChannel, });
        ///         // change preset in the current bank
        ///         MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.PatchChange, Value = index, Channel = StreamChannel, });
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="index"></param>
        /// <param name="indexList"></param>
        private void PopupBankPatchChanged(object tag, int index, int indexList)
        {
            bool ret = true;

            //Debug.Log($"Bank or Patch Change {tag} {index} {indexList}");

            switch ((string)tag)
            {
                case "BANK_INST":
                    CurrentBank = index;
                    // This method build the preset list for the selected bank.
                    // This call doesn't change the MIDI bank used to play an instrument.
                    MidiPlayerGlobal.MPTK_SelectBankInstrument(index);
                    if (Test_MPTK_ChannelPresetChange)
                    {
                        // Change the bank number but not the preset, we need to retrieve the current preset for this channel
                        int currentPresetInst = midiStreamPlayer.MPTK_ChannelPresetGetIndex(StreamChannel);
                        // Change the bank but not the preset. Return false if the preset is not found.
                        ret = midiStreamPlayer.MPTK_ChannelPresetChange(StreamChannel, currentPresetInst, index);
                    }
                    else
                        // Change bank withe the standard MIDI message
                        midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.BankSelectMsb, Value = index, Channel = StreamChannel, });

                    Debug.Log($"Instrument bank change - channel:{StreamChannel} bank:{midiStreamPlayer.MPTK_ChannelBankGetIndex(StreamChannel)} preset:{midiStreamPlayer.MPTK_ChannelPresetGetIndex(StreamChannel)}");
                    break;

                case "PATCH_INST":
                    CurrentPreset = index;
                    if (Test_MPTK_ChannelPresetChange)
                        // Change the preset number but not the bank. Return false if the preset is not found.
                        ret = midiStreamPlayer.MPTK_ChannelPresetChange(StreamChannel, index);
                    else
                        midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.PatchChange, Value = index, Channel = StreamChannel, });

                    Debug.Log($"Instrument Preset change - channel:{StreamChannel} bank:{midiStreamPlayer.MPTK_ChannelBankGetIndex(StreamChannel)} preset:{midiStreamPlayer.MPTK_ChannelPresetGetIndex(StreamChannel)}");
                    break;

                case "BANK_DRUM":
                    // This method build the preset list for the selected bank.
                    // This call doesn't change the MIDI bank used to play an instrument.
                    MidiPlayerGlobal.MPTK_SelectBankDrum(index);
                    if (Test_MPTK_ChannelPresetChange)
                    {
                        // Change the bank number but not the preset, we need to retrieve the current preset for this channel
                        int currentPresetDrum = midiStreamPlayer.MPTK_ChannelPresetGetIndex(DrumChannel);
                        // Change the bank but not the preset. Return false if the preset is not found.
                        ret = midiStreamPlayer.MPTK_ChannelPresetChange(DrumChannel, currentPresetDrum, index);
                    }
                    else
                        // Change bank with the standard MIDI message
                        midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.BankSelectMsb, Value = index, Channel = DrumChannel, });

                    Debug.Log($"Drum bank change - channel:{StreamChannel} bank:{midiStreamPlayer.MPTK_ChannelBankGetIndex(9)} preset:{midiStreamPlayer.MPTK_ChannelPresetGetIndex(DrumChannel)}");
                    break;

                case "PATCH_DRUM":
                    CurrentPatchDrum = index;
                    if (Test_MPTK_ChannelPresetChange)
                        // Change the preset number but not the bank. Return false if the preset is not found.
                        ret = midiStreamPlayer.MPTK_ChannelPresetChange(DrumChannel, index);
                    else
                        midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.PatchChange, Value = index, Channel = DrumChannel });

                    Debug.Log($"Drum preset change - channel:{StreamChannel} bank:{midiStreamPlayer.MPTK_ChannelBankGetIndex(DrumChannel)} preset:{midiStreamPlayer.MPTK_ChannelPresetGetIndex(DrumChannel)}");
                    break;
            }
        }

        private void PopupGeneratorChanged(object tag, int index, int indexList)
        {
            int iGenerator = Convert.ToInt32(tag);
            indexGenerator[iGenerator] = index;
            labelGenerator[iGenerator] = GenModifier.RealTimeGenerator[indexList].Label;
            valueGenerator[iGenerator] = GenModifier.DefaultNormalizedVal((fluid_gen_type)indexGenerator[iGenerator]) * 100f;
            Debug.Log($"indexList:{indexList} indexGenerator:{indexGenerator[iGenerator]} valueGenerator:{valueGenerator[iGenerator]} {labelGenerator[iGenerator]}");
        }

        void OnGUI()
        {
            GUIUtility.ScaleAroundPivot(scale, pivotPoint);
            //Debug.Log($"{Screen.width} x {Screen.height} safeArea:{Screen.safeArea} ScreenToGUIRect:{GUIUtility.ScreenToGUIRect(Screen.safeArea)}");
            // Set custom Style.
            if (myStyle == null) myStyle = new CustomStyle();

            // midiStreamPlayer must be defined with the inspector of this gameObject 
            // Otherwise  you could use : midiStreamPlayer fp = FindObjectOfType<MidiStreamPlayer>(); in the Start() method
            if (midiStreamPlayer != null)
            {
                scrollerWindow = GUILayout.BeginScrollView(scrollerWindow, false, false, GUILayout.Width(Screen.width));

                // If need, display the popup  before any other UI to avoid trigger it hidden
                if (HelperDemo.CheckSFExists())
                {
                    PopBankInstrument.Draw(MidiPlayerGlobal.MPTK_ListBank, CurrentBank, myStyle);
                    PopPatchInstrument.Draw(MidiPlayerGlobal.MPTK_ListPreset, CurrentPreset, myStyle);
                    PopBankDrum.Draw(MidiPlayerGlobal.MPTK_ListBank, MidiPlayerGlobal.ImSFCurrent.DrumKitBankNumber, myStyle);
                    PopPatchDrum.Draw(MidiPlayerGlobal.MPTK_ListPresetDrum, CurrentPatchDrum, myStyle);

                    for (int i = 0; i < nbrGenerator; i++)
                        PopGenerator[i].Draw(GenModifier.RealTimeGenerator, indexGenerator[i], myStyle);

                    MainMenu.Display("Test MIDI Stream - A very simple Generated Music Stream ", myStyle, "https://paxstellar.fr/midi-file-player-detailed-view-2-2/");

                    // Display soundfont available and select a new one
                    GUISelectSoundFont.Display(scrollerWindow, myStyle);

                    GUILayout.BeginVertical(myStyle.BacgDemos1);

                    try
                    {
                        //
                        // Select bank & Patch for Instrument
                        // ----------------------------------
                        SelectBankAndPatchForInstrument();

                        //
                        // Select bank & Patch for Drum
                        // ----------------------------
                        SelectBankAndPatchForDrum();

                        //
                        // Change bank or preset with free value with MPTK_ChannelPresetChange
                        // -------------------------------------------------------------------
                        ChangeBankAndPresetWithFreeValue();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Error in  Select bank & Patch {ex}");
                        GUILayout.EndHorizontal();
                    }
                    finally
                    {
                        GUILayout.EndVertical();
                    }
                }

                //
                // Display info and synth stats
                // ----------------------------
                GUILayout.BeginVertical(myStyle.BacgDemos1);
                try
                {
                    HelperDemo.DisplayInfoSynth(midiStreamPlayer, 500, myStyle);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error in DisplayInfoSynth() {ex}");
                    GUILayout.EndHorizontal();
                }
                finally
                {
                    GUILayout.EndVertical();
                }

                GUILayout.Space(spaceVertical);

                //
                // Play one note 
                // --------------
                GUILayout.BeginVertical(myStyle.BacgDemos1);
                try
                {
                    PlayNote();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error in PlayNote() {ex}");
                    GUILayout.EndHorizontal();
                }
                finally
                {
                    GUILayout.EndVertical();
                }

                GUILayout.Space(spaceVertical);

                //
                // Play note loop and preset loop
                // ------------------------------
                GUILayout.BeginVertical(myStyle.BacgDemos1);
                try
                {
                    PlayNoteAndPresetInLoop();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error in PlayNoteAndPresetInLoop() {ex}");
                    GUILayout.EndHorizontal();
                }
                finally
                {
                    GUILayout.EndVertical();
                }

                GUILayout.Space(spaceVertical);
#if DEBUG_MULTI
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(" ", myStyle.TitleLabel3, GUILayout.Width(widthFirstCol));
                                CountNoteToPlay = (int)Slider("Play Multiple Notes", CountNoteToPlay, 1, 200, false, 70);
                                GUILayout.EndHorizontal();
                                GUILayout.Space(spaceVertical);
#endif

                //
                // Build chord and scale (Pro)
                // ---------------------------
                GUILayout.BeginVertical(myStyle.BacgDemos1);
                try
                {
                    BuildChordAndScale();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error in BuildChordAndScale() {ex}");
                    GUILayout.EndHorizontal();
                }
                finally
                {
                    GUILayout.EndVertical();
                }

                GUILayout.Space(spaceVertical);


                //
                // Change value from Midi Command
                // ------------------------------
                GUILayout.BeginVertical(myStyle.BacgDemos1);
                try
                {
                    RealTimeMidiCommandChange();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error in RealTimeMidiCommandChange() {ex}");
                    GUILayout.EndHorizontal();
                }
                finally
                {
                    GUILayout.EndVertical();
                }

                GUILayout.Space(spaceVertical);

                //
                // Change value from Generator Synth
                // ---------------------------------
                GUILayout.BeginVertical(myStyle.BacgDemos1);
                try
                {
                    RealTimeVoiceParametersChange();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error in RealTimeVoiceParametersChange() {ex}");
                    GUILayout.EndHorizontal();
                }
                finally
                {
                    GUILayout.EndVertical();
                }

                GUILayout.Space(spaceVertical);

                // Display footer
                // --------------
                GUILayout.BeginVertical(myStyle.BacgDemos);
                GUILayout.BeginHorizontal();
                if (!string.IsNullOrEmpty(Application.consoleLogPath))
                {
                    //if (GUILayout.Button("Clear Log ")) Debug.ClearDeveloperConsole();
                    if (GUILayout.Button("Open Folder " + System.IO.Path.GetDirectoryName(Application.consoleLogPath))) Application.OpenURL("file://" + System.IO.Path.GetDirectoryName(Application.consoleLogPath));
                    if (GUILayout.Button("Open Log File")) Application.OpenURL("file://" + Application.consoleLogPath);
                }
                else
                    GUILayout.Label("current platform does not support log files");
                GUILayout.EndHorizontal();

                GUILayout.Label("Go to your Hierarchy, select GameObject MidiStreamPlayer: inspector contains a lot of parameters to control the sound.", myStyle.TitleLabel2);
                GUILayout.EndVertical();

                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Space(spaceVertical);
                GUILayout.Label("MidiStreamPlayer not defined, check hierarchy.", myStyle.TitleLabel3);
            }
        }

        private void ChangeBankAndPresetWithFreeValue()
        {
            GUILayout.BeginHorizontal();
            //! [Example MPTK_ChannelPresetChange]

            // Change bank or preset with free value with MPTK_ChannelPresetChange
            // -------------------------------------------------------------------

            // Select any value in the range 0 and 16383 for the bank 
            int bank = (int)Slider("Free Bank", midiStreamPlayer.MPTK_ChannelBankGetIndex(StreamChannel), 0, 128 * 128 - 1, alignright: false, wiLab: 80, wiSlider: 200, wiLabelValue: 100);
            // Select any value in the range 0 and 127 for the preset
            int prst = (int)Slider("Free Preset", midiStreamPlayer.MPTK_ChannelPresetGetIndex(StreamChannel), 0, 127, alignright: false, wiLab: 80, wiSlider: 200);
            // If user made change for bank or preset ...
            if (bank != midiStreamPlayer.MPTK_ChannelBankGetIndex(StreamChannel) ||
                prst != midiStreamPlayer.MPTK_ChannelPresetGetIndex(StreamChannel))
            {
                // ... apply the change to the MidiStreamPlayer for the current channel.
                // If the bank or the preset doestn't exist 
                //      - the method returns false 
                //      - the bank and preset are still registered in the channel
                //      - when a note-on is received on this channel, the first preset of the first bank is used to play (usually piano).
                bool ret = midiStreamPlayer.MPTK_ChannelPresetChange(StreamChannel, prst, bank);

                // Read the current bank, preset and preset name selected
                int newbank = midiStreamPlayer.MPTK_ChannelBankGetIndex(StreamChannel);
                int newpreset = midiStreamPlayer.MPTK_ChannelPresetGetIndex(StreamChannel);
                string newname = midiStreamPlayer.MPTK_ChannelPresetGetName(StreamChannel);
                Debug.Log($"MPTK_ChannelPresetChange result:{ret} bank:{newbank} preset:{newpreset} '{newname}'");
            }
            //! [Example MPTK_ChannelPresetChange]

            //GUILayout.Label("If preset not found, The first found is selected i", myStyle.TitleLabel3);
            GUILayout.EndHorizontal();
        }

        private void SelectBankAndPatchForDrum()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Drum", myStyle.TitleLabel3, GUILayout.Width(widthFirstCol));

            // Open the popup to select a bank for drum
            if (GUILayout.Button(MidiPlayerGlobal.ImSFCurrent.DrumKitBankNumber + " - Bank", GUILayout.Width(buttonWidth)))
                PopBankDrum.Show = !PopBankDrum.Show;
            PopBankDrum.PositionWithScroll(ref scrollerWindow);

            // Open the popup to select an instrument for drum
            if (GUILayout.Button(
                CurrentPatchDrum.ToString() + " - " +
                MidiPlayerGlobal.MPTK_GetPatchName(MidiPlayerGlobal.ImSFCurrent.DrumKitBankNumber, CurrentPatchDrum),
                GUILayout.Width(buttonWidth)))
                PopPatchDrum.Show = !PopPatchDrum.Show;
            PopPatchDrum.PositionWithScroll(ref scrollerWindow);

            GUILayout.Label(" ", GUILayout.Width(42));

            bool newDrumKit = GUILayout.Toggle(DrumKit, "Activate Drum Mode", GUILayout.Width(buttonLargeWidth));
            if (newDrumKit != DrumKit)
            {
                DrumKit = newDrumKit;
                // Set channel to dedicated drum channel 9 
                StreamChannel = DrumKit ? 9 : 0;
            }
            GUILayout.EndHorizontal();
        }

        private void SelectBankAndPatchForInstrument()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Instrument", myStyle.TitleLabel3, GUILayout.Width(widthFirstCol));

            // Open the popup to select a bank
            if (GUILayout.Button(MidiPlayerGlobal.ImSFCurrent.DefaultBankNumber + " - Bank", GUILayout.Width(buttonWidth)))
                PopBankInstrument.Show = !PopBankInstrument.Show;
            PopBankInstrument.PositionWithScroll(ref scrollerWindow);

            // Open the popup to select an instrument
            if (GUILayout.Button(CurrentPreset.ToString() + " - " + MidiPlayerGlobal.MPTK_GetPatchName(MidiPlayerGlobal.ImSFCurrent.DefaultBankNumber, CurrentPreset), GUILayout.Width(buttonWidth)))
                PopPatchInstrument.Show = !PopPatchInstrument.Show;
            PopPatchInstrument.PositionWithScroll(ref scrollerWindow);

            int channel = (int)Slider("Channel", StreamChannel, 0, 15, true, 100, 200);
            if (channel != StreamChannel)
            {
                StreamChannel = channel;
                Debug.Log($"Change to channel:{StreamChannel}");
                Debug.Log($"        bank: {midiStreamPlayer.MPTK_ChannelBankGetIndex(StreamChannel)}");
                Debug.Log($"        preset: {midiStreamPlayer.MPTK_ChannelPresetGetIndex(StreamChannel)}");
                Debug.Log($"        name: '{midiStreamPlayer.MPTK_ChannelPresetGetName(StreamChannel)}'");
            }
            GUILayout.EndHorizontal();
        }

        private void RealTimeVoiceParametersChange()
        {
            GUILayout.Label("Real Time Voice Parameters Change [Availablle with MPTK Pro]. Experimental feature.", myStyle.TitleLabel3);
            float gene;
            for (int i = 0; i < nbrGenerator; i += 2) // 2 generators per line
            {
                GUILayout.BeginHorizontal(GUILayout.Width(650));

                for (int j = 0; j < 2; j++) // 2 generators per line
                {
                    int numGenerator = i + j;
                    // Open the popup to select an instrument
                    if (GUILayout.Button(indexGenerator[numGenerator] + " - " + labelGenerator[numGenerator], GUILayout.Width(buttonWidth)))
                        PopGenerator[numGenerator].Show = !PopGenerator[numGenerator].Show;
                    // Get real time value
                    gene = Slider("Value", valueGenerator[numGenerator], 0f, 100f, true, 50f, 80f);
                    if (indexGenerator[numGenerator] >= 0)
                    {
#if MPTK_PRO
                        // If value is different then applied to the current note playing
                        if (valueGenerator[numGenerator] != gene && NotePlaying != null)
                            NotePlaying.MTPK_ModifySynthParameter((fluid_gen_type)indexGenerator[numGenerator], valueGenerator[numGenerator] / 100f, MPTKModeGeneratorChange.Override);

                        //MPTKEvent mptkEvent = new MPTKEvent()
                        //{
                        //    Command = MPTKCommand.NoteOn, // midi command
                        //    Value = 50, // from 0 to 127, 48 for C3, 60 for C4, ...
                        //    Channel = 0, // from 0 to 15, 9 reserved for drum
                        //    Duration = -1, // note duration in millisecond, -1 to play indefinitely, MPTK_StopEvent to stop
                        //    Velocity = 100, // from 0 to 127, sound can vary depending on the velocity
                        //    Delay = 0, // delay in millisecond before playing the note
                        //};
                        //mptkEvent.MTPK_ModifySynthParameter(fluid_gen_type.GEN_FILTERFC, 0.5f, MPTKModeGeneratorChange.Override);
                        //midiStreamPlayer.MPTK_PlayEvent(mptkEvent);   


#endif
                        valueGenerator[numGenerator] = gene;
                    }
                    GUILayout.Label(" ", myStyle.TitleLabel3, GUILayout.Width(60));
                }

                GUILayout.EndHorizontal();
            }
        }

        private void PlayNote()
        {
            GUILayout.BeginHorizontal(GUILayout.Width(350));
            GUILayout.Label("One Shot", myStyle.TitleLabel3, GUILayout.Width(widthFirstCol));
            if (GUILayout.Button("Play", myStyle.BtStandard, GUILayout.Width(buttonSmallWidth)))
            {
                // Stop current note if playing
                StopOneNote();
                // Play one note 
                PlayOneNote();
            }
            if (GUILayout.Button("Stop", myStyle.BtStandard, GUILayout.Width(buttonSmallWidth)))
            {
                StopOneNote();
                StopChord();
            }

            if (GUILayout.Button("Clear", myStyle.BtStandard, GUILayout.Width(buttonSmallWidth)))
            {
                midiStreamPlayer.MPTK_ClearAllSound(true);
            }

            if (GUILayout.Button("Re-init", myStyle.BtStandard, GUILayout.Width(buttonSmallWidth)))
            {
                midiStreamPlayer.MPTK_InitSynth();
                CurrentPreset = CurrentPatchDrum = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Width(500));

            //if (GUILayout.Button("Test", myStyle.BtStandard, GUILayout.Width(buttonWidth * 0.666f)))
            //{
            //    //midiStreamPlayer.MPTK_KillByExclusiveClass = false;

            //    NotePlaying = new MPTKEvent() { Command = MPTKCommand.NoteOn, Value = 36, Channel = 9, Duration = 1000, Velocity = 10, };// Bass_drum channel 9
            //    midiStreamPlayer.MPTK_PlayEvent(NotePlaying);

            //    NotePlaying = new MPTKEvent() { Command = MPTKCommand.NoteOn, Value = 42, Channel = 9, Duration = 1000, Velocity = 80, };// Closed Hihat channel 9 
            //    midiStreamPlayer.MPTK_PlayEvent(NotePlaying);
            //}

            CurrentNote = (int)Slider("Note", CurrentNote, 0, 127);
            int preset = (int)Slider("Preset", CurrentPreset, 0, 127, true);
            if (preset != CurrentPreset)
            {
                CurrentPreset = preset;
                midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent()
                {
                    Command = MPTKCommand.PatchChange,
                    Value = CurrentPreset,
                    Channel = StreamChannel,
                });
            }
            NoteDuration = Slider("Duration", NoteDuration, -1f, 10f, true);
            NoteDelay = Slider("Delay", NoteDelay, 0f, 1f, true);
            GUILayout.EndHorizontal();
        }

        private void PlayNoteAndPresetInLoop()
        {
            GUILayout.BeginHorizontal(GUILayout.Width(500));
            GUILayout.Label("Loop on Notes and Presets", myStyle.TitleLabel3, GUILayout.Width(220));
            Frequency = Slider("Loop Delay (s)", Frequency, 0.01f, 5f, true, 120, 100);
            NoteDuration = Slider("Duration", NoteDuration, -1f, 10f, true);
            GUILayout.Label(" ", myStyle.TitleLabel3, GUILayout.Width(30));
            RandomPlay = GUILayout.Toggle(RandomPlay, "Random Notes", GUILayout.Width(120));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Width(350));
            GUILayout.Label("Loop Notes", myStyle.TitleLabel3, GUILayout.Width(widthFirstCol));
            if (GUILayout.Button("Start / Stop", IsplayingLoopNotes ? myStyle.BtSelected : myStyle.BtStandard, GUILayout.Width(buttonSmallWidth))) IsplayingLoopNotes = !IsplayingLoopNotes;
            StartNote = (int)Slider("From", StartNote, 0, 127, true);
            EndNote = (int)Slider("To", EndNote, 0, 127, true);
            GUILayout.EndHorizontal();

            //         GUILayout.Space(spaceVertical);

            GUILayout.BeginHorizontal(GUILayout.Width(350));
            GUILayout.Label("Loop Presets", myStyle.TitleLabel3, GUILayout.Width(widthFirstCol));
            if (GUILayout.Button("Start / Stop", IsplayingLoopPresets ? myStyle.BtSelected : myStyle.BtStandard, GUILayout.Width(buttonSmallWidth))) IsplayingLoopPresets = !IsplayingLoopPresets;
            StartPreset = (int)Slider("From", StartPreset, 0, 127, true);
            EndPreset = (int)Slider("To", EndPreset, 0, 127, true);
            GUILayout.EndHorizontal();
        }

        private void BuildChordAndScale()
        {
            GUILayout.Label("Build Chord and Range [Availablle with MPTK Pro]", myStyle.TitleLabel3);

            GUILayout.BeginHorizontal();
            GUILayout.Label(" ", myStyle.TitleLabel3, GUILayout.Width(widthFirstCol));

            ChordPlay = GUILayout.Toggle(ChordPlay, "Play Chord From Degree", GUILayout.Width(170));
            if (ChordPlay) { ChordLibPlay = false; RangeLibPlay = false; }

            ChordLibPlay = GUILayout.Toggle(ChordLibPlay, "Play Chord From Lib", GUILayout.Width(170));
            if (ChordLibPlay) { ChordPlay = false; RangeLibPlay = false; }

            RangeLibPlay = GUILayout.Toggle(RangeLibPlay, "Play Range From Lib", GUILayout.Width(170));
            if (RangeLibPlay) { ChordPlay = false; ChordLibPlay = false; }

            GUILayout.EndHorizontal();

            // Build a chord from degree
            if (ChordPlay)
            {
                BuildChordFromdegree();
            }

            // Build a chord from a library
            if (ChordLibPlay)
            {
                BuildChordFromLibrary();
            }

            if (RangeLibPlay)
            {
                BuildRangeFromLib();
            }
            //#else
            //if (ChordPlay || ChordLibPlay || RangeLibPlay)
            //{
            //    GUILayout.BeginVertical(myStyle.BacgDemos1);
            //    GUILayout.Space(spaceVertival);
            //    GUILayout.Label("Chord and Range are available only with MPTK PRO", myStyle.TitleLabel3);
            //    GUILayout.Space(spaceVertival);
            //    GUILayout.EndVertical();
            //}
            //#endif
        }

        private void RealTimeMidiCommandChange()
        {
            GUILayout.Label("Real Time MIDI Command Change", myStyle.TitleLabel3);

            GUILayout.BeginHorizontal(GUILayout.Width(350));

            // Change pitch (automatic return to center as a physical keyboard!)
            // 0 is the lowest bend positions(default is 2 semitones), 
            // 0.5 centered value, the sounding notes aren't being transposed up or down,
            // 1 is the highest pitch bend position (default is 2 semitones)
            float pitchChange = Slider("Pitch Change", PitchChange, 0, 1, false);
            if (pitchChange != PitchChange)
            {
                LastTimePitchChange = Time.realtimeSinceStartup;
                PitchChange = pitchChange;
#if MPTK_PRO
                midiStreamPlayer.MPTK_PlayPitchWheelChange(StreamChannel, PitchChange);
#else
                    Debug.Log("Pitch change: Pro only");
#endif
            }

            // midi pitch sensitivity change for all notes on the channel.
            // Pitch change sensitivity from 0 to 24 semitones up and down. Default value 2.
            // Example: 4, means semitons range is from -4 to 4 when MPTK_PlayPitchWheelChange change from 0 to 1.
            int pitchSensi = (int)Slider("Pitch Sensibility", PitchSensi, 0, 24, true);
            if (pitchSensi != PitchSensi)
            {
                PitchSensi = pitchSensi;
#if MPTK_PRO
                midiStreamPlayer.MPTK_PlayPitchWheelSensitivity(StreamChannel, PitchSensi);
#else
                    Debug.Log("Pitch sensitivity change: Pro only");
#endif
            }

            midiStreamPlayer.MPTK_Transpose = (int)Slider("Transpose", midiStreamPlayer.MPTK_Transpose, -24, 24, true);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Width(350));

            // Change volume
            midiStreamPlayer.MPTK_Volume = Slider("Global Volume", midiStreamPlayer.MPTK_Volume, 0, 1);

            // Change velocity of the note: what force is applied on the key. Change volume and sound of the note.
            Velocity = (int)Slider("Velocity", (int)Velocity, 0f, 127f, true);

            // Change left / right stereo
            int panChange = (int)Slider("Panoramic", PanChange, 0, 127, true);
            if (panChange != PanChange)
            {
                PanChange = panChange;
                midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.Pan, Value = PanChange, Channel = StreamChannel });
            }
            GUILayout.EndHorizontal();

            // Change modulation. Often applied a vibrato, this effect is defined in the SoundFont 
            GUILayout.BeginHorizontal(GUILayout.Width(350));
            int modChange = (int)Slider("Modulation", ModulationChange, 0, 127);
            if (modChange != ModulationChange)
            {
                ModulationChange = modChange;
                midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.Modulation, Value = ModulationChange, Channel = StreamChannel });
            }

            // Change modulation. Often applied volume, this effect is defined in the SoundFont 
            int expChange = (int)Slider("Expression", ExpressionChange, 0, 127, true);
            if (expChange != ExpressionChange)
            {
                ExpressionChange = expChange;
                midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.Expression, Value = ExpressionChange, Channel = StreamChannel });
            }

            // Change modulation. Often applied volume, this effect is defined in the SoundFont 
            int expAttenuation = (int)Slider("Attenuation", AttenuationChange, 0, 127, true);
            if (expAttenuation != AttenuationChange)
            {
                AttenuationChange = expAttenuation;
                midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.VOLUME_MSB, Value = AttenuationChange, Channel = StreamChannel });
            }
            GUILayout.EndHorizontal();

            // Change modulation. Sustain 
            // The Sustain Pedal CC64 is one of the most commont MIDI CC messages, used to hold played notes
            // while the sustain pedal is active/depressed. Values of 0-63 indicate OFF. Values 64-127 indicate ON.
            // https://www.presetpatch.com/midi-cc-list.aspx
            GUILayout.BeginHorizontal();
            GUILayout.Label("Sustain switch", myStyle.LabelRight, GUILayout.Width(120), GUILayout.Height(25));
            bool sustain = midiStreamPlayer.MPTK_ChannelControllerGet(StreamChannel, (int)MPTKController.Sustain) < 64 ? false : true;
            if (GUILayout.Button(sustain ? "Sustain On" : "Sustain Off", sustain ? myStyle.BtSelected : myStyle.BtStandard, GUILayout.Width(buttonTinyWidth)))
                if (sustain)
                    midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent()
                    { Command = MPTKCommand.ControlChange, Controller = MPTKController.Sustain, Value = 0, Channel = StreamChannel });
                else
                    midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent()
                    { Command = MPTKCommand.ControlChange, Controller = MPTKController.Sustain, Value = 100, Channel = StreamChannel });
            GUILayout.EndHorizontal();

            //GUILayout.EndHorizontal();
        }

        private void BuildRangeFromLib()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("From Lib", myStyle.TitleLabel3, GUILayout.Width(widthFirstCol));
            DelayPlayScale = (int)Slider("Delay (ms)", DelayPlayScale, 100, 1000, false, 70);
            GUILayout.EndHorizontal();
            GUIForScale();
        }

        private void BuildChordFromLibrary()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("From Lib", myStyle.TitleLabel3, GUILayout.Width(widthFirstCol));
            GUILayout.Label("Chord", myStyle.TitleLabel3, GUILayout.MaxWidth(70));

            if (GUILayout.Button("-", GUILayout.Width(30)))
            {
#if MPTK_PRO
                CurrentChord--;
                if (CurrentChord < 0) CurrentChord = MPTKChordLib.Chords.Count - 1;
                Play(true);
#endif
            }

            string strChord = GUILayout.TextField(CurrentChord.ToString(), 2, GUILayout.Width(50));
            int chord = 0;
            try
            {
                chord = Convert.ToInt32(strChord);
            }
            catch (Exception) { }
            if (chord != CurrentChord)
            {
#if MPTK_PRO
                CurrentChord = Mathf.Clamp(chord, 0, MPTKChordLib.Chords.Count - 1);
                Play(true);
#endif
            }

            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
#if MPTK_PRO
                CurrentChord++;
                if (CurrentChord >= MPTKChordLib.Chords.Count) CurrentChord = 0;
                Play(true);
#endif
            }

            if (GUILayout.Button("Play", GUILayout.Width(50)))
            {
                Play(true);
            }
#if MPTK_PRO
            GUILayout.Label($"{MPTKChordLib.Chords[CurrentChord].Name}", myStyle.TitleLabel3, GUILayout.MaxWidth(100));
            GUILayout.Label("See file ChordLib.csv in folder Resources/GeneratorTemplate", myStyle.TitleLabel3, GUILayout.Width(500));
#endif
            GUILayout.EndHorizontal();
        }

        private void BuildChordFromdegree()
        {
            GUILayout.BeginHorizontal(GUILayout.Width(600));
            GUILayout.Label("From Degree", myStyle.TitleLabel3, GUILayout.Width(widthFirstCol));

            int countNote = (int)Slider("Count", CountNoteChord, 2, 17, false, 70);
            if (countNote != CountNoteChord)
            {
                CountNoteChord = countNote;
                Play(true);
            }

            int degreeChord = (int)Slider("Degree", DegreeChord, 1, 7, false, 70);
            if (degreeChord != DegreeChord)
            {
                DegreeChord = degreeChord;
                Play(true);
            }

            ArpeggioPlayChord = (int)Slider("Arpeggio (ms)", ArpeggioPlayChord, 0, 500, false, 70);

            GUILayout.EndHorizontal();
            GUIForScale();
        }

        //#if MPTK_PRO

        /// <summary>@brief
        /// Common UI for building and playing a chord or a scale from the library of scale
        /// See in GUI "Play Chord From Degree" and "Play Range From Lib"
        /// </summary>
        private void GUIForScale()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(" ", myStyle.TitleLabel3, GUILayout.Width(widthFirstCol));
            GUILayout.Label("Scale", myStyle.TitleLabel3, GUILayout.MaxWidth(70));

            // Display and play with the previous scale 
            if (GUILayout.Button("-", GUILayout.Width(30)))
            {
#if MPTK_PRO
                CurrentScale--;
                if (CurrentScale < 0) CurrentScale = MPTKRangeLib.RangeCount - 1;
                // Select the range from a list of range. See file Resources/GeneratorTemplate/GammeDefinition.csv 
                midiStreamPlayer.MPTK_RangeSelected = CurrentScale;
                Play(true);
#endif
            }

            // Seizes the index of the scale
            string strScale = GUILayout.TextField(CurrentScale.ToString(), 2, GUILayout.Width(50));
            int scale = 0;
            try
            {
                scale = Convert.ToInt32(strScale);
            }
            catch (Exception) { }

            if (scale != CurrentScale)
            {
#if MPTK_PRO
                CurrentScale = Mathf.Clamp(scale, 0, MPTKRangeLib.RangeCount - 1);
                // Select the range from a list of range. See file Resources/GeneratorTemplate/GammeDefinition.csv 
                midiStreamPlayer.MPTK_RangeSelected = CurrentScale;
                Play(true);
#endif
            }

            // Display and play with the next scale/range 
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
#if MPTK_PRO
                CurrentScale++;
                if (CurrentScale >= MPTKRangeLib.RangeCount) CurrentScale = 0;
                // Select the range from a list of range. See file Resources/GeneratorTemplate/GammeDefinition.csv 
                midiStreamPlayer.MPTK_RangeSelected = CurrentScale;
                Play(true);
#endif
            }

            // Button play yo play the current range/scale
            if (GUILayout.Button("Play", GUILayout.Width(50)))
            {
                Play(true);
            }
#if MPTK_PRO
            GUILayout.Label($"{midiStreamPlayer.MPTK_RangeName}", myStyle.TitleLabel3, GUILayout.MaxWidth(100));
            GUILayout.Label("See GammeDefinition.csv in folder Resources/GeneratorTemplate", myStyle.TitleLabel3, GUILayout.Width(500));
#endif
            GUILayout.EndHorizontal();
        }
        //#endif

        private float Slider(string title, float val, float min, float max, bool alignright = false, float wiLab = 100, float wiSlider = 100, float wiLabelValue = 30)
        {
            float ret;
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, alignright ? myStyle.LabelRight : myStyle.LabelLeft, GUILayout.Width(wiLab), GUILayout.Height(25));
            GUILayout.Label(Math.Round(val, 2).ToString(), myStyle.LabelRight, GUILayout.Width(wiLabelValue), GUILayout.Height(25));
            ret = GUILayout.HorizontalSlider(val, min, max, myStyle.SliderBar, myStyle.SliderThumb, GUILayout.Width(wiSlider));
            GUILayout.EndHorizontal();
            return ret;
        }

        private MPTKEvent[] eventsMidi;
        // blues en C minor: C,D#,F,F#,G,A# http://patrick.murris.com/musique/gammes_piano.htm?base=3&scale=0%2C3%2C5%2C6%2C7%2C10&octaves=1
        private int[] keysToNote = { 60, 63, 65, 66, 67, 70, 72, 75, 77 };

        // Update is called once per frame
        void Update()
        {

            // Check that SoundFont is loaded and add a little wait (0.5 s by default) because Unity AudioSource need some time to be started
            if (!MidiPlayerGlobal.MPTK_IsReady())
                return;

            // Better in Start(), it's here only for the demo clarity
            if (eventsMidi == null)
                eventsMidi = new MPTKEvent[10];

            for (int key = 0; key < 9; key++)
            {
                // Check if key 1 to 9 is down (top alpha keyboard)
                if (Input.GetKeyDown(KeyCode.Alpha1 + key))
                {
                    // Create a new note and play
                    eventsMidi[key] = new MPTKEvent()
                    {
                        Command = MPTKCommand.NoteOn,
                        Channel = StreamChannel,
                        Duration = -1,
                        Value = keysToNote[key],
                        Velocity = 100
                    };
                    // Send the note-on to the MIDI synth
                    midiStreamPlayer.MPTK_PlayEvent(eventsMidi[key]);
                }

                // If the note is active and the corresponding key is up then stop the note
                if (eventsMidi[key] != null && Input.GetKeyUp(KeyCode.Alpha1 + key))
                {
                    midiStreamPlayer.MPTK_StopEvent(eventsMidi[key]);
                    eventsMidi[key] = null;
                }
            }

            // Change preset with arrow key
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (Input.GetKeyDown(KeyCode.DownArrow)) CurrentPreset--;
                if (Input.GetKeyDown(KeyCode.UpArrow)) CurrentPreset++;
                CurrentPreset = Mathf.Clamp(CurrentPreset, 0, 127);
                midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent()
                {
                    Command = MPTKCommand.PatchChange,
                    Value = CurrentPreset,
                    Channel = StreamChannel,
                });
            }

#if MPTK_PRO
            if (PitchChange != DEFAULT_PITCH)
            {
                // If user change the pitch, wait 1/2 second before return to median value
                if (Time.realtimeSinceStartup - LastTimePitchChange > 0.5f)
                {
                    PitchChange = Mathf.SmoothDamp(PitchChange, DEFAULT_PITCH, ref currentVelocityPitch, 0.5f, 10000, Time.unscaledDeltaTime);
                    if (Mathf.Abs(PitchChange - DEFAULT_PITCH) < 0.001f)
                        PitchChange = DEFAULT_PITCH;
                    //Debug.Log("DEFAULT_PITCH " + DEFAULT_PITCH + " " + PitchChange + " " + currentVelocityPitch);
                    midiStreamPlayer.MPTK_PlayPitchWheelChange(StreamChannel, PitchChange);
                }
            }
#endif
            if (midiStreamPlayer != null && (IsplayingLoopPresets || IsplayingLoopNotes))
            {
                float time = Time.realtimeSinceStartup - LastTimeChange;
                if (time > Frequency)
                {
                    // It's time to generate some notes ;-)
                    LastTimeChange = Time.realtimeSinceStartup;


                    for (int indexNote = 0; indexNote < CountNoteToPlay; indexNote++)
                    {
                        if (IsplayingLoopPresets)
                        {
                            if (++CurrentPreset > EndPreset) CurrentPreset = StartPreset;
                            if (CurrentPreset < StartPreset) CurrentPreset = StartPreset;

                            midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent()
                            {
                                Command = MPTKCommand.PatchChange,
                                Value = CurrentPreset,
                                Channel = StreamChannel,
                            });
                        }

                        if (IsplayingLoopNotes)
                        {
                            if (++CurrentNote > EndNote) CurrentNote = StartNote;
                            if (CurrentNote < StartNote) CurrentNote = StartNote;
                        }

                        // Play note or chrod or scale without stopping the current (useful for performance test)
                        Play(false);

                    }
                }
            }
        }

        /// <summary>@brief
        /// Play music depending the parameters set
        /// </summary>
        /// <param name="stopCurrent">stop current note playing</param>
        void Play(bool stopCurrent)
        {
            if (RandomPlay)
            {
                if (StartNote >= EndNote)
                    CurrentNote = StartNote;
                else
                    CurrentNote = UnityEngine.Random.Range(StartNote, EndNote);
            }

#if MPTK_PRO
            if (ChordPlay || ChordLibPlay || RangeLibPlay)
            {
                if (RandomPlay)
                {
                    CountNoteChord = UnityEngine.Random.Range(3, 5);
                    DegreeChord = UnityEngine.Random.Range(1, 8);
                    CurrentChord = UnityEngine.Random.Range(StartNote, EndNote);
                }

                if (stopCurrent)
                    StopChord();

                if (ChordPlay)
                    PlayOneChord();

                if (ChordLibPlay)
                    PlayOneChordFromLib();

                if (RangeLibPlay)
                    PlayScale();
            }
            else
#endif
            {
                if (stopCurrent)
                    StopOneNote();
                PlayOneNote();
            }
        }

#if MPTK_PRO
        MPTKChordBuilder ChordPlaying;
        MPTKChordBuilder ChordLibPlaying;

        /// <summary>@brief
        /// Play note from a scale
        /// </summary>
        private void PlayScale()
        {
            // get the current scale selected
            MPTKRangeLib range = MPTKRangeLib.Range(CurrentScale, true);
            for (int ecart = 0; ecart < range.Count; ecart++)
            {
                NotePlaying = new MPTKEvent()
                {
                    Command = MPTKCommand.NoteOn, // midi command
                    Value = CurrentNote + range[ecart], // from 0 to 127, 48 for C4, 60 for C5, ...
                    Channel = StreamChannel, // from 0 to 15, 9 reserved for drum
                    Duration = DelayPlayScale, // note duration in millisecond, -1 to play indefinitely, MPTK_StopEvent to stop
                    Velocity = Velocity, // from 0 to 127, sound can vary depending on the velocity
                    Delay = ecart * DelayPlayScale, // delau in millisecond before playing the note
                };
                midiStreamPlayer.MPTK_PlayEvent(NotePlaying);
            }
        }

        private void PlayOneChord()
        {
            // Start playing a new chord and save in ChordPlaying to stop it later
            ChordPlaying = new MPTKChordBuilder(true)
            {
                // Parameters to build the chord
                Tonic = CurrentNote,
                Count = CountNoteChord,
                Degree = DegreeChord,

                // Midi Parameters how to play the chord
                Channel = StreamChannel,
                Arpeggio = ArpeggioPlayChord, // delay in milliseconds between each notes of the chord
                Duration = Convert.ToInt64(NoteDuration * 1000f), // millisecond, -1 to play indefinitely
                Velocity = Velocity, // Sound can vary depending on the velocity
                Delay = Convert.ToInt64(NoteDelay * 1000f),
            };
            //Debug.Log(DegreeChord);
            midiStreamPlayer.MPTK_PlayChordFromRange(ChordPlaying);
        }
        private void PlayOneChordFromLib()
        {
            // Start playing a new chord and save in ChordLibPlaying to stop it later
            ChordLibPlaying = new MPTKChordBuilder(true)
            {
                // Parameters to build the chord
                Tonic = CurrentNote,
                FromLib = CurrentChord,

                // Midi Parameters how to play the chord
                Channel = StreamChannel,
                Arpeggio = ArpeggioPlayChord, // delay in milliseconds between each notes of the chord
                Duration = Convert.ToInt64(NoteDuration * 1000f), // millisecond, -1 to play indefinitely
                Velocity = Velocity, // Sound can vary depending on the velocity
                Delay = Convert.ToInt64(NoteDelay * 1000f),
            };
            //Debug.Log(DegreeChord);
            midiStreamPlayer.MPTK_PlayChordFromLib(ChordLibPlaying);
        }

        private void StopChord()
        {
            if (ChordPlaying != null)
                midiStreamPlayer.MPTK_StopChord(ChordPlaying);

            if (ChordLibPlaying != null)
                midiStreamPlayer.MPTK_StopChord(ChordLibPlaying);

        }
#else
        private void PlayScale(){}
        private void PlayOneChord(){}
        private void PlayOneChordFromLib(){}
        private void StopChord(){}
#endif
        //! [ExampleMPTK_PlayEvent]
        /// <summary>@brief
        /// Send the note to the player. Notes are plays in a thread, so call returns immediately.
        /// The note is stopped automatically after the Duration defined.
        /// </summary>
        private void PlayOneNote()
        {
            //Debug.Log($"{StreamChannel} {midiStreamPlayer.MPTK_ChannelPresetGetName(StreamChannel)}");
            // Start playing a new note
            NotePlaying = new MPTKEvent()
            {
                Command = MPTKCommand.NoteOn,
                Value = CurrentNote, // note to played, ex 60=C5. Use the method from class HelperNoteLabel to convert to string
                Channel = StreamChannel,
                Duration = Convert.ToInt64(NoteDuration * 1000f), // millisecond, -1 to play indefinitely
                Velocity = Velocity, // Sound can vary depending on the velocity
                Delay = Convert.ToInt64(NoteDelay * 1000f),
            };

#if MPTK_PRO
            // Applied to the current note playing all the real time generators defined
            for (int i = 0; i < nbrGenerator; i++)
                if (indexGenerator[i] >= 0)
                    NotePlaying.MTPK_ModifySynthParameter((fluid_gen_type)indexGenerator[i], valueGenerator[i] / 100f, MPTKModeGeneratorChange.Override);
#endif
            midiStreamPlayer.MPTK_PlayEvent(NotePlaying);
        }
        //! [ExampleMPTK_PlayEvent]

        private void StopOneNote()
        {
            if (NotePlaying != null)
            {
                //Debug.Log("Stop note");
                // Stop the note (method to simulate a real human on a keyboard : 
                // duration is not known when note is triggered)
                midiStreamPlayer.MPTK_StopEvent(NotePlaying);
                NotePlaying = null;
            }
        }
    }
}