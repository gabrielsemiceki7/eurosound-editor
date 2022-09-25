﻿using ESUtils;
using EuroSound_Editor.Audio_Classes;
using EuroSound_Editor.Objects;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace EuroSound_Editor.Forms
{
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    public partial class SfxOutputForm
    {
        //-------------------------------------------------------------------------------------------------------------------------------
        private long OutputTempFiles(Dictionary<string, uint> hashCodesDict, Dictionary<string, SFX> fileData, string[] sampleList, string[] streamsList, string outputPlatform, string outputPath, string outputBank, StreamWriter debugFile, bool isBigEndian, Stopwatch SFXData, Stopwatch Samples)
        {
            //Get Files Path
            long sampleBankSize = 0;

            //Write Temporal Files
            using (BinaryWriter sbfWritter = new BinaryWriter(File.Open(Path.ChangeExtension(outputPath, ".sbf"), FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                using (BinaryWriter sfxWritter = new BinaryWriter(File.Open(Path.ChangeExtension(outputPath, ".sfx"), FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    using (BinaryWriter sifWritter = new BinaryWriter(File.Open(Path.ChangeExtension(outputPath, ".sif"), FileMode.Create, FileAccess.Write, FileShare.Read)))
                    {
                        List<byte[]> dspHeaderData = new List<byte[]>();

                        //Write SFX Data
                        SFXData.Start();
                        WriteSfxFile(hashCodesDict, fileData, sampleList, streamsList, outputPlatform, outputBank, sfxWritter, isBigEndian, debugFile);
                        SFXData.Stop();
                        if (!abortQuickOutput)
                        {
                            //Write SFX Samples
                            Samples.Start();
                            sampleBankSize = WriteSifFile(sifWritter, sbfWritter, sampleList, outputPlatform, dspHeaderData, isBigEndian);
                            Samples.Stop();

                            if (dspHeaderData.Count > 0)
                            {
                                using (BinaryWriter ssfWritter = new BinaryWriter(File.Open(Path.ChangeExtension(outputPath, ".ssf"), FileMode.Create, FileAccess.Write, FileShare.Read)))
                                {
                                    for (int i = 0; i < dspHeaderData.Count; i++)
                                    {
                                        ssfWritter.Write(dspHeaderData[i]);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return (long)Math.Round(decimal.Divide(sampleBankSize, 1024));
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private void WriteSfxFile(Dictionary<string, uint> hashCodesDict, Dictionary<string, SFX> fileData, string[] sampleList, string[] streamsList, string outputPlatform, string outputBank, BinaryWriter sfxWritter, bool isBigEndian, StreamWriter debugFile)
        {
            List<long> sfxLut = new List<long>();

            //Sfx Header
            sfxWritter.Write(BytesFunctions.FlipInt32(fileData.Count, isBigEndian));
            foreach (KeyValuePair<string, SFX> sfxItem in fileData)
            {
                sfxWritter.Write(BytesFunctions.FlipInt32(sfxItem.Value.HashCode, isBigEndian));
                sfxWritter.Write(0);
            }

            //Sfx Parameter Entry
            int streamFileCheckSum = 0;
            foreach (KeyValuePair<string, SFX> sfxData in fileData)
            {
                if (abortQuickOutput)
                {
                    break;
                }
                sfxLut.Add(sfxWritter.BaseStream.Position);
                sfxWritter.Write(BytesFunctions.FlipShort((short)sfxData.Value.Parameters.DuckerLength, isBigEndian));
                sfxWritter.Write(BytesFunctions.FlipShort((short)sfxData.Value.SamplePool.MinDelay, isBigEndian));
                sfxWritter.Write(BytesFunctions.FlipShort((short)sfxData.Value.SamplePool.MaxDelay, isBigEndian));
                sfxWritter.Write(BytesFunctions.FlipShort((short)sfxData.Value.Parameters.InnerRadius, isBigEndian));
                sfxWritter.Write(BytesFunctions.FlipShort((short)sfxData.Value.Parameters.OuterRadius, isBigEndian));
                sfxWritter.Write((sbyte)sfxData.Value.Parameters.ReverbSend);
                sfxWritter.Write((sbyte)sfxData.Value.Parameters.TrackingType);
                sfxWritter.Write((sbyte)sfxData.Value.Parameters.MaxVoices);
                sfxWritter.Write((sbyte)sfxData.Value.Parameters.Priority);
                sfxWritter.Write((sbyte)sfxData.Value.Parameters.Ducker);
                sfxWritter.Write((sbyte)sfxData.Value.Parameters.MasterVolume);
                sfxWritter.Write((ushort)GetFlags(sfxData.Value));

                //Calculate references
                sfxWritter.Write(BytesFunctions.FlipUShort((ushort)sfxData.Value.Samples.Count, isBigEndian));
                foreach (SfxSample sampleToCheck in sfxData.Value.Samples)
                {
                    int fileRef = 0;
                    if (sfxData.Value.SamplePool.EnableSubSFX)
                    {
                        string hashCode = Path.GetFileNameWithoutExtension(sampleToCheck.FilePath);
                        if (hashCodesDict.ContainsKey(hashCode))
                        {
                            fileRef = (short)hashCodesDict[hashCode];
                        }
                        else
                        {
                            Invoke(method: new Action(() => { MessageBox.Show(string.Format("HashCode Not Found {0}", sampleToCheck.FilePath), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error); }));
                        }
                    }
                    else
                    {
                        //If there is a missing sample, cancel output at this point. 
                        if (fastOutput)
                        {
                            string samplePath = sampleToCheck.FilePath;
                            string sampleFolder = "XBox_adpcm";
                            switch (outputPlatform.Trim().ToLower())
                            {
                                case "pc":
                                    sampleFolder = "PC";
                                    break;
                                case "playstation2":
                                    sampleFolder = "PlayStation2_VAG";
                                    samplePath = Path.ChangeExtension(sampleToCheck.FilePath, ".vag");
                                    break;
                                case "gamecube":
                                    sampleFolder = "GameCube_dsp_adpcm";
                                    samplePath = Path.ChangeExtension(sampleToCheck.FilePath, ".dsp");
                                    break;
                            }
                            string fullPath = Path.Combine(GlobalPrefs.ProjectFolder, sampleFolder, samplePath.TrimStart(Path.DirectorySeparatorChar));
                            if (!File.Exists(fullPath))
                            {
                                MessageBox.Show(string.Format("Output Error: Sample File Missing\n{0}\n\nIn SFX : {1}\nWithin SoundBank : {2}", fullPath, sfxData.Key, outputBank), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                                abortQuickOutput = true;
                                break;
                            }
                        }

                        fileRef = (short)Array.FindIndex(sampleList, s => s.Equals(sampleToCheck.FilePath, StringComparison.OrdinalIgnoreCase));
                        if (fileRef == -1)
                        {
                            fileRef = (short)Array.FindIndex(streamsList, s => s.Equals(sampleToCheck.FilePath, StringComparison.OrdinalIgnoreCase));
                            if (fileRef >= 0)
                            {
                                fileRef += 1;
                                fileRef *= -1;

                                //Debug File
                                debugFile.WriteLine("{0}    \\{1}", fileRef, sampleToCheck.FilePath);
                                streamFileCheckSum -= fileRef;
                            }
                            else
                            {
                                Invoke(method: new Action(() => { MessageBox.Show(string.Format("Stream Ref Not Found {0}", sampleToCheck.FilePath), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error); }));
                            }
                        }
                    }
                    sfxWritter.Write(BytesFunctions.FlipShort((short)fileRef, isBigEndian));
                    sfxWritter.Write(BytesFunctions.FlipShort((short)Math.Round(sampleToCheck.PitchOffset * 1024), isBigEndian));
                    sfxWritter.Write(BytesFunctions.FlipShort((short)Math.Round(sampleToCheck.RandomPitch * 1024), isBigEndian));
                    sfxWritter.Write(sampleToCheck.BaseVolume);
                    sfxWritter.Write(sampleToCheck.RandomVolume);
                    sfxWritter.Write(sampleToCheck.Pan);
                    sfxWritter.Write(sampleToCheck.RandomPan);
                    sfxWritter.Write((byte)0);
                    sfxWritter.Write((byte)0);
                }
            }
            debugFile.WriteLine("StreamFileRefCheckSum = {0}", streamFileCheckSum * -1);

            if (!abortQuickOutput)
            {
                //Write Start Offsetss
                sfxWritter.BaseStream.Seek(4, SeekOrigin.Begin);
                for (int i = 0; i < sfxLut.Count; i++)
                {
                    sfxWritter.BaseStream.Seek(4, SeekOrigin.Current);
                    sfxWritter.Write(BytesFunctions.FlipUInt32((uint)sfxLut[i], isBigEndian));
                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private long WriteSifFile(BinaryWriter sifWritter, BinaryWriter sbfWritter, string[] sampleList, string platform, List<byte[]> dspHeader, bool isBigEndian)
        {
            long sampleBankSize = 0;
            sifWritter.Write(BytesFunctions.FlipInt32(sampleList.Length, isBigEndian));
            for (int i = 0; i < sampleList.Length; i++)
            {
                string masterFile = Path.Combine(GlobalPrefs.CurrentProject.SampleFilesFolder, "Master", sampleList[i].TrimStart(Path.DirectorySeparatorChar));
                WavInfo masterFileData = new WavInfo();
                if (File.Exists(masterFile))
                {
                    masterFileData = wavFunctions.ReadWaveProperties(masterFile);
                }

                //-------------------------------------------------------------------------------[ PC ]-------------------------------------------------------------------
                if (platform.Equals("PC", StringComparison.OrdinalIgnoreCase))
                {
                    string pcFilepath = Path.Combine(GlobalPrefs.ProjectFolder, "PC", sampleList[i].TrimStart(Path.DirectorySeparatorChar));
                    if (File.Exists(pcFilepath))
                    {
                        WavInfo pcFileData = wavFunctions.ReadWaveProperties(pcFilepath);
                        byte[] pcmData = wavFunctions.GetByteWaveData(pcFilepath);

                        //Write Header Data
                        uint loopOffset = 0;
                        if (masterFileData.HasLoop)
                        {
                            loopOffset = BytesFunctions.AlignNumber((uint)CalculusLoopOffset.RuleOfThreeLoopOffset(masterFileData.SampleRate, pcFileData.SampleRate, masterFileData.LoopStart * 2), 2);
                        }
                        WriteSampleInfo(sifWritter, sbfWritter, masterFileData, pcFileData, BytesFunctions.AlignNumber((uint)pcFileData.Length, 4), (int)pcFileData.Length, i * 96, loopOffset, isBigEndian);

                        //Write Sample Data
                        byte[] filedata = new byte[BytesFunctions.AlignNumber((uint)pcFileData.Length, 4)];
                        Array.Copy(pcmData, filedata, pcmData.Length);
                        sbfWritter.Write(filedata);

                        //Update value
                        sampleBankSize += pcmData.Length;
                    }
                    else if (!fastOutput)
                    {
                        throw new IOException(string.Format("Output Error: Sample File Missing: UNKNOWN SFX & BANK\n{0}", pcFilepath));
                    }
                }

                //-------------------------------------------------------------------------------[ GameCube ]-------------------------------------------------------------------
                if (platform.Equals("GameCube", StringComparison.OrdinalIgnoreCase))
                {
                    string wavFilePath = Path.Combine(GlobalPrefs.ProjectFolder, "GameCube", sampleList[i].TrimStart(Path.DirectorySeparatorChar));
                    string dspFilePath = Path.ChangeExtension(Path.Combine(GlobalPrefs.ProjectFolder, "GameCube_dsp_adpcm", sampleList[i].TrimStart(Path.DirectorySeparatorChar)), ".dsp");

                    if (File.Exists(wavFilePath))
                    {
                        WavInfo wavFileData = wavFunctions.ReadWaveProperties(wavFilePath);
                        if (File.Exists(dspFilePath))
                        {

                            byte[] dspData = CommonFunctions.RemoveFileHeader(dspFilePath, 96);
                            dspHeader.Add(GetDspHeaderData(dspFilePath));

                            //Write Header Data
                            uint loopOffset = 0;
                            if (masterFileData.HasLoop)
                            {
                                loopOffset = (uint)CalculusLoopOffset.RuleOfThreeLoopOffset(masterFileData.SampleRate, wavFileData.SampleRate, masterFileData.LoopStart * 2);
                            }
                            WriteSampleInfo(sifWritter, sbfWritter, masterFileData, wavFileData, BytesFunctions.AlignNumber((uint)dspData.Length, 32), dspData.Length, i * 96, loopOffset, isBigEndian);

                            //Write Sample Data
                            byte[] filedata = new byte[BytesFunctions.AlignNumber((uint)dspData.Length, 32)];
                            Array.Copy(dspData, filedata, dspData.Length);
                            sbfWritter.Write(filedata);

                            //Update value
                            sampleBankSize += dspData.Length;
                        }
                        else
                        {
                            throw new IOException(string.Format("Output Error: Sample File Missing: UNKNOWN SFX & BANK\n{0}", dspFilePath));
                        }
                    }
                    else if (!fastOutput)
                    {
                        throw new IOException(string.Format("Output Error: Sample File Missing: UNKNOWN SFX & BANK\n{0}", wavFilePath));
                    }
                }

                //-------------------------------------------------------------------------------[ PlayStation 2 ]-------------------------------------------------------------------
                if (platform.Equals("PlayStation2", StringComparison.OrdinalIgnoreCase))
                {
                    string aifFilePath = Path.ChangeExtension(Path.Combine(GlobalPrefs.ProjectFolder, "PlayStation2", sampleList[i].TrimStart(Path.DirectorySeparatorChar)), ".aif");
                    string vagFilePath = Path.ChangeExtension(Path.Combine(GlobalPrefs.ProjectFolder, "PlayStation2_VAG", sampleList[i].TrimStart(Path.DirectorySeparatorChar)), ".vag");

                    if (File.Exists(aifFilePath))
                    {
                        WavInfo aifFileData = aiffFunctions.ReadWaveProperties(aifFilePath);
                        if (File.Exists(vagFilePath))
                        {
                            byte[] vagData = CommonFunctions.RemoveFileHeader(vagFilePath, 48);

                            //Write Header Data
                            uint loopOffset = 0;
                            if (masterFileData.HasLoop)
                            {
                                loopOffset = (uint)CalculusLoopOffset.RuleOfThreeLoopOffset(masterFileData.SampleRate, aifFileData.SampleRate, masterFileData.LoopStart * 2);
                            }
                            WriteSampleInfo(sifWritter, sbfWritter, masterFileData, aifFileData, BytesFunctions.AlignNumber((uint)vagData.Length, 64), vagData.Length, i * 96, loopOffset, isBigEndian);

                            //Write Sample Data
                            byte[] filedata = new byte[BytesFunctions.AlignNumber((uint)vagData.Length, 64)];
                            Array.Copy(vagData, filedata, vagData.Length);
                            sbfWritter.Write(filedata);

                            //Update value
                            sampleBankSize += vagData.Length;
                        }
                        else
                        {
                            throw new IOException(string.Format("Output Error: Sample File Missing: UNKNOWN SFX & BANK\n{0}", vagFilePath));
                        }
                    }
                    else if (!fastOutput)
                    {
                        throw new IOException(string.Format("Output Error: Sample File Missing: UNKNOWN SFX & BANK\n{0}", aifFilePath));
                    }
                }

                //-------------------------------------------------------------------------------[ Xbox ]-------------------------------------------------------------------
                if (platform.Equals("Xbox", StringComparison.OrdinalIgnoreCase) || platform.Equals("X Box", StringComparison.OrdinalIgnoreCase))
                {
                    string wavFilePath = Path.Combine(GlobalPrefs.ProjectFolder, "X Box", sampleList[i].TrimStart(Path.DirectorySeparatorChar));
                    if (File.Exists(wavFilePath))
                    {
                        string adpcmFilePath = Path.Combine(GlobalPrefs.ProjectFolder, "XBox_adpcm", sampleList[i].TrimStart(Path.DirectorySeparatorChar));
                        if (File.Exists(adpcmFilePath))
                        {
                            byte[] adpcmData = CommonFunctions.RemoveFileHeader(adpcmFilePath, 48);

                            //Write Header Data
                            uint loopOffset = 0;
                            if (masterFileData.HasLoop)
                            {
                                loopOffset = CalculusLoopOffset.GetXboxAlignedNumber((uint)masterFileData.LoopStart);
                            }
                            WriteSampleInfo(sifWritter, sbfWritter, masterFileData, wavFunctions.ReadWaveProperties(wavFilePath), (uint)adpcmData.Length, adpcmData.Length, i * 96, loopOffset, isBigEndian);

                            //Write Sample Data
                            sbfWritter.Write(adpcmData);

                            //Update value
                            sampleBankSize += adpcmData.Length;
                        }
                        else
                        {
                            throw new IOException(string.Format("Output Error: Sample File Missing: UNKNOWN SFX & BANK\n{0}", adpcmFilePath));
                        }
                    }
                    else if (!fastOutput)
                    {
                        throw new IOException(string.Format("Output Error: Sample File Missing: UNKNOWN SFX & BANK\n{0}", wavFilePath));
                    }
                }
            }

            return sampleBankSize;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private void WriteSampleInfo(BinaryWriter sifWritter, BinaryWriter sbfWritter, WavInfo masterFileData, WavInfo wavFileData, uint lengthAligned, int formatLength, int psiSampleHeader, uint loopOffset, bool isBigEndian)
        {
            //Write Header Data
            sifWritter.Write(BytesFunctions.FlipInt32(Convert.ToInt32(masterFileData.HasLoop), isBigEndian));
            sifWritter.Write(BytesFunctions.FlipUInt32((uint)sbfWritter.BaseStream.Position, isBigEndian));
            sifWritter.Write(BytesFunctions.FlipUInt32(lengthAligned, isBigEndian));
            sifWritter.Write(BytesFunctions.FlipInt32(wavFileData.SampleRate, isBigEndian));
            sifWritter.Write(BytesFunctions.FlipInt32(formatLength, isBigEndian));
            sifWritter.Write(BytesFunctions.FlipInt32(wavFileData.Channels, isBigEndian));
            sifWritter.Write(BytesFunctions.FlipInt32(4, isBigEndian));
            sifWritter.Write(BytesFunctions.FlipInt32(psiSampleHeader, isBigEndian));
            sifWritter.Write(BytesFunctions.FlipUInt32(loopOffset, isBigEndian));
            sifWritter.Write((uint)masterFileData.TotalTime.TotalMilliseconds);
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private void UpdateDuckerLength(Dictionary<string, SFX> fileData, string outputPlatform)
        {
            foreach (KeyValuePair<string, SFX> soundToCheck in fileData)
            {
                int duckerLength = 0;
                if (soundToCheck.Value.Parameters.Ducker > 0)
                {
                    //Get Length of all samples
                    foreach (SfxSample sampleToCheck in soundToCheck.Value.Samples)
                    {
                        string sampleFilePath = Path.Combine(GlobalPrefs.ProjectFolder, outputPlatform, sampleToCheck.FilePath.TrimStart(Path.DirectorySeparatorChar));
                        if (outputPlatform.Equals("PlayStation2", StringComparison.OrdinalIgnoreCase))
                        {
                            sampleFilePath = Path.ChangeExtension(sampleFilePath, ".aif");
                            if (File.Exists(sampleFilePath))
                            {
                                using (AiffFileReader reader = new AiffFileReader(sampleFilePath))
                                {
                                    decimal cents = Math.Round(decimal.Divide((decimal)reader.TotalTime.TotalMilliseconds, 10));
                                    duckerLength += (int)cents;
                                }
                            }
                        }
                        else
                        {
                            if (File.Exists(sampleFilePath))
                            {
                                using (WaveFileReader reader = new WaveFileReader(sampleFilePath))
                                {
                                    decimal cents = Math.Round(decimal.Divide((decimal)reader.TotalTime.TotalMilliseconds, 10));
                                    duckerLength += (int)cents;
                                }
                            }
                        }
                    }

                    //Apply Value
                    if (soundToCheck.Value.Parameters.DuckerLength < 0)
                    {
                        duckerLength -= Math.Abs(soundToCheck.Value.Parameters.DuckerLength);
                    }
                    else
                    {
                        duckerLength += Math.Abs(soundToCheck.Value.Parameters.DuckerLength);
                    }
                    soundToCheck.Value.Parameters.DuckerLength = duckerLength;
                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private int GetFlags(SFX sfxFile)
        {
            int flags = 0;
            if (sfxFile.Parameters.Action1 == 1)
            {
                flags |= 1 << 0;
            }
            if (sfxFile.Parameters.Doppler)
            {
                flags |= 1 << 1;
            }
            if (sfxFile.Parameters.IgnoreAge)
            {
                flags |= 1 << 2;
            }
            if (sfxFile.SamplePool.Action1 == 1)
            {
                flags |= 1 << 3;
            }
            if (sfxFile.SamplePool.RandomPick)
            {
                flags |= 1 << 4;
            }
            if (sfxFile.SamplePool.Shuffled)
            {
                flags |= 1 << 5;
            }
            if (sfxFile.SamplePool.isLooped)
            {
                flags |= 1 << 6;
            }
            if (sfxFile.SamplePool.Polyphonic)
            {
                flags |= 1 << 7;
            }
            if (sfxFile.Parameters.Outdoors)
            {
                flags |= 1 << 8;
            }
            if (sfxFile.Parameters.PauseInNis)
            {
                flags |= 1 << 9;
            }
            if (sfxFile.SamplePool.EnableSubSFX)
            {
                flags |= 1 << 10;
            }
            if (sfxFile.Parameters.StealOnAge)
            {
                flags |= 1 << 11;
            }
            if (sfxFile.Parameters.MusicType)
            {
                flags |= 1 << 12;
            }
            return flags;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private byte[] GetDspHeaderData(string dspFilePath)
        {
            byte[] dspFileWithHeader = File.ReadAllBytes(dspFilePath);
            byte[] dspHeaderData = new byte[96];
            if (dspFileWithHeader.Length > 95)
            {
                Array.Copy(dspFileWithHeader, 0, dspHeaderData, 0, 96);
            }

            return dspHeaderData;
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------
}
