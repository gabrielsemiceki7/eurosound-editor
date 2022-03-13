﻿Namespace ReaderClasses
    Partial Public Class FileParsers
        Friend Sub ReadMiscFile(miscFilePath As String)
            Try
                FileOpen(1, miscFilePath, OpenMode.Input, OpenAccess.Read, OpenShare.LockWrite)
                Do Until EOF(1)
                    'Read text file
                    Dim currentLine As String = Trim(LineInput(1))
                    'Streams Block
                    If StrComp(currentLine, "#STREAMS", CompareMethod.Text) = 0 Then
                        'Read line
                        currentLine = Trim(LineInput(1))
                        While StrComp(currentLine, "#END", CompareMethod.Text) <> 0
                            Dim lineData = currentLine.Split(New Char() {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
                            If StrComp(UCase(lineData(0)), "RESAMPLESTREAMS") = 1 Then
                                ReSampleStreams = CUInt(lineData(1))
                            End If
                            'Continue Reading
                            currentLine = Trim(LineInput(1))
                        End While
                    End If

                    'HashCodes Block
                    If StrComp(currentLine, "#HASHCODES", CompareMethod.Text) = 0 Then
                        'Read line
                        currentLine = Trim(LineInput(1))
                        While StrComp(currentLine, "#END", CompareMethod.Text) <> 0
                            Dim lineData = currentLine.Split(New Char() {" "c, ChrW(9)}, StringSplitOptions.RemoveEmptyEntries)
                            Select Case UCase(lineData(0))
                                Case "SFXHASHCODENUMBER"
                                    SFXHashCodeNumber = CInt(lineData(1))
                                Case "SOUNDBANKHASHCODENUMBER"
                                    SoundBankHashCodeNumber = CByte(lineData(1))
                                Case "MFXHASHCODENUMBER"
                                    MFXHashCodeNumber = CInt(lineData(1))
                            End Select
                            'Continue Reading
                            currentLine = Trim(LineInput(1))
                        End While
                    End If
                Loop
                FileClose(1)
            Catch ex As Exception
                MsgBox(ex.Message, vbOKOnly + vbCritical, "Error")
            End Try
        End Sub
    End Class
End Namespace
