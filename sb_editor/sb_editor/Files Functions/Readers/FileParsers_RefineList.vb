﻿Namespace ReaderClasses
    Partial Public Class FileParsers
        Friend Function ReadRefineList(refineFilePath As String) As String()
            Dim refineKeywords As New List(Of String)

            FileOpen(1, refineFilePath, OpenMode.Input, OpenAccess.Read, OpenShare.LockWrite)
            Do Until EOF(1)
                'Read text file
                Dim currentLine As String = Trim(LineInput(1))
                'Streams Block
                If StrComp(currentLine, "#RefineSearch", CompareMethod.Text) = 0 Then
                    'Read line
                    currentLine = Trim(LineInput(1))
                    While StrComp(currentLine, "#END", CompareMethod.Text) <> 0
                        refineKeywords.Add(currentLine)
                        'Continue Reading
                        currentLine = Trim(LineInput(1))
                    End While
                End If
            Loop
            FileClose(1)

            Return refineKeywords.ToArray
        End Function
    End Class
End Namespace
