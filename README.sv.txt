==============================================================================
 MACHINE TRANSLATION into sv. The English README.txt is
 authoritative; where this file disagrees with it, it is this file
 that is wrong. Corrections are very welcome.

 source-sha256: e67ddcc749a9e9c5
==============================================================================

N1MM-DXKeeper Gateway 2.0
=========================

Bär QSOs Inloggad TR4W eller N1MM Logger+ rakt in DXKeeper och frågar DXView
och Pathfinder för att titta upp callsigns du arbetar.

Full dokumentation: https://ny4i.com/n1mm-dxkeeper-gateway/


För dig START
-------------

1. DXKeeper måste installeras. Gateway gör ingenting på egen hand, det är en
   Go-between.

Microsoft .NET 8 DESKTOP Runtime, 64-bitars (x64).

   Om du redan kör JTAlert 2.80 eller senare har du det - JTAlert behov
   samma sak, och det måste bara installeras en gång. Windows håller den uppdaterad
   efteråt som en del av det normala Windows Update.

   Om Gateway inte startar eller Windows erbjuder att leta efter
   Något, detta är vad som saknas:

       https://dotnet.microsoft.com/download/dotnet/8.0

   Välj "Desktop Runtimex64. Inte SDK, och inte slätten.NET
   Runtime - The Desktop Runtime är den som innehåller vad detta program
   behov. Den tidigare VB6 Gateway behövde ingen sådan installation; den här är en
   skriva och göra.

3. Windows 10 eller Windows 11.


Runting it
----------

Starta den från Start menu, eller från skrivbordet genväg om du frågade
installatören för en.

Starta porten, DXKeeper, DXView, Pathfinder och din logger i någon ordning.
Gateway ansluter till varje som det verkar.

Dina inställningar bor i Windows-registret, under samma nyckel som VB6 Gateway
använde, så inställningar från den gamla versionen överförs av sig själva.

och DXLab Launcher kan starta Gateway tillsammans med din andra DXLab program,
se "Specifying a non-DXLab Applikationens namn" ämne i Launchers hjälp.


POINTING YOUR LOGGGER AT IT
---------------------------

Gateway lyssnar på UDP Portport 12060 Som standard. Du kan ändra det i
nätverksdelen av dess fönster.

  N1MM Logger+   Config > Configure Ports ... > > > > > Broadcast Data Tab.
                 Tick "Kontakter" och ange adressen bredvid den till din
                 datorns IPv4 adress och port, t.ex. 192.168.1.11:12060
                 Tick "External Callsign LookupOch sätt det på samma sätt.

  TR4W           Set UDP BROADCAST ADDRESS till samma adress och port.

  WSJT-X         Settings > ReportingTick "Aktivera inloggad kontakt ADIF
                 Sändning" och ange din IP-adress - 127.0.0.1 om WSJT-X är att
                 på samma dator - och 12060 i den Server port number
                 fältet.

                 Vi föreslår att du använder JTAlert istället, eller skicka kontakter direkt
                 till DXLab ansökningar; se DXLab instruktioner. Detta detta
                 rutten fungerar, men de är de bättre stigarna.

  SDR-Control    Peka sin loggning sänds i hamn 12060.

Om du kör mer än en av dessa på en gång, var noga med att inte ha samma QSO nå
porten två gånger - till exempel WSJT-X sändning direkt till porten och
matning N1MM som sedan sänder det också. DXKeeper Detekterar inte dubbletter
och loggar båda.


POINTING IT AT DXKEEPER
-----------------------

Inget att konfigurera. Gateway läser DXKeeper"Ett eget Base Port Inställning
och använder den. Om du ändrar DXKeeper"S Base Port (b)Config > Defaults tab >
Network Serviceomstarta porten efteråt.

Samma panel rubrik berättar om DXKeeper Nätverkstjänsten lyssnar. Om Gateway
rapporterar att den inte kan ansluta, titta där först.


Vad händerna
------------

  Settings          UDP Port, valfri multicast grupp, vad DXKeeper bör
                    gör med varje QSO (callbook lookup, eQSL, LoTW, Club Log),
                    loggningsalternativ och gränssnittsspråket.

  Connection Status DXKeeper, DXView och Pathfinder Disconnected är normalt
                    för program du inte kör.

  Operation Log     Vad Gateway har gjort, nyast på botten. Problem
                    är färgade. Detta är den första platsen att se, och den
                    Kopiera knappen sätter den på klippbordet för en buggrapport.

Minimering sätter porten i anmälningsområdet (av klockan) snarare än
aktivitetsfältet, där det håller ett löpande antal av vad det har fått och
loggat. Windows 11 döljer nya meddelandeikoner som standard - om du vill se
det, dra det ur "dolda ikoner" flyout på aktivitetsfältet. Stänga fönstret
slutar Gateway.


Change/delete QSOs OCH UPPLOAD TOGGLES
--------------------------------------

Läs detta innan du slår på Upload to eQSL.cc, Upload to LoTW eller Upload to
Club Log.

Dessa växlar berättar DXKeeper för att ladda upp varje QSO till online-
loggboken så snart den är inloggad. Separat stöder Gateway redigering och
borttagning QSOs När din logger skickar en förändring, tar porten bort QSO
Från DXKeeper och loggar den rättade, för DXKeeper Har ingen enskild "ersätt"
operation.

Dessa två funktioner kombinerar inte bra, och varken porten eller DXKeeper kan
göra dem. En uppladdning som redan har gått ut kan inte återkallas. LoTW I
synnerhet har inget sätt att radera en QSO Du har laddat upp. Så en QSO
uppladdade och sedan redigerade lämnar ORIGINAL stående på LoTW För alltid,
med korrigeringen läggs bredvid den snarare än att ersätta den. Ett QSO
uppladdade och sedan raderade vistelser på LoTW Efter det har det gått från
din egen logg.

Innan Gateway stödde redigering och borttagning kunde detta inte uppstå: varje
QSO Den loggade var slutgiltig.

Vad att göra om det

Det enkla svaret, och den författaren använder, är att lämna alla tre
uppladdningsväxlar bytte OFF medan de tävlar, och att ladda upp från DXKeeper
för hand när loggen är slutgiltig och eventuella korrigeringar har gjorts.
DXKeeper laddar upp en hel logg så enkelt som en QSO Och då finns det inget
kvar att korrigera.

Byt dem om du föredrar - Gateway varnar dig en gång och gör sedan som det
berättas - men var medveten om att en senare korrigering inte kommer att nå
online-loggboken rent.

Detta gäller inte för Query Callbook eller Lookup previous QSOs De läser bara.


FILES IT WRITES
---------------

Båda visas i Gateways egen mapp. Om porten installerades någonstans låter
Windows inte det skriva - under C:\Program Filer, till exempel - det använder
en per-user-mapp istället och registrerar vilken på toppen av ErrorLog.txt.

  ErrorLog.txt          Diagnostik. En röd "see ErrorLoglänk visas i
                        fönster när något har skrivits till det. Tick
                        "Log debugging informationför mycket mer detaljer när
                        Jagar ett problem.

  FailedQSOs_<date>_<time>.adi
                        QSOs DXKeeper bekräftade inte. VIKTIGT: Gateway
                        Aldrig tyst kastar en QSO Men det också aldrig
                        retries one, för DXKeeper Detekterar inte
                        dubbletter och en retry kan logga det två gånger. Om detta
                        fil existerar, importera den till DXKeeper för hand och sedan
                        ta bort den.”Failed QSOs På botten av fönstret
                        blir röd med ett räkning när detta händer; klicka på det för att
                        öppna mappen med den markerade filen. Räknet går
                        tillbaka till noll när filen är borta.

                        En fil per run. En körning som förlorar ingenting lämnar ingen
                        fil, så filen som finns betyder alltid något
                        behöver din uppmärksamhet.


Om en QSO Är inte ARRIVE
------------------------

  Gör det Operation Log visa QSO tas emot? Om inte, är loggern
    inte nå porten: kontrollera adressen och porten och kontrollera en brandvägg
    Blockerar inte UDP.

  Visar den att den skickas men inte bekräftas? DXKeeper inte erkänna
    Det. Kolla in DXKeeper är igång och att dess Network Service säger Listening.
    och QSO kommer att vara i FailedQSOs.

  - DXKeeper kan köra flera sekunder bakom under en upptagen tävling. Gateway
    Skickar en QSO i taget och väntar på DXKeeper för att bekräfta varje, så en
    backlog är normalt och dränerar på egen hand.


Lycka
-----

Gateway följer ditt Windows-visningsspråk om det har en översättning för det,
och du kan välja en uttryckligen under Inställningar> General. En förändring
träder i kraft nästa gång den börjar.

Översättningar än engelska är maskintillverkade och korrigeras av volontärer.
Om din läser dåligt, är korrigeringar mycket välkomna - och översättarens namn
visas i fönstret Om.


Licens
------

Fri programvara under GNU General Public License version 3 eller senare, med
ABSOLUTELY NO WARRANTY. Den fullständiga texten finns i COPYING.txt;
NOTICE.txt registrerar upphovsrätten, tredjepartskomponenterna och deras
licenser.

Du kan använda den för alla ändamål, studera hur den fungerar, dela den och
ändra den.


Hjälp
-----

  Documentation   https://ny4i.com/n1mm-dxkeeper-gateway/
  Questions       DXLab Diskussion Group, DXLab@groups.io

När du rapporterar ett problem, om fönstret "Copy details"Knappen sätter
versionen och din miljö på klippbordet. Vänligen inkludera detta och den
relevanta delen av Operation Log eller ErrorLog.txt.
