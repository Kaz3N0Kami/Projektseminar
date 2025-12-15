import os
import math
import datetime

class DynPlus100:
    def __init__(self):
        # Speicher für Felddaten (Namen und Werte)
        self.meparts_names = []
        self.meparts = []
        self.meparts0 = []
        
        # Logging Setup
        self.sim_log_counter = 0
        self.save_log = False
        self.simlog_names = [""] * 100
        self.simlog_data = [] 

        # Globale Variablen
        self.anzahl = 0
        
        # Zeitreihen Listen (Time Series)
        self.ts_datum = []
        self.ts_p0 = [] # Netto
        self.ts_pb = [] # Brutto
        
        # GUI Simulation Values Defaults
        self.gui_szenario_id = "7" 
        self.gui_debug_level = "3"
        self.gui_save_log = False

    def app_log(self, text, flag_crlf=True, options=""):
        """Ersatz für GUI-Log"""
        prefix = ""
        if "Error" in options:
            prefix = "[ERROR] "
        print(f"{prefix}{text}")

    def to_german_format(self, value):
        """Wandelt Punkt in Komma um für deutsche Excel/CSV Ausgabe"""
        return str(value).replace('.', ',')

    def to_float(self, s):
        """Hilfsfunktion für robustes Parsen von deutschen Zahlen"""
        if isinstance(s, (int, float)): return float(s)
        return float(str(s).replace(',', '.')) if s else 0.0

    def mepart_set(self, parname, value):
        """Setzt einen Wert im meparts Array und formatiert ihn deutsch"""
        try:
            index = self.meparts_names.index(parname)
            self.meparts[index] = self.to_german_format(value)
            return True
        except ValueError:
            return False
            
    def meparts_by_name(self, parname):
        try:
            index = self.meparts_names.index(parname)
            return self.meparts[index]
        except ValueError:
            return ""

    def compare_meparts_all(self):
        """Vergleicht Ergebnisse mit Originalwerten (Debugging)"""
        for ii, name in enumerate(self.meparts_names):
            pname = name.strip()
            val_old = self.meparts0[ii] if ii < len(self.meparts0) else ""
            val_new = self.meparts[ii]
            # Hier könnten Unterschiede geloggt werden
            pass

    def export_to_csv_meparts(self, filename):
        """Exportiert die Ergebnisse in CSV (cp1252 für Excel)"""
        try:
            with open(filename, 'w', encoding='cp1252', errors='replace') as f:
                f.write(";".join(self.meparts_names) + "\n")
                f.write(";".join(self.meparts) + "\n")
            self.app_log(f"Exported meparts to {filename}")
        except Exception as e:
            self.app_log(f"Error writing CSV: {e}", True, "Error")

    def export_to_csv_simlog(self, filename):
        """Exportiert das detaillierte SimLog"""
        try:
            with open(filename, 'w', encoding='cp1252', errors='replace') as f:
                valid_names = [n for n in self.simlog_names if n]
                f.write(";".join(valid_names) + "\n")
                for row in self.simlog_data:
                    f.write(";".join(row) + ";\n")
            self.app_log(f"Exported SimLog to {filename}")
        except Exception as e:
            self.app_log(f"Error writing SimLog CSV: {e}", True, "Error")

    def set_simlog(self, parname, value):
        while len(self.simlog_data) <= self.sim_log_counter:
            self.simlog_data.append([""] * len(self.simlog_names))
        try:
            if parname in self.simlog_names:
                index = self.simlog_names.index(parname)
                self.simlog_data[self.sim_log_counter][index] = self.to_german_format(value)
                return True
        except ValueError:
            pass
        return False

    def lookahead(self, interval, akt_id, dringend, alg, minimal_war_aktiv, p0_list):
        """
        Zukunftsschau für günstige Preise.
        KORREKTUR: Die If-Bedingungen müssen unabhängig sein, wie im VB-Code.
        """
        # VB Logik: Begrenzung auf 'anzahl' (der letzte Index ist der Dummy-Wert)
        id_end = akt_id + interval
        if id_end > self.anzahl: 
            id_end = self.anzahl
            
        akt_preis = p0_list[akt_id]
        id_found = -1
        
        # VB Loop: For ID = aktId To idEnd
        for temp_id in range(akt_id, id_end + 1):
            val = p0_list[temp_id]
            
            # Check 1: Neues Minimum gefunden?
            if akt_preis > val:
                akt_preis = val
                id_found = temp_id
            
            # Check 2: Greedy Exit ("Nimm diesen Preis sofort")
            # WICHTIG: Dies muss UNABHÄNGIG von Check 1 geprüft werden!
            if val < 0.01 or (dringend and val <= 0.05):
                akt_preis = val
                id_found = temp_id
                break
                
        return id_found

    def get_dyn_plus_preis(self, p0, pb, proz_limit, alg, minimal_war_aktiv, kein_aufschlag, std_aufschlag):
        aufschlag_aktiv = False
        abg = pb - p0
        if p0 <= 0: abg = 0
        else:
            proz = abg / p0
            if proz > (proz_limit / 100.0):
                proz = proz_limit / 100.0
                abg = p0 * proz
            
            aufschlag = 0.0
            if alg == 200 and not kein_aufschlag:
                if p0 >= 0.04: aufschlag = 0.06
                if p0 >= 0.05: aufschlag = 0.12
                if p0 >= 0.06: aufschlag = 0.12
                if p0 >= 0.07: aufschlag = 0.12
                if p0 >= 0.08: aufschlag = 0.12
                if p0 >= 0.09: aufschlag = 0.06
                if p0 >= 0.1:  aufschlag = 0.06
                if p0 >= 0.11: aufschlag = 0.04
                if p0 >= 0.12: aufschlag = 0.0

            if alg == 210 and not kein_aufschlag:
                if minimal_war_aktiv and p0 >= 0.05:
                    diff = 0.35 - abg - p0
                    if diff > 0: aufschlag = diff

            if aufschlag > 0: aufschlag_aktiv = True
            abg += aufschlag

        pb = p0 + abg
        return pb, aufschlag_aktiv

    def run_simulation(self, szenario_id, deblevel, options=""):
        global_error = 0
        self.ts_datum = []
        self.ts_p0 = []
        self.ts_pb = []

        # 1. Szenario Datei laden
        filename_szenario = os.path.join("data", f"sim_szenario_AccessVB_szid_{szenario_id}.csv")
        self.app_log(f"Szenario {szenario_id} loading from {filename_szenario}")
        
        # Fallback für Pfad
        if not os.path.exists(filename_szenario):
             filename_szenario = f"sim_szenario_AccessVB_szid_{szenario_id}.csv"
        
        if not os.path.exists(filename_szenario):
            self.app_log(f"FILE NOT FOUND: {filename_szenario}", True, "ERROR")
            return

        try:
            with open(filename_szenario, 'r', encoding='cp1252') as f:
                szenario_lines = f.readlines()
        except UnicodeDecodeError:
            with open(filename_szenario, 'r', encoding='latin-1') as f:
                szenario_lines = f.readlines()
        
        if len(szenario_lines) >= 2:
            self.meparts_names = szenario_lines[0].strip().split(';')
            self.meparts = szenario_lines[1].strip().split(';')
            self.meparts0 = list(self.meparts)

        # 2. SimLog Struktur laden
        filename_simlog_struct = os.path.join("data", "sim_logdata_reference.csv")
        if not os.path.exists(filename_simlog_struct): filename_simlog_struct = "sim_logdata_reference.csv"
        
        if os.path.exists(filename_simlog_struct):
            with open(filename_simlog_struct, 'r', encoding='cp1252') as f:
                line = f.readline()
                self.simlog_names = line.strip().split(';')
        else:
             # Fallback Header
             self.simlog_names = ["SimRunID", "SzenarioID", "DatumID", "ID", "p0", "pb", "DynPlusPreis", "pbDynPreisDiff", "AufschlagAktiv", "VerbrauchNetz", "SummeVerbrauchNetz", "SummeDynPlusPreis", "Speicher", "LADENbyAufschlagAktivWert", "NOTLADENAufschlagAktivWert", "SummeVerlust"]

        # 3. Preisdaten laden
        filename_price_data = os.path.join("data", "Preisdaten_Tab1.csv")
        if not os.path.exists(filename_price_data): filename_price_data = "Preisdaten_Tab1.csv"
        
        self.app_log(f"Loading data from: {filename_price_data}")
        if not os.path.exists(filename_price_data):
            self.app_log("CRITICAL: Preisdaten_Tab1.csv not found!", True, "ERROR")
            return

        with open(filename_price_data, 'r', encoding='cp1252') as f:
            lines = f.readlines()
        
        linecount = 0
        startup = True
        
        for line in lines:
            line = line.strip()
            if not line: continue
            parts = line.split(';')
            if len(parts) < 3: continue
            
            if startup:
                startup = False
            else:
                try:
                    # VB Code erwartet: Datum(2), p0(9), pb(10)
                    if len(parts) > 10:
                        d_val = int(parts[2]) if parts[2] else 0
                        p0_val = self.to_float(parts[9])
                        pb_val = self.to_float(parts[10])
                        
                        self.ts_datum.append(d_val)
                        self.ts_p0.append(p0_val)
                        self.ts_pb.append(pb_val)
                        linecount += 1
                except Exception:
                    pass
        
        self.anzahl = linecount
        
        # WICHTIG: Padding für VB-Kompatibilität
        # VB greift in der Schleife bis 'anzahl' zu. Da Arrays 0-basiert sind, 
        # ist Index 'anzahl' eigentlich out-of-bounds, aber in VB statisch reserviert (Wert 0.0).
        # Wir fügen dieses 0-Element hinzu.
        self.ts_datum.append(0)
        self.ts_p0.append(0.0)
        self.ts_pb.append(0.0)
        
        self.app_log(f"CSV-Daten geladen: {linecount} Zeilen (plus Padding)")

        # Init Simulation Variablen
        summe_verbrauch_netz0 = 0.0
        summe_dyn_plus_preis0 = 0.0
        
        self.app_log("### Starte Simulation ####################")
        
        # Optionen parsen
        kein_aufschlag = False
        if "KeinAufschlag" in options: kein_aufschlag = True
        
        std_aufschlag = False
        if "AufschlagNurStandardKunde" in options:
            std_aufschlag = True
            kein_aufschlag = True
        
        # Parameter aus Szenario
        val_sp8 = self.meparts_by_name("sp8")
        std_aufschlag_wert = self.to_float(val_sp8)

        alg_str = self.meparts_by_name("Algorithmus")
        alg = int(alg_str) if alg_str else 10
        self.app_log(f" Alg={alg}")
        
        counter_vorgabe = 1200
        counter = counter_vorgabe
        
        val_sp3 = self.meparts_by_name("sp3")
        leistung = self.to_float(val_sp3)
        
        val_sp7 = self.meparts_by_name("sp7")
        std_aufschlag_bis = self.to_float(val_sp7)
        
        val_sp1 = self.meparts_by_name("sp1")
        preis_konstant = self.to_float(val_sp1)
        
        summe_preis_konstant = 0.0
        summe_preis_dyn = 0.0
        speicher = 0.0
        cap = 0.0
        anzahl_normal_kunden = 0
        limit_proz = 0.0
        
        if alg > 10:
            val_sp5 = self.meparts_by_name("sp5")
            cap = self.to_float(val_sp5)
            if alg >= 200:
                val_sp6 = self.meparts_by_name("sp6")
                anzahl_normal_kunden = int(val_sp6) if val_sp6 else 0
                val_sp4 = self.meparts_by_name("sp4")
                limit_proz = self.to_float(val_sp4)

        # Statistik Vars
        laden_by_aufschlag_aktiv_wert = 0.0
        not_laden_aufschlag_aktiv_wert = 0.0
        laden_by_aufschlag_aktiv_dauer = 0
        summe_verlust = 0.0
        summe_verlust_dyn_plus_kunde = 0.0
        input_dyn_kunde = 0.0
        speicher_leer_stunden = 0
        minimal_periode = 0
        minimal_war_aktiv = False
        std_aufschlag_benutzt = 0
        std_aufschlag_offen = 0
        lh_found = 0

        # HAUPTSCHLEIFE
        # Wir starten bei Index 1 (um VB "For ID = 1 To anzahl" zu matchen)
        for curr_id in range(1, self.anzahl + 1):
            
            ladeverbrauch = 0.0
            aufschlag_aktiv = False
            
            current_p0 = self.ts_p0[curr_id]
            current_pb = self.ts_pb[curr_id]
            current_datum = self.ts_datum[curr_id]
            
            summe_preis_konstant += preis_konstant * leistung
            
            if alg == 10:
                summe_preis_dyn += current_pb * leistung
                verbrauch_netz = leistung
                dyn_plus_preis = current_pb
            else:
                # Lookahead aufrufen
                lh = self.lookahead(24, curr_id, speicher <= 0.25 * cap, alg, minimal_war_aktiv, self.ts_p0)
                if lh > 0:
                    if lh_found < lh or lh_found == 0:
                        lh_found = lh
                
                # Preis berechnen
                dyn_plus_preis, aufschlag_aktiv = self.get_dyn_plus_preis(
                    current_p0, current_pb, limit_proz, alg, minimal_war_aktiv, kein_aufschlag, std_aufschlag
                )
                
                verbrauch_netz = 0.0
                verbrauch = 0.0
                
                # Ladelogik
                should_load = (lh_found == curr_id and not aufschlag_aktiv)
                
                if should_load:
                    ladeverbrauch = cap - speicher
                    verbrauch = ladeverbrauch + leistung
                    speicher += ladeverbrauch
                    verbrauch_netz = verbrauch
                else:
                    if speicher >= leistung:
                        verbrauch = leistung
                        speicher -= verbrauch
                        verbrauch_netz = 0
                    else:
                        if speicher == 0:
                            verbrauch = leistung
                            verbrauch_netz = verbrauch
                        else:
                            verbrauch_netz = leistung - speicher
                            speicher = 0

                # Alg Spezifika
                if alg == 100:
                    summe_preis_dyn += current_pb * verbrauch_netz
                    if current_pb < 0:
                        input_dyn_kunde += current_pb * verbrauch_netz * -1
                
                elif alg == 110:
                    summe_preis_dyn += current_p0 * verbrauch_netz
                    summe_verlust += (current_pb - current_p0) * verbrauch_netz
                    if current_p0 < 0:
                        input_dyn_kunde += current_p0 * verbrauch_netz * -1
                        
                if speicher < 1:
                    speicher_leer_stunden += 1
                
                if alg >= 200:
                    if current_p0 < 0.03:
                        minimal_periode += 1
                    else:
                        if minimal_periode > 0:
                            minimal_war_aktiv = True
                            minimal_periode = 0
                    
                    if summe_verlust < -10:
                        minimal_war_aktiv = False
                        std_aufschlag_offen += 1
                    else:
                        minimal_war_aktiv = True
                        
                    if current_p0 <= std_aufschlag_bis and minimal_war_aktiv:
                        dyn_preis_std = current_pb + std_aufschlag_wert
                        std_aufschlag_benutzt += 1
                    else:
                        dyn_preis_std = current_pb
                    
                    # Neuberechnung für Statistik (analog VB)
                    dyn_plus_preis, aufschlag_aktiv = self.get_dyn_plus_preis(
                        current_p0, current_pb, limit_proz, alg, minimal_war_aktiv, kein_aufschlag, std_aufschlag
                    )
                    
                    summe_preis_dyn += dyn_plus_preis * verbrauch_netz
                    summe_verlust_dyn_plus_kunde += (current_pb - dyn_plus_preis) * verbrauch_netz
                    
                    preis_diff = current_pb - dyn_plus_preis
                    preis_diff_std = current_pb - dyn_preis_std
                    
                    summe_verlust += (preis_diff * leistung * 1) + (preis_diff_std * leistung * anzahl_normal_kunden)
                    
                    if dyn_plus_preis < 0:
                         input_dyn_kunde += dyn_plus_preis * verbrauch_netz * -1
                    
                    if verbrauch_netz > 0:
                        if aufschlag_aktiv:
                            laden_by_aufschlag_aktiv_dauer += 1
                            laden_by_aufschlag_aktiv_wert += dyn_plus_preis * verbrauch_netz
                        else:
                            not_laden_aufschlag_aktiv_wert += dyn_plus_preis * verbrauch_netz

            # Logging
            if self.save_log:
                self.sim_log_counter += 1
                self.set_simlog("SimRunID", 1020)
                self.set_simlog("SzenarioID", szenario_id)
                self.set_simlog("DatumID", current_datum)
                self.set_simlog("ID", curr_id)
                self.set_simlog("p0", current_p0)
                self.set_simlog("pb", current_pb)
                self.set_simlog("DynPlusPreis", dyn_plus_preis)
                self.set_simlog("AufschlagAktiv", "True" if aufschlag_aktiv else "False")
                self.set_simlog("VerbrauchNetz", verbrauch_netz)
                
                summe_verbrauch_netz0 += verbrauch_netz
                self.set_simlog("SummeVerbrauchNetz", summe_verbrauch_netz0)
                summe_dyn_plus_preis0 += dyn_plus_preis
                self.set_simlog("SummeDynPlusPreis", summe_dyn_plus_preis0)
                self.set_simlog("Speicher", speicher)
                self.set_simlog("SummeVerlust", summe_verlust)

            # Zwischenupdates
            if counter == 0 or curr_id == self.anzahl:
                self.mepart_set("DatumEnde", self.ts_datum[curr_id-1] if curr_id > 0 else 0)
                self.mepart_set("r1", summe_preis_konstant)
                self.mepart_set("r2", summe_preis_dyn)
                self.mepart_set("r3", summe_preis_konstant - summe_preis_dyn)
                self.mepart_set("r5", summe_verlust_dyn_plus_kunde)
                self.mepart_set("r6", summe_verlust)
                counter = counter_vorgabe
            counter -= 1
        
        # Endergebnisse und Datum
        dauer_tage = round(self.anzahl / 24.0) 
        dauer_jahre = dauer_tage / 365.0
        
        self.mepart_set("DatumDauerStunden", self.anzahl)
        self.mepart_set("DatumDauerTage", dauer_tage)
        self.mepart_set("DatumDauerJahre", str(dauer_jahre).replace('.', ','))
        
        if dauer_jahre > 0:
            val_r3 = (summe_preis_konstant - summe_preis_dyn) / dauer_jahre
            self.mepart_set("r4", val_r3)
        
        if alg >= 100:
            self.mepart_set("r5", summe_verlust_dyn_plus_kunde)
            self.mepart_set("r6", summe_verlust)
            self.mepart_set("r7", speicher_leer_stunden)
            std_aufschlag_gesamt = std_aufschlag_benutzt + std_aufschlag_offen
            if std_aufschlag_gesamt > 0:
                self.mepart_set("r9", std_aufschlag_benutzt / std_aufschlag_gesamt * 10)

        self.mepart_set("LastSimulationbyName", "Sim by Python Port (Final Fix)")
        self.mepart_set("LastSimulationDateTime", datetime.datetime.now().strftime("%d.%m.%Y %H:%M"))
        
        self.compare_meparts_all()
        
        fn_export_meparts = filename_szenario.replace(".csv", "_Export.csv")
        self.export_to_csv_meparts(fn_export_meparts)
        if self.save_log:
            self.app_log("Starte SaveLog ...")
            fn_export_simlog = filename_simlog_struct.replace(".csv", "_Export.csv")
            self.export_to_csv_simlog(fn_export_simlog)
        
        if global_error != 0:
            self.app_log(f"Global Error : {global_error}", True, "Red")

    def init_and_start_simulation(self, options_input=""):
        self.app_log("Init ..")
        sz = 7 
        deblevel = 3
        options = options_input
        if self.gui_save_log: 
            options += "Savelog"
            self.save_log = True

        self.app_log(f"Start Simulation of SzenarioID={sz} Debuglevel={deblevel}")
        self.run_simulation(sz, deblevel, options)

if __name__ == "__main__":
    if not os.path.exists("data"):
        # Notfall-Erstellung, falls User data nicht hat
        os.makedirs("data", exist_ok=True)
    
    sim = DynPlus100()
    # Starte Simulation (mit Logging Option wenn gewünscht, hier leer)
    sim.init_and_start_simulation("")