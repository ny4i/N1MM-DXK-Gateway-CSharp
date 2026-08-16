==============================================================================
 MACHINE TRANSLATION into nb. The English README.txt is
 authoritative; where this file disagrees with it, it is this file
 that is wrong. Corrections are very welcome.

 source-sha256: e67ddcc749a9e9c5
==============================================================================

N1MM-DXKeeper Gateway 2.0
=========================

Carries QSOs Logget inn TR4W eller N1MM Logger+ Rett inn i DXKeeper, og spør
DXView og Pathfinder å se på kallene du jobber.

Full dokumentasjon: https://ny4i.com/n1mm-dxkeeper-gateway/


Før du starter
--------------

1. DXKeeper Må installeres. Gateway gjør ingenting på egen hånd; det er en
   Gå mellom.

2. Microsoft .NET 8 DESKTOP Runtime, 64-bit (x64).

   Hvis du allerede kjører JTAlert 2.80 eller senere har du det - JTAlert trenger
   Det samme, og det må bare installeres én gang. Windows holder det oppdatert
   etterpå som en del av normalen Windows Update..

   Hvis Gateway ikke vil starte, eller Windows tilbyr å gå på jakt etter
   noe, det er det som mangler:

       https://dotnet.microsoft.com/download/dotnet/8.0

   Velg "Desktop Runtimex64. Ikke SDK, og ikke sletten.NET
   Runtime - den Desktop Runtime Det er den som inkluderer det programmet
   behov. Den tidligere VB6 Gateway trengte ingen slik installasjon; denne er en
   omskriv og gjør.

3. Windows 10 eller Windows 11..


RUNNING IT
----------

Start det fra Start menu, eller fra skrivebordssnarveien hvis du spør
installasjonsprogrammet om en.

Start porten, DXKeeper, DXView, Pathfinder og loggeren din i enhver
rekkefølge. Gateway kobles til hver som den vises.

Dine innstillinger bor i Windows-registeret, under samme nøkkel som
VB6-gatewayen brukte, så innstillinger fra den gamle versjonen overføres av
seg selv.

Den DXLab Launcher kan starte gateway sammen med din andre DXLab programmer;
se - Specifying en ikke-DXLab programmets stinavn" emne i Launchers hjelp.


POINTING DIN LOGGER på det
--------------------------

Gatewayen hører på UDP port 12060 som standard. Du kan endre det i
nettverksdelen av vinduet.

  N1MM Logger+   Config > Configure Ports ... > Broadcast Data tab.
                 Tick " Kontakter" og angi adressen ved siden av den til din
                 Computers IPv4 Adresse og havn, f.eks. 192.168.1.11:12060
                 Tick "External Callsign Lookup" og sett det på samme måte.

  TR4W           Sett UDP BROADCAST ADDRESS til samme adresse og havn.

  WSJT-X         Settings > Reporting. Tick " Aktiver logget kontakt ADIF
                 sending" og skriv inn IP-adressen din - 127.0.0.1 hvis WSJT-X er
                 på samme datamaskin - og 12060 i Server port number
                 felt.

                 Vi foreslår at du bruker JTAlert i stedet, eller sende kontakter direkte
                 til DXLab applikasjoner; se DXLab instruksjoner. Dette
                 ruten fungerer, men det er de bedre reisende stiene.

  SDR-Control    Punkt på loggføringen i havn 12060..

Hvis du kjører mer enn én av disse samtidig, vær forsiktig med å ikke ha det
samme QSO å nå Gateway to ganger - for eksempel WSJT-X Kringkasting direkte
til Gateway OG fôring N1MM som så også sender den. DXKeeper ikke oppdager
dupliserer og ville logge begge.


POINTING I DXKEPER
------------------

Ingenting å konfigurere. Gatewayen leser DXKeeper egen Base Port innstilling
og bruk det. Hvis du endrer deg DXKeeper's Base Port (Config > Defaults tab >
Network Service), starte gateway etterpå.

Det samme panelets overskrift forteller deg om DXKeeper Nettverkstjenesten
lytter. Hvis Gateway rapporterer at den ikke kan koble til, se det først.


HVA WINDOW SHOWS
----------------

  Settings          UDP port, valgfri multicast gruppe, hva DXKeeper bør
                    Gjør med hver QSO (kallebokoppslag, eQSL, LoTW, Club Log),
                    Loggealternativer og grensesnittspråk.

  Connection Status DXKeeper, DXView og Pathfinder Koble fra er normalt
                    For programmer du ikke kjører.

  Operation Log     Hva Gateway har gjort, nyeste nederst. Problemer
                    De er farget. Dette er det første stedet å se på og
                    Kopier-knappen legger den på utklippstavlen for en feilmelding.

Minimerer setter Gateway i varslingsområdet (ved klokken) i stedet for
oppgavelinjen, hvor det holder et løpende antall av hva det har mottatt og
logget. Windows 11 skjuler nye varslingsikoner som standard - hvis du vil se
det, dra det ut av the skjult ikoner" fly ut på oppgavelinjen. Lukking av
vinduet avslutter Gateway.


Endring/DELETE QSOs OG UPLOAD TOGGLES
-------------------------------------

Les dette før du slår på Upload to eQSL.cc, Upload to LoTW eller Upload to
Club Log..

De slår forteller DXKeeper å laste opp hver QSO til nettloggen så snart den er
logget. Separat støtter Gateway redigering og sletting QSOs Når loggeren
sender en endring, sletter Gateway QSO fra DXKeeper og logger den rette, fordi
DXKeeper har ingen enkel operation replace" operasjon.

Disse to funksjonene kombinerer ikke godt, og verken gateway eller DXKeeper
kan gjøre dem. En opplastning som allerede har gått ut kan ikke huskes. LoTW
Spesielt har ingen måte å slette en QSO Du har lastet opp. Så a QSO Lastet opp
og redigert forlater ORIGINAL stående på LoTW for alltid, med rettelsen lagt
ved siden av det i stedet for å erstatte det. A QSO Lastet opp og deretter
slettet opphold på LoTW Etter at det har gått fra din egen logg.

Før Gateway støttet redigering og sletting, kunne dette ikke oppstå: hver QSO
Den logget var endelig.

Hva skal man gjøre om det

Den enkle svaret, og den som forfatteren bruker, er å forlate alle tre
opplastingsbrytere byttet OFF mens du konkurrerer, og å laste fra DXKeeper For
hånd når loggen er endelig og eventuelle rettelser er gjort. DXKeeper Laster
opp en hel logg så enkelt som en QSO Og til da er det ingenting igjen å rette.

Slå dem på hvis du foretrekker - Gateway advarer deg en gang og deretter gjør
som det er fortalt - men vær oppmerksom på at en senere rettelse ikke vil nå
online loggbok rent.

Dette gjelder ikke Query Callbook eller Lookup previous QSOs De som bare
leser.


FILER DET VARER
---------------

Begge vises i gateways egen mappe. Hvis Gateway ble installert et sted Windows
ikke la det skrive - under C:\Program Filer, for eksempel - det bruker en per-
bruker mappe i stedet og poster som en øverst på ErrorLog.txt..

  ErrorLog.txt          Diagnostika. Rød "see ErrorLogLinken vises i
                        vinduet når det er skrevet noe til det. Tick
                        "Log debugging informationFor mye mer detalj når
                        Jeg jakter på et problem.

  FailedQSOs_<date>_<time>.adi
                        QSOs DXKeeper Ikke bekreftet. VIGTIG: Gateway
                        aldri stille og stille kaster en QSO, men det heller aldri
                        en, fordi DXKeeper Oppdager ikke
                        Duplikater og et forsøk kan logge det to ganger. Hvis dette
                        filen eksisterer, importere den til DXKeeper For hånd og deretter
                        Slett den.Failed QSOs" nederst i vinduet
                        blir rødt med en telling når dette skjer; klikk det til
                        Åpne mappen med den valgte filen. Antallet går
                        tilbake til null når filen er borte.

                        En fil per løp. En løp som ikke taper noe etterlater ingen
                        fil, så filen eksisterende alltid betyr noe
                        trenger oppmerksomhet.


IF A QSO Gjør ikke ARRIVE
-------------------------

  - Gjør det Operation Log Vis QSO Er du mottatt? Hvis ikke, er loggeren
    ikke når Gateway: Sjekk adresse og port, og sjekk en brannmur
    er ikke blokkering UDP..

  - Viser det det å bli sendt, men ikke bekreftet? DXKeeper Ikke anerkjent
    Det. Sjekk DXKeeper løper og det er Network Service sier Listening..
    Den QSO vil være i FailedQSOs..

  - DXKeeper Kan kjøre flere sekunder bak under en travel konkurranse. Gateway
    Sender en QSO En gang og venter på DXKeeper å bekrefte hver, så a
    backlog er normalt og drenerer på egen hånd.


LANGUAGE
--------

Gateway følger Windows-displayspråket hvis det har en oversettelse for det, og
du kan velge en eksplisitt under Innstillinger > Generelt. En endring trer i
kraft neste gang den starter.

Oversettelser utenom engelsk er maskinfremstillede og korrigeret av
frivillige. Hvis din leser dårlig, er rettelser svært velkomne - og
oversetterens navn vises i Om-vinduet.


LICENCE
-------

Gratis programvare under GNU General Public License versjon 3 eller nyere, med
ABSOLUTELY INGEN GARANTI. Den fulle teksten er i COPYING.txt; NOTICE.txt
registrerer opphavsretten, tredjepartskomponentene og deres lisenser.

Du kan bruke det til ethvert formål, studere hvordan det fungerer, dele det og
endre det.


Help
----

  Documentation   https://ny4i.com/n1mm-dxkeeper-gateway/
  Questions       DXLab Diskusjonsgruppe, DXLab@groups.io

Når du rapporterer et problem, om vinduetsCopy details" knappen setter
versjonen og miljøet på utklippstavlen. Ta med det og den relevante delen av
Operation Log eller ErrorLog.txt..
