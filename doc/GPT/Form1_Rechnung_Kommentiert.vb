' For ID = 1 To anzahl REM Zähle ALLE vorhandenen Stunden durch (by Default vom 1.1.2023 bis kurz vor Ende 2024 )
    ' Initialisierung diverser Variablen für jede Stunde
    Dim ladeverbrauch As Single : Dim ladeinfo As String : Dim ladeinfo2 As String
    Dim sinfo As String
    Dim SpeicherStatus As String
    Dim LH As Long
    Dim Verbrauch As Single

    ' Debug-Stop für bestimmte Zeitschritte (auskommentiert)
    'If datum(ID) = 2023010113 Then
    '  Debug.Print("STOP")
    'End If

    ' Preisberechnung für den konstanten Tarif
    SummePreisKonstant = SummePreisKonstant + PreisKonstant * Leistung
    REM Strompreis bei konstanten Tarif

    ' Reset für Ladeverbrauch und Logvariablen
    ladeverbrauch = 0 '  REM aktueller Verbrauch für Speicherladung
    ladeinfo = "" : ladeinfo2 = "" : SpeicherStatus = "" ' REM Log-Infos

    ' Einfacher Algorithmus ohne Speicher
    If Alg = 10 Then
        REM simplest calculation - no storage and fixed price 
        SummePreisDyn = SummePreisDyn + pb(ID) * Leistung
        REM  pb(..) Bruttopreis je Stunde * Leistung - Hausverbrauch
    End If

    ' *** Dynamische Stromtarife mit Speicher ***
    If Alg >= 100 Then
        ' Lookahead über 24 Stunden, ob günstigere Preise kommen
        LH = Lookahead(24, ID, Speicher <= 0.25 * Cap, Alg, MinimalwarAKTIV, p0)
        If LH > 0 Then
            ' Wenn besserer Preis gefunden wurde
            If lhfound < LH Or lhfound = 0 Then
                lhfound = LH ' Update des besten gefundenen Zeitpunkts
            End If
        Else
            ' Kein günstiger Preis im Lookahead gefunden
        End If

        ' Neu: Lade nur, wenn KEIN Aufschlag aktiv ist
        DynPlusPreis = GetDynPlusPreis(p0(ID), pb(ID), limitProz, AufschlagAktiv, Alg, MinimalwarAKTIV, keinAufschlag, STDAufschlag)

        ' Prüfe: Sind wir genau in der Lookahead-Stunde und ohne Preisaufschlag?
        If lhfound = ID And Not AufschlagAktiv Then
            ' Günstige Stunde zum Laden → Speicher maximal laden
            ladeverbrauch = Cap - Speicher  ' Speicher komplett auffüllen
            Verbrauch = ladeverbrauch + Leistung ' Netzverbrauch: Laden + Hausverbrauch
            Speicher = Speicher + ladeverbrauch   ' Speicher aktualisieren
            VerbrauchNetz = Verbrauch

        Else
            ' *** Modell der Haus-Steuerung ***
            If Speicher >= Leistung Then
                ' Speicher deckt Hausverbrauch komplett
                Verbrauch = Leistung
                Speicher = Speicher - Verbrauch
                VerbrauchNetz = 0
            Else
                ' Speicher reicht nicht aus → Netzverbrauch notwendig
                If Speicher = 0 Then
                    Verbrauch = Leistung
                    VerbrauchNetz = Verbrauch
                Else
                    ' Teilweise Speicher, Rest Netz
                    VerbrauchNetz = Leistung - Speicher
                    Speicher = 0
                End If
            End If
        End If  ' Ende des dynamischen Verbrauchsmodells

        ' Dynamische Tarifabrechnung Standardmodus (wie Tibber)
        If Alg = 100 Then
            SummePreisDyn = SummePreisDyn + pb(ID) * VerbrauchNetz
            If pb(ID) < 0 Then
                ' Negativer Strompreis → Kunde bekommt Geld
                InputDynKunde = InputDynKunde + pb(ID) * VerbrauchNetz * -1
            End If
        End If

        ' Dynamischer Tarif mit Speicher
        If Alg = 110 Then
            SummePreisDyn = SummePreisDyn + p0(ID) * VerbrauchNetz
            SummeVerlust = SummeVerlust + (pb(ID) - p0(ID)) * VerbrauchNetz ' Differenz Brutto-Nettopreis
            If p0(ID) < 0 Then
                ' Negativer Nettopreis
                InputDynKunde = InputDynKunde + p0(ID) * VerbrauchNetz * -1
            End If
        End If

        sinfolog = ""

        ' Log: Speicher leer
        If Speicher < 1 Then
            ladeinfo = ladeinfo & "--SPLEER--"
            SpeicherLeerStunden = SpeicherLeerStunden + 1
            sinfolog = sinfolog & "SpLEER!! "
        End If

        ' *** DynPlus – Spezialberechnungen ***
        If Alg >= 200 Then
            ' Verwaltung der Minimalperiode (Abgabenverschiebung)
            If p0(ID) < 0.03 Then
                Minimalperiode = Minimalperiode + 1
            Else
                If Minimalperiode > 0 Then MinimalwarAKTIV = True : ladeinfo2 = " MinimalwarAKTIV" : Minimalperiode = 0
            End If

            ' Verlustlogik → Umschalten auf Abgabenverschiebung
            If SummeVerlust < -10 Then
                MinimalwarAKTIV = False : ladeinfo2 = " PREISAUSGLEICH ->MinimalwarAKTIV=False "
                STDAufschlagOffen = STDAufschlagOffen + 1
            Else
                MinimalwarAKTIV = True
            End If

            ' Standardpreis mit Aufschlag, falls aktiv
            If p0(ID) <= STDAufschlagBis And MinimalwarAKTIV Then
                DynPreiSTD = pb(ID) + STDAufschlagWert
                STDAufschlagBENUTZT = STDAufschlagBENUTZT + 1
            Else
                DynPreiSTD = pb(ID)
            End If

            ' Optimalen DynPlus-Preis bestimmen
            DynPlusPreis = GetDynPlusPreis(p0(ID), pb(ID), limitProz, AufschlagAktiv, Alg, MinimalwarAKTIV, keinAufschlag, STDAufschlag)
            SummePreisDyn = SummePreisDyn + DynPlusPreis * VerbrauchNetz
            SummeVerlustDynPlusKunde = SummeVerlustDynPlusKunde + (pb(ID) - DynPlusPreis) * VerbrauchNetz

            preisdiff = pb(ID) - DynPlusPreis
            preisdiffSTD = pb(ID) - DynPreiSTD

            ' Gesamtverlustberechnung
            SummeVerlust = SummeVerlust + preisdiff * Leistung * 1 + preisdiffSTD * Leistung * AnzahlNormalKundenproDynPlusKunde

            ' Logging und Statistik
            If AufschlagAktiv Then sinfolog = sinfolog & "A! "
            If DynPlusPreis < 0 Then
                InputDynKunde = InputDynKunde + DynPlusPreis * VerbrauchNetz * -1
                ladeinfo2 = ladeinfo2 & " -NegLeistSumt=" & DynPlusPreis * VerbrauchNetz * -1 & " RealGewinn=" & InputDynKunde
            End If

            If VerbrauchNetz > 0 Then
                If AufschlagAktiv Then
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

        ' Ladeindikator
        If ladeverbrauch > 0 Then ladeinfo = ladeinfo & "++SPLADUNG++"

        ' Verlustinfo für Alg >= 200
        If Alg >= 200 Then
            ladeinfo = ladeinfo + " SummeVerlust= " & SummeVerlust & " preisdiff=" & preisdiff
        End If

        ' Zusammenstellung der Log-Ausgabe
        sinfo = "" & datum(ID) & " #Sp=" & Speicher & " €Sum=" & SummePreisDyn & "€ PN=" & p0(ID) & "€ PB=" & pb(ID) & "€ VBnetz=" & VerbrauchNetz & " kWh "
        sinfo = sinfo & " SPL=" & ladeverbrauch & " kWh - LH=" & lhfound & "(P0=" & p0(lhfound) & ")" & ladeinfo & ladeinfo2

        If deblevel > 4 Then
            Debug.Print(sinfo)
        End If
    End If ' Ende der dynamischen Tariflogik

    ' *** Log-Speicherung ***
    If SaveLog Then
        SimLogCounter = SimLogCounter + 1

        SetSimlog("SimRunID", 1020)
        SetSimlog("SzenarioID", szid)
        SetSimlog("DatumID", datum(ID))
        SetSimlog("ID", ID)
        SetSimlog("p0", p0(ID))
        SetSimlog("pb", pb(ID))
        SetSimlog("DynPlusPreis", DynPlusPreis)
        SetSimlog("pbDynPreisDiff", pb(ID) - DynPlusPreis)
        SetSimlog("AufschlagAktiv", AufschlagAktiv)
        SetSimlog("VerbrauchNetz", VerbrauchNetz)

        ' Kumulative Netzverbrauchstatistik
        SummeVerbrauchNetz0 = SummeVerbrauchNetz0 + VerbrauchNetz
        SetSimlog("SummeVerbrauchNetz", SummeVerbrauchNetz0)

        ' Kumulative Preisstatistik
        SummeDynPlusPreis0 = SummeDynPlusPreis0 + DynPlusPreis
        SetSimlog("SummeDynPlusPreis", SummeDynPlusPreis0)

        SetSimlog("Speicher", Speicher)
        SetSimlog("LADENbyAufschlagAktivWert", LADENbyAufschlagAktivWert)
        SetSimlog("NOTLADENAufschlagAktivWert", NOTLADENAufschlagAktivWert)
        SetSimlog("SummeVerlust", SummeVerlust)
    End If

    ' *** Zwischenausgaben und GUI-Updates ***
    If counter = 0 Or ID = anzahl Then
        If ID = anzahl Then
            Applog("Saving results ...")
        End If
        MePartSET("DatumEnde", datum(ID - 1))
        MePartSET("r1", SummePreisKonstant)
        MePartSET("r2", SummePreisDyn)
        MePartSET("r3", SummePreisKonstant - SummePreisDyn)
        MePartSET("r5", SummeVerlustDynPlusKunde)
        MePartSET("r6", SummeVerlust)

        counter = Countervorgabe
    End If

    counter = counter - 1
Next ID
