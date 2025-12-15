import os
import math
import datetime
from decimal import Decimal, getcontext

# Setze Präzision hoch genug (Standard ist 28, das reicht locker für Finanzmathe)
getcontext().prec = 50

class DynPlus100:
    def __init__(self):
        # Tools & Helpers Storage
        self.meparts_names = []
        self.meparts = []
        self.meparts0 = []
        
        # Logging Setup
        self.sim_log_counter = 0
        self.save_log = False
        self.simlog_names = [""] * 100
        self.simlog_data = [] 

        # Global Vars
        self.anzahl = 0
        
        # Simulation Arrays (Time Series) - Jetzt als Decimal
        self.ts_datum = []
        self.ts_p0 = [] # Netto
        self.ts_pb = [] # Brutto
        
        # GUI Simulation Values Defaults
        self.gui_szenario_id = "7" 
        self.gui_debug_level = "3"
        self.gui_save_log = False

    def app_log(self, text, flag_crlf=True, options=""):
        prefix = ""
        if "Error" in options:
            prefix = "[ERROR] "
        print(f"{prefix}{text}")

    def to_german_format(self, value):
        """Wandelt Decimal/Float für die CSV-Ausgabe in deutsches Format um."""
        if isinstance(value, Decimal):
            # Quantize für saubere Ausgabe (optional, hier nehmen wir str)
            return str(value).replace('.', ',')
        return str(value).replace('.', ',')

    def to_decimal(self, s):
        """Robuste Konvertierung zu Decimal, behandelt Komma als Trenner."""
        if isinstance(s, Decimal): return s
        if isinstance(s, (int, float)): return Decimal(str(s))
        if not s: return Decimal('0')
        # Ersetze Komma durch Punkt für Python-Parsing
        clean_s = str(s).replace(',', '.')
        try:
            return Decimal(clean_s)
        except:
            return Decimal('0')

    def mepart_set(self, parname, value):
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
        for ii, name in enumerate(self.meparts_names):
            pname = name.strip()
            val_old = self.meparts0[ii] if ii < len(self.meparts0) else ""
            val_new = self.meparts[ii]
            # Optional: Unterschiede loggen
            pass

    def export_to_csv_meparts(self, filename):
        try:
            with open(filename, 'w', encoding='cp1252', errors='replace') as f:
                f.write(";".join(self.meparts_names) + "\n")
                f.write(";".join(self.meparts) + "\n")
            self.app_log(f"Exported meparts to {filename}")
        except Exception as e:
            self.app_log(f"Error writing CSV: {e}", True, "Error")

    def export_to_csv_simlog(self, filename):
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
        id_end = akt_id + interval
        if id_end > self.anzahl: 
            id_end = self.anzahl
            
        akt_preis = p0_list[akt_id]
        id_found = -1
        
        # Konstanten als Decimal definieren für Vergleich
        ZERO_POINT_ZERO_ONE = Decimal('0.01')
        ZERO_POINT_ZERO_FIVE = Decimal('0.05')

        for temp_id in range(akt_id, id_end + 1):
            val = p0_list[temp_id]
            
            # Check 1
            if akt_preis > val:
                akt_preis = val
                id_found = temp_id
            
            # Check 2 (Unabhängig!)
            if val < ZERO_POINT_ZERO_ONE or (dringend and val <= ZERO_POINT_ZERO_FIVE):
                akt_preis = val
                id_found = temp_id
                break
                
        return id_found

    def get_dyn_plus_preis(self, p0, pb, proz_limit, alg, minimal_war_aktiv, kein_aufschlag, std_aufschlag):
        aufschlag_aktiv = False
        abg = pb - p0
        
        # Konstanten
        ZERO = Decimal('0')
        HUNDRED = Decimal('100.0') # Wichtig: Als Decimal float string
        
        if p0 <= ZERO: 
            abg = ZERO
        else:
            proz = abg / p0
            limit_factor = proz_limit / HUNDRED
            
            if proz > limit_factor:
                proz = limit_factor
                abg = p0 * proz
            
            aufschlag = ZERO
            
            # Algorithmus Logik
            if alg == 200 and not kein_aufschlag:
                if p0 >= Decimal('0.04'): aufschlag = Decimal('0.06')
                if p0 >= Decimal('0.05'): aufschlag = Decimal('0.12')
                if p0 >= Decimal('0.06'): aufschlag = Decimal('0.12')
                if p0 >= Decimal('0.07'): aufschlag = Decimal('0.12')
                if p0 >= Decimal('0.08'): aufschlag = Decimal('0.12')
                if p0 >= Decimal('0.09'): aufschlag = Decimal('0.06')
                if p0 >= Decimal('0.1'):  aufschlag = Decimal('0.06')
                if p0 >= Decimal('0.11'): aufschlag = Decimal('0.04')
                if p0 >= Decimal('0.12'): aufschlag = ZERO

            if alg == 210 and not kein_aufschlag:
                if minimal_war_aktiv and p0 >= Decimal('0.05'):
                    diff = Decimal('0.35') - abg - p0
                    if diff > 0: aufschlag = diff

            if aufschlag > 0: 
                aufschlag_aktiv = True
            
            abg += aufschlag

        pb = p0 + abg
        return pb, aufschlag_aktiv

    def run_simulation(self, szenario_id, deblevel, options=""):
        global_error = 0
        self.ts_datum = []
        self.ts_p0 = []
        self.ts_pb = []

        # 1. Szenario Laden
        filename_szenario = os.path.join("data", f"sim_szenario_AccessVB_szid_{szenario_id}.csv")
        self.app_log(f"Szenario {szenario_id} loading from {filename_szenario}")
        
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

        # 2. SimLog Laden
        filename_simlog_struct = os.path.join("data", "sim_logdata_reference.csv")
        if not os.path.exists(filename_simlog_struct): filename_simlog_struct = "sim_logdata_reference.csv"
        
        if os.path.exists(filename_simlog_struct):
            with open(filename_simlog_struct, 'r', encoding='cp1252') as f:
                line = f.readline()
                self.simlog_names = line.strip().split(';')
        else:
             self.simlog_names = ["SimRunID", "SzenarioID", "DatumID", "ID", "p0", "pb", "DynPlusPreis", "pbDynPreisDiff", "AufschlagAktiv", "VerbrauchNetz", "SummeVerbrauchNetz", "SummeDynPlusPreis", "Speicher", "LADENbyAufschlagAktivWert", "NOTLADENAufschlagAktivWert", "SummeVerlust"]

        # 3. Preisdaten Laden
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
                    if len(parts) > 10:
                        d_val = int(parts[2]) if parts[2] else 0
                        # WICHTIG: Hier als Decimal parsen
                        p0_val = self.to_decimal(parts[9])
                        pb_val = self.to_decimal(parts[10])
                        
                        self.ts_datum.append(d_val)
                        self.ts_p0.append(p0_val)
                        self.ts_pb.append(pb_val)
                        linecount += 1
                except Exception:
                    pass
        
        self.anzahl = linecount
        
        # Padding für VB-Kompatibilität (auch als Decimal)
        self.ts_datum.append(0)
        self.ts_p0.append(Decimal('0'))
        self.ts_pb.append(Decimal('0'))
        
        self.app_log(f"CSV-Daten geladen: {linecount} Zeilen (plus Padding)")

        # Init Simulation Vars (Decimal)
        summe_verbrauch_netz0 = Decimal('0')
        summe_dyn_plus_preis0 = Decimal('0')
        
        self.app_log("### Starte Simulation ####################")
        
        kein_aufschlag = False
        if "KeinAufschlag" in options: kein_aufschlag = True
        
        std_aufschlag = False
        if "AufschlagNurStandardKunde" in options:
            std_aufschlag = True
            kein_aufschlag = True
        
        val_sp8 = self.meparts_by_name("sp8")
        std_aufschlag_wert = self.to_decimal(val_sp8)

        alg_str = self.meparts_by_name("Algorithmus")
        alg = int(alg_str) if alg_str else 10
        self.app_log(f" Alg={alg}")
        
        counter_vorgabe = 1200
        counter = counter_vorgabe
        
        val_sp3 = self.meparts_by_name("sp3")
        leistung = self.to_decimal(val_sp3)
        
        val_sp7 = self.meparts_by_name("sp7")
        std_aufschlag_bis = self.to_decimal(val_sp7)
        
        val_sp1 = self.meparts_by_name("sp1")
        preis_konstant = self.to_decimal(val_sp1)
        
        summe_preis_konstant = Decimal('0')
        summe_preis_dyn = Decimal('0')
        speicher = Decimal('0')
        cap = Decimal('0')
        anzahl_normal_kunden = 0
        limit_proz = Decimal('0')
        
        if alg > 10:
            val_sp5 = self.meparts_by_name("sp5")
            cap = self.to_decimal(val_sp5)
            if alg >= 200:
                val_sp6 = self.meparts_by_name("sp6")
                anzahl_normal_kunden = int(val_sp6) if val_sp6 else 0
                val_sp4 = self.meparts_by_name("sp4")
                limit_proz = self.to_decimal(val_sp4)

        # Statistik Vars (Decimal)
        laden_by_aufschlag_aktiv_wert = Decimal('0')
        not_laden_aufschlag_aktiv_wert = Decimal('0')
        laden_by_aufschlag_aktiv_dauer = 0
        summe_verlust = Decimal('0')
        summe_verlust_dyn_plus_kunde = Decimal('0')
        input_dyn_kunde = Decimal('0')
        speicher_leer_stunden = 0
        minimal_periode = 0
        minimal_war_aktiv = False
        std_aufschlag_benutzt = 0
        std_aufschlag_offen = 0
        lh_found = 0

        # Konstanten für Schleife
        ONE = Decimal('1')
        ZERO = Decimal('0')
        MINUS_ONE = Decimal('-1')
        ZERO_POINT_ZERO_THREE = Decimal('0.03')
        MINUS_TEN = Decimal('-10')

        # HAUPTSCHLEIFE
        for curr_id in range(1, self.anzahl + 1):
            
            ladeverbrauch = ZERO
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
                # Lookahead
                lh = self.lookahead(24, curr_id, speicher <= (cap * Decimal('0.25')), alg, minimal_war_aktiv, self.ts_p0)
                if lh > 0:
                    if lh_found < lh or lh_found == 0:
                        lh_found = lh
                
                # Preis berechnen
                dyn_plus_preis, aufschlag_aktiv = self.get_dyn_plus_preis(
                    current_p0, current_pb, limit_proz, alg, minimal_war_aktiv, kein_aufschlag, std_aufschlag
                )
                
                verbrauch_netz = ZERO
                verbrauch = ZERO
                
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
                        verbrauch_netz = ZERO
                    else:
                        if speicher == ZERO:
                            verbrauch = leistung
                            verbrauch_netz = verbrauch
                        else:
                            verbrauch_netz = leistung - speicher
                            speicher = ZERO

                # Alg Spezifika
                if alg == 100:
                    summe_preis_dyn += current_pb * verbrauch_netz
                    if current_pb < 0:
                        input_dyn_kunde += current_pb * verbrauch_netz * MINUS_ONE
                
                elif alg == 110:
                    summe_preis_dyn += current_p0 * verbrauch_netz
                    summe_verlust += (current_pb - current_p0) * verbrauch_netz
                    if current_p0 < 0:
                        input_dyn_kunde += current_p0 * verbrauch_netz * MINUS_ONE
                        
                if speicher < 1:
                    speicher_leer_stunden += 1
                
                if alg >= 200:
                    if current_p0 < ZERO_POINT_ZERO_THREE:
                        minimal_periode += 1
                    else:
                        if minimal_periode > 0:
                            minimal_war_aktiv = True
                            minimal_periode = 0
                    
                    if summe_verlust < MINUS_TEN:
                        minimal_war_aktiv = False
                        std_aufschlag_offen += 1
                    else:
                        minimal_war_aktiv = True
                        
                    if current_p0 <= std_aufschlag_bis and minimal_war_aktiv:
                        dyn_preis_std = current_pb + std_aufschlag_wert
                        std_aufschlag_benutzt += 1
                    else:
                        dyn_preis_std = current_pb
                    
                    dyn_plus_preis, aufschlag_aktiv = self.get_dyn_plus_preis(
                        current_p0, current_pb, limit_proz, alg, minimal_war_aktiv, kein_aufschlag, std_aufschlag
                    )
                    
                    summe_preis_dyn += dyn_plus_preis * verbrauch_netz
                    summe_verlust_dyn_plus_kunde += (current_pb - dyn_plus_preis) * verbrauch_netz
                    
                    preis_diff = current_pb - dyn_plus_preis
                    preis_diff_std = current_pb - dyn_preis_std
                    
                    summe_verlust += (preis_diff * leistung * ONE) + (preis_diff_std * leistung * Decimal(anzahl_normal_kunden))
                    
                    if dyn_plus_preis < 0:
                         input_dyn_kunde += dyn_plus_preis * verbrauch_netz * MINUS_ONE
                    
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

            if counter == 0 or curr_id == self.anzahl:
                self.mepart_set("DatumEnde", self.ts_datum[curr_id-1] if curr_id > 0 else 0)
                self.mepart_set("r1", summe_preis_konstant)
                self.mepart_set("r2", summe_preis_dyn)
                self.mepart_set("r3", summe_preis_konstant - summe_preis_dyn)
                self.mepart_set("r5", summe_verlust_dyn_plus_kunde)
                self.mepart_set("r6", summe_verlust)
                counter = counter_vorgabe
            counter -= 1
        
        # Abschlussrechnung
        dauer_tage = round(self.anzahl / 24.0) 
        dauer_jahre = Decimal(dauer_tage) / Decimal('365.0')
        
        self.mepart_set("DatumDauerStunden", self.anzahl)
        self.mepart_set("DatumDauerTage", dauer_tage)
        self.mepart_set("DatumDauerJahre", dauer_jahre)
        
        if dauer_jahre > 0:
            val_r3 = (summe_preis_konstant - summe_preis_dyn) / dauer_jahre
            self.mepart_set("r4", val_r3)
        
        if alg >= 100:
            self.mepart_set("r5", summe_verlust_dyn_plus_kunde)
            self.mepart_set("r6", summe_verlust)
            self.mepart_set("r7", speicher_leer_stunden)
            std_aufschlag_gesamt = std_aufschlag_benutzt + std_aufschlag_offen
            if std_aufschlag_gesamt > 0:
                self.mepart_set("r9", Decimal(std_aufschlag_benutzt) / Decimal(std_aufschlag_gesamt) * Decimal('10'))

        self.mepart_set("LastSimulationbyName", "Sim by Python Port (Decimal Precision)")
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
        os.makedirs("data", exist_ok=True)
    
    sim = DynPlus100()
    sim.init_and_start_simulation("")