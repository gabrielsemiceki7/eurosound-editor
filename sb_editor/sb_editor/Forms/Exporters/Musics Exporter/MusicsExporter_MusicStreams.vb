﻿Imports System.IO

Partial Public Class MusicsExporter
    '*===============================================================================================
    '* CREATE MUSIC STREAMS - MAIN METHOD
    '*===============================================================================================
    Private Sub CreateMusicStreams(waveOutputFolder As String, outputPlatforms As String())
        If Not MarkerFileOnly Then
            Dim temporalLeft As String = Path.Combine(WorkingDirectory, "System", "TempWave.wav")
            Dim temporalRight As String = Path.Combine(WorkingDirectory, "System", "TempWave2.wav")

            'Start main loop
            For fileIndex As Integer = 0 To outputQueue.Rows.Count - 1
                Dim musicItem As DataRow = outputQueue.Rows(fileIndex)
                Dim waveFilePath As String = Path.Combine(WorkingDirectory, "Music", musicItem.ItemArray(0) & ".wav")
                Dim musicHashCode As Integer = musicItem.ItemArray(2)
                Dim ResampleAndSplit As Boolean = True

                'Split Wave channels with SoX (PC & GC)
                For platformIndex As Integer = 0 To outputPlatforms.Length - 1
                    'Get the current platform
                    Dim currentPlatform As String = outputPlatforms(platformIndex)
                    Dim soundSampleData As String = Path.Combine(GetOutputFolder(musicHashCode, currentPlatform), "MFX_" & musicHashCode & ".ssd")

                    'Update title bar and progress
                    BackgroundWorker.ReportProgress(Decimal.Divide(platformIndex + (fileIndex * outputPlatforms.Length), outputQueue.Rows.Count * outputPlatforms.Length) * 100.0, "Making Music Stream: " & musicItem.ItemArray(0) & " for " & currentPlatform)

                    'Split channels - All platforms has the same sample rate at exception of Xbox
                    If ResampleAndSplit Then
                        ReSampleAndSplitWithSox(waveFilePath, temporalLeft, temporalRight, 32000)
                        ResampleAndSplit = False
                    End If

                    'Start ReSampling
                    Select Case currentPlatform
                        Case "PC", "GameCube"
                            'Wave to IMA
                            Dim PcOutLeft As String = Path.Combine(waveOutputFolder, musicItem.ItemArray(0))
                            Dim PcOutRight As String = Path.Combine(waveOutputFolder, musicItem.ItemArray(0))
                            CreateImaAdpcm(temporalLeft, temporalRight, PcOutLeft, PcOutRight)

                            'Music Stream (.ssd)
                            MergeChannels(File.ReadAllBytes(PcOutLeft & "_L.ima"), File.ReadAllBytes(PcOutRight & "_R.ima"), 1, soundSampleData)
                        Case "PlayStation2"
                            'Vag Tool
                            RunConsoleProcess("SystemFiles\AIFF2VAG.exe", """" & temporalLeft & """")
                            RunConsoleProcess("SystemFiles\AIFF2VAG.exe", """" & temporalRight & """")

                            'Get File Paths
                            Dim ps2VagL As String = Path.Combine(waveOutputFolder, musicItem.ItemArray(0)) & "_L.vag"
                            Dim ps2VagR As String = Path.Combine(waveOutputFolder, musicItem.ItemArray(0)) & "_R.vag"
                            Dim encoderOutputFileL As String = Path.ChangeExtension(temporalLeft, ".vag")
                            Dim encoderOutputFileR As String = Path.ChangeExtension(temporalRight, ".vag")

                            'Move files
                            File.Copy(encoderOutputFileL, ps2VagL, True)
                            File.Copy(encoderOutputFileR, ps2VagR, True)
                            File.Delete(encoderOutputFileL)
                            File.Delete(encoderOutputFileR)

                            'Music Stream (.ssd)
                            MergeChannels(GetVagFileDataChunk(ps2VagL), GetVagFileDataChunk(ps2VagR), 128, soundSampleData)
                        Case Else
                            'Split channels
                            ReSampleAndSplitWithSox(waveFilePath, temporalLeft, temporalRight, 44100)
                            'Xbox Tool
                            Dim xbxVagL As String = Path.Combine(waveOutputFolder, musicItem.ItemArray(0)) & "_L.adpcm"
                            Dim xbxVagR As String = Path.Combine(waveOutputFolder, musicItem.ItemArray(0)) & "_R.adpcm"
                            RunConsoleProcess("SystemFiles\xbadpcmencode.exe", """" & temporalLeft & """ """ & xbxVagL & """")
                            RunConsoleProcess("SystemFiles\xbadpcmencode.exe", """" & temporalRight & """ """ & xbxVagR & """")
                            MergeChannels(GetXboxAdpcmDataChunk(xbxVagL), GetXboxAdpcmDataChunk(xbxVagR), 4, soundSampleData)
                    End Select
                Next
            Next
        End If
    End Sub

    '*===============================================================================================
    '* GET OUTPUT FOLDER (DEPENDING OF THE HASHCODE)
    '*===============================================================================================
    Private Function GetOutputFolder(fileHashCode As UInteger, currentPlatform As String) As String
        Dim folderNumber = (fileHashCode And &HF0) >> 4
        Dim markersFilePath As String = Path.Combine(WorkingDirectory, "TempOutputFolder", currentPlatform, "Music", "MFX_" & folderNumber)
        Directory.CreateDirectory(markersFilePath)
        Return markersFilePath
    End Function

    '*===============================================================================================
    '* MERGE CHANNELS (CREATES THE .SSD FILE)
    '*===============================================================================================
    Private Sub MergeChannels(LeftChannelData As Byte(), RightChannelData As Byte(), interleave_block_size As Integer, outputFilePath As String)
        Dim IndexLC, IndexRC As Integer

        'Read data and align array size
        If interleave_block_size > 1 Then
            Array.Resize(LeftChannelData, ((LeftChannelData.Length + (interleave_block_size - 1)) And Not (interleave_block_size - 1)))
            Array.Resize(RightChannelData, ((RightChannelData.Length + (interleave_block_size - 1)) And Not (interleave_block_size - 1)))
        End If

        'Get total length
        Dim ArrayLength As Integer = LeftChannelData.Length + RightChannelData.Length
        Dim interleavedData As Byte() = New Byte(ArrayLength - 1) {}

        'Start channels interleaving
        For i As Integer = 0 To ArrayLength - 1
            If (i Mod 2) = 0 Then
                If IndexLC < LeftChannelData.Length Then
                    Buffer.BlockCopy(LeftChannelData, IndexLC, interleavedData, i * interleave_block_size, interleave_block_size)
                End If
                IndexLC += interleave_block_size
            Else
                If IndexRC < RightChannelData.Length Then
                    Buffer.BlockCopy(RightChannelData, IndexRC, interleavedData, i * interleave_block_size, interleave_block_size)
                End If
                IndexRC += interleave_block_size
            End If
        Next
        File.WriteAllBytes(outputFilePath, interleavedData)
    End Sub

    '*===============================================================================================
    '* IMA ADPCM FUNCTIONS
    '*===============================================================================================
    Private Sub CreateImaAdpcm(inputLeft As String, inputRight As String, outputLeft As String, outputRight As String)
        'FilePaths
        Dim wavePcmLeft As String = outputLeft & "_L.pcm"
        Dim wavePcmRight As String = outputRight & "_R.pcm"

        'Resampled wav
        RunConsoleProcess("SystemFiles\Sox.exe", """" & inputLeft & """ -t raw """ & wavePcmLeft & """")
        RunConsoleProcess("SystemFiles\Sox.exe", """" & inputRight & """ -t raw """ & wavePcmRight & """")

        'Wave to ima
        Dim imaLeftData As Byte() = ESUtils.ImaCodec.Encode(ConvertByteArrayToShortArray(File.ReadAllBytes(wavePcmLeft)))
        Dim imaRightData As Byte() = ESUtils.ImaCodec.Encode(ConvertByteArrayToShortArray(File.ReadAllBytes(wavePcmRight)))
        File.WriteAllBytes(outputLeft & "_L.ima", imaLeftData)
        File.WriteAllBytes(outputRight & "_R.ima", imaRightData)
    End Sub
End Class
