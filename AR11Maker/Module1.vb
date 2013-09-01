Imports System.Windows.Forms
Module Module1

    Sub Main()
        Dim ofd As New OpenFileDialog
        ofd.ShowDialog()
        ProcessBeatmap(ofd.FileName)
        '  Dim ofd As New FolderBrowserDialog
        ' ofd.ShowDialog()

        '        Dim di As IO.DirectoryInfo() = New IO.DirectoryInfo(ofd.SelectedPath).GetDirectories
        '        Dim maxcount As Integer = di.Length
        '        Dim currentcount As Integer = 0
        '        For Each d In di
        ' Dim fi As IO.FileInfo() = d.GetFiles()
        ' For Each fia As IO.FileInfo In fi
        ' If fia.FullName.Substring(fia.FullName.LastIndexOf(".")) = ".osu" Then
        ' ProcessBeatmap(fia.FullName)
        ' End If
        ' Next
        ' currentcount += 1
        ' Console.Title = "Processing: " & currentcount & "/" & maxcount & " directories processed"
        '  Next
    End Sub

    Sub ProcessBeatmap(ByVal file As String)
            Dim beatmapname As String = file.Substring(file.LastIndexOf("\") + 1, file.LastIndexOf(".") - (file.LastIndexOf("\") + 1))
            Dim beatmaplocation As String = file.Substring(0, file.LastIndexOf("\"))
            Dim beatmapcontents As New IO.StreamReader(file)
            Dim mp3filename As String = ""
            Do While beatmapcontents.Peek <> -1
                Dim s As String = beatmapcontents.ReadLine
                If s.Contains("AudioFilename") Then
                    mp3filename = s.Substring(s.IndexOf("AudioFilename") + 15)
                    Exit Do
                End If
            Loop
            Dim newbeatmapname As String = beatmapname.Substring(0, beatmapname.LastIndexOf("]")) & "AR11 +DT" & "]"
            Dim newmp3filename As String = (mp3filename.Substring(0, mp3filename.ToLower.IndexOf(".mp3")) & "forAR11.mp3").Replace(" ", "")

            'Use lame to convert mp3 to wav -> Change tempo by -33.333% with soundstretch -> Use lame to convert wav to mp3
            If My.Computer.FileSystem.FileExists(beatmaplocation & "\" & newmp3filename) = False Then
                My.Computer.FileSystem.CopyFile(beatmaplocation & "\" & mp3filename, Application.StartupPath & "\temp.mp3")
                Shell(Application.StartupPath & "\lame.exe --decode temp.mp3 temp.wav", AppWinStyle.Hide, True)
                Shell(Application.StartupPath & "\soundstretch.exe temp.wav temp2.wav -tempo=-33.333333333333%", AppWinStyle.Hide, True)
                Shell(Application.StartupPath & "\lame.exe temp2.wav " & newmp3filename, AppWinStyle.Hide, True)
                My.Computer.FileSystem.DeleteFile(Application.StartupPath & "\temp.mp3")
                My.Computer.FileSystem.DeleteFile(Application.StartupPath & "\temp.wav")
                My.Computer.FileSystem.DeleteFile(Application.StartupPath & "\temp2.wav")
                My.Computer.FileSystem.CopyFile(Application.StartupPath & "\" & newmp3filename, beatmaplocation & "\" & newmp3filename, True)
            End If

            'Process the beatmap changing all the timings and name of beatmap
            Dim newbeatmapcontents As String = IO.File.ReadAllText(file)
            Dim lines As New List(Of String)
            Dim newlines As New List(Of String)
            Dim currentline As Integer = 0
            Do
                Dim nextline As Integer = newbeatmapcontents.IndexOf(vbNewLine, currentline + 1)
                If nextline = -1 Then
                    Exit Do
                Else
                    lines.Add(newbeatmapcontents.Substring(currentline, nextline - currentline))
                    currentline = nextline
                End If
            Loop
            'Check if the beatmap contains an ApproachRate value
            Dim containsAR As Boolean = IIf(newbeatmapcontents.Contains("ApproachRate:"), True, False)

            'Start replacing contents
            newbeatmapcontents = newbeatmapcontents.Replace(mp3filename, newmp3filename)
            'Add ApproachRate value of 10 if it doesn't exists
            If containsAR = False Then
                For i = 0 To lines.Count - 1
                If lines(i).Contains("[Difficulty]") Then
                    Dim temp1 As New List(Of String)
                    Dim temp2 As New List(Of String)
                    For n = 0 To i
                        temp1.Add(lines(n))
                    Next
                    For n = i + 1 To lines.Count - 1
                        temp2.Add(lines(n))
                    Next
                    temp1.Add("ApproachRate:10")
                    lines.Clear()
                    For Each l In temp1
                        lines.Add(l)
                    Next
                    For Each l In temp2
                        lines.Add(l)
                    Next
                    Exit For
                End If
                Next
            End If
            Dim currentsection As String = ""
            For Each l In lines
                l = l.Replace(vbNewLine, "")
                Dim temp As String = l
                If (l.Contains("[General]")) Or (l.Contains("[Metadata]")) Or (l.Contains("[Difficulty]")) Or (l.Contains("[TimingPoints]")) Or (l.Contains("[HitObjects]")) Or (l.Contains("[Events]")) Then
                    currentsection = l
                End If
                If (currentsection.Contains("[General]")) And (l.Contains("AudioFilename:")) Then
                    temp = "AudioFilename: " & newmp3filename
                End If

                If (currentsection.Contains("[Metadata]")) And (l.Contains("Version:")) Then
                    temp = l & "AR11"
                End If
                If (currentsection.Contains("[Difficulty]")) And (l.Contains("ApproachRate")) Then
                    If l.Substring(l.IndexOf(":") + 1) <> 10 Then
                        temp = "ApproachRate:10"
                    Else
                        temp = l
                    End If
                End If
                If (currentsection.Contains("[Events]")) Then
                    Try
                        If l.Substring(0, 1) = "2" Then
                            Dim breaktiming1 As Integer = l.Substring(l.IndexOf(",") + 1, l.LastIndexOf(",") - (l.IndexOf(",") + 1))
                            Dim breaktiming2 As Integer = l.Substring(l.LastIndexOf(",") + 1)
                            Dim newbreaktiming1 As Integer = breaktiming1 * 1.5
                            Dim newbreaktiming2 As Integer = breaktiming2 * 1.5
                            temp = "2," & newbreaktiming1 & "," & newbreaktiming2
                        End If
                    Catch
                    End Try
                End If
                If (currentsection.Contains("[TimingPoints]")) And (l.Contains("[TimingPoints]") = False) Then
                    Try
                        Dim timing As String = l.Substring(0, l.IndexOf(","))
                        Dim bpmratio As Double = l.Substring(l.IndexOf(",") + 1, l.IndexOf(",", l.IndexOf(",") + 1) - (l.IndexOf(",") + 1))
                        Dim newtiming As String = Math.Round(CInt(timing) * 1.5).ToString
                        If bpmratio > 0 Then
                            Dim bpm As Double = 60000 / bpmratio
                            bpmratio = 60000 / (bpm - 0.3333333333333 * bpm)
                        End If
                        temp = newtiming & "," & bpmratio & l.Substring(l.IndexOf(",", l.IndexOf(",") + 1))
                    Catch
                    End Try
                End If
                If (currentsection.Contains("[HitObjects]")) And (l.Contains("[HitObjects]") = False) Then
                    Dim timing As String = l.Substring(l.IndexOf(",", l.IndexOf(",") + 1) + 1, l.IndexOf(",", l.IndexOf(",", l.IndexOf(",") + 1) + 1) - (l.IndexOf(",", l.IndexOf(",") + 1) + 1))
                    'Check for spinner
                    'Side note: wtf am I even doing? There is a much simpler way to do this, yet I choose
                    'the most ridiculous and confusing method. I don't even know how the following codes
                    'worked on the first attempt...
                    If l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",") + 1) + 1) + 1) + 1) + 1) <> -1 Then
                        Dim s As String = l.Substring(l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",") + 1) + 1) + 1) + 1) + 1, l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",") + 1) + 1) + 1) + 1) + 1) - (l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",") + 1) + 1) + 1) + 1) + 1))
                        If (s.Contains(":")) Or (s.Contains("L")) Or (s.Contains("P")) Or (s.Contains("|")) Then
                            Dim newtiming As String = Math.Round(CInt(timing) * 1.5).ToString
                            temp = l.Substring(0, l.IndexOf(",", l.IndexOf(",") + 1) + 1) & newtiming & l.Substring(l.IndexOf(",", l.IndexOf(",", l.IndexOf(",") + 1) + 1))
                        Else
                            Try
                                Dim newtiming As String = Math.Round(CInt(timing) * 1.5).ToString
                                Dim newsecondtiming As String = Math.Round(CInt(s) * 1.5).ToString
                                temp = l.Substring(0, l.IndexOf(",", l.IndexOf(",") + 1) + 1) & newtiming & l.Substring(l.IndexOf(",", l.IndexOf(",", l.IndexOf(",") + 1) + 1), (l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",") + 1) + 1) + 1) + 1) + 1) - (l.IndexOf(",", l.IndexOf(",", l.IndexOf(",") + 1) + 1))) & newsecondtiming & l.Substring(l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",", l.IndexOf(",") + 1) + 1) + 1) + 1) + 1))
                            Catch
                                Dim newtiming As String = Math.Round(CInt(timing) * 1.5).ToString
                                temp = l.Substring(0, l.IndexOf(",", l.IndexOf(",") + 1) + 1) & newtiming & l.Substring(l.IndexOf(",", l.IndexOf(",", l.IndexOf(",") + 1) + 1))
                            End Try
                        End If
                    Else
                        Dim newtiming As String = Math.Round(CInt(timing) * 1.5).ToString
                        temp = l.Substring(0, l.IndexOf(",", l.IndexOf(",") + 1) + 1) & newtiming & l.Substring(l.IndexOf(",", l.IndexOf(",", l.IndexOf(",") + 1) + 1))
                    End If

                End If
                newlines.Add(temp)
            Next
            For Each l In newlines
                My.Computer.FileSystem.WriteAllText(beatmaplocation & "\" & newbeatmapname & ".osu", l & vbNewLine, True)
            Next
    End Sub
End Module
