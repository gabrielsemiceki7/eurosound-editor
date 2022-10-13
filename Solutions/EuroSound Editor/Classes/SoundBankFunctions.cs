﻿using ESUtils;
using NAudio.Wave;
using sb_editor.Audio_Classes;
using sb_editor.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace sb_editor.Classes
{
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    internal class SoundBankFunctions
    {
        //-------------------------------------------------------------------------------------------------------------------------------
        internal string[] GetSFXs(string[] DataBases, string platform = "")
        {
            HashSet<string> soundBankSFX = new HashSet<string>();

            for (int i = 0; i < DataBases.Length; i++)
            {
                string filePath = Path.Combine(GlobalPrefs.ProjectFolder, "DataBases", DataBases[i] + ".txt");
                if (File.Exists(filePath))
                {
                    string[] fileData = File.ReadAllLines(filePath);
                    int index = Array.IndexOf(fileData, "#DEPENDENCIES") + 1;
                    if (index > 0)
                    {
                        string currentLine = fileData[index];
                        while (!currentLine.Equals("#END", StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.IsNullOrEmpty(platform))
                            {
                                soundBankSFX.Add(currentLine);
                            }
                            else
                            {
                                string specificFormat = Path.Combine(GlobalPrefs.ProjectFolder, "SFXs", platform, currentLine + ".txt");
                                if (File.Exists(specificFormat))
                                {
                                    soundBankSFX.Add(string.Format("{0}/{1}", platform, currentLine));
                                }
                                else
                                {
                                    soundBankSFX.Add(currentLine);
                                }
                            }
                            currentLine = fileData[index++].Trim();
                        }
                    }
                }
            }

            //Hashset to array
            string[] SfxArray = soundBankSFX.ToArray();
            Array.Sort(SfxArray);

            return SfxArray;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        internal string[] GetSampleList(string[] SFXs, string outputLanguage)
        {
            HashSet<string> samplesList = new HashSet<string>();

            for (int i = 0; i < SFXs.Length; i++)
            {
                string filePath = Path.Combine(GlobalPrefs.ProjectFolder, "SFXs", SFXs[i] + ".txt");
                string[] fileData = File.ReadAllLines(filePath);
                int index = Array.IndexOf(fileData, "#SFXSamplePoolFiles") + 1;
                if (index > 0)
                {
                    string currentLine = fileData[index];
                    while (!currentLine.Equals("#END", StringComparison.OrdinalIgnoreCase))
                    {
                        string sampleName = CommonFunctions.GetSampleFromSpeechFolder(currentLine, outputLanguage);
                        if (!string.IsNullOrEmpty(sampleName))
                        {
                            samplesList.Add(sampleName);
                        }
                        currentLine = fileData[index++].Trim();
                    }
                }
            }

            //Hashset to array
            string[] samplesArray = samplesList.ToArray();
            Array.Sort(samplesArray);

            return samplesArray;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        internal string[] GetSampleList(Dictionary<string, SFX> fileData, string outputLanguage)
        {
            HashSet<string> samplesList = new HashSet<string>();

            foreach (KeyValuePair<string, SFX> sfxItem in fileData)
            {
                if (!sfxItem.Value.SamplePool.EnableSubSFX && sfxItem.Value.Samples.Count > 0)
                {
                    foreach (SfxSample sampleData in sfxItem.Value.Samples)
                    {
                        string sampleName = CommonFunctions.GetSampleFromSpeechFolder(sampleData.FilePath, outputLanguage);
                        if (!string.IsNullOrEmpty(sampleName))
                        {
                            samplesList.Add(sampleName);
                        }
                    }
                }
            }

            //Hashset to array
            string[] samplesArray = samplesList.ToArray();
            Array.Sort(samplesArray);

            return samplesArray;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        internal long GetSampleSize(string samplesFolder, SamplePool samplePool, string[] Samples)
        {
            long sampleSize = 0;

            for (int i = 0; i < Samples.Length; i++)
            {
                string fileName = MultipleFilesFunctions.GetFullFileName(Samples[i]);
                if (samplePool.SamplePoolItems.ContainsKey(fileName) && Path.HasExtension(fileName) && !Path.IsPathRooted(Samples[i]))
                {
                    //Get wave length
                    long waveLength = 0;
                    if (Directory.Exists(samplesFolder))
                    {
                        string samplePath = Path.Combine(samplesFolder, Samples[i]);
                        if (File.Exists(samplePath))
                        {
                            using (WaveFileReader WReader = new WaveFileReader(samplePath))
                            {
                                waveLength = WReader.Length;
                            }
                        }
                    }

                    //Count soundbank size
                    if (samplePool.SamplePoolItems[fileName].StreamMe)
                    {
                        sampleSize += 2 * Math.Max(waveLength, 1);
                    }
                    else
                    {
                        sampleSize += 4 * Math.Max(waveLength, 1);
                    }
                }
            }

            return sampleSize;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        internal long GetEstimatedOutputFileSize(string[] samplesList, SamplePool samplePool, string outputPlatform)
        {
            decimal fileSize = 0;

            //With Master folder
            if (Directory.Exists(Path.Combine(GlobalPrefs.ProjectFolder, "Master")))
            {
                for (int i = 0; i < samplesList.Length; i++)
                {
                    if (!Path.HasExtension(samplesList[i]) || Path.IsPathRooted(samplesList[i]))
                    {
                        continue;
                    }
                    //Master wave freq
                    long masterWaveSize;
                    int masterWaveFreq;
                    using (WaveFileReader waveReader = new WaveFileReader(Path.Combine(GlobalPrefs.ProjectFolder, "Master", samplesList[i].TrimStart('\\'))))
                    {
                        masterWaveSize = waveReader.Length;
                        masterWaveFreq = waveReader.WaveFormat.SampleRate;
                    }

                    //ReSampled wave size
                    string keyToCheck = MultipleFilesFunctions.GetFullFileName(samplesList[i]);
                    if (samplePool.SamplePoolItems.ContainsKey(keyToCheck))
                    {
                        SamplePoolItem sampleItem = samplePool.SamplePoolItems[keyToCheck];
                        int sampleRateIndex = GlobalPrefs.CurrentProject.ResampleRates.IndexOf(sampleItem.ReSampleRate);
                        int formatRate = GlobalPrefs.CurrentProject.platformData[outputPlatform].ReSampleRates[sampleRateIndex];
                        decimal resampledWaveSize = decimal.Divide(masterWaveSize, decimal.Divide(masterWaveFreq, formatRate));
                        switch (outputPlatform)
                        {
                            case "PC":
                                fileSize += CalculusLoopOffset.GetStreamLoopOffsetPCandGC((uint)resampledWaveSize / 2);
                                break;
                            case "GameCube":
                                decimal dspFileSize = decimal.Divide(resampledWaveSize, (decimal)3.46);
                                fileSize += CalculusLoopOffset.GetStreamLoopOffsetPCandGC((uint)dspFileSize);
                                break;
                            case "PlayStation2":
                                fileSize += CalculusLoopOffset.GetStreamLoopOffsetPlayStation2((uint)resampledWaveSize / 4);
                                break;
                            default:
                                decimal xboxAdpcm = decimal.Divide(resampledWaveSize, (decimal)2.36);
                                fileSize += CalculusLoopOffset.GetXboxAlignedNumber((uint)xboxAdpcm);
                                break;
                        }
                    }
                }
            }
            else
            {
                //Without master folder
                for (int i = 0; i < samplesList.Length; i++)
                {
                    if (!Path.IsPathRooted(samplesList[i]))
                    {
                        string fileName = MultipleFilesFunctions.GetFullFileName(samplesList[i]);
                        if (samplePool.SamplePoolItems.ContainsKey(fileName) && Path.HasExtension(fileName))
                        {
                            //Skip streams for these two platforms
                            if (samplePool.SamplePoolItems[fileName].StreamMe && (outputPlatform.Equals("PlayStation2", StringComparison.OrdinalIgnoreCase) || outputPlatform.Equals("PC", StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }

                            //Calculate sample size
                            if (outputPlatform.Equals("Xbox", StringComparison.OrdinalIgnoreCase) || outputPlatform.Equals("X Box", StringComparison.OrdinalIgnoreCase))
                            {
                                fileSize += 36;
                            }
                            else
                            {
                                fileSize += 32;
                            }
                        }
                    }
                }
            }

            return (long)fileSize;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        internal Dictionary<string, SFX> GetSfxDataDict(string[] sbSfxs, string platform, string language)
        {
            Dictionary<string, SFX> sfxFilesData = new Dictionary<string, SFX>();

            for (int i = 0; i < sbSfxs.Length; i++)
            {
                string filePath = Path.Combine(GlobalPrefs.ProjectFolder, "SFXs", platform, sbSfxs[i].TrimStart(Path.DirectorySeparatorChar) + ".txt");
                if (!File.Exists(filePath))
                {
                    filePath = Path.Combine(GlobalPrefs.ProjectFolder, "SFXs", sbSfxs[i].TrimStart(Path.DirectorySeparatorChar) + ".txt");
                }

                //Update Sample File Paths
                SFX sfxData = TextFiles.ReadSfxFile(filePath);
                foreach (SfxSample sampleData in sfxData.Samples)
                {
                    string samplePath = CommonFunctions.GetSampleFromSpeechFolder(sampleData.FilePath, language);
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        sampleData.FilePath = samplePath;
                    }
                }

                //Add Data To Dictionary
                sfxFilesData.Add(Path.GetFileNameWithoutExtension(filePath), sfxData);
            }

            return sfxFilesData;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        internal long GetMaxBankSize(string currentPlatform, SoundBank sbData)
        {
            string systemIniFilePath = Path.Combine(GlobalPrefs.ProjectFolder, "System", "EuroSound.ini");
            if (File.Exists(systemIniFilePath))
            {
                //Max Sizes
                IniFile systemIni = new IniFile(systemIniFilePath);
                switch (currentPlatform.ToLower())
                {
                    case "pc":
                        return sbData.PCSize > 0 ? sbData.PCSize : Convert.ToUInt32(systemIni.Read("PCSize", "PropertiesForm"));
                    case "playstation2":
                        return sbData.PlayStationSize > 0 ? sbData.PlayStationSize : Convert.ToUInt32(systemIni.Read("PlayStationSize", "PropertiesForm"));
                    case "gamecube":
                        return sbData.GameCubeSize > 0 ? sbData.GameCubeSize : Convert.ToUInt32(systemIni.Read("GameCubeSize", "PropertiesForm"));
                    case "xbox":
                    case "x box":
                        return sbData.XboxSize > 0 ? sbData.XboxSize : Convert.ToUInt32(systemIni.Read("XBoxSize", "PropertiesForm"));
                }
            }

            return 0;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        internal void UpdateDuckerLength(Dictionary<string, SFX> fileData, string outputPlatform)
        {
            foreach (KeyValuePair<string, SFX> soundToCheck in fileData)
            {
                if (soundToCheck.Value.Parameters.Ducker > 0)
                {
                    int duckerLength = 0;

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
        internal int GetFlags(SFX sfxFile)
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
        internal byte[] GetDspHeaderData(string dspFilePath)
        {
            byte[] dspFileWithHeader = File.ReadAllBytes(dspFilePath);
            byte[] dspHeaderData = new byte[96];
            if (dspFileWithHeader.Length > 95)
            {
                Array.Copy(dspFileWithHeader, 0, dspHeaderData, 0, 96);
            }

            return dspHeaderData;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        internal void WriteSampleInfo(BinaryWriter sifWritter, BinaryWriter sbfWritter, WavInfo masterFileData, WavInfo wavFileData, uint lengthAligned, int formatLength, int psiSampleHeader, uint loopOffset, bool isBigEndian)
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
        internal Dictionary<string, uint> GetHashCodesDictionary(string folder, string keyWord)
        {
            Dictionary<string, uint> HashCodesDict = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

            string[] files = Directory.GetFiles(Path.Combine(GlobalPrefs.ProjectFolder, folder), "*.txt", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                string filePath = Path.GetFileNameWithoutExtension(files[i]);
                if (!HashCodesDict.ContainsKey(filePath))
                {
                    string[] fileData = File.ReadAllLines(files[i]);
                    int hashCodeIndex = Array.FindIndex(fileData, s => s.Equals(keyWord, StringComparison.OrdinalIgnoreCase));
                    string[] data = fileData[hashCodeIndex + 1].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (data.Length > 1)
                    {
                        HashCodesDict.Add(filePath, Convert.ToUInt32(data[1].Trim()));
                    }
                }
            }

            return HashCodesDict;
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------
}