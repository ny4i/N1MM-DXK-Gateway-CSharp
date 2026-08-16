==============================================================================
 MACHINE TRANSLATION into de. The English README.txt is
 authoritative; where this file disagrees with it, it is this file
 that is wrong. Corrections are very welcome.

 source-sha256: e67ddcc749a9e9c5
==============================================================================

N1MM-DXKeeper Gateway 2.0
=========================

Beförderungen QSOs eingeloggt TR4W oder N1MM Logger+ geradeaus DXKeeper, und
fragt DXView und Pathfinder um die Rufzeichen nachzuschlagen, die Sie
arbeiten.

Vollständige Dokumentation: https://ny4i.com/n1mm-dxkeeper-gateway/


Bevor Sie beginnen
------------------

1. DXKeeper muss installiert werden. Das Gateway tut nichts von sich aus; es ist eine
   go-between.

2. Die Microsoft .NET 8 DESKTOP Runtime, 64-Bit (x64).

   Wenn Sie bereits laufen JTAlert 2.80 oder später, Sie haben es - JTAlert Bedürfnisse der
   Das gleiche, und es muss nur einmal installiert werden. Windows hält es aktualisiert
   Danach als Teil des Normalen Windows Update.

   Wenn das Gateway nicht startet oder Windows anbietet, nach
   etwas, das ist, was fehlt:

       https://dotnet.microsoft.com/download/dotnet/8.0

   Wählen Sie:Desktop Runtime", x64. Nicht das SDK und nicht das einfache ".NET"
   Runtime - die Desktop Runtime ist derjenige, der enthält, was dieses Programm
   Bedürfnisse. Das frühere VB6-Gateway benötigte keine solche Installation; dieses ist ein
   Umschreiben und tun.

3. Windows 10 oder Windows 11.


LEITUNG
-------

Beginnen Sie es von der Start menu, oder von der Desktop-Verknüpfung, wenn Sie
den Installer nach einem gefragt haben.

Starte das Gateway, DXKeeper, DXView, Pathfinder und Ihren Logger in
beliebiger Reihenfolge. Das Gateway verbindet sich mit jedem, wie es
erscheint.

Ihre Einstellungen befinden sich in der Windows-Registrierung, unter demselben
Schlüssel, den das VB6-Gateway verwendet hat, so dass Einstellungen aus der
alten Version von selbst übernommen werden.

Die DXLab Launcher kann das Gateway neben dem anderen starten DXLab Programme;
siehe "Angabe eines Nicht-DXLab Das Thema "Pfadname" der Anwendung in der
Hilfe von Launcher.


PUNKT IHRES LOGGERS HINSICHTLICH
--------------------------------

Das Gateway hört auf UDP Hafen 12060 Standardmäßig. Sie können dies im
Netzwerkabschnitt des Fensters ändern.

  N1MM Logger+   Config > Configure Ports ... > Broadcast Data Tab.
                 Klicken Sie auf "Kontakte" und legen Sie die Adresse daneben auf Ihre
                 Computer IPv4 Adresse und Port, z.B. 192.168.1.11:12060
                 ZeckenExternal Callsign Lookup" und setzen Sie es auf die gleiche Weise."

  TR4W           Setz UDP BROADCAST ADDRESS an dieselbe Adresse und denselben Port.

  WSJT-X         Settings > ReportingAnkreuzen "Loged Contact aktivieren" ADIF
                 broadcast" und geben Sie Ihre IP-Adresse - 127.0.0.1 wenn WSJT-X ist
                 auf demselben Computer - und 12060 im Server port number
                 Feld.

                 Wir empfehlen Ihnen, JTAlert Stattdessen oder senden Sie direkt Kontakte
                 zum DXLab Anwendungen; siehe DXLab Anweisungen. Dies
                 Route funktioniert, aber das sind die besser befahrenen Wege.

  SDR-Control    Richten Sie seine Logging-Sendung im Hafen 12060.

Wenn Sie mehr als eine davon auf einmal ausführen, achten Sie darauf, nicht
dasselbe zu haben QSO das Gateway zweimal erreichen - zum Beispiel WSJT-X
Broadcasting direkt an das Gateway UND Fütterung N1MM, die es dann auch
sendet. DXKeeper erkennt keine Duplikate und würde beide protokollieren.


ANGABEN ZU DXKEEPER
-------------------

Nichts zu konfigurieren. Das Gateway liest DXKeeper„eigene Base Port Setzen
und verwenden. Wenn Sie sich ändern DXKeeper s Base Port ()Config > Defaults
tab > Network ServiceAnschließend starten Sie das Gateway neu.

Die Überschrift des gleichen Panels sagt Ihnen, ob DXKeeper Der Netzwerkdienst
hört zu. Wenn das Gateway meldet, dass es keine Verbindung herstellen kann,
schauen Sie zuerst dort nach.


WAS DIE UNTERNEHMEN STEHEN
--------------------------

  Settings          UDP Port, optionale Multicast-Gruppe, was DXKeeper sollte
                    Machen Sie mit jedem QSO (Callbook-Lookup), eQSL, LoTW, Club Log,
                    Logging-Optionen und die Schnittstellensprache.

  Connection Status DXKeeper, DXView und Pathfinder Trennung ist normal
                    für Programme, die Sie nicht ausführen.

  Operation Log     Was das Gateway getan hat, das neueste unten. Probleme
                    gefärbt sind. Dies ist der erste Ort, um zu schauen, und die
                    Die Schaltfläche Copy legt sie auf die Zwischenablage für einen Fehlerbericht.

Durch Minimierung wird das Gateway in den Benachrichtigungsbereich (durch die
Uhr) und nicht in die Taskleiste gebracht, wo es eine laufende Zählung dessen
hält, was es erhalten und protokolliert hat. Windows 11 verbirgt standardmäßig
neue Benachrichtigungssymbole - wenn Sie es sehen möchten, ziehen Sie es aus
dem Flyout "versteckte Symbole" in die Taskleiste. Das Schließen des Fensters
verlässt das Gateway.


ÄNDERUNG/LÖSUNG QSOs UND UPLOAD-AUFGABEN
----------------------------------------

Lesen Sie dies vor dem Einschalten Upload to eQSL.cc, Upload to LoTW oder
Upload to Club Log.

Diese Toggles sagen DXKeeper Upload jeder QSO zum Online-Logbuch, sobald es
protokolliert ist. Separat unterstützt das Gateway das Bearbeiten und Löschen
QSOs Wenn Ihr Logger eine Änderung sendet, löscht das Gateway die QSO von
DXKeeper und protokolliert den korrigierten, weil DXKeeper hat keine einzige
"Ersatz"-Operation.

Diese beiden Funktionen kombinieren sich nicht gut, und weder das Gateway noch
DXKeeper kann sie machen. Ein Upload, der bereits ausgegangen ist, kann nicht
abgerufen werden. LoTW insbesondere keine Möglichkeit hat, eine QSO Sie haben
hochgeladen. Also a QSO hochgeladen und dann bearbeitet lässt das ORIGINAL
stehen LoTW Für immer, mit der korrektur hinzugefügt, anstatt sie zu ersetzen.
A QSO hochgeladen und dann gelöscht bleibt bei LoTW Nachdem es aus Ihrem
eigenen Protokoll verschwunden ist.

Bevor das Gateway das Bearbeiten und Löschen unterstützte, konnte dies nicht
auftreten: QSO it logged war endgültig.

Was darüber zu tun ist

Die einfache Antwort und die, die der Autor verwendet, besteht darin, alle
drei Upload-Schalter während des Wettbewerbs ausgeschaltet zu lassen und von
DXKeeper von Hand, sobald das Protokoll endgültig ist und alle Korrekturen
vorgenommen wurden. DXKeeper Uploads eines ganzen Logs so einfach wie eines
QSO Und bis dahin gibt es nichts mehr zu korrigieren.

Schalten Sie sie ein, wenn Sie möchten - das Gateway warnt Sie einmal und tut
dann, was es gesagt wird - aber seien Sie sich bewusst, dass eine spätere
Korrektur das Online-Logbuch nicht sauber erreicht.

Dies gilt nicht für Query Callbook oder Lookup previous QSOs Die lesen nur.


FILES IT WRITS
--------------

Beide erscheinen im eigenen Ordner des Gateways. Wenn das Gateway irgendwo
installiert wurde, lässt Windows es nicht schreiben - unter C:\Program Dateien
zum Beispiel - es verwendet stattdessen einen Benutzerordner und zeichnet auf,
welche oben aufgeführt ist ErrorLog.txt.

  ErrorLog.txt          Diagnose. Eine rotesee ErrorLogDer Link erscheint im
                        Fenster, wenn etwas darauf geschrieben wurde. Zeck
                        „Log debugging informationFür viel mehr Details, wenn
                        Ein Problem jagen.

  FailedQSOs_<date>_<time>.adi
                        QSOs DXKeeper nicht bestätigt. Wichtig: Das Gateway
                        Niemals leise wegwerfen a QSO, aber auch nie
                        versucht einen, weil DXKeeper nicht erkennt
                        Duplikate und ein Wiederholungsversuch könnte es zweimal protokollieren. Wenn dies
                        Datei existiert, importieren Sie sie in DXKeeper von Hand und dann
                        löschen.Failed QSOs" unten am Fenster"
                        wird rot mit einer Zählung, wenn dies geschieht; klicken Sie auf
                        Öffnen Sie den Ordner mit der ausgewählten Datei. Der Count geht
                        Zurück zu Null, wenn die Datei weg ist.

                        Eine Datei pro Durchlauf. Ein Lauf, der nichts verliert, lässt kein
                        Datei, also bedeutet die vorhandene Datei immer etwas
                        braucht deine Aufmerksamkeit.


IF A QSO KEIN ANTRIEB
---------------------

  - Hat die Operation Log zeigen die QSO empfangen werden? Wenn nicht, ist der Logger
    Das Gateway nicht erreichen: Überprüfen Sie die Adresse und den Port und überprüfen Sie eine Firewall
    ist nicht blockiert UDP.

  - Zeigt es an, dass es gesendet, aber nicht bestätigt wird? DXKeeper nicht bestätigt
    es. Überprüfung DXKeeper läuft und das seine Network Service sagt Listening.
    Die QSO wird in FailedQSOs.

  - DXKeeper kann während eines anstrengenden Wettbewerbs mehrere Sekunden zurückliegen. Das Gateway
    sendet eine QSO zu einer Zeit und wartet auf DXKeeper um jeden zu bestätigen, so a
    Backlog ist normal und läuft von selbst ab.


SPRACHEN
--------

Das Gateway folgt Ihrer Windows-Anzeigesprache, wenn es eine Übersetzung dafür
hat, und Sie können eine explizit unter Einstellungen > General. Eine Änderung
wird beim nächsten Start wirksam.

Andere Übersetzungen als Englisch werden maschinell hergestellt und von
Freiwilligen korrigiert. Wenn Sie schlecht lesen, sind Korrekturen sehr
willkommen - und der Name des Übersetzers erscheint im Fenster Über.


LIZENZ
------

Freie Software unter der GNU General Public License Version 3 oder höher, mit
ABSOLUTELY NO WARRANTY. Der vollständige Text ist in COPYING.txt; NOTICE.txt
das Urheberrecht, die Komponenten Dritter und deren Lizenzen aufzeichnet.

Sie können es für jeden Zweck verwenden, studieren, wie es funktioniert,
teilen und ändern.


HILFE
-----

  Documentation   https://ny4i.com/n1mm-dxkeeper-gateway/
  Questions       DXLab Diskussionsgruppe, DXLab@groups.io

Wenn Sie ein Problem melden, ist das Fenster "Über"Copy details" Der Button
stellt die Version und Ihre Umgebung in die Zwischenablage. " Bitte geben Sie
diesen und den entsprechenden Teil des Operation Log oder ErrorLog.txt.
