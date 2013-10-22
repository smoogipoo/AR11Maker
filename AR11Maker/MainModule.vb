Imports System.Windows.Forms
Module MainModule
    Private Const Version As String = "1.0.0.3"

    Sub Main()
        Application.CurrentCulture = New Globalization.CultureInfo("en-US", False)
        Console.WriteLine("Welcome to AR11Maker for osu! version: {0}", Version)
        Do
            Using ofd As New OpenFileDialog
                ofd.Title = "Please select the .osu file you want to convert."
                ofd.Filter = "osu! Beatmap Files (*.osu)|*.osu"
                If ofd.ShowDialog = DialogResult.OK Then
                    Console.ForegroundColor = ConsoleColor.White
                    If ofd.FileName.Substring(ofd.FileName.LastIndexOf(".")) = ".osu" Then
                        Try
                            ProcessBeatmap(ofd.FileName)
                        Catch ex As Exception
                            Console.WriteLine("A fatal error has occured and the program was not able to complete the conversion process.")
                            Console.WriteLine("Please take a screenshot of the following error and follow up on the forum:")
                            Console.WriteLine()
                            Dim location As String = ofd.FileName.Substring(0, ofd.FileName.LastIndexOf("\"))
                            Dim beatmapname As String = ofd.FileName.Substring(ofd.FileName.LastIndexOf("\") + 1)
                            Dim beatmap As String = ""
                            beatmap = beatmapname.Substring(beatmapname.LastIndexOf("\") + 1)
                            Console.WriteLine("Map: " & beatmap & "\" & beatmapname)
                            Console.WriteLine("Message: " & ex.Message)
                        End Try
                    Else
                        Console.WriteLine("The selected file was not a valid osu beatmap file (.osu)")
                    End If
                    Console.WriteLine("Press any key to select another beatmap")
                    Console.ReadKey()
                Else
                    Exit Do
                End If
            End Using
        Loop
    End Sub

    Sub ProcessBeatmap(ByVal file As String)
        Console.ForegroundColor = ConsoleColor.White
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
        Dim newbeatmapname As String = beatmapname.Substring(0, beatmapname.LastIndexOf("]")) & "AR11" & "]"
        Dim newmp3filename As String = (mp3filename.Substring(0, mp3filename.ToLower.IndexOf(".mp3")) & "forAR11.mp3").Replace(" ", "")

        Console.ForegroundColor = ConsoleColor.Green
        Console.WriteLine("Processing audio data")
        Console.ForegroundColor = ConsoleColor.White

        'Use lame to convert mp3 to wav -> Change tempo by -33.333% with soundstretch -> Use lame to convert wav to mp3
        If My.Computer.FileSystem.FileExists(beatmaplocation & "\" & newmp3filename) = False Then
            My.Computer.FileSystem.CopyFile(beatmaplocation & "\" & mp3filename, Application.StartupPath & "\temp.mp3")
            Shell(Application.StartupPath & "\lame.exe --decode temp.mp3 temp.wav", AppWinStyle.Hide, True)
            Shell(Application.StartupPath & "\soundstretch.exe temp.wav temp2.wav -tempo=-33.333333333333%", AppWinStyle.Hide, True)
            Shell(Application.StartupPath & "\lame.exe temp2.wav " & newmp3filename, AppWinStyle.Hide, True)
            My.Computer.FileSystem.DeleteFile(Application.StartupPath & "\temp.mp3")
            My.Computer.FileSystem.DeleteFile(Application.StartupPath & "\temp.wav")
            My.Computer.FileSystem.DeleteFile(Application.StartupPath & "\temp2.wav")
            My.Computer.FileSystem.MoveFile(Application.StartupPath & "\" & newmp3filename, beatmaplocation & "\" & newmp3filename, True)
        End If

        Console.ForegroundColor = ConsoleColor.Green
        Console.WriteLine("Done. Now processing beatmap")
        Console.ForegroundColor = ConsoleColor.White

        Dim newbeatmapcontents As String = IO.File.ReadAllText(file)
        Dim lines() As String = System.IO.File.ReadAllLines(file)
        Dim linecount As Integer = lines.Length

        'Check if the beatmap contains an ApproachRate value
        Dim containsAR As Boolean = IIf(newbeatmapcontents.Contains("ApproachRate:"), True, False)

        newbeatmapcontents = newbeatmapcontents.Replace(mp3filename, newmp3filename)
        'Add ApproachRate value of 10 if it doesn't exists
        If containsAR = False Then
            Dim temp2(lines.Length) As String
            For i = 0 To lines.Length - 1
                If lines(i).Contains("[Difficulty]") Then
                    temp2(i) = lines(i)
                    temp2(i + 1) = "ApproachRate:10"
                    i += 1
                Else
                    temp2(i) = lines(i)
                End If
            Next
            lines = temp2
        End If

        'Process
        Dim processedlines As Integer = 0
        Dim newlines As New List(Of String)
        Dim currentsection As String = ""
        For Each l In lines
            If l = "" Then
                newlines.Add(l)
                processedlines += 1
                Console.WriteLine("Processed {0}/{1} objects", processedlines, linecount)
                Continue For
            End If
            Dim temp As String = l

            If (l = "[General]") Or (l = "[Metadata]") Or (l = "[Difficulty]") Or (l = "[TimingPoints]") Or (l = "[HitObjects]") Or (l = "[Events]") Then
                newlines.Add(temp)
                currentsection = l
                processedlines += 1
                Console.WriteLine("Processed {0}/{1} lines", processedlines, linecount)
                Continue For
            End If

            If (currentsection = "[General]") And (l.Contains("AudioFilename:")) Then
                temp = "AudioFilename: " & newmp3filename
            End If

            If (currentsection = "[Metadata]") And (l.Contains("Version:")) Then
                temp = l & " AR11 +DT"
            End If

            If (currentsection = "[Difficulty]") And (l.Contains("ApproachRate:")) Then
                If l.Substring(l.IndexOf(":") + 1) <> 10 Then
                    temp = "ApproachRate:10"
                Else
                    temp = l
                End If
            End If

            If (currentsection = "[Events]") Then
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

            If (currentsection = "[TimingPoints]") Then
                Try
                    Dim timing As String = l.Substring(0, l.IndexOf(","))
                    Dim bpmratio As Double = CDbl(SubStr(l, nthDexOf(l, ",", 0) + 1, nthDexOf(l, ",", 1)))
                    Dim newtiming As String = Math.Round(CInt(timing) * 1.5).ToString
                    If bpmratio > 0 Then
                        Dim bpm As Double = 60000 / bpmratio
                        bpmratio = 60000 / (bpm - 0.3333333333333 * bpm)
                    End If
                    temp = newtiming & "," & bpmratio & l.Substring(l.IndexOf(",", l.IndexOf(",") + 1))
                Catch
                End Try
            End If

            If (currentsection = "[HitObjects]") Then
                Dim timing As String = SubStr(l, nthDexOf(l, ",", 1) + 1, nthDexOf(l, ",", 2))
                Dim newtiming As String = Math.Round(CInt(timing) * 1.5).ToString
                If nthDexOf(l, ",", 5) <> -1 Then
                    Dim s As String = SubStr(l, nthDexOf(l, ",", 4) + 1, nthDexOf(l, ",", 5))
                    If (s.Contains("L")) Or (s.Contains("P")) Or (s.Contains("B")) Or (s.Contains("|")) Then
                        'Slider
                        temp = SubStr(l, 0, nthDexOf(l, ",", 1) + 1) & newtiming & SubStr(l, nthDexOf(l, ",", 2))
                    Else
                        Try
                            'Spinner
                            Dim newsecondtiming As String = Math.Round(CInt(s) * 1.5).ToString
                            temp = SubStr(l, 0, nthDexOf(l, ",", 1) + 1) & newtiming & SubStr(l, nthDexOf(l, ",", 2), nthDexOf(l, ",", 4) + 1) & newsecondtiming & SubStr(l, nthDexOf(l, ",", 5))
                        Catch
                            'Circle
                            temp = SubStr(l, 0, nthDexOf(l, ",", 1) + 1) & newtiming & SubStr(l, (nthDexOf(l, ",", 2)))
                        End Try
                    End If
                Else
                    Try
                        'Spinner
                        Dim newsecondtiming As String = Math.Round(CInt(SubStr(l, nthDexOf(l, ",", 4) + 1)) * 1.5).ToString
                        temp = SubStr(l, 0, nthDexOf(l, ",", 1) + 1) & newtiming & SubStr(l, nthDexOf(l, ",", 2), nthDexOf(l, ",", 4) + 1) & newsecondtiming
                    Catch
                        'Circle
                        temp = SubStr(l, 0, nthDexOf(l, ",", 1) + 1) & newtiming & SubStr(l, nthDexOf(l, ",", 2))
                    End Try

                End If
            End If
            newlines.Add(temp)
            processedlines += 1
            Console.WriteLine("Processed {0}/{1} lines", processedlines, linecount)
        Next

        Console.ForegroundColor = ConsoleColor.Green
        Console.WriteLine("Done. Now saving")
        Console.ForegroundColor = ConsoleColor.White


        Using sw As New IO.StreamWriter(beatmaplocation & "\" & newbeatmapname & ".osu", True)
            For Each l In newlines
                sw.WriteLine(l)
            Next
        End Using
        Console.WriteLine("Done. Press the F5 button to refresh the osu! map list and the map will be shown!")
    End Sub

    'Homebrewed functions which work close to as fast as Substring & IndexOf in CPU time and faster than String.Split()
    Function nthDexOf(ByVal str As String, ByVal splitter As String, ByVal n As Integer) As Integer
        Dim camnt As Integer = -1
        Dim indx As Integer = 0
        Do Until (camnt = n) Or (indx = -1)
            indx = str.IndexOf(splitter, indx + 1)
            If indx = -1 Then
                Return -1
            End If
            camnt += 1
        Loop
        Return indx
    End Function
    Function SubStr(ByVal str As String, ByVal startindex As Integer, Optional ByVal endindex As Integer = -1) As String
        If endindex = -1 Then
            Return str.Substring(startindex, str.Length - startindex)
        Else
            Return str.Substring(startindex, endindex - startindex)
        End If
    End Function
End Module
