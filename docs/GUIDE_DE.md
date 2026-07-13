[ English ](GUIDE.md) • [ Русский ](GUIDE_RU.md) • [ Deutsch ](GUIDE_DE.md)

# ShortcutDock Benutzerhandbuch

Willkommen bei **ShortcutDock** — einer modernen und funktionalen Schnellstartleiste zur Organisation Ihrer Anwendungen, Ordner und zur Verwaltung von Systemelementen unter Windows 10 und 11.

Dieses Handbuch hilft Ihnen, sich schnell mit allen Funktionen der Anwendung vertraut zu machen und sie an Ihre Bedürfnisse anzupassen.

---

## Inhaltsverzeichnis
1. [Systemanforderungen](#1-systemanforderungen)
2. [Installation und erster Start](#2-installation-und-erster-start)
3. [Hinzufügen von Verknüpfungen zur Leiste](#3-hinzugügen-von-verknüpfungen-zur-leiste)
4. [Verknüpfungen verwalten und sortieren](#4-verknüpfungen-verwalten-und-sortieren)
5. [Einstellung von Aussehen und Effekten](#5-einstellung-von-aussehen-und-effekten)
6. [Integration des Papierkorbs](#6-integration-des-papierkorbs)
7. [Arbeiten über das System-Tray](#7-arbeiten-über-das-system-tray)
8. [Speicherort der Einstellungen (Portabilität)](#8-speicherort-der-einstellungen-portabilität)
9. [Problembehandlung](#9-problembehandlung)

---

## 1. Systemanforderungen
* **Betriebssystem:** Windows 10 (Build 19041 und höher) oder Windows 11.
* **Architektur:** x64.
* **Optional:** Für die Leichtgewichts-Version (Lightweight) ist die Installation der [.NET 10 Desktop-Laufzeitumgebung](https://dotnet.microsoft.com/download/dotnet/10.0) (oder höher) erforderlich. Die eigenständige Version (Self-Contained) läuft ohne zusätzliche Bibliotheken.

---

## 2. Installation und erster Start
Die Anwendung wird als einzelne Datei `ShortcutDock.exe` geliefert und erfordert keine vollständige Installation:

> [!TIP]
> **Holen Sie sich die Anwendung:**  
> 📥 **[Laden Sie die neueste Version von der GitHub Releases-Seite herunter](https://github.com/Almanex/ShortcutDock/releases)**

1. Legen Sie die Datei `ShortcutDock.exe` in einen beliebigen Ordner auf Ihrem Computer (z. B. `C:\Program Files\ShortcutDock` oder Ihren persönlichen Benutzerordner).
2. Doppelklicken Sie auf die Datei `ShortcutDock.exe`, um sie zu starten.
3. Beim ersten Start erscheint eine leere, halbtransparente Leiste mit einer `+`-Schaltfläche am unteren Bildschirmrand.

> [!IMPORTANT]
> **Windows Defender SmartScreen-Warnung:**  
> Da die ausführbare Datei der Anwendung nicht mit einem kostenpflichtigen digitalen Zertifikat des Entwicklers signiert ist (was bei kostenlosen Open-Source-Projekten üblich ist), blockiert Windows Defender SmartScreen möglicherweise den Start beim ersten Ausführen und zeigt ein Popup *"Der Computer wurde durch Windows geschützt"* an.  
> **So starten Sie die Anwendung:** Klicken Sie auf den Link **„Weitere Informationen“** (oben links im Warnungs-Popup) und dann auf die Schaltfläche **„Trotzdem ausführen“**. Windows merkt sich Ihre Entscheidung und die Warnung wird bei zukünftigen Starts nicht mehr angezeigt.

---

## 3. Hinzufügen von Verknüpfungen zur Leiste
Sie können neue Programme, Ordner oder Laufwerke auf drei Arten zur Leiste hinzufügen:

### Methode A: Drag-and-Drop
* Ziehen Sie einfach eine ausführbare Datei (`.exe`), eine Verknüpfung (`.lnk`), einen Ordner oder ein ganzes Laufwerk aus dem Windows-Explorer an eine beliebige freie Stelle auf der Leiste. ShortcutDock extrahiert automatisch das hochauflösende Symbol und fügt das Element hinzu.

### Methode B: Über die „+“-Schaltfläche
1. Wenn die Option zum Anzeigen der `+`-Schaltfläche in den Einstellungen aktiviert ist, klicken Sie darauf.
2. Wählen Sie im geöffneten Dialogfenster die gewünschte Programmdatei oder Verknüpfung aus und klicken Sie auf „Öffnen“.

### Methode C: Über das Kontextmenü
* Klicken Sie mit der rechten Maustaste auf eine freie Stelle der Leiste und wählen Sie **„Anwendung hinzufügen“**.

---

## 4. Verknüpfungen verwalten und sortieren

### Ändern der Symbolreihenfolge
* Halten Sie die linke Maustaste auf einem Symbol gedrückt und ziehen Sie es nach links/rechts (oder bei einer vertikalen Leiste nach oben/unten), um seine Position im Verhältnis zu anderen Elementen zu ändern. Lassen Sie die Maustaste an der gewünschten Stelle los — die Reihenfolge wird automatisch gespeichert.

### Als Administrator ausführen
* Klicken Sie mit der rechten Maustaste (RMB) auf die gewünschte Anwendung auf der Leiste und wählen Sie **„Als Administrator ausführen“**.

### Symbol (Icon) ändern
1. Klicken Sie mit der rechten Maustaste auf die Verknüpfung und wählen Sie **„Symbol ändern...“**.
2. Wählen Sie ein beliebiges `.png`-Bild oder eine `.ico`-Symboldatei aus. Die Leiste aktualisiert die Anzeige des Symbols sofort.

### Von der Leiste entfernen
* Klicken Sie mit der rechten Maustaste auf die Verknüpfung auf der Leiste und wählen Sie **„Von der Leiste entfernen“** (die Programmdatei auf der Festplatte wird dabei nicht gelöscht).

---

## 5. Einstellung von Aussehen und Effekten
Um das Einstellungsfenster zu öffnen, klicken Sie mit der rechten Maustaste auf eine freie Stelle auf der Leiste oder auf das Anwendungssymbol im System-Tray und wählen Sie **„Leisten-Einstellungen“**.

* **Bildschirmposition:** 
  * *Unten* oder *Oben* (horizontale Leiste).
  * *Links* oder *Rechts* (die Leiste wechselt automatisch in den vertikalen Modus).
  * Wenn die Leiste an die Ränder des Bildschirms verschoben wird, reserviert sie automatisch Platz auf dem Desktop (AppBar-Technologie) — andere Fenster überdecken sie beim Maximieren nicht.
* **Weichzeichnungseffekt:**
  * *Kein:* Völlig transparenter Hintergrund (nur Symbole schweben über dem Hintergrundbild).
  * *Mica:* Der typische halbtransparente Windows 11-Effekt, der sich an die Farbe Ihres Hintergrundbilds anpasst. Wechselt automatisch zwischen dunklen und hellen Designs je nach Systemeinstellung.
  * *Acrylic:* Transluzenter Milchglaseffekt. Reagiert ebenfalls dynamisch auf Änderungen des Systemdesigns (hell/dunkel).
* **Symbolgröße:**
  * Sie können die Symbolgröße wählen: **32px**, **40px**, **48px** oder **64px**. Die Leiste ändert ihre Größe sofort.
* **Im Vordergrund (Keep on Top):**
  * Wenn aktiviert, ist die Leiste immer sichtbar und reserviert Platz auf dem Desktop. Wenn deaktiviert, verhält sich die Leiste wie ein normales Fenster und kann von anderen Programmen überdeckt werden.
* **Oberflächensprache:**
  * Ermöglicht das Umschalten der Anwendungssprache (*English*, *Русский*, *Deutsch*). Die Änderungen werden sofort auf alle Elemente angewendet, einschließlich Kontextmenüs und System-Tray. Beim ersten Start wird die Sprache automatisch basierend auf den Windows-Einstellungen ausgewählt.
* **„+“-Schaltfläche auf der Leiste anzeigen:**
  * Ermöglicht das Ausblenden der Hinzufügen-Schaltfläche auf der Leiste für ein klares, minimalistisches Erscheinungsbild (standardmäßig beim ersten Start aktiviert).
* **Mit Windows starten:**
  * Fügt die Anwendung zum Windows-Autostart hinzu, sodass die Leiste direkt beim Start des Computers angezeigt wird.
* **Automatisch ausblenden (Auto-Hide):**
  * Wenn aktiviert, gleitet die Leiste reibungslos aus dem Bildschirm, wenn sie den Mausfokus verliert, und hinterlässt einen dünnen 2-px-Auslöserstreifen. Wenn Sie die Maus über diesen Streifen bewegen, kehrt die Leiste sofort zurück. Die AppBar-Platzreservierung wird vorübergehend deaktiviert, während die Leiste ausgeblendet ist, sodass andere Fenster den gesamten Bildschirm einnehmen können. Die Leiste wird nicht ausgeblendet, wenn ein Kontextmenü geöffnet ist.
* **Hover-Zoom-Effekt:**
  * Aktiviert einen wellenartigen Skalierungseffekt der Symbole beim Bewegen der Maus darüber, ähnlich wie beim macOS-Dock. Dies hebt die ausgewählte Verknüpfung hervor und erleichtert das Anklicken.
* **Indikatoren für laufende Programme:**
  * Zeigt kleine Punkte in der Akzentfarbe unter den Symbolen der derzeit aktiven Programme an.
  * Durch Klicken auf ein aktives Programmsymbol wird das vorhandene Fenster fokussiert und in den Vordergrund gebracht, anstatt einen neuen doppelten Prozess zu starten.

---

## 6. Integration des Papierkorbs
In den Leisten-Einstellungen können Sie die Option **„Papierkorb auf der Leiste anzeigen“** aktivieren.

* **Dynamischer Status:** Das Papierkorbsymbol ändert sich in Echtzeit, je nachdem, ob er leer ist oder gelöschte Dateien enthält.
* **Papierkorb leeren:** Klicken Sie mit der rechten Maustaste auf das Papierkorbsymbol auf der Leiste und wählen Sie **„Papierkorb leeren“**.
* **Papierkorb openen:** Klicken Sie mit der linken Maustaste auf das Papierkorbsymbol, um ihn im Windows-Explorer zu öffnen.
* *Hinweis:* Der Papierkorb ist immer am Ende der Leiste angeheftet; er kann nicht in die Mitte gezogen werden.

---

## 7. Arbeiten über das System-Tray
Wenn ShortcutDock ausgeführt wird, wird sein Symbol im System-Tray (neben der Windows-Uhr) angezeigt.

* **Schnelles Ausblenden/Einblenden:** Doppelklicken Sie auf das Tray-Symbol, um die Leiste schnell auszublenden (oder wiederherzustellen).
* **Tray-Kontextmenü:** Klicken Sie mit der rechten Maustaste auf das Tray-Symbol, um die Einstellungen zu öffnen oder die Anwendung vollständig zu beenden („Beenden“).

---

## 8. Speicherort der Einstellungen (Portabilität)
Alle Benutzereinstellungen, hinzugefügten Verknüpfungen und Symbol-Caches werden in Ihrem Benutzerprofilverzeichnis gespeichert:
`%AppData%\ShortcutDock\` (normalerweise `C:\Users\Benutzername\AppData\Roaming\ShortcutDock`).

* **Konfigurationsdatei:** `settings.json` — enthält Einstellungen für Positionen, Größen und die Liste der Verknüpfungen.
* **Cache-Ordner:** `Cache\` — speichert extrahierte PNG-Symbole für schnelles Rendern beim Start.
* Alle Pfade in der Konfigurationsdatei werden in einem portablen Format gespeichert (unter Verwendung der Variable `%AppData%`), sodass Sie Einstellungen problemlos auf einen anderen Computer kopieren können.

---

## 9. Problembehandlung

### Anwendungssymbole werden nicht korrekt angezeigt
* Versuchen Sie, die Verknüpfung zu entfernen und die Anwendung erneut hinzuzufügen, oder ersetzen Sie das Symbol manuell über das Kontextmenü -> „Symbol ändern...“.

### Die Leiste überlappt die Windows-Taskleiste
* Ändern Sie die Position der Leiste in den Einstellungen. Wenn sich beispielsweise die Windows-Taskleiste unten befindet, heften Sie ShortcutDock an den oberen, linken oder rechten Rand an, um Interface-Überlappungen zu vermeiden.

### Autostart funktioniert nicht
* Stellen Sie sicher, dass Sie die Datei `ShortcutDock.exe` nach dem Aktivieren des Autostarts nicht verschoben haben. Wenn Sie dies getan haben, deaktivieren und aktivieren Sie den Autostart einfach erneut in den Einstellungen, um den Pfad in der Windows-Registrierung zu aktualisieren.
