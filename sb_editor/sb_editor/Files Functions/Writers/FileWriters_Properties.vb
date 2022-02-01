﻿Partial Public Class FileWriters
    Friend Sub CreateEmptyPropertiesFile(filePath As String)
        Dim created = Date.Now.ToString(filesDateFormat)

        'Update file
        FileOpen(1, filePath, OpenMode.Output, OpenAccess.Write, OpenShare.LockReadWrite)
        PrintLine(1, "## EuroSound Properties File")
        PrintLine(1, "## First Created ... " & created)
        PrintLine(1, "## Created By ... " & EuroSoundUser)
        PrintLine(1, "## Last Modified ... " & created)
        PrintLine(1, "## Last Modified By ... " & EuroSoundUser)
        PrintLine(1, "")
        PrintLine(1, "#AvailableFormats")
        PrintLine(1, "0")
        PrintLine(1, "#END")
        PrintLine(1, "")
        PrintLine(1, "#AvailableReSampleRates")
        PrintLine(1, "Default")
        PrintLine(1, "#END")
        PrintLine(1, "")
        PrintLine(1, "#MiscProperites")
        PrintLine(1, "DefaultRate  0")
        PrintLine(1, "SampleFileFolder " & ProjMasterFolder)
        PrintLine(1, "HashCodeFileFolder ")
        PrintLine(1, "EngineXFolder ")
        PrintLine(1, "EuroLandHashCodeServerPath ")
        PrintLine(1, "#END")
        FileClose(1)
    End Sub
End Class
