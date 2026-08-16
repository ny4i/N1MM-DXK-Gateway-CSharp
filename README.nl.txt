==============================================================================
 MACHINE TRANSLATION into nl. The English README.txt is
 authoritative; where this file disagrees with it, it is this file
 that is wrong. Corrections are very welcome.

 source-sha256: e67ddcc749a9e9c5
==============================================================================

N1MM-DXKeeper Gateway 2.0
=========================

Carrie QSOs ingelogd TR4W of N1MM Logger+ recht in DXKeeper, en vraagt DXView
en Pathfinder Om de roepnamen op te zoeken die je werkt.

Volledige documentatie: https://ny4i.com/n1mm-dxkeeper-gateway/


WAT U MOET WETEN VOORDAT U begint
---------------------------------

1. DXKeeper moet geïnstalleerd zijn. De Gateway doet niets alleen; het is een
   Een tussenpersoon.

2. De Microsoft .NET 8 DESKTOP Runtime, 64-bit (x64).

   Als u al loopt JTAlert 2.80 of later, je hebt het - JTAlert de
   Het hoeft maar één keer geïnstalleerd te worden. Windows houdt het op de hoogte
   daarna als onderdeel van normaal Windows Update.

   Als de Gateway niet zal starten, of Windows biedt te gaan zoeken
   Dit is wat er ontbreekt:

       https://dotnet.microsoft.com/download/dotnet/8.0

   Kies "Desktop Runtime", x64. Niet de SDK, en niet de gewone ".NET
   "Runtime" Desktop Runtime is degene die bevat wat dit programma
   behoeften. De eerdere VB6 Gateway had geen dergelijke installatie nodig; deze is een
   Herschrijven en doen.

3. Windows 10 of Windows 11.


RUNNING it
----------

Begin met de Start menu, of van het bureaublad snelkoppeling als u gevraagd
het installatieprogramma voor een.

Start de Gateway, DXKeeper, DXView, Pathfinder En je logger in elke volgorde.
De Gateway verbindt met elk zoals het lijkt.

Uw instellingen live in het Windows-register, onder dezelfde sleutel als de
gebruikte VB6 Gateway, dus instellingen van de oude versie dragen zelf over.

De DXLab Launcher kan de Gateway beginnen naast je andere DXLab programma's;
zie de "Specifiëren van een niet-DXLab applicatie's padnaam" onderwerp in
Lancerer's hulp.


Je LOGGER erop richten
----------------------

The Gateway luistert mee UDP poort 12060 standaard. U kunt dat veranderen in
de Netwerk sectie van het venster.

  N1MM Logger+   Config > Configure Ports ... > Broadcast Data tab.
                 Tik op "Contacten" en zet het adres ernaast op uw
                 computer IPv4 adres en haven, bv. 192.168.1.11:12060
                 Tik "External Callsign Lookup" en zet het op dezelfde manier.

  TR4W           Instellen UDP BROADCAST ADDRESS naar hetzelfde adres en dezelfde haven.

  WSJT-X         Settings > Reporting. Tik op "Gelogd contact inschakelen ADIF
                 broadcast" en voer uw IP-adres in - 127.0.0.1 als WSJT-X is
                 op dezelfde computer - en 12060 in de Server port number
                 veld.

                 We raden u aan om te gebruiken JTAlert in plaats daarvan, of stuur direct contacten
                 aan de DXLab toepassingen; zie de DXLab instructies. Dit
                 Maar dat zijn de betere wegen.

  SDR-Control    Wijs zijn logging uitzending in de haven 12060.

Als u meer dan een van deze tegelijk, wees voorzichtig niet hetzelfde QSO de
Gateway twee keer bereiken - bijvoorbeeld WSJT-X rechtstreeks uitzenden naar
de Gateway EN voeding N1MM, die het dan ook uitzendt. DXKeeper niet duplicaten
detecteren en zou beide loggen.


Het richten op DXKEEPER
-----------------------

Niets te configureren. The Gateway leest DXKeeper van Base Port instellen en
gebruiken. Als u verandert DXKeeper's Base Port (Config > Defaults tab >
Network Service), herstart de Gateway daarna.

Datzelfde paneel vertelt u of DXKeeper De netwerkdienst luistert. Als de
Gateway meldt dat het geen verbinding kan maken, kijk dan eerst daar.


WHAT THE WINDOW SHOWS
---------------------

  Settings          UDP port, optionele multicast groep, wat DXKeeper moet
                    doen met elk QSO (opzoeken van het telefoonboek, eQSL, LoTW, Club Log),
                    logopties en de interfacetaal.

  Connection Status DXKeeper, DXView en Pathfinder. Verbinding verbroken is normaal
                    voor programma's die u niet uitvoert.

  Operation Log     Wat de Gateway heeft gedaan, nieuwste onderaan. Problemen
                    zijn gekleurd. Dit is de eerste plek om te kijken, en de
                    Kopieknop zet het op het klembord voor een foutrapport.

Minimaliseren plaatst de Gateway in het meldingsgebied (door de klok) in
plaats van de taakbalk, waar het een lopende telling van wat het heeft
ontvangen en aangemeld houdt. Windows 11 Verbergt standaard nieuwe
notificatiepictogrammen - als u het wilt zien, sleept u het uit de "verborgen
pictogrammen" flyout naar de taakbalk. Het sluiten van het raam sluit de
Poort.


VERANDERING/DELETE QSOs EN UPLOAD TOGGLES
-----------------------------------------

Lees dit voordat u inschakelt Upload to eQSL.cc, Upload to LoTW of Upload to
Club Log.

Die toggles vertellen DXKeeper om elk te uploaden QSO naar het online logboek
zodra het is geregistreerd. Apart, de Gateway ondersteunt bewerken en
verwijderen QSOs: wanneer uw logger een wijziging stuurt, verwijdert de
Gateway de QSO van DXKeeper en logt de gecorrigeerde, omdat DXKeeper geen
enkele "vervang"-operatie heeft.

Deze twee functies combineren niet goed, en noch de Gateway noch DXKeeper Kan
ze maken. Een upload die al weg is, kan niet worden teruggeroepen. LoTW in het
bijzonder kan een QSO Je hebt geüpload. Dus een QSO geüpload en vervolgens
bewerkt laat het ORIGINEEL staan op LoTW Voor altijd, met de correctie
toegevoegd naast het in plaats van vervangen. A QSO geüpload en vervolgens
verwijderd blijft op LoTW nadat het uit je eigen logboek is gegaan.

Voordat de Gateway bewerken en verwijderen ondersteund, kon dit niet ontstaan:
elke QSO Het was definitief.

Wat moet ik eraan doen?

Het simpele antwoord, en degene die de auteur gebruikt, is om alle drie upload
toggles uitgeschakeld tijdens de wedstrijd, en te uploaden van DXKeeper met de
hand zodra het logboek definitief is en eventuele correcties zijn aangebracht.
DXKeeper uploadt een heel logboek zo gemakkelijk als één QSO, en dan is er
niets meer te corrigeren.

Schakel ze in als je wilt - de Gateway waarschuwt je eens en doet dan wat er
gezegd wordt - maar wees je ervan bewust dat een latere correctie het online
logboek niet zuiver zal bereiken.

Dit geldt niet voor Query Callbook of Lookup previous QSOs Die lezen alleen
maar.


BESTANDEN HET SCHRIFT
---------------------

Beide verschijnen in de eigen map van de Gateway. Als de Gateway is ergens
geïnstalleerd Windows laat het niet schrijven - onder C:\Program Bestanden,
bijvoorbeeld - het maakt gebruik van een per-user map in plaats daarvan en
registreert welke een aan de bovenkant van ErrorLog.txt.

  ErrorLog.txt          Diagnose. Een rode "see ErrorLog" link verschijnt in de
                        Als er iets geschreven is. Tik
                        "Log debugging information" voor veel meer detail wanneer
                        Een probleem achterna zitten.

  FailedQSOs_<date>_<time>.adi
                        QSOs DXKeeper niet bevestigd. BELANGRIJK: de Gateway
                        nooit stilletjes een QSO, maar het ook nooit
                        opnieuw een, omdat DXKeeper detecteert niet
                        duplicaten en een herhaling kan het twee keer loggen. Als dit
                        bestand bestaat, importeer het in DXKeeper met de hand en dan
                        Verwijder het."Failed QSOs" onderaan het raam
                        wordt rood met een telling wanneer dit gebeurt; klik erop
                        open de map met het geselecteerde bestand. De telling gaat
                        terug naar nul als het bestand weg is.

                        Eén bestand per run. Een run die niets verliest laat geen
                        bestand, zodat het bestaande bestand altijd iets betekent
                        Ik heb je aandacht nodig.


IF A QSO NEEMT NIET AAN
-----------------------

  - Doet de Operation Log toon de QSO ontvangen? Zo niet, dan is de logger
    de poort niet bereiken: controleer het adres en de poort, en controleer een firewall
    blokkeert niet UDP.

  - Staat het erop dat het verstuurd wordt maar niet bevestigd? DXKeeper niet bevestigd
    Het. Controleren DXKeeper Hij loopt en dat zijn Network Service zegt Listening.
    De QSO zal binnen zijn FailedQSOs.

  - DXKeeper kan enkele seconden achterlopen tijdens een drukke wedstrijd. De poort
    stuurt een QSO per keer en wacht op DXKeeper om elk te bevestigen, dus een
    De achterstand is normaal en loopt vanzelf weg.


TAAL
----

De Gateway volgt uw Windows-schermtaal als het een vertaling heeft, en u kunt
er expliciet een kiezen onder Instellingen > Generaal. Een verandering wordt
de volgende keer van kracht.

Andere vertalingen dan Engels zijn machine-made en worden gecorrigeerd door
vrijwilligers. Als de jouwe slecht leest, zijn correcties zeer welkom - en de
naam van de vertaler verschijnt in het venster Over.


Vergunning
----------

Vrije software onder de GNU General Public License versie 3 of later, met
Absoluut geen garantie. De volledige tekst is in COPYING.txt; NOTICE.txt het
auteursrecht, de onderdelen van derden en hun licenties registreert.

U kunt het gebruiken voor elk doel, bestuderen hoe het werkt, delen en
wijzigen.


HELP
----

  Documentation   https://ny4i.com/n1mm-dxkeeper-gateway/
  Questions       DXLab discussiegroep, DXLab@groups.io

Bij het rapporteren van een probleem, het over venster "Copy details" knop zet
de versie en uw omgeving op het klembord. Vermeld dat en het relevante deel
van de Operation Log of ErrorLog.txt.
