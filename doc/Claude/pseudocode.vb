FUNKTION SimulationHauptschleife():
    FÜR jede Stunde ID von 1 bis anzahl:
        // Initialisierung
        DEKLARIERE ladeverbrauch, ladeinfo, ladeinfo2, sinfo, SpeicherStatus, LH, Verbrauch
        
        SETZE ladeverbrauch = 0
        SETZE ladeinfo = ""
        SETZE ladeinfo2 = ""
        SETZE SpeicherStatus = ""
        
        // Berechne Kosten bei konstantem Tarif
        SummePreisKonstant += PreisKonstant × Leistung
        
        // === Modus 10: Einfache Berechnung ohne Speicher ===
        WENN Alg == 10:
            SummePreisDyn += pb[ID] × Leistung
        ENDE WENN
        
        // === Modus 100+: Dynamischer Strom mit Speicher ===
        WENN Alg >= 100:
            // Suche nach günstigen Preisen in den nächsten 24 Stunden
            LH = Lookahead(24 Stunden, ID, Speicher <= 0.25 × Kapazität, Alg, MinimalwarAKTIV, p0)
            
            WENN LH > 0:
                WENN lhfound < LH ODER lhfound == 0:
                    lhfound = LH
                ENDE WENN
            ENDE WENN
            
            // Berechne DynPlus-Preis
            DynPlusPreis = GetDynPlusPreis(p0[ID], pb[ID], limitProz, AufschlagAktiv, Alg, MinimalwarAKTIV, keinAufschlag, STDAufschlag)
            
            // --- Hauptlogik: Entscheidung Laden vs. Verbrauch ---
            WENN lhfound == ID UND NICHT AufschlagAktiv:
                // Günstiger Strom JETZT verfügbar - Speicher maximal laden
                ladeverbrauch = Kapazität - Speicher
                Verbrauch = ladeverbrauch + Leistung
                Speicher += ladeverbrauch
                VerbrauchNetz = Verbrauch
                
            SONST:
                // Normaler Betrieb - aus Speicher oder Netz beziehen
                WENN Speicher >= Leistung:
                    // Ausreichend Speicher vorhanden
                    Verbrauch = Leistung
                    Speicher -= Verbrauch
                    VerbrauchNetz = 0
                    
                SONST:
                    // Speicher leer oder unzureichend
                    WENN Speicher == 0:
                        Verbrauch = Leistung
                        VerbrauchNetz = Verbrauch
                    SONST:
                        // Restlichen Speicher nutzen + aus Netz beziehen
                        VerbrauchNetz = Leistung - Speicher
                        Speicher = 0
                    ENDE WENN
                ENDE WENN
            ENDE WENN
            
            // --- Modus-spezifische Berechnungen ---
            WENN Alg == 100:
                // Einfacher DynPreis (wie Tibber)
                SummePreisDyn += pb[ID] × VerbrauchNetz
                
                WENN pb[ID] < 0:
                    InputDynKunde += |pb[ID] × VerbrauchNetz|
                ENDE WENN
                
            SONST WENN Alg == 110:
                // DynPreis mit Speicher
                SummePreisDyn += p0[ID] × VerbrauchNetz
                SummeVerlust += (pb[ID] - p0[ID]) × VerbrauchNetz
                
                WENN p0[ID] < 0:
                    InputDynKunde += |p0[ID] × VerbrauchNetz|
                ENDE WENN
            ENDE WENN
            
            // Log für leeren Speicher
            WENN Speicher < 1:
                ladeinfo += "--SPLEER--"
                SpeicherLeerStunden++
                sinfolog += "SpLEER!! "
            ENDE WENN
            
            // --- DynPlus-Spezialberechnung (Modus 200+) ---
            WENN Alg >= 200:
                // Minimalperioden-Tracking
                WENN p0[ID] < 0.03:
                    Minimalperiode++
                SONST:
                    WENN Minimalperiode > 0:
                        MinimalwarAKTIV = True
                        ladeinfo2 = " MinimalwarAKTIV"
                        Minimalperiode = 0
                    ENDE WENN
                ENDE WENN
                
                // Abgabenverschiebung basierend auf Verlusten
                WENN SummeVerlust < -10:
                    MinimalwarAKTIV = False
                    ladeinfo2 = " PREISAUSGLEICH ->MinimalwarAKTIV=False "
                    STDAufschlagOffen++
                SONST:
                    MinimalwarAKTIV = True
                ENDE WENN
                
                // Standard-Aufschlag berechnen
                WENN p0[ID] <= STDAufschlagBis UND MinimalwarAKTIV:
                    DynPreiSTD = pb[ID] + STDAufschlagWert
                    STDAufschlagBENUTZT++
                SONST:
                    DynPreiSTD = pb[ID]
                ENDE WENN
                
                // Optimalen DynPlus-Preis ermitteln
                DynPlusPreis = GetDynPlusPreis(p0[ID], pb[ID], limitProz, AufschlagAktiv, Alg, MinimalwarAKTIV, keinAufschlag, STDAufschlag)
                
                // Kosten und Verluste berechnen
                SummePreisDyn += DynPlusPreis × VerbrauchNetz
                SummeVerlustDynPlusKunde += (pb[ID] - DynPlusPreis) × VerbrauchNetz
                
                preisdiff = pb[ID] - DynPlusPreis
                preisdiffSTD = pb[ID] - DynPreiSTD
                
                SummeVerlust += (preisdiff × Leistung) + (preisdiffSTD × Leistung × AnzahlNormalKundenproDynPlusKunde)
                
                // Logging
                WENN AufschlagAktiv:
                    sinfolog += "A! "
                ENDE WENN
                
                WENN DynPlusPreis < 0:
                    InputDynKunde += |DynPlusPreis × VerbrauchNetz|
                    ladeinfo2 += " -NegLeistSumt=" + (DynPlusPreis × VerbrauchNetz × -1) + " RealGewinn=" + InputDynKunde
                ENDE WENN
                
                // Statistiken für Netzverbrauch
                WENN VerbrauchNetz > 0:
                    WENN AufschlagAktiv:
                        ladeinfo2 += "##AufsAktiv!!!"
                        LADENbyAufschlagAktivDauer++
                        LADENbyAufschlagAktivWert += DynPlusPreis × VerbrauchNetz
                        sinfolog += "Netzverbrauch mit Preisaufschlag! "
                    SONST:
                        NOTLADENAufschlagAktivWert += DynPlusPreis × VerbrauchNetz
                        sinfolog += "Netzverbrauch! "
                    ENDE WENN
                    
                    WENN VerbrauchNetz > Leistung:
                        sinfolog += " STARKER VERBRAUCH +++++++++"
                    ENDE WENN
                ENDE WENN
            ENDE WENN
            
            // Statistiken für Speicherladung
            WENN ladeverbrauch > 0:
                ladeinfo += "++SPLADUNG++"
            ENDE WENN
            
            WENN Alg >= 200:
                ladeinfo += " SummeVerlust= " + SummeVerlust + " preisdiff=" + preisdiff
            ENDE WENN
            
            // Erstelle detaillierte Log-Information
            sinfo = datum[ID] + " #Sp=" + Speicher + " €Sum=" + SummePreisDyn + 
                    "€ PN=" + p0[ID] + "€ PB=" + pb[ID] + "€ VBnetz=" + VerbrauchNetz + " kWh " +
                    " SPL=" + ladeverbrauch + " kWh - LH=" + lhfound + "(P0=" + p0[lhfound] + ")" + 
                    ladeinfo + ladeinfo2
            
            WENN deblevel > 4:
                AUSGABE(sinfo)
            ENDE WENN
            
        ENDE WENN // Ende Alg >= 100
        
        // === Log-Datei schreiben ===
        WENN SaveLog:
            SimLogCounter++
            
            SetSimlog("SimRunID", 1020)
            SetSimlog("SzenarioID", szid)
            SetSimlog("DatumID", datum[ID])
            SetSimlog("ID", ID)
            SetSimlog("p0", p0[ID])
            SetSimlog("pb", pb[ID])
            SetSimlog("DynPlusPreis", DynPlusPreis)
            SetSimlog("pbDynPreisDiff", pb[ID] - DynPlusPreis)
            SetSimlog("AufschlagAktiv", AufschlagAktiv)
            SetSimlog("VerbrauchNetz", VerbrauchNetz)
            
            SummeVerbrauchNetz0 += VerbrauchNetz
            SetSimlog("SummeVerbrauchNetz", SummeVerbrauchNetz0)
            
            SummeDynPlusPreis0 += DynPlusPreis
            SetSimlog("SummeDynPlusPreis", SummeDynPlusPreis0)
            
            SetSimlog("Speicher", Speicher)
            SetSimlog("LADENbyAufschlagAktivWert", LADENbyAufschlagAktivWert)
            SetSimlog("NOTLADENAufschlagAktivWert", NOTLADENAufschlagAktivWert)
            SetSimlog("SummeVerlust", SummeVerlust)
        ENDE WENN
        
        // === Zwischenausgaben / UI-Update ===
        WENN counter == 0 ODER ID == anzahl:
            WENN ID == anzahl:
                Applog("Saving results ...")
            ENDE WENN
            
            MePartSET("DatumEnde", datum[ID - 1])
            MePartSET("r1", SummePreisKonstant)
            MePartSET("r2", SummePreisDyn)
            MePartSET("r3", SummePreisKonstant - SummePreisDyn)
            MePartSET("r5", SummeVerlustDynPlusKunde)
            MePartSET("r6", SummeVerlust)
            
            counter = Countervorgabe
        ENDE WENN
        
        counter--
        
    ENDE FÜR
ENDE FUNKTION