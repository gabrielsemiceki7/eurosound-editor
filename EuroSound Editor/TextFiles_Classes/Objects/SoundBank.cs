﻿namespace EuroSound_Editor.Objects
{
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    public class SoundBank
    {
        public FileHeader HeaderData = new FileHeader();
        public string[] DataBases = new string[0];
        public int HashCode;
        public uint PlayStationSize;
        public uint PCSize;
        public uint XboxSize;
        public uint GameCubeSize;
    }

    //-------------------------------------------------------------------------------------------------------------------------------
}
