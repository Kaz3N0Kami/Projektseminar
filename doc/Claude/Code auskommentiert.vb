For ID = 1 To anzahl REM Zähle ALLE vorhandenen Stunden durch (by Default vom 1.1.2023 bis kurz vor Ende 2024 )
    ' === Variablendeklaration für diese Iteration ===
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
    ' ^ Akkumuliere die Gesamtkosten bei konstantem Tarif
    
    ladeverbrauch = 0 '  REM aktueller Verbrauch für Speicehrladeb
    ladeinfo = "" : ladeinfo2 = "" : SpeicherStatus = "" ' REM Log-Infos

    ' === Algorithmus 10: Einfachste Berechnung ===
    If Alg = 10 Then
        REM simplest calculation - no storage and fixed price 
        SummePreisDyn = SummePreisDyn + pb(ID) * Leistung
        REM  pb(..) Bruttopreis je stunde /  Leistung - aktuelle Leistungsaufnahme im Haus (meist konstant aus Maske )
    End If

    REM DynStrom with storage =========================================================================================================
    ' === Dynamischer Strom mit Speicher (Alg >= 100) ===
    If Alg >= 100 Then
        REM DynStrom :  ermittle mögliches Zeiten günstigen Stroms -> mit Lookup für 24 h (Par1)
        REM please check following ffunction:  it looks for "good" prices the next 24 hours !
        ' Führe Lookahead durch, um günstige Preise in den nächsten 24h zu finden
        LH = Lookahead(24, ID, Speicher <= 0.25 * Cap, Alg, MinimalwarAKTIV, p0)
        If LH > 0 Then
            REM besseren Preis gefundenn in lookahead-Intervall
            ' Speichere den gefundenen Zeitpunkt mit günstigem Preis
            If lhfound < LH Or lhfound = 0 Then
                lhfound = LH REM bisher kein guter oder schlechterer Preis
            End If
        Else
            REM nix gefunden
        End If

        REM neu - nur laden bei KEINEM Aufschlag !
        ' Berechne den DynPlus-Preis unter Berücksichtigung der Aufschläge
        DynPlusPreis = GetDynPlusPreis(p0(ID), pb(ID), limitProz, AufschlagAktiv, Alg, MinimalwarAKTIV, keinAufschlag, STDAufschlag)

        REM genereller Checck
        ' === Hauptentscheidung: Laden vs. normaler Betrieb ===
        If lhfound = ID And Not AufschlagAktiv Then
            REM JETZT gibt es günstigen Strom !
            REM bei Minum Preis Maximal JETZT Speicher laden
            REM jetzt Speicher laden !!!
            ' Lade den Speicher maximal auf, da jetzt günstiger Strom verfügbar ist
            ladeverbrauch = Cap - Speicher  REM Lade Speichet voll (wenn fast voll, nur noch Teilverbrauch)
            Verbrauch = ladeverbrauch + Leistung REM Netzverbrauch = Ladeverbrauch + Hausverbrauch
            Speicher = Speicher + ladeverbrauch   REM Speicher aufladen
            VerbrauchNetz = Verbrauch

        Else

            ' Model of house control unit ############################################################
            ' === Normaler Betrieb: Verbrauch aus Speicher oder Netz ===

            REM kein Minimum - normaler Verbrauch
            REM verbrauch = Leistung
            If Speicher >= Leistung Then
                REM Speicher hat noch Ladung
                ' Decke Verbrauch komplett aus Speicher
                Verbrauch = Leistung
                Speicher = Speicher - Verbrauch
                VerbrauchNetz = 0
            Else
                REM Speicher ist leer oder fast leer  ==>  Leistung aus Stromnetz holen !
                ' Speicher reicht nicht aus, beziehe aus Netz
                If Speicher = 0 Then
                    Verbrauch = Leistung
                    VerbrauchNetz = Verbrauch
                Else
                    REM  es ist noch etwas da, nicht ganz ausreecihend für eine Abdekung
                    ' Nutze Rest im Speicher und beziehe Differenz aus Netz
                    VerbrauchNetz = Leistung - Speicher
                    Speicher = 0

                End If

            End If

        End If REM ==========================================================================================

        ' === Algorithmus 100: Einfacher DynPreis-Modus ===
        If Alg = 100 Then
            REM einfacher DynPreos-Modus (wie bei Tibber)  - Standardverbrauch
            SummePreisDyn = SummePreisDyn + pb(ID) * VerbrauchNetz
            If pb(ID) < 0 Then
                REM bei negativen Bruttopreiesen verdient der DynKunde
                ' Kunde verdient Geld bei negativen Preisen (z.B. Überschuss an erneuerbarer Energie)
                InputDynKunde = InputDynKunde + pb(ID) * VerbrauchNetz * -1

            End If
        End If
        
        ' === Algorithmus 110: DynPreis mit Speicher ===
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

        ' === Logging wenn Speicher leer ===
        If Speicher < 1 Then
            REM speicher ist leer -> log-Ausgaben
            ladeinfo = ladeinfo & "--SPLEER--"
            SpeicherLeerStunden = SpeicherLeerStunden + 1
            sinfolog = sinfolog & "SpLEER!! "
        End If

        REM DynPlus -Spezialberechungen ##################################################################################
        ' === Algorithmus 200+: DynPlus mit Abgabenverschiebung ===

        If Alg >= 200 Then
            REM DynPlus Abgabenverschiebung
            'If datum(ID) = 2023010118 Then
            '  Debug.Print("Break?") REM nur für testzwecke
            'End If
            
            ' === Minimalperioden-Tracking ===
            If p0(ID) < 0.03 Then
                Minimalperiode = Minimalperiode + 1 REM für die spezielle Abgabenverschiebung - Zeit hochzählen für Statistisk
            Else
                If Minimalperiode > 0 Then MinimalwarAKTIV = True : ladeinfo2 = " MinimalwarAKTIV" : Minimalperiode = 0 :                         Rem Infoausgabe
            End If
            
            ' === Verlust-Check und Steuerung der Abgabenverschiebung ===
            If SummeVerlust < -10 Then
                REM Verluste erzeugt ->  In Abgabenverschiebung gehen
                MinimalwarAKTIV = False : ladeinfo2 = " PREISAUSGLEICH ->MinimalwarAKTIV=False "
                STDAufschlagOffen = STDAufschlagOffen + 1
            Else
                MinimalwarAKTIV = True
            End If

            ' === Standard-Aufschlag berechnen ===
            If p0(ID) <= STDAufschlagBis And MinimalwarAKTIV Then
                DynPreiSTD = pb(ID) + STDAufschlagWert
                STDAufschlagBENUTZT = STDAufschlagBENUTZT + 1
            Else
                DynPreiSTD = pb(ID)
            End If

            REM ermittle optimalen DynPlus-Preis
            DynPlusPreis = GetDynPlusPreis(p0(ID), pb(ID), limitProz, AufschlagAktiv, Alg, MinimalwarAKTIV, keinAufschlag, STDAufschlag)
            
            ' === Kosten- und Verlustberechnungen ===
            SummePreisDyn = SummePreisDyn + DynPlusPreis * VerbrauchNetz
            SummeVerlustDynPlusKunde = SummeVerlustDynPlusKunde + (pb(ID) - DynPlusPreis) * VerbrauchNetz
            preisdiff = pb(ID) - DynPlusPreis
            preisdiffSTD = pb(ID) - DynPreiSTD
            SummeVerlust = SummeVerlust + preisdiff * Leistung * 1 + preisdiffSTD * Leistung * AnzahlNormalKundenproDynPlusKunde

            ' === Logging und Statistiken ===
            If AufschlagAktiv Then sinfolog = sinfolog & "A! "
            If DynPlusPreis < 0 Then
                ' Kunde verdient bei negativem DynPlus-Preis
                InputDynKunde = InputDynKunde + DynPlusPreis * VerbrauchNetz * -1
                ladeinfo2 = ladeinfo2 & " -NegLeistSumt=" & DynPlusPreis * VerbrauchNetz * -1 & " RealGewinn=" & InputDynKunde
            End If
            
            ' === Statistiken für Netzverbrauch ===
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
        ' === Detaillierte Log-String-Erstellung ===
        sinfo = "" & datum(ID) & " #Sp=" & Speicher & " €Sum=" & SummePreisDyn & "€ PN=" & p0(ID) & "€ PB=" & pb(ID) & "€ VBnetz=" & VerbrauchNetz & " kWh "
        sinfo = sinfo & " SPL=" & ladeverbrauch & " kWh - LH=" & lhfound & "(P0=" & p0(lhfound) & ")" & ladeinfo & ladeinfo2
        If deblevel > 4 Then
            Debug.Print(sinfo)
        End If

        REM Ende modus = 20 or 30
    End If


    ' === Log-Datei schreiben ===
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
    ' === UI-Update und Zwischenausgaben ===
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