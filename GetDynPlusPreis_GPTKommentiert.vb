Public Function GetDynPlusPreis(ByVal p0 As Double, ByVal pb As Double, ByVal ProzLimit As Double, ByRef AufschlagAktiv As Boolean, Alg%, MinimalwarAKTIV As Boolean, keinAufschlag As Boolean, STDAufschlag As Boolean) As Decimal
    ' Berechnung eines dynamischen DynPlus-Preises auf Basis von p0 und pb
    ' p0 = Nettopreis
    ' pb = Bruttopreis
    ' ProzLimit = Limit der maximalen Preisabweichung in Prozent
    ' AufschlagAktiv = Flag, ob ein Aufschlag gesetzt wurde
    ' Alg = Algorithmusmodus (200, 210 = DynPlus)
    ' MinimalwarAKTIV = True wenn Minimalpreisphase aktiv
    ' keinAufschlag = True → Aufschlag wird komplett unterdrückt
    ' STDAufschlag wird hier nicht verwendet, bleibt aber Parameter

    Dim abg As Decimal, proz As Single, aufschlag As Decimal

    AufschlagAktiv = False                ' Standard: kein Preisaufschlag
    abg = pb - p0                         ' Grundabgabe: Differenz Brutto - Netto

    If p0 <= 0 Then abg = 0               ' Schutz: keine Abgabe wenn Preis negativ oder 0

    If p0 = 0 Then                        ' Doppelabsicherung für Division durch Null
        abg = 0
    Else
        proz = abg / p0                   ' Abgabe in Prozent vom Nettopreis

        ' Prozentlimitierung: wenn Abgabe % über Grenze → Abgabe begrenzen
        If proz > ProzLimit / 100 Then
            proz = ProzLimit / 100        ' Prozent auf Limit setzen
            abg = p0 * proz               ' Abgabe neu berechnen
        End If

        ' Aufschlaglogik wird vorbereitet
        REM von 0,04 bis 0,0 - ab 0,03 e Aufschlag
        aufschlag = 0                     ' Initial keine zusätzlichen Aufschläge

        ' Dynamischer Aufschlag für Algorithmus 200
        If Alg = 200 And Not keinAufschlag Then
            REM Parameter für alte Version - breite Staffelung über Preisstufen
            If p0 >= 0.04 Then aufschlag = 0.06
            If p0 >= 0.05 Then aufschlag = 0.12
            If p0 >= 0.06 Then aufschlag = 0.12
            If p0 >= 0.07 Then aufschlag = 0.12
            If p0 >= 0.08 Then aufschlag = 0.12
            If p0 >= 0.09 Then aufschlag = 0.06
            If p0 >= 0.1 Then aufschlag = 0.06
            If p0 >= 0.11 Then aufschlag = 0.04
            If p0 >= 0.12 Then aufschlag = 0#
        End If

        ' Alternative Aufschlaglogik für Algorithmus 210
        If Alg = 210 And Not keinAufschlag Then
            REM Aufschlag nur direkt nach Minimalpreisperiode
            If MinimalwarAKTIV And p0 >= 0.05 Then
                Dim diff As Decimal
                diff = 0.35 - abg - p0    ' Ziel: Gesamtpreis Richtung 0,35 €/kWh verschieben
                If diff > 0 Then aufschlag = diff
            End If
        End If

        ' Wenn ein Aufschlag gesetzt wird → Flag aktivieren
        If aufschlag > 0 Then AufschlagAktiv = True

        abg += aufschlag                  ' Endgültige Abgabe inkl. Aufschlag
    End If

    pb = p0 + abg                         ' Endpreis: Netto + Abgabe (ggf. mit Aufschlag)

    GetDynPlusPreis = pb                  ' Ergebnis zurückgeben
End Function
