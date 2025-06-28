# Unity OpenAI NPC Quick Setup – Automatisiertes Setup & Best Practices

## Übersicht
Dieses Projekt enthält ein vollautomatisiertes Setup für ein OpenAI-Realtime-NPC-System in Unity. Das Setup erstellt und verlinkt alle benötigten GameObjects, UI-Elemente und Referenzen, sodass keine manuellen Schritte mehr nötig sind. Die Lösung ist modular, robust und für Erweiterungen vorbereitet.

---

## Automatisches Setup: Architektur & Ablauf

### 1. Modularer Aufbau
Das Setup ist in einzelne "Step"-Klassen unterteilt, die jeweils einen klar abgegrenzten Teil des Setups übernehmen:

- **FindOrValidateAssetsStep**: Sucht oder erstellt die benötigten Settings-Assets (z.B. `OpenAISettings`).
- **CreateUISystemStep**: Erstellt Canvas, Panel, Buttons, InputField, Dropdown, Slider, Toggle und den `NpcUiManager` automatisch. Sorgt für korrektes Layout, Farben und Funktionalität.
- **ConfigureAudioSystemStep**: Initialisiert Audio-Komponenten und deren Referenzen.
- **SetupLipSyncSystemStep**: (Optional) Initialisiert LipSync-Komponenten.
- **LinkReferencesStep**: Verlinkt alle Komponenten (z.B. Settings auf RealtimeClient, Buttons auf UI-Manager) per Reflection.

Der Haupt-Editor-Eintrag (`OpenAINPCMenuSetup.cs`) ruft diese Steps in der richtigen Reihenfolge auf. Die gesamte Setup-Logik ist in der statischen Utility-Klasse `OpenAINPCSetupUtility` gekapselt und wird direkt aus dem Editor-Kontext ausgeführt – es wird **kein temporäres Setup-GameObject** mehr in die Szene gelegt!

---

### 2. UI-Erstellung & Besonderheiten
- **Canvas & Panel** werden immer neu erstellt, alte Objekte werden entfernt.
- **EventSystem**: Es wird geprüft, ob ein EventSystem existiert (auch inaktive Objekte). Nur wenn keines existiert, wird eines erstellt.
- **GraphicRaycaster**: Der Canvas erhält immer einen GraphicRaycaster.
- **Raycast Target**: Alle Textfelder (z.B. Statusanzeige) haben `raycastTarget = false`, damit sie keine Klicks blockieren. Das war eine häufige Fehlerquelle!
- **Buttons, Slider, Dropdown, Toggle** werden mit korrekten RectTransforms, Farben und Hierarchie erstellt. Die Referenzen werden per Reflection im `NpcUiManager` gesetzt.
- **Dropdown (Voice)**: Das TMP_Dropdown wird mit vollständigem Template (inkl. Toggle) erstellt, damit es zur Laufzeit keine Fehler gibt.

---

### 3. Automatische Referenzierung
- **Reflection**: Nach dem Erstellen der UI-Elemente werden die Referenzen (z.B. Buttons, Slider, Dropdown) per Reflection im `NpcUiManager` gesetzt, sofern die Felder existieren.
- **OpenAISettings**: Die Referenz auf das Settings-Asset wird automatisch im `RealtimeClient` gesetzt.
- **Dropdown-Änderung**: Ein EventListener sorgt dafür, dass beim Wechsel der Stimme im Dropdown das `voice`-Feld im `OpenAISettings`-Objekt zur Laufzeit gesetzt wird (per Reflection). Anschließend wird ein Session-Update an den RealtimeClient gesendet.

---

### 4. Erweiterbarkeit & Best Practices
- **Neue UI-Elemente** können einfach in `CreateUISystemStep` ergänzt werden. Die Referenzierung im Manager erfolgt automatisch, wenn das Feld existiert.
- **Weitere Komponenten** (z.B. neue Settings, weitere Manager) können in den jeweiligen Steps ergänzt und im `LinkReferencesStep` verknüpft werden.
- **Debugging**: Ausführliche Debug.Log-Ausgaben zeigen an, welche Objekte erstellt und verlinkt wurden. Die Methode `FinalizeAndLogSetup` prüft abschließend, ob alle wichtigen Komponenten korrekt existieren und referenziert sind.
- **Kein temporäres Setup-Objekt**: Das Setup läuft jetzt ausschließlich über das Editor-Menü und die Utility-Klasse. Die Szene bleibt sauber, es werden nur die wirklich benötigten Objekte erzeugt.

---

## Typische Fehlerquellen & Lessons Learned
- **Raycast Target**: TextMeshPro-Komponenten blockieren standardmäßig Klicks. Immer `raycastTarget = false` setzen, wenn keine Interaktion nötig ist.
- **EventSystem**: Prüfe auf alle EventSystem-Objekte (auch inaktive), um doppelte zu vermeiden.
- **Referenzen**: Felder im Manager müssen exakt so heißen wie im Code (z.B. `connectButton`). Die Reflection sucht nach diesen Namen.
- **ScriptableObject-Änderungen**: Änderungen am Settings-Objekt wirken sich sofort auf alle Komponenten aus, die eine Referenz darauf halten.
- **Dropdown-Template**: Das TMP_Dropdown benötigt ein vollständiges Template mit Toggle, sonst gibt es Laufzeitfehler.

---

## Wo eigenen Code ergänzen?
- **Eigene UI-Elemente**: In `CreateUISystemStep` neue Methoden nach dem Muster der bestehenden anlegen und im Execute-Aufruf ergänzen.
- **Eigene Settings/Manager**: In den jeweiligen Steps (z.B. `LinkReferencesStep`) per Reflection verknüpfen.
- **Eigene Logik**: Im `NpcUiManager` oder eigenen Komponenten, die im Setup automatisch referenziert werden.

---

## Einstiegspunkt
- **Setup starten:** Über das Menü `OpenAI NPC/Quick Setup` im Unity Editor. Es wird kein temporäres Setup-Objekt mehr erzeugt!
- **Setup-Logik:** Liegt in `OpenAINPCSetupUtility` und den modularen Step-Klassen.
- **Szenen-Setup:** Auch Test-Szenen (z.B. SafeTestSceneCreator) erzeugen kein SetupHelper-Objekt mehr, sondern verweisen auf das Editor-Menü.
