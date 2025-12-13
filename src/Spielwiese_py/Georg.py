import os
import math
import datetime

class DynPlus100:
    def __init__(self):
        # Tools & Helpers Storage
        self.meparts_names = []  # Die Namen der Felder
        self.meparts = []        # Die Input-Daten
        self.meparts0 = []       # Original Input-Daten für Vergleich
        
        # Logging Setup
        self.sim_log_counter = 0
        self.max_sim_log_counter = 75000
        self.save_log = False
        self.simlog_names = [""] * 100
        # Wir nutzen eine Liste von Listen für die Log-Daten in Python
        self.simlog_data = [] 

        # Global Vars
        self.deblevel = 0
        self.anzahl = 0
        self.flag = False
        
        # Simulation Arrays (Time Series)
        # In Python nutzen wir dynamische Listen statt fester Arrays
        self.ts_datum = []
        self.ts_p0 = [] # Netto
        self.ts_pb = [] # Brutto
        
        # GUI Simulation Values (Defaults, die sonst aus Textfeldern kämen)
        self.gui_szenario_id = "1"  # Default Szenario ID
        self.gui_debug_level = "3"
        self.gui_save_log = False

    def app_log(self, text, flag_crlf=True, options=""):
        """Ersatz für die GUI-Log-Ausgabe."""
        prefix = ""
        if "Error" in options:
            prefix = "[ERROR] "
        
        # In Python einfach print, crlf ist automatisch (end='\n')
        print(f"{prefix}{text}")

    def meparts_by_name(self, parname):
        """Hole Wert anhand des Namens aus der Liste."""
        try:
            index = self.meparts_names.index(parname)
            return self.meparts[index]
        except ValueError:
            self.app_log(f" ERROR MepartsbyName did not find the parameter {parname}", True, "Error")
            return ""

    def mepart_set(self, parname, value):
        """Setze Wert anhand des Namens."""
        try:
            index = self.meparts_names.index(parname)
            self.meparts[index] = str(value)
            return True
        except ValueError:
            self.app_log(f" ERROR MepartsSET did not find the parameter {parname}", True, "Error")
            return False

    def show_meparts_all(self):
        for i, name in enumerate(self.meparts_names):
            self.app_log(f"{name} : {self.meparts[i]}")

    def compare_meparts_all(self):
        for ii, name in enumerate(self.meparts_names):
            pname = name.strip()
            # Logic für Labels (vereinfacht für Python, da keine GUI-Labels existieren)
            
            # Compare
            val_old = self.meparts0[ii] if ii < len(self.meparts0) else ""
            val_new = self.meparts[ii]
            
            if val_old or val_new:
                info_string = ""
                opt_str = ""
                if val_old != val_new:
                    info_string = " Diff"
                    opt_str = "Error"
                
                s = f" par {pname} : pold: {val_old} : pnew {val_new} {info_string}"
                if opt_str: # Nur Änderungen oder Fehler loggen, um Spam zu vermeiden
                    self.app_log(s, True, opt_str)

    def export_to_csv_meparts(self, filename):
        try:
            with open(filename, 'w', encoding='utf-8') as f:
                # Zeile 1: Namen
                f.write(";".join(self.meparts_names) + "\n")
                # Zeile 2: Werte
                f.write(";".join(self.meparts) + "\n")
            self.app_log(f"Exported meparts to {filename}")
        except Exception as e:
            self.app_log(f"Error writing CSV: {e}", True, "Error")

    def export_to_csv_simlog(self, filename):
        try:
            with open(filename, 'w', encoding='utf-8') as f:
                # Header
                # Filtere leere Namen raus
                valid_names = [n for n in self.simlog_names if n]
                f.write(";".join(valid_names) + "\n")
                
                # Data
                for row in self.simlog_data:
                    # Row ist eine Map oder Liste. Wir müssen sicherstellen, dass die Reihenfolge stimmt.
                    # In set_simlog speichern wir in self.simlog_data[counter] -> list
                    line = ";".join(row)
                    f.write(line + ";\n")
                    
            self.app_log(f"Exported SimLog to {filename}")
        except Exception as e:
            self.app_log(f"Error writing SimLog CSV: {e}", True, "Error")

    def set_simlog(self, parname, value):
        # Stelle sicher, dass die aktuelle Zeile existiert
        while len(self.simlog_data) <= self.sim_log_counter:
            self.simlog_data.append([""] * len(self.simlog_names))

        try:
            index = self.simlog_names.index(parname)
            self.simlog_data[self.sim_log_counter][index] = str(value)
            return True
        except ValueError:
            self.app_log(f" ERROR SetSimlog did not find the parameter {parname}", True, "Error")
            return False

    def is_null(self, text):
        return text is None or str(text).strip() == ""

    def lookahead(self, interval, akt_id, dringend, alg, minimal_war_aktiv, p0_list):
        # SIM function for Dyyn prices - look into the future
        akt_preis = p0_list[akt_id]
        id_end = akt_id + interval
        if id_end >= self.anzahl:
            id_end = self.anzahl - 1 # Python index correction

        id_found = -1
        
        # Range in Python ist exklusiv am Ende, daher +1
        for temp_id in range(akt_id, id_end + 1):
            # Prüfen ob günstiger
            if akt_preis > p0_list[temp_id]:
                akt_preis = p0_list[temp_id]
                id_found = temp_id
                
                # Abbruchbedingungen
                if p0_list[temp_id] < 0.01 or (dringend and p0_list[temp_id] <= 0.05):
                    akt_preis = p0_list[temp_id]
                    id_found = temp_id
                    break
            
            if dringend and (temp_id - akt_id > 5) and id_found > 0:
                break
                
        return id_found

    def get_dyn_plus_preis(self, p0, pb, proz_limit, alg, minimal_war_aktiv, kein_aufschlag, std_aufschlag):
        # Returns: (NewPrice, AufschlagAktiv_Boolean)
        aufschlag_aktiv = False
        abg = pb - p0
        
        if p0 <= 0:
            abg = 0
        elif p0 == 0:
            abg = 0
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
                    if diff > 0:
                        aufschlag = diff

            if aufschlag > 0:
                aufschlag_aktiv = True
            
            abg += aufschlag

        pb = p0 + abg
        return pb, aufschlag_aktiv

    def run_simulation(self, szenario_id, deblevel, options=""):
        global_error = 0
        # Listen zurücksetzen
        self.ts_datum = []
        self.ts_p0 = []
        self.ts_pb = []

        # Load Szenario
        filename_szenario = os.path.join("data", f"sim_szenario_AccessVB_szid_{szenario_id}.csv")
        self.app_log(f"Szenario {szenario_id} loading from {filename_szenario}")
        
        if not os.path.exists(filename_szenario):
            self.app_log(f"FILE NOT FOUND: {filename_szenario}", True, "ERROR")
            return

        with open(filename_szenario, 'r', encoding='utf-8') as f:
            szenario_lines = f.readlines()
        
        self.app_log(f"Szenario {szenario_id} loaded with linecount = {len(szenario_lines)}")
        if len(szenario_lines) >= 2:
            self.meparts_names = szenario_lines[0].strip().split(';')
            self.meparts = szenario_lines[1].strip().split(';')
            self.meparts0 = list(self.meparts) # Copy
        
        self.app_log(f" Loaded meparts= {len(self.meparts)}")

        # Load SimLog Structure
        filename_simlog_struct = os.path.join("data", "sim_logdata_reference.csv")
        if os.path.exists(filename_simlog_struct):
            with open(filename_simlog_struct, 'r', encoding='utf-8') as f:
                line = f.readline()
                self.simlog_names = line.strip().split(';')
            self.app_log(f"Simlog Structure loaded, columns: {len(self.simlog_names)}")
        else:
             self.app_log("Warning: Simlog Reference file not found, creating generic log.")
             self.simlog_names = ["SimRunID", "SzenarioID", "DatumID", "ID", "p0", "pb", "DynPlusPreis", "pbDynPreisDiff", "AufschlagAktiv", "VerbrauchNetz", "SummeVerbrauchNetz", "SummeDynPlusPreis", "Speicher", "LADENbyAufschlagAktivWert", "NOTLADENAufschlagAktivWert", "SummeVerlust"]

        # Load Data (TimeSeries)
        if self.anzahl == 0:
            filename_price_data = os.path.join("data", "Preisdaten_Tab1.csv")
            self.app_log(f"Loading data from: {filename_price_data}")
            
            if not os.path.exists(filename_price_data):
                self.app_log("CRITICAL: Preisdaten_Tab1.csv not found!", True, "ERROR")
                return

            with open(filename_price_data, 'r', encoding='utf-8') as f:
                lines = f.readlines()
            
            linecount = 0
            startup = True
            
            for line in lines:
                line = line.strip()
                if not line: continue
                parts = line.split(';')
                
                if len(parts) < 3: continue
                
                if startup:
                    # Header Check
                    if len(parts) > 1 and parts[1] != "DatenTypID":
                        self.app_log("WRONG FORMAT in CSV Header", True, "ERROR")
                        global_error = -100
                        break
                    startup = False
                else:
                    # Data parsing
                    try:
                        # CSV Index: Datum(2), p0(9), pb(10) based on VB code
                        d_val = int(parts[2]) if parts[2] else 0
                        p0_val = float(parts[9].replace(',', '.')) if parts[9] else 0.0
                        pb_val = float(parts[10].replace(',', '.')) if parts[10] else 0.0
                        
                        self.ts_datum.append(d_val)
                        self.ts_p0.append(p0_val)
                        self.ts_pb.append(pb_val)
                        linecount += 1
                    except Exception as e:
                        # self.app_log(f"Parse Error line {linecount}: {e}")
                        pass
            
            self.anzahl = linecount
            self.app_log(f"CSV-Daten geladen: {linecount} Zeilen")

        # Init Simulation Vars
        summe_verbrauch_netz0 = 0.0
        summe_dyn_plus_preis0 = 0.0
        
        self.app_log("### Starte Simulation ####################")
        
        kein_aufschlag = False
        if "KeinAufschlag" in options: kein_aufschlag = True
        
        std_aufschlag = False
        if "AufschlagNurStandardKunde" in options:
            std_aufschlag = True
            kein_aufschlag = True
        
        val_sp8 = self.meparts_by_name("sp8")
        std_aufschlag_wert = float(val_sp8.replace(',', '.')) if val_sp8 else 0.0

        # Algorithmus Typ
        alg_str = self.meparts_by_name("Algorithmus")
        alg = int(alg_str) if alg_str else 10
        self.app_log(f" Alg={alg}")
        
        # Params
        counter_vorgabe = 1200
        counter = counter_vorgabe
        
        val_sp3 = self.meparts_by_name("sp3")
        leistung = float(val_sp3.replace(',', '.')) if val_sp3 else 0.0
        
        val_sp7 = self.meparts_by_name("sp7")
        std_aufschlag_bis = float(val_sp7.replace(',', '.')) if val_sp7 else 0.0
        
        val_sp1 = self.meparts_by_name("sp1")
        preis_konstant = float(val_sp1.replace(',', '.')) if val_sp1 else 0.0
        
        summe_preis_konstant = 0.0
        summe_preis_dyn = 0.0
        speicher = 0.0
        
        cap = 0.0
        anzahl_normal_kunden = 0
        limit_proz = 0.0
        
        if alg > 10:
            val_sp5 = self.meparts_by_name("sp5")
            cap = float(val_sp5.replace(',', '.')) if val_sp5 else 0.0
            if alg >= 200:
                val_sp6 = self.meparts_by_name("sp6")
                anzahl_normal_kunden = int(val_sp6) if val_sp6 else 0
                val_sp4 = self.meparts_by_name("sp4")
                limit_proz = float(val_sp4.replace(',', '.')) if val_sp4 else 0.0

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

        # Hauptschleife
        # In VB: For ID = 1 To anzahl (skipping index 0 usually, or 1-based logic)
        # Wir iterieren über alle geladenen Daten.
        
        for curr_id in range(self.anzahl):
            # Reset per Loop
            ladeverbrauch = 0.0
            ladeinfo = ""
            ladeinfo2 = ""
            sinfo = ""
            aufschlag_aktiv = False
            
            # Basiswerte
            current_p0 = self.ts_p0[curr_id]
            current_pb = self.ts_pb[curr_id]
            current_datum = self.ts_datum[curr_id]
            
            summe_preis_konstant += preis_konstant * leistung
            
            if alg == 10:
                summe_preis_dyn += current_pb * leistung
                verbrauch_netz = leistung
                dyn_plus_preis = current_pb # Fallback
            
            else: # Alg >= 100
                # Lookahead
                lh = self.lookahead(24, curr_id, speicher <= 0.25 * cap, alg, minimal_war_aktiv, self.ts_p0)
                if lh > 0:
                    if lh_found < lh or lh_found == 0:
                        lh_found = lh
                
                # Berechne Preis
                dyn_plus_preis, aufschlag_aktiv = self.get_dyn_plus_preis(
                    current_p0, current_pb, limit_proz, alg, minimal_war_aktiv, kein_aufschlag, std_aufschlag
                )
                
                # Speicher Logik
                verbrauch_netz = 0.0
                verbrauch = 0.0
                
                # Check ob wir laden
                should_load = (lh_found == curr_id and not aufschlag_aktiv)
                
                if should_load:
                    ladeverbrauch = cap - speicher
                    verbrauch = ladeverbrauch + leistung
                    speicher += ladeverbrauch
                    verbrauch_netz = verbrauch
                else:
                    # Entladen / Verbrauch aus Speicher
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
                    ladeinfo += "--SPLEER--"
                    speicher_leer_stunden += 1
                
                # DynPlus >= 200
                if alg >= 200:
                    if current_p0 < 0.03:
                        minimal_periode += 1
                    else:
                        if minimal_periode > 0:
                            minimal_war_aktiv = True
                            ladeinfo2 = " MinimalwarAKTIV"
                            minimal_periode = 0
                    
                    if summe_verlust < -10:
                        minimal_war_aktiv = False
                        ladeinfo2 = " PREISAUSGLEICH ->MinimalwarAKTIV=False"
                        std_aufschlag_offen += 1
                    else:
                        minimal_war_aktiv = True
                        
                    if current_p0 <= std_aufschlag_bis and minimal_war_aktiv:
                        dyn_preis_std = current_pb + std_aufschlag_wert
                        std_aufschlag_benutzt += 1
                    else:
                        dyn_preis_std = current_pb
                    
                    # Recalculate with potentially updated minimal_war_aktiv? 
                    # VB Code calls GetDynPlusPreis again inside the logic block, we use the value calculated above?
                    # No, VB calls it again explicitly at line 113. Let's update.
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

            # Logging Logic
            if self.save_log:
                self.sim_log_counter += 1
                self.set_simlog("SimRunID", 1020)
                self.set_simlog("SzenarioID", szenario_id)
                self.set_simlog("DatumID", current_datum)
                self.set_simlog("ID", curr_id)
                self.set_simlog("p0", str(current_p0).replace('.', ','))
                self.set_simlog("pb", str(current_pb).replace('.', ','))
                self.set_simlog("DynPlusPreis", str(dyn_plus_preis).replace('.', ','))
                self.set_simlog("AufschlagAktiv", "True" if aufschlag_aktiv else "False")
                self.set_simlog("VerbrauchNetz", str(verbrauch_netz).replace('.', ','))
                
                summe_verbrauch_netz0 += verbrauch_netz
                self.set_simlog("SummeVerbrauchNetz", str(summe_verbrauch_netz0).replace('.', ','))
                
                summe_dyn_plus_preis0 += dyn_plus_preis
                self.set_simlog("SummeDynPlusPreis", str(summe_dyn_plus_preis0).replace('.', ','))
                
                self.set_simlog("Speicher", str(speicher).replace('.', ','))
                self.set_simlog("SummeVerlust", str(summe_verlust).replace('.', ','))

            # Zwischenupdates & Progress
            if counter == 0 or curr_id == self.anzahl - 1:
                self.mepart_set("DatumEnde", self.ts_datum[curr_id])
                self.mepart_set("r1", summe_preis_konstant)
                self.mepart_set("r2", summe_preis_dyn)
                self.mepart_set("r3", summe_preis_konstant - summe_preis_dyn)
                self.mepart_set("r5", summe_verlust_dyn_plus_kunde)
                self.mepart_set("r6", summe_verlust)
                counter = counter_vorgabe
            
            counter -= 1
        
        # Post-Loop Calculations
        dauer_tage = self.anzahl / 24.0
        dauer_jahre = dauer_tage / 365.0
        
        self.mepart_set("DatumDauerStunden", self.anzahl)
        self.mepart_set("DatumDauerTage", dauer_tage)
        self.mepart_set("DatumDauerJahre", dauer_jahre)
        
        if dauer_jahre > 0:
            val_r3 = (summe_preis_konstant - summe_preis_dyn) / dauer_jahre
            self.mepart_set("r4", val_r3)
        
        # Stats Aggregation
        if alg >= 100:
            self.mepart_set("r5", summe_verlust_dyn_plus_kunde)
            self.mepart_set("r6", summe_verlust)
            self.mepart_set("r7", speicher_leer_stunden)
            
            std_aufschlag_gesamt = std_aufschlag_benutzt + std_aufschlag_offen
            if std_aufschlag_gesamt > 0:
                self.mepart_set("r9", std_aufschlag_benutzt / std_aufschlag_gesamt * 10)

        self.mepart_set("LastSimulationbyName", "Sim by Python Port")
        self.mepart_set("LastSimulationDateTime", datetime.datetime.now().strftime("%d.%m.%Y %H:%M"))
        
        self.compare_meparts_all() # Final Compare print
        
        # Exports
        fn_export_meparts = filename_szenario.replace(".csv", "_Export.csv")
        self.export_to_csv_meparts(fn_export_meparts)
        
        if self.save_log:
            self.app_log("Starte SaveLog ...")
            fn_export_simlog = filename_simlog_struct.replace(".csv", "_Export.csv")
            self.export_to_csv_simlog(fn_export_simlog)

        if global_error != 0:
            self.app_log(f"Global Error : {global_error}", True, "Red")

    def init_and_start_simulation(self, options_input=""):
        # Setup Inputs
        self.app_log("Init ..")
        
        # Hier hardcodiert, da keine GUI Eingabe
        # Wenn du es dynamisch willst, lies sys.argv aus
        sz = 7 
        deblevel = 3
        
        # Optionen zusammenbauen
        options = options_input
        if self.gui_save_log: 
            options += "Savelog"
            self.save_log = True

        self.app_log(f"Start Simulation of SzenarioID={sz} Debuglevel={deblevel}")
        self.run_simulation(sz, deblevel, options)


if __name__ == "__main__":
    # Create required directory structure if missing
    if not os.path.exists("data"):
        print("WARNUNG: Ordner 'data' fehlt. Bitte erstelle ihn und lege die CSV Dateien ab.")
        print("Erwartet: ./data/Preisdaten_Tab1.csv und ./data/sim_szenario_AccessVB_szid_7.csv")
        os.makedirs("data", exist_ok=True)
    
    sim = DynPlus100()
    
    # Beispielaufruf
    # Simuliert den Klick auf "Start Simulation"
    # Du kannst "Savelog" im String übergeben, um das Logging zu aktivieren
    sim.init_and_start_simulation("Savelog")