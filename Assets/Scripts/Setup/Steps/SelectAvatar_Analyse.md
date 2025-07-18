# Analyse: Erstellung und Konfiguration der Select Avatar Buttons

## Übersicht
Die Methode `CreateSelectAvatarUI()` erstellt eine UI-Komponente zur Auswahl von Avataren. Sie besteht aus einem Container mit Buttons, Bildern und einem Beschreibungstext. Die Buttons sind so konfiguriert, dass beim Klick jeweils ein Avatar aktiviert und die anderen deaktiviert werden.

## Aufbau der UI
- **Container:** Ein GameObject namens "Select Avatar" wird als Hauptcontainer erstellt und dem Panel hinzugefügt.
- **Buttons-Container:** Enthält die drei Avatar-Buttons (Robert, Leonard, RPM).
- **Images-Container:** Enthält die zugehörigen Bilder der Avatare.
- **Beschreibungstext:** Zeigt den aktuell ausgewählten Avatar an.

## Erstellung der Buttons
Für jeden Avatar wird:
- Ein Button-GameObject erstellt und dem Buttons-Container hinzugefügt.
- Die Position und Größe des Buttons werden festgelegt.
- Ein Image-Komponente als Hintergrund hinzugefügt.
- Die Button-Komponente konfiguriert (Farben, Transition).
- Ein OnClick-Event hinzugefügt:
  - **Runtime Listener:** Beim Klick wird die Methode `OnAvatarButtonClicked` aufgerufen, die den ausgewählten Avatar aktiviert und die anderen deaktiviert.
  - **Persistent Calls:** Im Editor werden per SerializedObject SetActive-Calls für die jeweiligen Avatar-GameObjects angelegt (nur im Editor).
- Ein TextMeshProUGUI-Text als Button-Beschriftung hinzugefügt.

## SetActive-Funktionalität
- Die Methode `OnAvatarButtonClicked` erhält den Index des gewählten Avatars und die Namen der Avatar-GameObjects.
- Für jeden Avatar:
  - Wird das zugehörige GameObject per `AvatarManager.Instance.GetAvatar` geholt.
  - Das gewählte Avatar-GameObject wird aktiviert (`SetActive(true)`), die anderen werden deaktiviert (`SetActive(false)`).
  - Im Editor werden die Persistent Calls so gesetzt, dass beim Klick auf einen Button die SetActive-Methoden der jeweiligen GameObjects korrekt ausgeführt werden.

## Logging und UI-Feedback
- Nach jeder Aktion wird ein Log ausgegeben.
- Der Beschreibungstext wird aktualisiert, um den ausgewählten Avatar anzuzeigen.
- Die Button-Farben werden angepasst, um die Auswahl visuell darzustellen.

## Sonstiges
- Die Bilder der Avatare werden aus den Ressourcen oder per AssetDatabase geladen.
- Fallback: Falls kein Bild gefunden wird, wird ein farbiges Platzhalterbild erzeugt.

---

**Fazit:**
Die Button-Erstellung und -Konfiguration ist modular und robust umgesetzt. Die Aktivierung/Deaktivierung der Avatare erfolgt sowohl zur Laufzeit als auch im Editor über Events und Persistent Calls. Die UI gibt visuelles und textuelles Feedback zur Auswahl.


# Analyse: Eigener Avatar-Upload im Quick Setup

## Übersicht
Im Quick Setup ist es möglich, einen eigenen Avatar über eine URL hochzuladen und zu laden. Dies wird durch das Skript `PersonalAvatarLoader` realisiert, das in der Szene die UI-Logik und den Ladevorgang steuert.

## UI-Elemente und Ablauf
- **Avatar-URL-Feld:** Ein Eingabefeld, in das der Nutzer die URL seines Avatars (GLB/GLTF) einträgt.
- **Laden-Button:** Wird aktiviert, sobald eine gültige URL eingegeben wurde. Klick lädt den Avatar.
- **Panel-Logik:** Ein Panel mit Close- und Link-Buttons steuert die Sichtbarkeit und bietet einen Link zum Ready Player Me Avatar-Creator.
- **Ladeanzeige:** Während des Ladevorgangs wird ein Lade-Overlay angezeigt.

## Technischer Ablauf
1. **Panel öffnen:**
   - Klick auf den "Eigenen Avatar hochladen"-Button öffnet das Panel und blendet die Third-Person-Steuerung aus.
2. **URL-Eingabe:**
   - Das Eingabefeld prüft live, ob die URL gültig ist. Nur dann wird der "Laden"-Button aktiv.
3. **Avatar laden:**
   - Klick auf "Laden" ruft `thirdPersonLoader.LoadAvatar(avatarUrlField.text)` auf.
   - Während des Ladevorgangs wird ein Lade-Overlay angezeigt und der Button deaktiviert.
   - Nach Abschluss wird das Overlay ausgeblendet und die Steuerung wieder aktiviert.
4. **Link-Button:**
   - Öffnet die Ready Player Me-Webseite im Browser, um einen eigenen Avatar zu erstellen.

## Events und Logging
- Es werden Analytics-Events für das Öffnen des Panels und das Laden eines Avatars getrackt.
- Nach erfolgreichem Laden wird ein Callback (`OnLoadComplete`) ausgeführt, der die UI zurücksetzt.

## Fazit
Der Avatar-Upload im Quick Setup ist benutzerfreundlich umgesetzt: Die UI prüft die Eingabe, gibt Feedback und steuert den Ladevorgang robust. Die Integration mit Ready Player Me ermöglicht es, eigene Avatare einfach per URL zu importieren und direkt im Projekt zu verwenden.
