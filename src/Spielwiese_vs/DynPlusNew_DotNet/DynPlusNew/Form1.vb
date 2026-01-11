Imports System.CodeDom.Compiler
Imports System.IO
Imports System.Net.Sockets
Imports System.Runtime.CompilerServices
Imports System.Runtime.Intrinsics.X86
Imports Microsoft.VisualBasic.Logging

Public Class DynPlus100

    '  Tools  & Helpers 
    '  Replacment for  Access Form fields ->  Me!* to meparts("*") 
    Dim mepartsNames() As String  '  the names of the fields
    Dim meparts() As String ' the input data 
    Dim meparts0() As String ' the original input data for comparing with the new results 
    Dim mepartsResults() As String ' the results of the run 

    '  Replacment for  Access database tab for logging  ->  log data is saved in list and than saved to CSV-file 
    Dim SimLogCounter As Long = 0 : Const MaxSimlogCounter As Long = 75000
    Dim SaveLog As Boolean  ' flag - it TRUE thnan log is written to list and saved 
    Dim simlogNames(100) As String '  the names of the fields
    Dim simlogdata(MaxSimlogCounter, 100) As String

    Dim deblevel As Long ' Debugging level 0 = nearly no outputs to GUI, 5 = maximumm outputs 

    ' Global vars for better access (not good, but easier) 
    Dim anzahl As Long
    Dim flag As Boolean  ' commong flag for tests - only temporary usage at local places !

    ' Aufschlagstabelle
    Dim aufschlagDict As Dictionary(Of Decimal, Decimal)


    Sub Applog(text As String, Optional Flag_crlf As Boolean = True, Optional options As String = "")
        ' write Applog - or to GUI or to somewhre (file 
        ' Pars:  text to write , flag for adding crlf, options (very open )
        log.SelectionColor = Color.Black
        Dim output_crlf As String
        If Flag_crlf Then output_crlf = vbCrLf
        If InStr(options, "Error") Then log.SelectionColor = Color.Red
        log.AppendText(text + vbCrLf)  REM  System Call to Window App window 
        REM´´ Add other / additional options here  /to files etc...)
        Application.DoEvents()  ' let Windows do the GUI jobs ....   
    End Sub

    Function MepartsbyName(Parname) As String
        ' get the values of the old Acess form fields - see meparts above 
        Dim ii As Long
        For ii = 0 To mepartsNames.Length - 1
            If mepartsNames(ii) = Parname Then
                MepartsbyName = meparts(ii)
                Exit Function
            End If
        Next
        MepartsbyName = ""
        Applog(" ERROR MepartsbyName did not find the parameter " & Parname, True, "Error")
    End Function

    Function MePartSET(Parname, value) As Boolean
        ' SET ONE value of the old Acess form fields - see meparts above 
        Dim ii As Long
        For ii = 0 To mepartsNames.Length - 1
            If mepartsNames(ii) = Parname Then
                meparts(ii) = value
                MePartSET = True
                Exit Function
            End If
        Next
        MePartSET = False
        Applog(" ERROR MepartsSET did not find the parameter " & Parname, True, "Error")



    End Function

    Function Show_meparts_all()
        For ii = 0 To mepartsNames.Length - 1
            Applog(mepartsNames(ii) & " : " & meparts(ii))
        Next ii
    End Function

    Function Compare_meparts_all()
        Dim pname As String : Dim infostring As String = "" : Dim optstr As String
        Dim pname_char1 As String, pnamelabel As String, pnamelabelvalue As String
        Dim s As String
        ' Dim diff As Double
        For ii = 0 To mepartsNames.Length - 1
            pname = mepartsNames(ii) & " "
            pname_char1 = pname.Chars(0)
            pnamelabel = "" : pnamelabelvalue = ""
            pname = Trim(pname)
            If pname_char1 = "s" And Len(pname) = 3 Then
                pnamelabel = pname & "Label"
            End If
            If pname_char1 = "r" And Len(pname) = 2 Then
                pnamelabel = pname & "Label"
            End If
            If Len(pnamelabel) > 0 Then
                pnamelabelvalue = " [" & MepartsbyName(pnamelabel) & "] "
            End If

            '  Compare new and old valuess (from Access) 
            If Len("" & meparts0(ii)) > 0 Or Len("" & meparts(ii)) > 0 Then
                ' Dim diff As Double
                infostring = "" : optstr = ""
                If meparts0(ii) <> meparts(ii) Then
                    infostring = " Diff" : optstr = "Error"
                    'diff = (meparts0(ii) - meparts(ii)) / meparts0(ii) * 100 ' Diff in %
                    ' infostring &= diff & "%"
                    ' If diff < 0.01 Then optstr = "SmallDiff" : infostring = "Small-Diff (" & diff & "%)"
                End If
                s = " par " & pname & pnamelabelvalue
                s &= " : pold: " & meparts0(ii) & " : pnew " & meparts(ii) & infostring
                Applog(s, True, optstr)
            End If


        Next ii
    End Function

    Function Export_to_CSV_meparts(filename As String)
        Dim csv1 As String = ""
        Dim csv2 As String = ""
        Dim ii As Long
        For ii = 0 To mepartsNames.Length - 1
            csv1 = csv1 & mepartsNames(ii) & ";"
        Next
        For ii = 0 To mepartsNames.Length - 1
            csv2 = csv2 & meparts(ii) & ";"
        Next
        Dim output As String
        output = csv1 & vbCrLf & csv2
        File.WriteAllText(filename, output)
        Applog("Exported meparts to " & filename)
    End Function

    Function Export_to_CSV_Simlog(filename As String)
        Dim csv1 As String = ""
        Dim csv2 As String = ""
        Dim csvall As String = ""
        Dim ii As Long, izeile As Long, ispalte As Long
        For ii = 0 To simlogNames.Length - 1
            csv1 = csv1 & simlogNames(ii) & ";"
        Next
        REM File.WriteAllText(filename, csv1)
        csvall = csv1
        Dim simlogLen As Long = SimLogCounter  REM   simlogdata.Length - 1
        Dim simlogNameslen As Long = simlogNames.Length
        For izeile = 0 To simlogLen
            csv2 = ""
            For ispalte = 0 To simlogNameslen
                csv2 = csv2 & simlogdata(izeile, ispalte) & ";"
            Next ispalte
            csvall = csvall & csv2 & vbCrLf

        Next izeile

        File.WriteAllText(filename, csvall)
        Applog("Exported SimLog to " & filename)
    End Function

    Function SetSimlog(Parname As String, value As String)

        Dim ii As Long
        For ii = 0 To simlogNames.Length - 1
            If simlogNames(ii) = Parname Then
                simlogdata(SimLogCounter, ii) = value
                SetSimlog = True
                Exit Function
            End If
        Next
        SetSimlog = False
        Applog(" ERROR SetSimlog did not find the parameter " & Parname, True, "Error")
    End Function
    Public Function IsNull(Text As String) As Boolean
        ' .NET Replacement for Access IsNUll-function - for better sync of source codes 
        If Len("" & Text) = 0 Then
            IsNull = True
        Else
            IsNull = False
        End If
    End Function

    Public Function Round(value As Double) As Double
        ' .NET Replacement
        Round = Math.Round(value)
    End Function

    Function Lookahead(interval As Long, aktId As Long, dringend%, Alg%, MinimalwarAKTIV As Boolean, p0() As Double) As Integer
        ' SIM function for Dyyn prices - look into the future and check for good energy prices  
        Dim aktPreis As Decimal, idfound%, idEnd%, ID%
        REM If alg >= 200 Then
        REM   aktPreis

        aktPreis = p0(aktId)
        idEnd = aktId + interval REM Suchbereich von aktueller zeit + Intervall (id.g. 24 h)
        If idEnd > anzahl Then idEnd = anzahl

        idfound = -1 REM Flag für nichts gefunden
        For ID = aktId To idEnd
            If aktPreis > p0(ID) Then aktPreis = p0(ID) : idfound = ID :             Rem neues Minimum gefunden !

            REM bei fast leerem Speicher (dringend = true)  wird trotz nicht-optimaler Preise der Speicher geladen
            If p0(ID) < 0.01 Or (dringend = True And p0(ID) <= 0.05) Then
                aktPreis = p0(ID)
                idfound = ID
                Exit For
            End If
            If dringend = True And ID - aktId > 5 And idfound > 0 Then Exit For

        Next ID

        Lookahead = idfound

    End Function

    Public Function GetDynPlusPreis(ByVal p0 As Double, ByVal pb As Double, ByVal ProzLimit As Double, ByRef AufschlagAktiv As Boolean, Alg%, MinimalwarAKTIV As Boolean, keinAufschlag As Boolean, STDAufschlag As Boolean) As Decimal
        ' SIM function for Dyyn prices - calculates a NEW DYN price 
        Dim abg As Decimal, proz As Single, aufschlag As Decimal

        AufschlagAktiv = False
        abg = pb - p0
        If p0 <= 0 Then abg = 0
        If p0 = 0 Then
            abg = 0
        Else
            proz = abg / p0
            If proz > ProzLimit / 100 Then
                proz = ProzLimit / 100
                abg = p0 * proz
            End If
            REM von 0,04 bis 0,0 - ab 0,03 e Aufschlag
            REM If p0 >= 0.02 Then aufschlag = 0.03:
            aufschlag = 0
            REM If p0 >= 0.03 Then aufschlag = 0.09
            If Alg = 200 And Not keinAufschlag Then
                REM weite Verschiebung == nicht mehr relevant - war nicht optimal !!!
                REM == nicht mehr relevant - war nicht optimal !!!

                Dim dict As Dictionary(Of Decimal, Decimal)

                If aufschlagDict Is Nothing Then
                    aufschlagDict = LoadAufschlagDict($"aufschlag{Aufschlagstabelle.Text}.csv")
                End If
                dict = aufschlagDict


                ' Alle p0_min suchen die <= p0 sind → größtes nehmen
                Dim match = dict _
                .Where(Function(kvp) p0 >= kvp.Key) _
                .OrderByDescending(Function(kvp) kvp.Key) _
                .FirstOrDefault()

                If match.Value > 0 Then
                    aufschlag = match.Value
                End If
            End If
            If Alg = 210 And Not keinAufschlag Then
                REM kurze Verschiebung des Aufschlags unmittelbar nach Minimalperiode
                If MinimalwarAKTIV And p0 >= 0.05 Then
                    Dim diff As Decimal
                    diff = 0.35 - abg - p0
                    If diff > 0 Then aufschlag = diff
                End If
            End If

            If aufschlag > 0 Then AufschlagAktiv = True

            abg += aufschlag
        End If
        pb = p0 + abg

        GetDynPlusPreis = pb
    End Function

    Function LoadAufschlagDict(path As String) As Dictionary(Of Decimal, Decimal)
        Dim dict As New Dictionary(Of Decimal, Decimal)

        For Each line In IO.File.ReadAllLines(path)
            If String.IsNullOrWhiteSpace(line) Then Continue For
            If line.Trim.StartsWith("#") Then Continue For

            Dim parts = line.Split(";"c)
            If parts.Length >= 2 Then
                Dim key As Decimal = CDec(parts(0))
                Dim value As Decimal = CDec(parts(1))

                If Not dict.ContainsKey(key) Then
                    dict.Add(key, value)
                End If
            End If
        Next

        Return dict
    End Function

    Function Test_DynPlusPreis(i As Integer) As String
        REM tests only
        Dim pn As Double, pbr As Double, AufschlagAktiv As Boolean
        Dim pbbak As Decimal : Dim diff As Decimal : Dim s As String
        pn = -0.1

        While pn <= 1.0#

            pbr = pn + 0.2
            If pn > 0.1 Then pbr = pn + 0.21
            If pn > 0.2 Then pbr = pn + 0.23
            pbbak = pbr

            pbr = GetDynPlusPreis(pn, pbr, 300, AufschlagAktiv, 200, False, False, False)
            diff = pbbak - pbr
            s = "p0=" & pn & " pb=" & pbr & " diff=" & diff & "  AufschlagAktiv=" & AufschlagAktiv
            Debug.Print(s)
            pn += 0.01
            Const eps = 0.0000001
            If Math.Abs(pn) < eps Then pn = 0
        End While

        Test_DynPlusPreis = "Fertig"
    End Function



    Private Sub Test_Click()
        REM tests only
        Test_DynPlusPreis(0)
    End Sub



    '  Main Program ################################################

    Sub Init_and_start_simulation(options As String)
        ' SIM function for Dyn prices - then main on-top simulation calling function 
        Dim InputData_filename As String
        InputData_filename = ".\data\Preisdaten_Tab1.csv"
        Applog("Init ..", False)
        Applog("Load data from: " + InputData_filename + vbCrLf)
        Dim sz As Long : sz = 7   REM SzenarioID
        Dim deblevelstring As String ' Debugglevel
        deblevelstring = "" & Debuglevel.Text
        flag = Long.TryParse(deblevelstring, deblevel)
        If SaveLog_GUI.Checked Then options &= "Savelog"

        Applog("Start Simulation of SzenarioID=" & sz & " Debugglevel = " & deblevel, True, "Error")
        Run_simulation(sz, deblevel, options)
        aufschlagDict = Nothing
    End Sub

    Private Sub DynPlus100_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Init_and_start_simulation()
    End Sub

    Sub Run_simulation(szenarioID As Long, deblebel As Long, Optional options As String = "")
        ' SIM function for Dyn prices - then main simulation function  - it executes the simulation
        ' pars:   szenarioID - must be integer , deblebel - debugging level (see start of prog) , Options as text 
        Dim Global_Error As Long = 0
        Const MaxArrayLen = 50000 REM maximal number of data elemenst -> can be changed in future
        ' Dim datacount As Integer  : weg Dopplung mit linecount !!! inksoistent 
        Dim s As String  REM  Helper string - Temp values only 
        Dim i As Long REM  Helper long  - Temp values only 
        REM local storages of dateTime, net and brutto prices
        Dim datum(MaxArrayLen) As Long REM datTime-values for prices
        Dim p0(MaxArrayLen) As Double REM reiner Strombörsenpreis == NETTO - price
        Dim pb(MaxArrayLen) As Double REM mit Abgaben   == BRUTTO price
        Dim slog As String
        '  veraltet Dim filename As String
        Dim ID As Long REM aktuelle Stunde (ID aus DB )
        Dim Leistung As Double
        Dim SummePreisKonstant As Double : Dim SummePreisDyn As Double : Dim VerbrauchNetz As Double
        Dim Countervorgabe As Integer
        Dim Alg As Integer : Dim limitProz As Double
        Dim Speicher As Single : Dim lhfound As Long
        Dim SpeicherLeerStunden As Long : Dim DynPlusModus As Integer
        Dim SummeVerlustDynPlusKunde As Double
        Dim AnzahlNormalKundenproDynPlusKunde As Long
        Dim InputDynKunde As Double
        Dim LADENbyAufschlagAktivDauer As Long
        Dim PreisProKWh As Double
        Dim AufschlagAktiv As Boolean : Dim keinAufschlag As Boolean : Dim STDAufschlag As Boolean : Dim STDAufschlaggesamt As Long
        Dim STDAufschlagWert As Double : Dim STDAufschlagOffen As Long : Dim STDAufschlagBENUTZT As Long
        Dim SummeVerbrauchNetz0 As Single : Dim PreisKonstant As Double
        Dim SummeVerlust As Single : Dim DynPlusPreis As Decimal
        Dim Minimalperiode As Long : Dim MinimalwarAKTIV As Boolean
        Dim preisdiff As Double : Dim preisdiffSTD As Double, DynPreiSTD As Decimal

        Dim sinfolog As String
        ' Dim s As String

        ' ############################  Simulationstarten 

        REM Load Szenario  ======================================================
        Dim filenameSzenario As String
        Dim szidtext = Szenario.Text
        Dim szid As Long = 0
        Dim flagconvszid As Long = False
        flagconvszid = Long.TryParse(szidtext, szid)
        If Not flagconvszid Then Applog("SzenarioID not recognized (no conversion possible to long value)", True, "ERROR")
        If szid <> 1 And szid <> 7 And szid <> 8 Then
            Applog("SzenarioID actually only 1 or 7 !!!", True, "ERROR")
            Exit Sub


        End If
        szenarioID = szid

        filenameSzenario = ".\data\sim_szenario_AccessVB_szid_" & szid & ".csv"
        Applog("Szenario " & szenarioID & " loading from " & filenameSzenario)
        Dim szenariolines() As String = IO.File.ReadAllLines(filenameSzenario)
        Applog("Szenario " & szenarioID & " loaded with linecout = " & szenariolines.Length)
        mepartsNames = szenariolines(0).Split(";"c)
        meparts = szenariolines(1).Split(";"c)
        meparts0 = szenariolines(1).Split(";"c)
        Applog(" Loaded meparts= " & meparts.Length)
        If deblevel > 3 Then
            For i = 0 To meparts.Length - 1
                If Len(meparts(i)) > 0 Then
                    Applog("  SzPar[" & i & "] '" & mepartsNames(i) & "' =" & meparts(i))
                End If
            Next i
        End If
        Applog("===========  Szenario loaded =========================")


        REM Load SimLog-Structure (only fiest line for structure)   ======================================================
        Dim filenameSimLogStruct = ".\data\sim_logdata_reference.csv"
        Dim simloglines() As String = IO.File.ReadAllLines(filenameSimLogStruct)
        Applog("SimlogStructture loaded with linecout = " & simloglines.Length)
        simlogNames = simloglines(0).Split(";"c)
        Applog(" Loaded simlogNames= " & simlogNames.Length)
        s = ""
        If deblevel > 2 Then
            For i = 0 To simlogNames.Length - 1
                s &= simlogNames(i) & "- "
            Next i
            Applog("  simlogNames = " & s)
        End If
        Applog("===========  Simlog Structure loaded ========================")


        ' =========================================================================
        REM check for existing data in array (see doc above)
        If anzahl = 0 Then
            REM Call daten_laden()
            REM falls keine Daten im RAM sind. laden , anzahl wird danach auf die anzahl der Datensätze gesetzt
            Applog("Keine TimeSeries-Daten geladen - lade Szenario mit ID=" & szenarioID)
            ' CSV-Import: Erwartet Spalten: datum,p0,pb (Long, Double, Double)
            REM filename = "C:\Daten\dev\__EnergieDynPreisPlus\___DBEnergyLux\___c#\Preisdaten_Tab1.csv"
            Dim filenamePriceData = ".\data\Preisdaten_Tab1.csv"

            ' ofd.Filter = "CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*"
            ' If ofd.ShowDialog() = DialogResult.OK Then
            Dim lines() As String = IO.File.ReadAllLines(filenamePriceData)
            Dim linecount = 0 : Dim showlines = 5 : Dim startup As Boolean = True
            If deblevel > 3 Then Applog("Log first " & showlines & " text lines of data ... ")
            REM Chedck fpoir data format 
            For Each line As String In lines
                REM real data lines from second line in CSV 
                If String.IsNullOrWhiteSpace(line) Then Continue For
                Dim parts() As String = line.Split(";"c)
                If parts.Length < 3 Then
                    Applog("L" & linecount & ": " & " TOO LESS data !!! " & line, False, "ERROR")
                    Continue For
                End If
                If linecount >= MaxArrayLen Then
                    Applog("L" & linecount & ": " & " TO MUCH data !!! > " & MaxArrayLen & line, False, "ERROR")
                    Global_Error = -99
                    Exit For
                End If
                If startup Then
                    REM Header line - check for correct CSV format 
                    If deblevel > 3 Then
                        slog = "L" & linecount & ": " & line : Applog(slog)
                    End If
                    If parts(1) <> "DatenTypID" Then
                        Applog("L" & linecount & ": " & " WRONG FORMAT of data !!! ", False, "ERROR")
                        Global_Error = -100
                        Exit For
                    End If
                    startup = False  REM Header checked !
                Else
                    Dim flagconv As Boolean
                    If linecount = 18305 Then
                        Applog("fast Datenende")  ' only for debuggiin at the end of sim 
                    End If
                    flagconv = Long.TryParse(parts(2), datum(linecount))
                    flagconv = Double.TryParse(parts(9), p0(linecount))
                    flagconv = Double.TryParse(parts(10), pb(linecount))
                    If deblevel > 3 Then
                        If linecount < showlines Then
                            slog = "L" & linecount & ": " & line
                            slog += " => data[" & linecount & "] " & " datum=[" & datum(linecount) & "] "
                            slog += " p0=[" & p0(linecount) & "] p0=[" & pb(linecount) & "]"
                            Applog(slog)
                        End If
                    End If
                    linecount = linecount + 1
                End If

            Next
            anzahl = linecount  REM - 1
            ' Optional: Daten in globale Variablen übernehmen
            ' Me.datum = datum
            ' Me.p0 = p0
            ' Me.pb = pb
            ' Me.anzahl = anzahl

            Applog("CSV-Daten geladen: " & linecount & " Zeilen")
            Applog("===========  Time series data loaded =========================")
            '  End If
            ' End Sub

        End If

        REM init Simlog (detailied sorage of calculated data for each hour !)
        'Dim reclog As Recordset, SaveLog As Boolean, sinfolog As String, STDAufschlagBis As Single
        ' Set reclog = db.OpenRecordset("simlog")
        SaveLog = False  ' True  ' Me!SaveLog REM Hole SimLog-Statsu
        If InStr(options, "Savelog") > 0 Then SaveLog = True

        'If SaveLog Then
        'On Error GoTo Err_Execute
        'db.Execute("DELETE * FROM simlog;") REM falls aktiv, lösche letztes Log !
        'On Error GoTo 0
        'End If

        SummeVerbrauchNetz0 = 0
        Dim SummeDynPlusPreis0 As Single
        SummeDynPlusPreis0 = 0
        ' Dim crlf$
        ' crlf = Chr(13) & Chr(10)

        '  2nd round ------------------  7/2025  (from code conversion ) 
        Applog("### Starte Simulation ####################")

        szid = szenarioID

        Dim opt As String
        opt = "" & options REM hole eventuelle Optionen aus der Maske (bzw. aus der DB)
        keinAufschlag = False
        If InStr(opt, "KeinAufschlag") > 0 Then keinAufschlag = True :         Rem Setze einAufschlag = True falls Option "KeinAufschlag" aktiv
        STDAufschlag = False
        If InStr(opt, "AufschlagNurStandardKunde") > 0 Then STDAufschlag = True : keinAufschlag = True :         Rem dito wie vorher



        If Trim(MepartsbyName("sp8")) = "" Then STDAufschlagWert = 0 Else STDAufschlagWert = MepartsbyName("sp8")         REM Hole den Standardaufschlag aus Eingabefeld SP8
        Applog("  STDAufschlagWert = " & STDAufschlagWert)

        DynPlusModus = False REM Flag zum Testen der DynPlus-Optimierungen
        If Alg >= 200 Then DynPlusModus = True :         Rem für alle Algorithmen >= 200 Aktiviere DynPlusMode in den Berechnungen


        ' weg NEW 2025-07 - NO DB ACCESS - only from CSV alreday loaded 
        ' weg init databse access
        ' weg Dim db As Database, rec As Recordset
        ' weg set db = CurrentDb

        ' ORI Alg = Me!Algorithmus REM hole Algorithmus-Typ aus Maske
        Alg = MepartsbyName("Algorithmus") REM hole Algorithmus-Typ aus Maske
        Applog(" Alg=" & Alg)

        Countervorgabe = 1200
        Dim counter As Long
        Dim Cap As Single
        Dim STDAufschlagBis As Double  REM CHECK25!!!! 
        REM hole alle Vorgabewerte aus der Maske - unterschiedlich je nach Alg
        Leistung = MepartsbyName("sp3")
        If IsNull(MepartsbyName("sp7")) Then STDAufschlagBis = 0 Else STDAufschlagBis = MepartsbyName("sp7")
        PreisProKWh = MepartsbyName("sp1")
        SummePreisKonstant = 0
        SummePreisDyn = 0
        ' Me!DatumStart = datum(0)
        REM (Windows Funktion zum Erlauben der Anzeige von Ausgabewerten ...)
        REM  raus DoEvents
        MePartSET("r4", 0)  REM delta value init (show on form)
        counter = Countervorgabe
        If Alg > 10 Then
            REM mit Speicher nur auf Börsenstrompreis
            Cap = MepartsbyName("sp5") '   REM Speicherkapzität aus Maske
            If Alg >= 200 Then
                AnzahlNormalKundenproDynPlusKunde = MepartsbyName("sp6")
                limitProz = MepartsbyName("sp4")     ' REM Absenkungsgrenze
            End If
        End If



        REM hole alle Vorgabewerte aus de rMaeke - unterschiedlich je nach Alg
        PreisKonstant = MepartsbyName("sp1")  REM Normaler Strompreis (ca. 0,35 € pro KWh als Vergleichswerte für die Einsparpotentiale )
        Dim LADENbyAufschlagAktivWert As Double
        Dim NOTLADENAufschlagAktivWert As Double
        LADENbyAufschlagAktivWert = 0
        NOTLADENAufschlagAktivWert = 0

        '  round3 

        For ID = 1 To anzahl REM Zähle ALLE vorhandenen Stunden durch (by Default vom 1.1.2023 bis kurz vor Ende 2024 )
            Dim ladeverbrauch As Single : Dim ladeinfo As String : Dim ladeinfo2 As String
            Dim sinfo As String
            Dim SpeicherStatus As String
            Dim LH As Long
            Dim Verbrauch As Single

            'If datum(ID) = 2023010113 Then
            '  Debug.Print("STOP")
            'End If

            SummePreisKonstant = SummePreisKonstant + PreisKonstant * Leistung
            REM Strompreis bei konstanten Tarif
            ladeverbrauch = 0 '  REM aktueller Verbrauch für Speicehrladeb
            ladeinfo = "" : ladeinfo2 = "" : SpeicherStatus = "" ' REM Log-Infos

            If Alg = 10 Then
                REM simplest calculation - no storage and fixed price 
                SummePreisDyn = SummePreisDyn + pb(ID) * Leistung
                REM  pb(..) Bruttopreis je stunde /  Leistung - aktuelle Leistungsaufnahme im Haus (meist konstant aus Maske )
            End If

            REM DynStrom with storage =========================================================================================================
            If Alg >= 100 Then
                REM DynStrom :  ermittle mögliches Zeiten günstigen Stroms -> mit Lookup für 24 h (Par1)
                REM please check following ffunction:  it looks for "good" prices the next 24 hours !
                LH = Lookahead(24, ID, Speicher <= 0.25 * Cap, Alg, MinimalwarAKTIV, p0)
                If LH > 0 Then
                    REM besseren Preis gefundenn in lookahead-Intervall
                    If lhfound < LH Or lhfound = 0 Then
                        lhfound = LH REM bisher kein guter oder schlechterer Preis
                    End If
                Else
                    REM nix gefunden
                End If

                REM neu - nur laden bei KEINEM Aufschlag !
                DynPlusPreis = GetDynPlusPreis(p0(ID), pb(ID), limitProz, AufschlagAktiv, Alg, MinimalwarAKTIV, keinAufschlag, STDAufschlag)

                REM genereller Checck
                If lhfound = ID And Not AufschlagAktiv Then
                    REM JETZT gibt es günstigen Strom !
                    REM bei Minum Preis Maximal JETZT Speicher laden
                    REM jetzt Speicher laden !!!
                    ladeverbrauch = Cap - Speicher  REM Lade Speichet voll (wenn fast voll, nur noch Teilverbrauch)
                    Verbrauch = ladeverbrauch + Leistung REM Netzverbrauch = Ladeverbrauch + Hausverbrauch
                    Speicher = Speicher + ladeverbrauch   REM Speicher aufladen
                    VerbrauchNetz = Verbrauch

                Else

                    ' Model of house control unit ############################################################

                    REM kein Minimum - normaler Verbrauch
                    REM verbrauch = Leistung
                    If Speicher >= Leistung Then
                        REM Speicher hat noch Ladung
                        Verbrauch = Leistung
                        Speicher = Speicher - Verbrauch
                        VerbrauchNetz = 0
                    Else
                        REM Speicher ist leer oder fast leer  ==>  Leistung aus Stromnetz holen !
                        If Speicher = 0 Then
                            Verbrauch = Leistung
                            VerbrauchNetz = Verbrauch
                        Else
                            REM  es ist noch etwas da, nicht ganz ausreecihend für eine Abdekung
                            VerbrauchNetz = Leistung - Speicher
                            Speicher = 0

                        End If

                    End If

                End If REM ==========================================================================================

                If Alg = 100 Then
                    REM einfacher DynPreos-Modus (wie bei Tibber)  - Standardverbrauch
                    SummePreisDyn = SummePreisDyn + pb(ID) * VerbrauchNetz
                    If pb(ID) < 0 Then
                        REM bei negativen Bruttopreiesen verdient der DynKunde
                        InputDynKunde = InputDynKunde + pb(ID) * VerbrauchNetz * -1

                    End If
                End If
                If Alg = 110 Then
                    REM DynPreis mit Speicher
                    SummePreisDyn = SummePreisDyn + p0(ID) * VerbrauchNetz
                    SummeVerlust = SummeVerlust + (pb(ID) - p0(ID)) * VerbrauchNetz REM was würde der Staat verlieren
                    If p0(ID) < 0 Then
                        REM negativer Nettopreis (passiert bei viel Wind & Sonne)
                        InputDynKunde = InputDynKunde + p0(ID) * VerbrauchNetz * -1
                    End If
                End If
                sinfolog = ""

                If Speicher < 1 Then
                    REM speicher ist leer -> log-Ausgaben
                    ladeinfo = ladeinfo & "--SPLEER--"
                    SpeicherLeerStunden = SpeicherLeerStunden + 1
                    sinfolog = sinfolog & "SpLEER!! "
                End If

                REM DynPlus -Spezialberechungen ##################################################################################

                If Alg >= 200 Then
                    REM DynPlus Abgabenverschiebung
                    'If datum(ID) = 2023010118 Then
                    '  Debug.Print("Break?") REM nur für testzwecke
                    'End If
                    If p0(ID) < 0.03 Then
                        Minimalperiode = Minimalperiode + 1 REM für die spezielle Abgabenverschiebung - Zeit hochzählen für Statistisk
                    Else
                        If Minimalperiode > 0 Then MinimalwarAKTIV = True : ladeinfo2 = " MinimalwarAKTIV" : Minimalperiode = 0 :                         Rem Infoausgabe
                    End If
                    If SummeVerlust < -10 Then
                        REM Verluste erzeugt ->  In Abgabenverschiebung gehen
                        MinimalwarAKTIV = False : ladeinfo2 = " PREISAUSGLEICH ->MinimalwarAKTIV=False "
                        STDAufschlagOffen = STDAufschlagOffen + 1
                    Else
                        MinimalwarAKTIV = True
                    End If

                    If p0(ID) <= STDAufschlagBis And MinimalwarAKTIV Then
                        DynPreiSTD = pb(ID) + STDAufschlagWert
                        STDAufschlagBENUTZT = STDAufschlagBENUTZT + 1
                    Else
                        DynPreiSTD = pb(ID)
                    End If

                    REM ermittle optimalen DynPlus-Preis
                    DynPlusPreis = GetDynPlusPreis(p0(ID), pb(ID), limitProz, AufschlagAktiv, Alg, MinimalwarAKTIV, keinAufschlag, STDAufschlag)
                    SummePreisDyn = SummePreisDyn + DynPlusPreis * VerbrauchNetz
                    SummeVerlustDynPlusKunde = SummeVerlustDynPlusKunde + (pb(ID) - DynPlusPreis) * VerbrauchNetz
                    preisdiff = pb(ID) - DynPlusPreis
                    preisdiffSTD = pb(ID) - DynPreiSTD
                    SummeVerlust = SummeVerlust + preisdiff * Leistung * 1 + preisdiffSTD * Leistung * AnzahlNormalKundenproDynPlusKunde

                    If AufschlagAktiv Then sinfolog = sinfolog & "A! "
                    If DynPlusPreis < 0 Then
                        InputDynKunde = InputDynKunde + DynPlusPreis * VerbrauchNetz * -1
                        ladeinfo2 = ladeinfo2 & " -NegLeistSumt=" & DynPlusPreis * VerbrauchNetz * -1 & " RealGewinn=" & InputDynKunde
                    End If
                    If VerbrauchNetz > 0 Then
                        If AufschlagAktiv Then
                            REM erzeuge Statistiken
                            ladeinfo2 = ladeinfo2 & "##AufsAktiv!!!"
                            LADENbyAufschlagAktivDauer = LADENbyAufschlagAktivDauer + 1
                            LADENbyAufschlagAktivWert = LADENbyAufschlagAktivWert + DynPlusPreis * VerbrauchNetz
                            sinfolog = sinfolog & "Netzverbrauch mit Preisaufschlag! "
                        Else
                            NOTLADENAufschlagAktivWert = NOTLADENAufschlagAktivWert + DynPlusPreis * VerbrauchNetz

                            sinfolog = sinfolog & "Netzverbrauch! "

                        End If
                        If VerbrauchNetz > Leistung Then sinfolog = sinfolog & " STARKER VERBRAUCH +++++++++"
                    End If
                End If

                REM erzeuge Statistiken
                If ladeverbrauch > 0 Then ladeinfo = ladeinfo & "++SPLADUNG++"

                If Alg >= 200 Then
                    ladeinfo = ladeinfo + " SummeVerlust= " & SummeVerlust & " preisdiff=" & preisdiff
                End If

                REM erzeuge Statistiken
                REM lang sinfo = "id=" & ID & " #Speicher=" & Speicher & " €SumPreis=" & SummePreisDyn & "€ BPreis=" & p0(ID) & "€ Brutto=" & pb(ID) & "€  Verbrauch=" & Verbrauch & " kWh "
                REM sinfo = sinfo & " Speicherladung=" & ladeverbrauch & " kWh -  Lookahead=" & lhfound & "(P0=" & p0(lhfound) & ")" & ladeinfo
                sinfo = "" & datum(ID) & " #Sp=" & Speicher & " €Sum=" & SummePreisDyn & "€ PN=" & p0(ID) & "€ PB=" & pb(ID) & "€ VBnetz=" & VerbrauchNetz & " kWh "
                sinfo = sinfo & " SPL=" & ladeverbrauch & " kWh - LH=" & lhfound & "(P0=" & p0(lhfound) & ")" & ladeinfo & ladeinfo2
                If deblevel > 4 Then
                    Debug.Print(sinfo)
                End If

                REM Ende modus = 20 or 30
            End If


            If SaveLog Then
                SimLogCounter = SimLogCounter + 1
                REM SCHREIBE  logfile
                ' simlogdata(SimLogCounter)  ' reclog.AddNew
                SetSimlog("SimRunID", 1020) '    REM to be changed to dynamci ID
                SetSimlog("SzenarioID", szid) ' REM NEW 2025 TW set SzenrioID from Szenario
                SetSimlog("DatumID", datum(ID))
                SetSimlog("ID", ID)
                SetSimlog("p0", p0(ID))
                SetSimlog("pb", pb(ID))
                SetSimlog("DynPlusPreis", DynPlusPreis)
                SetSimlog("pbDynPreisDiff", pb(ID) - DynPlusPreis)
                SetSimlog("AufschlagAktiv", AufschlagAktiv)
                SetSimlog("VerbrauchNetz", VerbrauchNetz)

                SummeVerbrauchNetz0 = SummeVerbrauchNetz0 + VerbrauchNetz
                SetSimlog("SummeVerbrauchNetz", SummeVerbrauchNetz0)

                SummeDynPlusPreis0 = SummeDynPlusPreis0 + DynPlusPreis
                SetSimlog("SummeDynPlusPreis", SummeDynPlusPreis0)

                SetSimlog("Speicher", Speicher)
                SetSimlog("LADENbyAufschlagAktivWert", LADENbyAufschlagAktivWert)
                SetSimlog("NOTLADENAufschlagAktivWert", NOTLADENAufschlagAktivWert)
                SetSimlog("SummeVerlust", SummeVerlust)
                'SetSimlog("sinfo , sinfolog)
                ' reclog.Update

            End If

            REM Zwischenausgaben // Animationen
            If counter = 0 Or ID = anzahl Then
                If ID = anzahl Then
                    Applog("Saving results ...") ' for debugging only  - last stop before end of simulation 
                End If
                MePartSET("DatumEnde", datum(ID - 1))
                MePartSET("r1", SummePreisKonstant)
                MePartSET("r2", SummePreisDyn)
                MePartSET("r3", SummePreisKonstant - SummePreisDyn)
                MePartSET("r5", SummeVerlustDynPlusKunde)
                MePartSET("r6", SummeVerlust)

                ' DoEvents
                counter = Countervorgabe
            End If
            counter = counter - 1
        Next ID

        Dim dauerTage As Long, dauerJahre As Double, resinfo$

        ' Me!DatumEnde = datum(anzahl - 1)
        MePartSET("DatumEnde", datum(anzahl - 1))
        MePartSET("DatumDauerStunden", anzahl)
        dauerTage = anzahl / 24
        MePartSET("DatumDauerTage", dauerTage)
        dauerJahre = dauerTage / 365
        MePartSET("DatumDauerJahre", dauerJahre)
        REM Normierung der Eergebnisse (speziell hir Einpsarungen bzw. DynPlus-Effekt AUF EIN Kalenderjahr)
        MePartSET("r4", (SummePreisKonstant - SummePreisDyn) / dauerJahre)
        resinfo = ""

        REM erzeuge Statistiken
        If Alg >= 100 Then
            resinfo = "SpeicherLeer=" & SpeicherLeerStunden & " h (" & Int(SpeicherLeerStunden / 24) & " Tage)=" & Int(10.0# * SpeicherLeerStunden / anzahl * 10) & "%"

            If Alg >= 200 And Alg < 20 Then
                resinfo = resinfo & vbCrLf & "LADENbyAufschlagAktivDauer=" & LADENbyAufschlagAktivDauer & "h =" & Int(10 * LADENbyAufschlagAktivDauer / anzahl * 10) & "%"
                resinfo = resinfo & vbCrLf & "LADENbyAufschlagAktivWert=" & Int(LADENbyAufschlagAktivWert) & " € = " & Int(10 * LADENbyAufschlagAktivWert / SummePreisDyn * 10) & "%"
                resinfo = resinfo & vbCrLf & "NOTLADENAufschlagAktivWert=" & Int(NOTLADENAufschlagAktivWert) & " €" & " (Diff=" & (NOTLADENAufschlagAktivWert + LADENbyAufschlagAktivWert - SummePreisDyn) & ")"
            End If
            If Alg >= 220 And Alg < 230 Then

                STDAufschlaggesamt = STDAufschlagBENUTZT + STDAufschlagOffen
                resinfo = resinfo & vbCrLf & " STDAufschlagBENUTZT=" & STDAufschlagBENUTZT & " (" & Round(1.0# * STDAufschlagBENUTZT / STDAufschlaggesamt * 100) & "%)"
                resinfo = resinfo & vbCrLf & " STDAufschlagOffen=" & STDAufschlagOffen & " (" & Round(1.0# * STDAufschlagOffen / STDAufschlaggesamt * 100.0#) & "%)"
            End If


            MePartSET("r5", SummeVerlustDynPlusKunde)
            MePartSET("r6", SummeVerlust)
            MePartSET("r7", SpeicherLeerStunden)
            MePartSET("r9", STDAufschlagBENUTZT / STDAufschlaggesamt * 10)

        End If
        REM Me!ResultatText = resinfo
        '  New add ons for results 
        MePartSET("LastSimulationbyName", "Sim by DotNETVB 2025 TW")
        s = Now.ToString("dd.MM.yyyy HH:mm")
        MePartSET("LastSimulationDateTime", s)

        If deblevel > 2 Then Applog(resinfo)
        If deblevel > 3 Then Show_meparts_all()
        If deblevel > 2 Then Compare_meparts_all()

        Dim filenameSzenarioExport As String
        filenameSzenarioExport = filenameSzenario.Substring(0, Len(filenameSzenario) - 4)
        filenameSzenarioExport = filenameSzenarioExport & $"_aufschlag_{Aufschlagstabelle.Text}" & "_Export.csv"
        Export_to_CSV_meparts(filenameSzenarioExport)
        Applog("Results exported to " & filenameSzenarioExport)
        If SaveLog Then
            Applog("Starte SaveLog ...")
            Dim filenameSimLogExport As String
            filenameSimLogExport = filenameSimLogStruct.Substring(0, Len(filenameSimLogStruct) - 4)
            filenameSimLogExport = filenameSimLogExport & "_Export.csv"
            Export_to_CSV_Simlog(filenameSimLogExport)
            Applog("Simlog-Results exported to " & filenameSimLogExport)
        End If

        If Global_Error <> 0 Then Applog("Global Error : " & Global_Error, True, "Red")



        Exit Sub

        'Err_Execute:
        ' Ori MsgBox(Error(Err))
        ' MsgBox("Error in Fusszeile")

        ' Exit Sub

    End Sub

    Private Sub Start_Simulation_Click(sender As Object, e As EventArgs) Handles Start_Simulation.Click
        Init_and_start_simulation("")
    End Sub

    Private Sub ClearScreen_Click(sender As Object, e As EventArgs) Handles ClearScreen.Click
        log.Clear()
    End Sub

End Class

