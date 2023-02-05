﻿//-------------------------------------------------------------------------------------------------------------------------------
//  ______                                           _ 
// |  ____|                                         | |
// | |__   _   _ _ __ ___  ___  ___  _   _ _ __   __| |
// |  __| | | | | '__/ _ \/ __|/ _ \| | | | '_ \ / _` |
// | |____| |_| | | | (_) \__ \ (_) | |_| | | | | (_| |
// |______|\__,_|_|  \___/|___/\___/ \__,_|_| |_|\__,_|
//
//-------------------------------------------------------------------------------------------------------------------------------
// MUSX FUNCTIONS -- FINAL SFX FILES
//-------------------------------------------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Text;
using static ESUtils.BytesFunctions;

namespace ESUtils
{
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    public static class MusXBuild_MusicDetails
    {
        //-------------------------------------------------------------------------------------------------------------------------------
        public static void BuildMusicDetails(string musicDetailsFilePath, string OutputFilePath, string platform)
        {
            //Ensure that the output file path is not null
            if (!string.IsNullOrEmpty(OutputFilePath))
            {
                //Create a new binary writer
                using (BinaryWriter binWriter = new BinaryWriter(File.Open(OutputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.ASCII))
                {
                    //--------------------------------------------------[File Header]--------------------------------------------------
                    //--magic[magic value]--
                    binWriter.Write(Encoding.ASCII.GetBytes("MUSX"));
                    //--hashc[Hashcode for the current soundbank without the section prefix]--
                    binWriter.Write(0x0000A000);
                    //--offst[Constant offset to the next section,]--
                    binWriter.Write(4);
                    //--fulls[Size of the whole file, in bytes. Unused. ]--
                    binWriter.Write(0);
                    //--Platform
                    binWriter.Write(Encoding.ASCII.GetBytes(platform));
                    //--Timespan
                    DateTime initialDate = new DateTime(2000, 1, 1, 1, 0, 0);
                    binWriter.Write((uint)(DateTime.Now.TimeOfDay - initialDate.TimeOfDay).TotalSeconds);
                    //--Adpcm Encoding
                    binWriter.Write(Convert.ToInt32(!platform.Equals("PS2_")));
                    //--Padding
                    binWriter.Write(0);

                    //--------------------------------------------------[Read and Write Files Content]--------------------------------------------------
                    if (File.Exists(musicDetailsFilePath))
                    {
                        //Read file Data
                        byte[] markersFileData = File.ReadAllBytes(musicDetailsFilePath);
                        binWriter.Write(markersFileData);
                    }
                }
            }
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------
}