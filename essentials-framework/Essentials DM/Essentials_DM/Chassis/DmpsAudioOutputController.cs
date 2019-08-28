﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;

using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.DM.Cards;

using PepperDash.Core;
using PepperDash.Essentials.Core;

using PepperDash.Essentials.DM.Config;

namespace PepperDash.Essentials.DM
{
    /// <summary>
    /// Exposes the volume levels for Program, Aux1 or Aux2 outputs on a DMPS3 chassis
    /// </summary>
    public class DmpsAudioOutputController : Device
    {
        Card.Dmps3OutputBase OutputCard;

        public DmpsAudioOutput MasterVolumeLevel { get; private set; }
        public DmpsAudioOutput SourceVolumeLevel { get; private set; }
        public DmpsAudioOutput Codec1VolumeLevel { get; private set; }
        public DmpsAudioOutput Codec2VolumeLevel { get; private set; }

        public DmpsAudioOutputController(string key, string name, Card.Dmps3OutputBase card)
            : base(key, name)
        {
            OutputCard = card;

            OutputCard.BaseDevice.DMOutputChange += new DMOutputEventHandler(BaseDevice_DMOutputChange);

            MasterVolumeLevel = new DmpsAudioOutput(card, eDmpsLevelType.Master);
            SourceVolumeLevel = new DmpsAudioOutput(card, eDmpsLevelType.Source);

            if (card is Card.Dmps3ProgramOutput)
            {
                Codec1VolumeLevel = new DmpsAudioOutput(card, eDmpsLevelType.Codec1);
                Codec2VolumeLevel = new DmpsAudioOutput(card, eDmpsLevelType.Codec2);
            }
            else if (card is Card.Dmps3Aux1Output)
            {
                Codec2VolumeLevel = new DmpsAudioOutput(card, eDmpsLevelType.Codec2);
            }
            else if (card is Card.Dmps3Aux2Output)
            {
                Codec1VolumeLevel = new DmpsAudioOutput(card, eDmpsLevelType.Codec1);
            }

        }

        void BaseDevice_DMOutputChange(Switch device, DMOutputEventArgs args)
        {
            switch (args.EventId)
            {
                case DMOutputEventIds.MasterVolumeFeedBackEventId:
                {
                    MasterVolumeLevel.VolumeLevelFeedback.FireUpdate();
                    break;
                }
                case DMOutputEventIds.MasterMuteOnFeedBackEventId:
                {
                    MasterVolumeLevel.MuteFeedback.FireUpdate();
                    break;
                }
                case DMOutputEventIds.SourceLevelFeedBackEventId:
                {
                    SourceVolumeLevel.VolumeLevelFeedback.FireUpdate();
                    break;
                }
                case DMOutputEventIds.Codec1LevelFeedBackEventId:
                {
                    if(Codec1VolumeLevel != null)
                        Codec1VolumeLevel.VolumeLevelFeedback.FireUpdate();
                    break;
                }
                case DMOutputEventIds.Codec1MuteOnFeedBackEventId:
                {
                    if (Codec1VolumeLevel != null)
                        Codec1VolumeLevel.MuteFeedback.FireUpdate();
                    break;
                }
                case DMOutputEventIds.Codec2LevelFeedBackEventId:
                {
                    if (Codec2VolumeLevel != null)
                        Codec2VolumeLevel.VolumeLevelFeedback.FireUpdate();
                    break;
                }
                case DMOutputEventIds.Codec2MuteOnFeedBackEventId:
                {
                    if (Codec2VolumeLevel != null)
                        Codec2VolumeLevel.MuteFeedback.FireUpdate();
                    break;
                }
            }
        }
    }

    public class DmpsAudioOutput : IBasicVolumeWithFeedback
    {
        Card.Dmps3OutputBase Output;

        UShortInputSig Level;

        eDmpsLevelType Type;

        public BoolFeedback MuteFeedback { get; private set; }
        public IntFeedback VolumeLevelFeedback { get; private set; }

        public Dictionary<int, DmpsAudioOutput> MicVolumeLevels { get; private set; }

        Action MuteOnAction;
        Action MuteOffAction;
        Action<bool> VolumeUpAction;
        Action<bool> VolumeDownAction;
 
        public DmpsAudioOutput(Card.Dmps3OutputBase output, eDmpsLevelType type)
        {
            Output = output;

            Type = type;

            switch (type)
            {
                case eDmpsLevelType.Master:
                    {
                        Level = output.MasterVolume;

                        MuteFeedback = new BoolFeedback( new Func<bool> (() => Output.MasterMuteOnFeedBack.BoolValue));
                        VolumeLevelFeedback = new IntFeedback(new Func<int>(() => Output.MasterVolumeFeedBack.UShortValue));
                        MuteOnAction = new Action(Output.MasterMuteOn);
                        MuteOffAction = new Action(Output.MasterMuteOff);
                        VolumeUpAction = new Action<bool>((b) => Output.MasterVolumeUp.BoolValue = b);
                        VolumeDownAction = new Action<bool>((b) => Output.MasterVolumeDown.BoolValue = b);

                        
                        break;
                    }
                case eDmpsLevelType.MicsMaster:
                    {
                        Level = output.MicMasterLevel;

                        MuteFeedback = new BoolFeedback(new Func<bool>(() => Output.MicMasterMuteOnFeedBack.BoolValue));
                        VolumeLevelFeedback = new IntFeedback(new Func<int>(() => Output.MicMasterLevelFeedBack.UShortValue));
                        MuteOnAction = new Action(Output.MicMasterMuteOn);
                        MuteOffAction = new Action(Output.MicMasterMuteOff);
                        VolumeUpAction = new Action<bool>((b) => Output.MicMasterLevelUp.BoolValue = b);
                        VolumeDownAction = new Action<bool>((b) => Output.MicMasterLevelDown.BoolValue = b);

                        break;
                    }
                case eDmpsLevelType.Source:
                    {
                        Level = output.SourceLevel;

                        MuteFeedback = new BoolFeedback(new Func<bool>(() => Output.SourceMuteOnFeedBack.BoolValue));
                        VolumeLevelFeedback = new IntFeedback(new Func<int>(() => Output.SourceLevelFeedBack.UShortValue));
                        MuteOnAction = new Action(Output.SourceMuteOn);
                        MuteOffAction = new Action(Output.SourceMuteOff);
                        VolumeUpAction = new Action<bool>((b) => Output.SourceLevelUp.BoolValue = b);
                        VolumeDownAction = new Action<bool>((b) => Output.SourceLevelDown.BoolValue = b);
                        break;
                    }
                case eDmpsLevelType.Codec1:
                    {
                        var programOutput = output as Card.Dmps3ProgramOutput;

                        if (programOutput != null)
                        {
                            Level = programOutput.Codec1Level;

                            MuteFeedback = new BoolFeedback(new Func<bool>(() => programOutput.CodecMute1OnFeedback.BoolValue));
                            VolumeLevelFeedback = new IntFeedback(new Func<int>(() => programOutput.Codec1LevelFeedback.UShortValue));
                            MuteOnAction = new Action(programOutput.Codec1MuteOn);
                            MuteOffAction = new Action(programOutput.Codec1MuteOff);
                            VolumeUpAction = new Action<bool>((b) => programOutput.Codec1LevelUp.BoolValue = b);
                            VolumeDownAction = new Action<bool>((b) => programOutput.Codec1LevelDown.BoolValue = b);

                        }
                        else
                        {
                            var auxOutput = output as Card.Dmps3Aux2Output;

                            Level = auxOutput.Codec1Level;

                            MuteFeedback = new BoolFeedback(new Func<bool>(() => auxOutput.CodecMute1OnFeedback.BoolValue));
                            VolumeLevelFeedback = new IntFeedback(new Func<int>(() => auxOutput.Codec1LevelFeedback.UShortValue));
                            MuteOnAction = new Action(auxOutput.Codec1MuteOn);
                            MuteOffAction = new Action(auxOutput.Codec1MuteOff);
                            VolumeUpAction = new Action<bool>((b) => auxOutput.Codec1LevelUp.BoolValue = b);
                            VolumeDownAction = new Action<bool>((b) => auxOutput.Codec1LevelDown.BoolValue = b);
                        }
                        break;
                    }
                case eDmpsLevelType.Codec2:
                    {
                        var programOutput = output as Card.Dmps3ProgramOutput;

                        if (programOutput != null)
                        {
                            Level = programOutput.Codec2Level;

                            MuteFeedback = new BoolFeedback(new Func<bool>(() => programOutput.CodecMute1OnFeedback.BoolValue));
                            VolumeLevelFeedback = new IntFeedback(new Func<int>(() => programOutput.Codec2LevelFeedback.UShortValue));
                            MuteOnAction = new Action(programOutput.Codec2MuteOn);
                            MuteOffAction = new Action(programOutput.Codec2MuteOff);
                            VolumeUpAction = new Action<bool>((b) => programOutput.Codec2LevelUp.BoolValue = b);
                            VolumeDownAction = new Action<bool>((b) => programOutput.Codec2LevelDown.BoolValue = b);

                        }
                        else
                        {
                            var auxOutput = output as Card.Dmps3Aux1Output;

                            Level = auxOutput.Codec2Level;

                            MuteFeedback = new BoolFeedback(new Func<bool>(() => auxOutput.CodecMute2OnFeedback.BoolValue));
                            VolumeLevelFeedback = new IntFeedback(new Func<int>(() => auxOutput.Codec2LevelFeedback.UShortValue));
                            MuteOnAction = new Action(auxOutput.Codec2MuteOn);
                            MuteOffAction = new Action(auxOutput.Codec2MuteOff);
                            VolumeUpAction = new Action<bool>((b) => auxOutput.Codec2LevelUp.BoolValue = b);
                            VolumeDownAction = new Action<bool>((b) => auxOutput.Codec2LevelDown.BoolValue = b);
                        }
                        break;
                    }
            }

            var numberOfMics = Global.ControlSystem.NumberOfMicrophones;

            for (int i = 1; i <= numberOfMics; i++)
            {
                // Construct each mic level here
            }
        }

        #region IBasicVolumeWithFeedback Members

        public void SetVolume(ushort level)
        {
            Level.UShortValue = level;
        }

        public void MuteOn()
        {
            MuteOnAction();
        }

        public void MuteOff()
        {
            MuteOffAction();
        }

        #endregion

        #region IBasicVolumeControls Members

        public void VolumeUp(bool pressRelease)
        {
            VolumeUpAction(pressRelease);
        }

        public void VolumeDown(bool pressRelease)
        {
            VolumeDownAction(pressRelease);
        }

        public void MuteToggle()
        {
            if (MuteFeedback.BoolValue)
                MuteOff();
            else
                MuteOn();
        }

        #endregion
    }

    public enum eDmpsLevelType
    {
        Master,
        Source,
        MicsMaster,
        Codec1,
        Codec2,
        Mic
    }
}