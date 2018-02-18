# OutlookSwissPTTimetable
Swiss public transport timetable add-in for Microsoft Outlook

## So funktioniert die Fahrplanabfrage f�r Microsoft Outlook

M�chten Sie die Reise mit �ffentlichen Verkehrsmitteln zu ihren Terminen direkt in Microsoft Outlook planen? So geht�s:

1. Laden Sie das Installationsprogramm [von hier](https://github.com/mbeer/OutlookSwissPTTimetable/raw/master/publish/setup.exe) herunter und f�hren Sie es aus.
1. Starten Sie Microsoft Outlook neu.
1. Selektieren Sie in Ihrem Kalender den Termin, f�r den Sie die An- und R�ckreise planen m�chten.
1. Klicken Sie im Men�band auf den Befehl �An-/R�ckreise planen�

<img src="docs/RibbonButton.png" width="250" title="Men�band">

1. W�hlen Sie nun im sich �ffnenden Dialogfenster
    1. die n�chstgelegene Haltestelle zum Besprechungsort
	1. die Haltestelle, von der aus Sie anreisen
	1. die Haltestelle, zu der Sie zur�ckreisen
	1. f�r jede der Haltestellen die Zeit in Minuten, die Sie f�r den �bergang ben�tigen

![Hauptfenster](docs/MainWindow.png)

1. Klicken Sie sowohl f�r die Anreise als auch f�r die R�ckreise auf �Abfragen�, um die passenden Verbindungen anzuzeigen.
1. W�hlen Sie die gew�nschten Verbindungen in der Liste aus und klicken Sie auf �Anreise eintragen� bzw. �R�ckreise eintragen�, um ein entsprechendes Kalenderelement anzulegen.

In den Programmeinstellungen lassen sich die am h�ufigsten genutzten Haltestellen (samt den zugeh�rigen Distanzen) festlegen, um sie bei der Verwendung des Add-ins aus der Auswahlliste w�hlen zu k�nnen.  

<img src="docs/SettingsWindow.png" width="250" title="Einstellungsfenster">


## Fahrplandaten

OutlookSwissPTTimetable bezieht die Fahrplandaten �ber die [Swiss public transport API](https://transport.opendata.ch/), powered by [Opendata.ch](https://opendata.ch/).

## Erstellt mit

* [Microsoft Visual Studio Community 2017](https://www.visualstudio.com/de/vs/)
* [Json.NET](https://www.newtonsoft.com/json)
* [Mahapps.Metro](https://github.com/MahApps/MahApps.Metro) und [Mahapps.Metro.IconPacks](https://github.com/MahApps/MahApps.Metro.IconPacks)

## Autor

* **Michael Beer** � [mbeer](https://github.com/mbeer/)

Sieh auch die Liste der [Mitwirkenden](https://github.com/mbeer/OutlookSwissPTTimetable/contributors) in diesem Projekt.

## Lizenz

Dieses Projekt wird unter der [MIT-Lizenz](https://de.wikipedia.org/wiki/MIT-Lizenz) ver�ffentlicht � siehe die Datei [LICENSE](LICENSE).



