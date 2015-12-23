Option Explicit

Private Declare Function EngineGetVersion Lib "engine.dll" (ByVal buffer As String) As Long

Public Declare Function ShellExecute Lib "shell32.dll" Alias "ShellExecuteA" (ByVal hwnd As Long, ByVal lpOperation As String, ByVal lpFile As String, ByVal lpParameters As String, ByVal lpDirectory As String, ByVal nShowCmd As Long) As Long
Public Declare Sub Sleep Lib "kernel32" (ByVal dwMilliseconds As Long)

Private Type VS_FIXEDFILEINFO
    dwSignature As Long
    dwStrucVersionl As Integer ' e.g. = &h0000 = 0
    dwStrucVersionh As Integer ' e.g. = &h0042 = .42
    dwFileVersionMSl As Integer ' e.g. = &h0003 = 3
    dwFileVersionMSh As Integer ' e.g. = &h0075 = .75
    dwFileVersionLSl As Integer ' e.g. = &h0000 = 0
    dwFileVersionLSh As Integer ' e.g. = &h0031 = .31
    dwProductVersionMSl As Integer ' e.g. = &h0003 = 3
    dwProductVersionMSh As Integer ' e.g. = &h0010 = .1
    dwProductVersionLSl As Integer ' e.g. = &h0000 = 0
    dwProductVersionLSh As Integer ' e.g. = &h0031 = .31
    dwFileFlagsMask As Long ' = &h3F for version "0.42"
    dwFileFlags As Long ' e.g. VFF_DEBUG Or VFF_PRERELEASE
    dwFileOS As Long ' e.g. VOS_DOS_WINDOWS16
    dwFileType As Long ' e.g. VFT_DRIVER
    dwFileSubtype As Long ' e.g. VFT2_DRV_KEYBOARD
    dwFileDateMS As Long ' e.g. 0
    dwFileDateLS As Long ' e.g. 0
End Type

Private Declare Function GetFileVersionInfo Lib "Version.dll" Alias "GetFileVersionInfoA" (ByVal lptstrFilename As String, ByVal dwhandle As Long, ByVal dwlen As Long, ByVal lpData As Long) As Long
Private Declare Function GetFileVersionInfoSize Lib "Version.dll" Alias "GetFileVersionInfoSizeA" (ByVal lptstrFilename As String, lpdwHandle As Long) As Long
Private Declare Function VerQueryValue Lib "Version.dll" Alias "VerQueryValueA" (ByVal pBlock As Long, ByVal lpSubBlock As String, lplpBuffer As Long, puLen As Long) As Long
Private Declare Sub MoveMemory Lib "kernel32" Alias "RtlMoveMemory" (ByVal dest As Long, ByVal Source As Long, ByVal length As Long)

Dim fs As New LarsaData.FileSystem
Dim enc As New EncryptionManager ' provides MD5
Dim reg As New RegistryHandler
Public taskm As New LarsaData.ProcessManager

Sub Main()
    ' Get the command-line arguments.
    Dim quietMode As Boolean, projectFile As String, engineFile As String, outputFile As String
    ParseCommandLine quietMode, projectFile, engineFile, outputFile
    
    If Not quietMode Then
        frmProgress.lblResultCase.Caption = "Loading..."
        frmProgress.lblResultType.Caption = projectFile
        frmProgress.Show
    End If
    
    ' Create a temporary directory to store analysis results in.
    Dim tmpPath As String
    tmpPath = fs.MakeTempFileName()
    fs.CreateFolder tmpPath
    
    ' Create the output DOM.
    Dim dom As New DOMDocument30
    dom.appendChild dom.createElement("larsaResults")
    
   
    
    
    ' Copy the project file and any databases (other files in the same
    ' directory with database extensions) to the temporary path. Record
    ' the copied files and their MD5s in the output so we can detect
    ' changes.
    CopyProjectFiles projectFile, tmpPath, filesNode
    
    ' Copy the engine too.
    CopyFile "engine.dll", engineFile, tmpPath + "\engine.dll", filesNode
    
    ' Copy engine.exe and the license file into place so things are all found in the current working directory.
    fs.FileCopy App.Path + "\engine.exe", tmpPath + "\engine.exe"
    fs.FileCopy App.Path + "\larsa.lic", tmpPath + "\larsa.lic"
    
    
    ' Pause. Not sure why but we need it
    DoEvents: Sleep 500: DoEvents
    
    ' Run analysis.
    StartAnalysis tmpPath, fs.ExtractFileName(projectFile), dom
    
    ' Export results to XML.
    ExportResults tmpPath + "\" + fs.ExtractFileName(projectFile), dom, quietMode
    
    ' Remove temporary directory.
    
    If Not quietMode Then
        frmProgress.lblResultCase.Caption = "Cleaning temporary files..."
        frmProgress.lblResultType.Caption = ""
        DoEvents
    End If
    
    fs.CleanDirectory tmpPath
    RmDir tmpPath
    
    ' Save the output XML with the results.
    
    If Not quietMode Then
        frmProgress.lblResultCase.Caption = "Saving output file..."
        frmProgress.lblResultType.Caption = outputFile
        DoEvents
    End If
    
    fs.WriteFile outputFile, mdlLarsaLicense.FormatXML(dom.xml)
    
    ' Since the form may be open, explicitly end.
    
    End
End Sub

Sub ParseCommandLine(ByRef quietMode As Boolean, ByRef projectFile As String, ByRef engineFile As String, ByRef outputFile As String)
    ' Parses the command line into arguments, checks that two arguments
    ' were given, and returns those arguments in the ByRef args.

    Dim i As Integer, mode As Integer, c As String
    Dim argc As Integer, argv(1 To 1000) As String
    
    mode = 0
    argc = 1
    
    For i = 1 To Len(Command$)
        c = Mid(Command$, i, 1)
        If mode = 0 Then
            ' Not within a quoted argument.
            If c = Chr(34) Then
                ' open quote
                mode = 1
            ElseIf c = " " Then
                ' space; starts new argument
                If argv(argc) <> "" Then argc = argc + 1
            Else
                ' append character
                argv(argc) = argv(argc) + c
            End If
        ElseIf mode = 1 Then
            ' Within a quoted argument.
            If c = Chr(34) Then
                ' close quote
                mode = 0
            Else
                ' append character
                argv(argc) = argv(argc) + c
            End If
        End If
    Next
    
    If argc < 3 Then
        MsgBox "Usage: ExportResults.exe ProjectFilePath EngineDllPath Output.xml"
        End
    End If
    
    i = 1
    If argv(i) = "/q" Then
        i = i + 1
        quietMode = True
    End If
    
    projectFile = argv(i)
    engineFile = argv(i + 1)
    outputFile = argv(i + 2)
End Sub

Sub CopyProjectFiles(larFile As String, dstDir As String, filesNode As IXMLDOMElement)
    Dim srcDir As String
    srcDir = fs.ExtractFolder(larFile)
    
    Dim dirfiles As Collection, f As String, ext As String, i As Integer
    Set dirfiles = fs.GetFiles(srcDir)
    
    For i = 1 To dirfiles.Count
        f = dirfiles(i)
        ext = fs.GetFileExtension(f)
        If f = fs.ExtractFileName(larFile) Or (ext = "drs" Or ext = "dml" Or ext = "dth" Or ext = "lpsx") Then
            CopyFile f, srcDir + "\" + f, dstDir + "\" + f, filesNode
        End If
    Next
End Sub

Sub CopyFile(fname As String, srcPath As String, dstDir As String, filesNode As IXMLDOMElement)
    ' Copy file.
    fs.FileCopy srcPath, dstDir
    
    ' Record in DOM with MD5.
    Dim fileNode As IXMLDOMElement
    Set fileNode = filesNode.ownerDocument.createElement("file")
    filesNode.appendChild fileNode
    fileNode.setAttribute "path", fname
    fileNode.setAttribute "md5", enc.GetMD5(srcPath)
End Sub



Sub StartAnalysis(tmpPath As String, projectFile As String, dom As DOMDocument)
    Dim t As Single:  t = Timer
    
    ' Execute engine.exe with the working directory in
    ShellExecute 0, "", "engine.exe", "/q " + """" & projectFile & """", tmpPath, 0
    
    ' Pause.
    Sleep 2000
     
    Dim memusage As Long
    Dim pId As Long: pId = GetProcessesHandle("engine.exe")
    memusage = 0
    
    ' wait for analysis to finish
    Do While taskm.IsProcessActive("Engine.exe")
        Dim musage As Long: musage = GetProcessMemory(pId)
        If musage > memusage Then memusage = musage
        Sleep 250
        DoEvents
    Loop
    
    Dim runtime As Long
    runtime = CLng(Timer - t)
    
    Dim node As IXMLDOMElement
    Set node = dom.createElement("run-stats")
    dom.documentElement.appendChild node
    node.setAttribute "run-time", runtime
    node.setAttribute "max-memory", memusage
End Sub

Sub ExportResults(projectFile As String, dom As DOMDocument, quietMode As Boolean)
    ' Initialize.
    
    Dim settings As New clsSettings
    Dim project As New clsProject
    Dim analysis As New clsAnalysisResults
    
    analysis.Load project ' must occur once before ReadFromFile so that post-analysis cases load correctly
    project.ReadFromFile projectFile, analysis
    analysis.Load project ' must also occur after so that results are available
    settings.grMemberForceStations = 2
    settings.dontSaveSettings = True
    
    ' Create a node.
    
    Dim resultsNode As IXMLDOMElement
    Set resultsNode = dom.createElement("results")
    dom.documentElement.appendChild resultsNode
    
    ' Loop through result cases.
    
    Dim rcases As New Collection
    analysis.GetAllCases rcases
    
    If Not quietMode Then
        frmProgress.pbProgress.Min = 0
        frmProgress.pbProgress.Max = rcases.Count * RESULTDATA_MAXBUILTIN
    End If
    
    Dim i As Long
    For i = 1 To rcases.Count
        Dim rcase As intResultCase
        Set rcase = rcases.Item(i)
        
        Dim rcaseNode As IXMLDOMElement
        Set rcaseNode = dom.createElement("result-case")
        resultsNode.appendChild rcaseNode
        rcaseNode.setAttribute "name", rcase.name
        rcaseNode.setAttribute "solved", rcase.solved
        rcaseNode.setAttribute "class", rcase.loadclass
        
        Dim infoNode As IXMLDOMElement
        Set infoNode = dom.createElement("info")
        rcaseNode.appendChild infoNode
        infoNode.Text = rcase.info
        
        If rcase.solved Then
            ' Select this result case.
            analysis.UnselectAllCases
            rcase.Selected = True
            Set analysis.activeCase = rcase
            
            ' Loop through all results.
            Dim x As RESULTDATA
            For x = 1 To RESULTDATA.RESULTDATA_MAXBUILTIN
                If Not quietMode Then
                    frmProgress.lblResultCase.Caption = rcase.name
                    frmProgress.lblResultType.Caption = analysis.dataDescription(x)
                    frmProgress.pbProgress.value = (i - 1) * RESULTDATA_MAXBUILTIN + (x - 1)
                    DoEvents
                End If
            
                ExportResultsSpreadsheet analysis, settings, rcase, x, rcaseNode
            Next
        End If
    Next

    ' Release files so we can delete files.
    
    ' need to do this otherwise it still holds onto a .mdl file and we can't delete temporary path
    Dim db As clsDatabaseLink
    For i = 1 To project.dbLinks.Count
        Set db = project.dbLinks.itemByIndex(1)
        db.CloseDatabase True, False
        db.unlink
    Next
    
    project.ResetData
    analysis.RaiseRelease
    Set project = Nothing
    Set analysis = Nothing
    Set settings = Nothing
End Sub

Sub ExportResultsSpreadsheet(analysis As clsAnalysisResults, settings As clsSettings, rcase As intResultCase, resultDataType As RESULTDATA, rcaseNode As IXMLDOMElement)
    ' Just skip if there is no data available.
    If Not analysis.dataAvailable(resultDataType) Then Exit Sub
    
    ' Prepare the request data structure.
    Dim info As RESULTS_SPREAD_INFO
    info.rq.envelopeCol = 0
    info.display = RDM_SPREADSHEET
    info.rq.dataType = resultDataType
    
    ' Normally record incremental results so that a change in one linear case
    ' won't propagage to all later cases.
    info.rq.incremental = True
    info.rq.inUCS = False ' TODO: True too?
    
    ' But not all result types support incremental.
    If TypeOf rcase Is clsResultCaseModeshape Then info.rq.incremental = False
    If resultDataType = 16 Or resultDataType = 17 Or resultDataType = 55 Then info.rq.incremental = False ' tendon results don't support incremental
    
    ' If the result case indicates these results need to be enveloped, get the
    ' envelope columns from the column headers
    Dim envelopeCol1 As Integer, envelopeCol2 As Integer
    envelopeCol1 = 0
    envelopeCol2 = 0
    Dim headers()
    analysis.getSpreadsheetColumnHeaders info, headers
    If rcase.hasEnvelope(info.rq) And resultDataType <> 49 Then ' analyzed plate loads rejects enveloping
        envelopeCol1 = headers(0) - 1
        envelopeCol2 = headers(1) - 1
    End If
    
    ' Loop over envelope columns.
    Dim envCol As Integer
    For envCol = envelopeCol1 To envelopeCol2
        info.rq.envelopeCol = envCol
    
        ' Get the spreadsheet layout.
        ' If no results are available, simply skip.
        Dim spreadrows() As SPREAD_ROW_LOOKUP, spreaddata() As Variant, spreadTitle As String
        On Error GoTo 1000
        If Not analysis.getSpreadsheetLayout(info, spreadTitle, spreadrows, spreaddata, True, 0, settings) Then
            GoTo 1000
        End If
        On Error GoTo 0

        ' If we have results, at the first enveloped column create an outer node.
        Dim spreadNode As IXMLDOMElement
        If spreadNode Is Nothing Then
            ' Create a result node.
            Set spreadNode = rcaseNode.ownerDocument.createElement("result")
            rcaseNode.appendChild spreadNode
            spreadNode.setAttribute "id", resultDataType
            spreadNode.setAttribute "name", spreadTitle
            
            ' Write headers.
            Dim z As Long
            Dim headersNode As IXMLDOMElement
            Set headersNode = rcaseNode.ownerDocument.createElement("headers")
            spreadNode.appendChild headersNode
            For z = 2 To UBound(headers)
                Dim headerNode As IXMLDOMElement
                Set headerNode = rcaseNode.ownerDocument.createElement("header")
                headersNode.appendChild headerNode
                headerNode.Text = headers(z)
            Next
        End If
        
        ' Create an envelope col node.
        Dim envelopeNode As IXMLDOMElement
        Set envelopeNode = rcaseNode.ownerDocument.createElement("envelope")
        envelopeNode.setAttribute "column", CStr(envCol)
        spreadNode.appendChild envelopeNode
        
        ' Write row data.
        For z = 1 To UBound(spreadrows)
            Dim rowNode As IXMLDOMElement
            Set rowNode = rcaseNode.ownerDocument.createElement("row")
            envelopeNode.appendChild rowNode
            
            Dim datamin() As Variant, datamax() As Variant, ok As Boolean, error_string As String
            On Error GoTo 2000
            error_string = ""
            ok = analysis.getSpreadsheetRow(z, datamin, datamax, spreadrows, spreaddata, info)
            On Error GoTo 0
            If Not ok Then
                ' No data for this row.
                Dim nodataNode As IXMLDOMElement
                If error_string = "" Then
                    Set nodataNode = rcaseNode.ownerDocument.createElement("no-data")
                Else
                    Set nodataNode = rcaseNode.ownerDocument.createElement("error")
                    nodataNode.Text = error_string
                End If
                rowNode.appendChild nodataNode
            Else
                ' Data.
                If info.rq.envelopeCol = 0 Then
                    writeRow rowNode, "", datamin
                Else
                    writeRow rowNode, "min", datamin
                    writeRow rowNode, "max", datamin
                End If
            End If
            
        Next
    
1000
    Next

Exit Sub
2000
    error_string = Err.Description
    Resume Next
End Sub

Sub writeRow(rowNode As IXMLDOMElement, tag As String, data())
    Dim node As IXMLDOMElement
    If tag <> "" Then
        Set node = rowNode.ownerDocument.createElement(tag)
        rowNode.appendChild node
    Else
        Set node = rowNode
    End If

    Dim i As Integer
    For i = LBound(data) To UBound(data)
        Dim col As IXMLDOMElement
        Set col = rowNode.ownerDocument.createElement("col")
        node.appendChild col
        col.Text = CStr(data(i))
        If VarType(data(i)) <> vbSingle And VarType(data(i)) <> vbDouble Then
            col.setAttribute "dataType", VarType(data(i))
        End If
    Next
End Sub
