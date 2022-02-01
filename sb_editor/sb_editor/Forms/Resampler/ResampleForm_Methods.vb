﻿Partial Public Class ResampleForm
    Private Sub DataTableToListView()
        ListView_Samples.BeginUpdate()
        For Each row As DataRow In dataTableInfo.Rows
            Dim item As New ListViewItem(row(0).ToString)
            For i As Integer = 1 To dataTableInfo.Columns.Count - 1
                item.SubItems.Add(row(i).ToString)
            Next
            ListView_Samples.Items.Add(item)
        Next
        ListView_Samples.EndUpdate()
    End Sub

    Private Function ListViewToDataTable() As DataTable
        Dim samplesData As New DataTable
        samplesData.Columns.Add("SampleFilename")
        samplesData.Columns.Add("ReSampleRate")
        samplesData.Columns.Add("Size")
        samplesData.Columns.Add("Date")
        samplesData.Columns.Add("ReSample")
        samplesData.Columns.Add("StreamMe")
        samplesData.Columns.Add("ReSmp4")
        samplesData.Columns.Add("ReSmp2")
        samplesData.Columns.Add("ReSmp3")
        samplesData.Columns.Add("ReSmp4duplicated")

        'Add item
        Dim listviewEnum As IEnumerator = ListView_Samples.Items.GetEnumerator
        While listviewEnum.MoveNext
            Dim cells As String() = New String(ListView_Samples.Columns.Count - 1) {}
            Dim currentItem As ListViewItem = listviewEnum.Current
            For colIndex As Integer = 0 To ListView_Samples.Columns.Count - 1
                cells(colIndex) = currentItem.SubItems(colIndex).Text
            Next
            samplesData.Rows.Add(cells)
        End While

        'Sort table
        Dim dv As DataView = samplesData.DefaultView
        dv.Sort = "SampleFilename asc"

        Return dv.ToTable
    End Function

    Private Function GetFileText(name As String) As String
        Dim fileContents As String = String.Empty
        ' If the file has been deleted since we took the snapshot, ignore it And return the empty string.  
        If fso.FileExists(name) Then
            fileContents = IO.File.ReadAllText(name)
        End If
        Return fileContents
    End Function

    Private Sub GetAvailableSampleRates(propsFilePath As String)
        'Open file and read it
        Dim currentLine As String
        FileOpen(1, propsFilePath, OpenMode.Input, OpenAccess.Read, OpenShare.LockWrite)
        Do Until EOF(1)
            'Read text file
            currentLine = LineInput(1)
            'Read Available Sample Rates
            If StrComp(currentLine, "#AvailableReSampleRates") = 0 Then
                'Read line
                currentLine = LineInput(1)
                Do
                    'Add item to listview
                    ComboBox_AvailableRates.Items.Add(currentLine)
                    'Continue Reading
                    currentLine = LineInput(1)
                Loop While StrComp(currentLine, "#END") <> 0
            End If

            'ReSample Rates for Format
            If InStr(currentLine, "#ReSampleRates") Then
                'Get index
                Dim index As Integer = Microsoft.VisualBasic.Right(currentLine, 1)
                'Collection to store values
                Dim values As New ArrayList
                'Read line
                currentLine = LineInput(1)
                Do
                    'Add item to ArrayList
                    values.Add(currentLine)
                    'Continue Reading
                    currentLine = LineInput(1)
                Loop While StrComp(currentLine, "#END") <> 0
                'Add data to dictionary
                If Not sampleRateFormats.ContainsKey(index) Then
                    sampleRateFormats.Add(index, values.ToArray(GetType(String)))
                End If
            End If
        Loop

        'Read misc properties block
        FileClose(1)
    End Sub

    Private Sub PlaySelectedSample()
        'Ensure that we selected an item from the list
        If ListView_Samples.SelectedItems.Count > 0 Then
            'Get wave file path
            Dim waveRelativePath = ListView_Samples.SelectedItems(0).Text
            Dim waveFilePath As String = fso.BuildPath(ProjMasterFolder, "Master" & waveRelativePath)

            'Ensure that the Wave Path exists
            If fso.FileExists(waveFilePath) Then
                'Play original
                If ComboBox_PreviewOptions.SelectedIndex = 0 Then
                    My.Computer.Audio.Play(waveFilePath)
                Else
                    'Temporal file and folder path
                    Dim outputFolder = fso.BuildPath(WorkingDirectory, "TempOutputFolder")
                    Dim outputFilePath = fso.BuildPath(outputFolder, "preview_resampled.wav")

                    'Ensure that the file exists
                    If fso.FileExists("SystemFiles\Sox.exe") Then
                        'Create output folder if not exists
                        If Not fso.FolderExists(outputFolder) Then
                            fso.CreateFolder(outputFolder)
                        End If
                        'Get sample rate
                        Dim formatRates As String() = sampleRateFormats(ComboBox_PreviewOptions.SelectedIndex - 1)
                        Dim sampleRate As Integer = formatRates(ComboBox_AvailableRates.SelectedIndex)
                        'Execute command
                        Shell("SystemFiles\Sox.exe" & " """ & waveFilePath & """ -r " & sampleRate & " """ & outputFilePath & """", AppWinStyle.Hide, True)
                        'Play audio
                        My.Computer.Audio.Play(outputFilePath)
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub EditAudioFile()
        'Ensure that the selection is not null
        If ListView_Samples.SelectedItems.Count > 0 Then
            'Get wave file path
            Dim waveFilePath As String = fso.BuildPath(ProjMasterFolder, "Master\" & ListView_Samples.SelectedItems(0).Text)
            'Open tool if files exists
            EditWaveFile(waveFilePath)
        End If
    End Sub
End Class
