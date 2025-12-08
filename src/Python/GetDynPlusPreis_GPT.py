def get_dyn_plus_preis(p0: float, pb: float, proz_limit: float,
                        alg: int, minimalwar_aktiv: bool,
                        kein_aufschlag: bool, std_aufschlag: bool):
    """
    Berechnung eines dynamischen DynPlus-Preises auf Basis von p0 und pb

    Parameter
    ---------
    p0 : Nettopreis
    pb : Bruttopreis
    proz_limit : Limit der Preisabweichung in Prozent
    alg : Algorithmusmodus (200, 210 = DynPlus)
    minimalwar_aktiv : True, wenn Minimalpreisphase aktiv
    kein_aufschlag : True → Aufschlag wird komplett unterdrückt
    std_aufschlag : aktuell nicht genutzt

    Rückgabe
    --------
    result : berechneter dynamischer Bruttopreis
    aufschlag_aktiv : Flag, ob ein Aufschlag gesetzt wurde
    """

    aufschlag_aktiv = False
    abg = pb - p0  # Grundabgabe (Brutto - Netto)

    # Schutz gegen negative oder 0-Preise
    if p0 <= 0:
        abg = 0
    else:
        # Prozentuale Abgabe berechnen
        proz = abg / p0

        # Prozentlimit beachten
        if proz > proz_limit / 100.0:
            proz = proz_limit / 100.0
            abg = p0 * proz

        # Aufschlag vorerst 0
        aufschlag = 0

        # Algorithmus 200: breite Staffelung über Preisstufen
        if alg == 200 and not kein_aufschlag:
            if p0 >= 0.04: aufschlag = 0.06
            if p0 >= 0.05: aufschlag = 0.12
            if p0 >= 0.06: aufschlag = 0.12
            if p0 >= 0.07: aufschlag = 0.12
            if p0 >= 0.08: aufschlag = 0.12
            if p0 >= 0.09: aufschlag = 0.06
            if p0 >= 0.10: aufschlag = 0.06
            if p0 >= 0.11: aufschlag = 0.04
            if p0 >= 0.12: aufschlag = 0.00

        # Algorithmus 210: enger, an Minimalpreisperiode gekoppelt
        if alg == 210 and not kein_aufschlag:
            if minimalwar_aktiv and p0 >= 0.05:
                diff = 0.35 - abg - p0  # Preis Richtung Zielpreis verschieben
                if diff > 0:
                    aufschlag = diff

        # Flag setzen, wenn es einen Aufschlag gibt
        if aufschlag > 0:
            aufschlag_aktiv = True

        # Endgültige Abgabe ist Basis + Aufschlag
        abg += aufschlag

    # Ergebnispreis
    result = p0 + abg
    return result, aufschlag_aktiv
