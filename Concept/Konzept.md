# Projektkonzept: Uncanny Npc – Interaktive NPCs mit LLM-Intelligenz

## Zielsetzung
Ziel dieses Projekts ist die Schaffung einer immersiven VR-Erfahrung, bei der Nutzer:innen mit virtuellen Charakteren (NPCs) interagieren, die mithilfe von Large Language Models (LLMs) gesteuert werden. Dabei soll untersucht werden, wie glaubwürdig diese NPCs auf verschiedene Nutzer:innen wirken und inwiefern sie den sogenannten *Uncanny Valley*-Effekt auslösen.

---

## Konzeptidee

Wir kombinieren visuelle, auditive und interaktive Faktoren in einem VR-Experiment. Die Proband:innen sollen mit mehreren NPCs sprechen, die sich in folgenden Aspekten unterscheiden:

- **Visuelle Darstellung:**  
  - Low-Poly  
  - Cartoon  
  - Semi-realistisch  
  - Fotorealistisch

- **Stimme:**  
  - Unterschiedliche Sprachprofile (z. B. natürlich vs. roboterhaft)

- **Dialogführung:**  
  - Echtzeit-Dialoge mit NPCs über ein angebundenes LLM  
  - Anpassung des NPC-Verhaltens in Echtzeit

Nach der Interaktion erfolgt eine kurze Bewertung der Glaubwürdigkeit/Humanität auf einer Skala von 1 bis 5 Sternen.

---

## Technische Umsetzung

- **Game Engine:** Unity mit XR Toolkit
- **Plattform:** Meta Quest 2/3
- **NPC-Charaktere:** Ready Player Me, Mixamo, ggf. eigene Modelle
- **Sprachausgabe:** Text-to-Speech APIs (z. B. ElevenLabs, Azure TTS)
- **LLM-Anbindung:** OpenAI API / lokales Modell (z. B. Ollama)
- **Logging & Auswertung:** CSV-Logging, manuelle Auswertung (ggf. Python)

---

## Evaluation / UX-Methodik

- Pro Interaktion: Bewertungsfragebogen (z. B. direkt in VR oder via Web)
- Qualitative Beobachtungen: Gesprächsfluss, Latenz, Irritationen
- Quantitativ: Bewertung der Glaubwürdigkeit / Natürlichkeit

---

## Aufgabenverteilung

| Aufgabe                                 | Verantwortlich     |
|----------------------------------------|--------------------|
| Unity-Projekt Setup & XR-Anbindung     | `crack666`         |
| NPC-Modellintegration & Varianten      | `maiossa`          |
| LLM-Integration                        | `crack666`         |
| TTS-API Anbindung                      | `maiossa`          |
| Dialog-Design / Prompt Engineering     | `crack666`         |
| Usability Testing & Feedbacksystem     | `maiossa`          |
| Logging / Datenexport                  | beide              |
| Präsentation & Dokumentation           | beide              |

---

## Zeitplan (Sprint-basiert, 6 Wochen)

| Woche | Meilenstein                                 |
|-------|----------------------------------------------|
| 1     | Feinkonzept, Unity Setup                     |
| 2     | Prototyp NPCs + Basisdialog                  |
| 3     | LLM-Integration + TTS                        |
| 4     | Erste Tests / Logging                        |
| 5     | Feinschliff, visuelle + auditive Varianten   |
| 6     | Evaluation, Auswertung, Präsentation         |

---