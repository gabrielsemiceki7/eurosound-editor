﻿Imports System.IO
Imports System.Text

Partial Public Class ExporterForm
    '*===============================================================================================
    '* BINARY FILE FUNCTIONS
    '*===============================================================================================
    Public Sub BuildTemporalFile(filesCount As Integer, streamsFolder As String, outputFolder As String)
        'Create variables
        Dim StartOffsets As New Queue(Of UInteger)

        'Get paths
        Dim tempStreamDataBin As String = fso.BuildPath(outputFolder, "STREAMS.bin")
        Dim tempStreamDataLut As String = fso.BuildPath(outputFolder, "STREAMS.lut")

        'Create directory if not exists
        If Not fso.FolderExists(outputFolder) Then
            fso.CreateFolder(outputFolder)
        End If

        'Create a new binary writer for the binary file
        Using binaryWriter As New BinaryWriter(File.Open(tempStreamDataBin, FileMode.Create, FileAccess.ReadWrite), Encoding.ASCII)
            'Convert audio to the destination platform rate
            For index As Integer = 0 To filesCount
                Dim adpcmFile As String = fso.BuildPath(streamsFolder, "STR_" & index & ".ssd")
                Dim markerFile As String = fso.BuildPath(streamsFolder, "STR_" & index & ".smf")

                'Ensure that the adpcm file exists
                If fso.FileExists(adpcmFile) AndAlso fso.FileExists(markerFile) Then
                    'Offset to write in look-up table
                    StartOffsets.Enqueue(binaryWriter.BaseStream.Position)

                    'Read files binary data
                    Dim markersFileData As Byte() = File.ReadAllBytes(markerFile)
                    Dim adpcmData As Byte() = File.ReadAllBytes(adpcmFile)

                    'Marker size
                    binaryWriter.Write(markersFileData.Length)
                    'Save position for the audio offset
                    Dim prevPosition As UInteger = binaryWriter.BaseStream.Position
                    'Audio Offset
                    binaryWriter.Write(0)
                    'Audio Size
                    binaryWriter.Write(adpcmData.Length)
                    'Marker Data
                    binaryWriter.Write(markersFileData)

                    'Alignment
                    Dim block As Byte() = New Byte(&H800 - 5) {}
                    binaryWriter.Write(block)
                    BinAlign(binaryWriter, &H800)

                    'Write adpcm data
                    Dim audioStartOffset As UInteger = binaryWriter.BaseStream.Position
                    binaryWriter.Write(adpcmData)

                    'Alignment
                    If index < filesCount Then
                        binaryWriter.Write(block)
                        BinAlign(binaryWriter, &H800)
                    End If

                    'Save current pos
                    Dim lastPosition As UInteger = binaryWriter.BaseStream.Position

                    'Go Back to write audio start pos
                    binaryWriter.Seek(prevPosition, SeekOrigin.Begin)
                    binaryWriter.Write(audioStartOffset)

                    'Return to current pos
                    binaryWriter.Seek(lastPosition, SeekOrigin.Begin)
                End If
            Next
            'Close file
            binaryWriter.Close()
        End Using

        'Ensure that we have items stored in the queue
        If StartOffsets.Count > 0 Then
            'Create a new binary writer for the lut file
            Using binaryWriter As New BinaryWriter(File.Open(tempStreamDataLut, FileMode.Create, FileAccess.ReadWrite), Encoding.ASCII)
                'Wirte all start offsets
                Do
                    binaryWriter.Write(StartOffsets.Dequeue)
                Loop While StartOffsets.Count > 0
                'Close file
                binaryWriter.Close()
            End Using
        End If
    End Sub

    Private Sub BinAlign(BWriter As BinaryWriter, alignment As Integer)
        BWriter.Seek((-BWriter.BaseStream.Position Mod alignment + alignment) Mod alignment, SeekOrigin.Current)
    End Sub
End Class
