# Annahme: Listen basieren auf 0-Index:
# datum[0] = ID 1 → Python-Index = VB-ID - 1

for ID in range(anzahl):
    # Initialisierung
    ladeverbrauch = 0.0
    ladeinfo = ""
    ladeinfo2 = ""
    SpeicherStatus = ""
    LH = 0
    Verbrauch = 0.0

    # Preis konstant
    SummePreisKonstant += PreisKonstant * Leistung

    # Einfacher Algorithmus ohne Speicher
    if Alg == 10:
        SummePreisDyn += pb[ID] * Leistung
        continue  # Speicherlogik überspringen

    # *** Dynamische Tarife mit Speicher ***
    if Alg >= 100:

        # Lookahead auf günstigere Preise
        LH = Lookahead(24, ID,
                       Speicher <= 0.25 * Cap,
                       Alg, MinimalwarAKTIV, p0)

        if LH > 0:
            if lhfound < LH or lhfound == 0:
                lhfound = LH

        # DynPlus-Preis bestimmen
        DynPlusPreis = GetDynPlusPreis(
            p0[ID], pb[ID], limitProz, AufschlagAktiv,
            Alg, MinimalwarAKTIV, keinAufschlag, STDAufschlag
        )

        # Laden, wenn Lookahead-Zeitpunkt erreicht & kein Aufschlag
        if lhfound == ID and not AufschlagAktiv:
            ladeverbrauch = Cap - Speicher
            Verbrauch = ladeverbrauch + Leistung
            Speicher += ladeverbrauch
            VerbrauchNetz = Verbrauch
        else:
            # Hausverbrauchslogik
            if Speicher >= Leistung:
                Verbrauch = Leistung
                Speicher -= Verbrauch
                VerbrauchNetz = 0
            else:
                VerbrauchNetz = max(0, Leistung - Speicher)
                Speicher = 0

        # Abrechnung Modi
        if Alg == 100:
            SummePreisDyn += pb[ID] * VerbrauchNetz
            if pb[ID] < 0:
                InputDynKunde += (-pb[ID]) * VerbrauchNetz

        if Alg == 110:
            SummePreisDyn += p0[ID] * VerbrauchNetz
            SummeVerlust += (pb[ID] - p0[ID]) * VerbrauchNetz
            if p0[ID] < 0:
                InputDynKunde += (-p0[ID]) * VerbrauchNetz

        # Speicher leer?
        if Speicher < 1:
            ladeinfo += "--SPLEER--"
            SpeicherLeerStunden += 1

        # *** DynPlus Logik ***
        if Alg >= 200:

            # Minimalperiode Verwaltung
            if p0[ID] < 0.03:
                Minimalperiode += 1
            else:
                if Minimalperiode > 0:
                    MinimalwarAKTIV = True
                    ladeinfo2 += " MinimalwarAKTIV "
                    Minimalperiode = 0

            # Verlustschwelle → Abgabenverschiebung aus
            if SummeVerlust < -10:
                MinimalwarAKTIV = False
                ladeinfo2 += " PREISAUSGLEICH! "
                STDAufschlagOffen += 1
            else:
                MinimalwarAKTIV = True

            # Standardpreis mit Aufschlag
            if p0[ID] <= STDAufschlagBis and MinimalwarAKTIV:
                DynPreiSTD = pb[ID] + STDAufschlagWert
                STDAufschlagBENUTZT += 1
            else:
                DynPreiSTD = pb[ID]

            # DynPlus final anwenden
            DynPlusPreis = GetDynPlusPreis(
                p0[ID], pb[ID], limitProz, AufschlagAktiv,
                Alg, MinimalwarAKTIV, keinAufschlag, STDAufschlag
            )

            SummePreisDyn += DynPlusPreis * VerbrauchNetz
            preisdiff = pb[ID] - DynPlusPreis
            preisdiffSTD = pb[ID] - DynPreiSTD

            SummeVerlustDynPlusKunde += preisdiff * VerbrauchNetz
            SummeVerlust += preisdiff * Leistung + \
                             preisdiffSTD * Leistung * AnzahlNormalKundenproDynPlusKunde

        # Lade-Indikator
        if ladeverbrauch > 0:
            ladeinfo += "++SPLADUNG++"

        # Logging (Konsole statt SetSimlog)
        print(f"{datum[ID]} | Speicher={Speicher:.1f} | Netz={VerbrauchNetz:.2f} kWh "
              f"| PB={pb[ID]:.2f}€ | P0={p0[ID]:.2f}€ "
              f"| SummeDyn={SummePreisDyn:.2f}€ "
              f"{ladeinfo} {ladeinfo2}")

    # GUI Updates → in Python überspringen
    counter -= 1
