﻿Imports System.ComponentModel
Imports System.IO
Imports NAudio.Wave

Partial Public Class ExporterForm
    '*===============================================================================================
    '* MAIN METHOD
    '*===============================================================================================
    Private Sub ResampleWaves(soundsTable As DataTable, outPlatforms As String(,), SoxTimer As Stopwatch, PCTimer As Stopwatch, GCTimer As Stopwatch, XBTimer As Stopwatch, PSTimer As Stopwatch)
        'Get Wave files to include
        Dim samplesCount As Integer = soundsTable.Rows.Count - 1
        If samplesCount > 0 Then
            'Create Folder structure for all platforms
            For platformIndex As Integer = 0 To outPlatforms.GetLength(0) - 1
                Dim currentPlatform As String = outPlatforms(platformIndex, 0)
                CopyDirectory(Path.Combine(ProjectSettingsFile.MiscProps.SampleFileFolder, "Master"), Path.Combine(WorkingDirectory, currentPlatform), True)
                Select Case currentPlatform
                    Case "PC"
                        CopyDirectory(Path.Combine(ProjectSettingsFile.MiscProps.SampleFileFolder, "Master"), Path.Combine(WorkingDirectory, currentPlatform & "_Software_adpcm"), True)
                    Case "GameCube"
                        CopyDirectory(Path.Combine(ProjectSettingsFile.MiscProps.SampleFileFolder, "Master"), Path.Combine(WorkingDirectory, currentPlatform & "_Software_adpcm"), True)
                        CopyDirectory(Path.Combine(ProjectSettingsFile.MiscProps.SampleFileFolder, "Master"), Path.Combine(WorkingDirectory, currentPlatform & "_dsp_adpcm"), True)
                    Case "PlayStation2"
                        CopyDirectory(Path.Combine(ProjectSettingsFile.MiscProps.SampleFileFolder, "Master"), Path.Combine(WorkingDirectory, currentPlatform & "_VAG"), True)
                    Case Else
                        CopyDirectory(Path.Combine(ProjectSettingsFile.MiscProps.SampleFileFolder, "Master"), Path.Combine(WorkingDirectory, "XBox_adpcm"), True)
                End Select
            Next

            'Reset progress bar
            Invoke(Sub() ProgressBar1.Value = 0)
            'Start inspecting each line of the datatable
            For rowIndex As Integer = 0 To samplesCount
                If soundsTable.Rows(rowIndex).Item(4) Then
                    'Get paths 
                    Dim sampleRelativePath As String = soundsTable.Rows(rowIndex).ItemArray(0)
                    Dim sourceFilePath As String = Path.Combine(ProjectSettingsFile.MiscProps.SampleFileFolder, "Master" & sampleRelativePath)
                    'Read WAVE Master file and get all required info
                    Dim masterWaveNumOfChannels, masterWaveBitsPerSample, masterWaveFrequency, masterWaveLength As Integer
                    Dim masterWaveLoopInfo As Integer()
                    Using reader As New WaveFileReader(sourceFilePath)
                        masterWaveNumOfChannels = reader.WaveFormat.Channels
                        masterWaveBitsPerSample = reader.WaveFormat.BitsPerSample
                        masterWaveFrequency = reader.WaveFormat.SampleRate
                        masterWaveLength = reader.Length
                        masterWaveLoopInfo = ReadWaveSampleChunk(reader)
                    End Using
                    If masterWaveNumOfChannels <> 1 Then
                        MsgBox("Sample " & sourceFilePath & " is not 1 channel.", vbOKOnly + vbCritical, "EuroSound")
                    Else
                        If masterWaveBitsPerSample <> 16 Then
                            MsgBox("Sample " & sourceFilePath & " is not 16 bit.", vbOKOnly + vbCritical, "EuroSound")
                        Else
                            'Resample for each platform 
                            For platformIndex As Integer = 0 To outPlatforms.GetLength(0) - 1
                                If outPlatforms(platformIndex, 2).Equals("On", StringComparison.OrdinalIgnoreCase) Then
                                    Dim currentPlatform As String = outPlatforms(platformIndex, 0)

                                    'Report progress and update title bar
                                    BackgroundWorker.ReportProgress(Decimal.Divide(rowIndex, samplesCount) * 100.0, "ReSampling: " & sampleRelativePath & "  " & currentPlatform)

                                    'Resample the wav for the destination platform
                                    Dim sampleRateLabel As String = soundsTable.Rows(rowIndex).ItemArray(1)
                                    Dim sampleRate As Integer = ProjectSettingsFile.sampleRateFormats(currentPlatform)(sampleRateLabel)
                                    If soundsTable.Rows(rowIndex).Item(5) Then
                                        sampleRate = 22050
                                    End If

                                    'ReSample for each platform
                                    Select Case currentPlatform
                                        Case "PC" 'IMA ADPCM For PC Streams
                                            '----------------------------------------------------------ReSample Master File
                                            SoxTimer.Start()
                                            Dim outputFilePath As String = Path.Combine(WorkingDirectory, currentPlatform & sampleRelativePath)
                                            ReSampleWithSox(sourceFilePath, outputFilePath, masterWaveFrequency, sampleRate, SoxEffect)
                                            SoxTimer.Stop()
                                            '----------------------------------------------------------Create IMA file if Required
                                            If soundsTable.Rows(rowIndex).Item(5) Then
                                                CreateImaAdpcm(currentPlatform, sampleRelativePath, outputFilePath, PCTimer)
                                            End If
                                        Case "GameCube" 'DSP for Nintendo GameCube
                                            '----------------------------------------------------------ReSample Master File
                                            SoxTimer.Start()
                                            Dim outputFilePath As String = Path.Combine(WorkingDirectory, currentPlatform & sampleRelativePath)
                                            ReSampleWithSox(sourceFilePath, outputFilePath, masterWaveFrequency, sampleRate, SoxEffect)
                                            SoxTimer.Stop()
                                            '----------------------------------------------------------Create IMA file if Required
                                            If soundsTable.Rows(rowIndex).Item(5) Then
                                                CreateImaAdpcm(currentPlatform, sampleRelativePath, outputFilePath, GCTimer)
                                            End If
                                            '----------------------------------------------------------Create DSP file
                                            GCTimer.Start()
                                            Dim dspOutputFilePath As String = Path.ChangeExtension(Path.Combine(WorkingDirectory, "GameCube_dsp_adpcm" & sampleRelativePath), ".dsp")
                                            'Default arguments
                                            Dim dspToolArgs As String = "-E """ & outputFilePath & """ """ & dspOutputFilePath & """"
                                            'Check loop info
                                            If masterWaveLoopInfo(0) = 1 AndAlso soundsTable.Rows(rowIndex).Item(5) = False Then 'Check if is NOT Streamed
                                                'Loop offset pos in the resampled wave
                                                Using parsedWaveReader As New WaveFileReader(outputFilePath)
                                                    Dim loopStart As UInteger = masterWaveLoopInfo(1) / (masterWaveLength / parsedWaveReader.Length)
                                                    dspToolArgs = dspToolArgs & " -l" & loopStart & "-" & (parsedWaveReader.SampleCount - 1)
                                                End Using
                                            End If
                                            'Execute Dsp Adpcm Tool
                                            RunProcess("SystemFiles\DSPADPCM.exe", dspToolArgs)
                                            'Move file
                                            Dim encoderOutputFile As String = Path.GetFileNameWithoutExtension(outputFilePath) & ".txt"
                                            File.Copy(encoderOutputFile, Path.Combine(WorkingDirectory, "GameCube", encoderOutputFile), True)
                                            File.Delete(encoderOutputFile)
                                            GCTimer.Stop()
                                        Case "PlayStation2"  'Sony VAG for PlayStation 2
                                            '----------------------------------------------------------ReSample Master File
                                            SoxTimer.Start()
                                            Dim outputFilePath As String = Path.ChangeExtension(Path.Combine(WorkingDirectory, currentPlatform & sampleRelativePath), ".aif")
                                            ReSampleWithSox(sourceFilePath, outputFilePath, masterWaveFrequency, sampleRate, SoxEffect)
                                            SoxTimer.Stop()
                                            '----------------------------------------------------------Create VAG file
                                            PSTimer.Start()
                                            Dim vagOutputFilePath As String = Path.ChangeExtension(Path.Combine(WorkingDirectory, "PlayStation2_VAG" & sampleRelativePath), ".vag")
                                            'Check loop info
                                            If masterWaveLoopInfo(0) = 1 AndAlso soundsTable.Rows(rowIndex).Item(5) = False Then 'Check if is NOT Streamed
                                                'Loop offset pos in the resampled wave
                                                Dim waveLength As Long
                                                Dim loopStart, loopEnd As Integer
                                                Using parsedWaveReader As New AiffFileReader(outputFilePath)
                                                    loopStart = masterWaveLoopInfo(1) / (masterWaveLength / parsedWaveReader.Length)
                                                    loopEnd = masterWaveLoopInfo(2) / (masterWaveLength / parsedWaveReader.Length)
                                                    waveLength = parsedWaveReader.Length
                                                End Using
                                                'Add new info to the Aif file
                                                AddLoopPointsToAiff(outputFilePath, loopStart, loopEnd, masterWaveLoopInfo(3))
                                            End If
                                            'Execute Vag Tool
                                            RunProcess("SystemFiles\AIFF2VAG.exe", """" & outputFilePath & """")
                                            'Move file
                                            Dim encoderOutputFile As String = Path.ChangeExtension(outputFilePath, ".vag")
                                            File.Copy(encoderOutputFile, vagOutputFilePath, True)
                                            File.Delete(encoderOutputFile)
                                            PSTimer.Stop()
                                        Case Else 'Xbox ADPCM for Xbox
                                            '----------------------------------------------------------ReSample Master File
                                            SoxTimer.Start()
                                            Dim outputFilePath As String = Path.Combine(WorkingDirectory, currentPlatform & sampleRelativePath)
                                            ReSampleWithSox(sourceFilePath, outputFilePath, masterWaveFrequency, sampleRate, SoxEffect)
                                            SoxTimer.Stop()
                                            '----------------------------------------------------------Create Xbox ADPCM
                                            XBTimer.Start()
                                            Dim xboxOutputFilePath As String = Path.Combine(WorkingDirectory, "XBox_adpcm" & sampleRelativePath)
                                            'Execute Dsp Adpcm Tool
                                            RunProcess("SystemFiles\xbadpcmencode.exe", """" & outputFilePath & """ """ & xboxOutputFilePath & """")
                                            XBTimer.Stop()
                                    End Select
                                End If
                            Next
                            'Update Property
                            soundsTable.Rows(rowIndex).Item(4) = "False"
                        End If
                    End If
                End If
            Next
        End If
    End Sub

    Private Sub CreateImaAdpcm(currentPlatform As String, sampleRelativePath As String, outputFilePath As String, platformTimer As Stopwatch)
        platformTimer.Start()

        'Get file paths
        Dim ImaOutputFilePath As String = Path.Combine(WorkingDirectory, currentPlatform & "_Software_adpcm" & sampleRelativePath)
        Dim smdFilePath As String = Path.ChangeExtension(ImaOutputFilePath, ".smd")

        'Get PCM Data
        Dim pcmData As Byte()
        Using parsedWaveReader As New WaveFileReader(outputFilePath)
            pcmData = New Byte(parsedWaveReader.Length - 1) {}
            parsedWaveReader.Read(pcmData, 0, pcmData.Length)
            File.WriteAllBytes(smdFilePath, pcmData)
        End Using

        'Wave to ima
        Dim imaData As Byte() = ESUtils.ImaCodec.Encode(ConvertByteArrayToShortArray(pcmData))
        File.WriteAllBytes(Path.ChangeExtension(ImaOutputFilePath, ".ssp"), imaData)

        platformTimer.Stop()
    End Sub
End Class
