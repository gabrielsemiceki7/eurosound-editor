﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PCAudioDLL.MusX_Objects
{
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    public class MusicBank
    {
        public byte[][] PcmData = new byte[1][];
        public int startPos;
        public int loopStartPoint;
        public int loopEndPoint;
        public bool isLooped;
        public int sampleRate;
        public int channels;
        public float volume = 1;
    }

    //-------------------------------------------------------------------------------------------------------------------------------
}