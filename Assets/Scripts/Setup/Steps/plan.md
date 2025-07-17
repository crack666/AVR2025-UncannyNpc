
# Plan: Custom Avatar Button in Select Avatar UI (Iteration 2, July 2025)

## Ziel
Im Select Avatar UI soll ein vierter Button für den Custom Avatar erscheinen, sobald im Quick Setup ein eigener Avatar über "Choose Avatar Prefab" geladen wurde. Beim Klick auf diesen Button wird nur der Custom Avatar eingeblendet, alle anderen Avatare werden ausgeblendet. Die OnClick-Events der anderen Buttons müssen ebenfalls angepasst werden, sodass sie den Custom Avatar ausblenden.

---

## Analyse: Warum erscheint der Custom Avatar Button nicht?

- Die Methode `CreateSelectAvatarUI()` prüft, ob ein Custom Avatar (`AvatarManager.Instance.GetAvatar("CustomAvatar")`) existiert. Ist das nicht der Fall, wird kein Button erzeugt.
- Im Quick Setup wird der Custom Avatar **nicht** automatisch mit dem Namen "CustomAvatar" registriert, sondern nur unter seinem Prefab-Namen (z.B. "MyCustomAvatar").
- Die UI prüft aber explizit auf den Namen "CustomAvatar". Deshalb erscheint der Button beim ersten UI-Build nicht.

**Root Cause:**
Der Custom Avatar wird im Quick Setup nicht als "CustomAvatar" registriert, sondern nur unter seinem Originalnamen. Die UI erwartet aber genau diesen Namen.

**Lösung:**
Nach dem Instanziieren des Custom Avatars im Quick Setup muss explizit `AvatarManager.Instance.RegisterAvatar("CustomAvatar", avatarInstance);` aufgerufen werden, bevor das Setup weiterläuft. Dadurch ist der Avatar sofort für die UI sichtbar und der Button erscheint direkt beim ersten Build.

---

## Schritt-für-Schritt-Plan (Iteration 2)

1. **Custom Avatar explizit als "CustomAvatar" registrieren**
   - Nach dem Instanziieren des Avatars im Quick Setup: `AvatarManager.Instance.RegisterAvatar("CustomAvatar", avatarInstance);` aufrufen.
   - Dadurch ist der Avatar sofort für die UI sichtbar.

2. **UI-Logik bleibt unverändert**
   - Die UI prüft weiterhin auf `GetAvatar("CustomAvatar")` und zeigt den Button nur, wenn vorhanden.
   - Das Event- und Button-Handling bleibt wie implementiert.

3. **Testen**
   - Nach Import eines Custom Avatars prüfen, ob der Button erscheint und die Umschaltung zwischen allen vier Avataren funktioniert.
   - Logging und UI-Feedback kontrollieren.

4. **Dokumentation & Review**
   - Plan und Code kommentieren, damit der Zusammenhang klar ist.
   - Ggf. weitere Optimierungen dokumentieren.

---

## Hinweise
- Die Referenz auf den Custom Avatar sollte zentral (z.B. AvatarManager.Instance.GetCustomAvatar()) verfügbar sein.
- Die UI-Erstellung und Event-Logik bleibt modular, sodass weitere Avatare leicht ergänzt werden können.
- Die Änderungen betreffen hauptsächlich die Methoden `CreateSelectAvatarUI()` und die Registrierung im Quick Setup.
