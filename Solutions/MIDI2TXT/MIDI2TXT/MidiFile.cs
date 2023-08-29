﻿//-------------------------------------------------------------------------------------------------------------------------------
//  __  __ _____ _____ _____   ___    _________   _________ 
// |  \/  |_   _|  __ \_   _| |__ \  |__   __\ \ / /__   __|
// | \  / | | | | |  | || |      ) |    | |   \ V /   | |   
// | |\/| | | | | |  | || |     / /     | |    > <    | |   
// | |  | |_| |_| |__| || |_   / /_     | |   / . \   | |   
// |_|  |_|_____|_____/_____| |____|    |_|  /_/ \_\  |_|   
//
//-------------------------------------------------------------------------------------------------------------------------------
// File Data Class
//-------------------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;

namespace MIDI2TXT
{
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    internal class MidiFile
    {
        internal int HeaderLength;
        internal int FormatType;
        internal int NumTracks;
        internal int PulsesPerQuarterNote;
        internal uint TempoPerQuarterNote;
        internal float BPM;
        internal TimeSignatureEvent TimeSignature = new TimeSignatureEvent();
        internal List<string> Events = new List<string>();
    }

    //-------------------------------------------------------------------------------------------------------------------------------
    internal class TimeSignatureEvent
    {
        public int DeltaTime;
        public int Numerator;
        public int Denominator;
        public int ClocksPerMetronomeClick;
        public int ThirtySecondNotesPerBeat;
    }

    //-------------------------------------------------------------------------------------------------------------------------------
}
