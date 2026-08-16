==============================================================================
 MACHINE TRANSLATION into cs. The English README.txt is
 authoritative; where this file disagrees with it, it is this file
 that is wrong. Corrections are very welcome.

 source-sha256: e67ddcc749a9e9c5
==============================================================================

N1MM-DXKeeper Gateway 2.0
=========================

Lodě pro přepravu nákladu QSOs přihlášen TR4W nebo N1MM Logger+ přímo do
DXKeeper, a ptá se DXView a Pathfinder Vyhledat volací značky, na kterých
pracuješ.

Úplná dokumentace: https://ny4i.com/n1mm-dxkeeper-gateway/


ČEMU MUSÍTE VĚNOVAT POZORNOST, NEŽ ZAČNETE ZAČNETE
--------------------------------------------------

1. DXKeeper musí být nainstalováno. Brána nedělá nic sám; je to
   Do toho.

2. Microsoft .NET 8 DESKTOP runtime, 64-bit (x64).

   Jestli už utečeš JTAlert 2.80 nebo později, máte to - JTAlert potřebuje
   To samé a musí to být nainstalováno jen jednou. Windows ji aktualizuje
   poté jako součást normální Windows Update.

   Pokud brána nezačne, nebo Windows nabízí jít hledat
   Něco tu chybí.

       https://dotnet.microsoft.com/download/dotnet/8.0

   Vyberte si "Desktop Runtime"x64. Ani SDK, ani pláň." NET
   Runtime "- Desktop Runtime je ten, který obsahuje, co tento program
   potřeby. Starší VB6 Gateway nepotřeboval žádnou takovou instalaci; tento je
   Přepsat a dělá.

3. Windows 10 nebo Windows 11.


Běžím.
------

Začněte od Start menu, nebo ze zkratky na ploše, pokud jste se zeptali
instalátoru pro jeden.

Start brány, DXKeeper, DXView, Pathfinder a váš záznamník v jakémkoli pořadí.
Gateway se ke každému připojí, jak se zdá.

Vaše nastavení žijí v registru Windows, pod stejným klíčem jako VB6 Gateway,
takže nastavení ze staré verze přenášejí sami.

• DXLab Launcher může spustit bránu vedle vašeho druhého DXLab programy; viz
"Specifikace non -DXLab Path name aplikace "téma v Launcher pomoc.


UMÍSTĚJTE NA TO SVŮJ LOGGER
---------------------------

Brána poslouchá dál UDP přístav 12060 standardně. Můžete to změnit v sekci Síť
v okně.

  N1MM Logger+   Config > Configure Ports ... > Broadcast Data Účet.
                 Zaškrtněte "Kontakty" a nastavte adresu vedle ní
                 počítače IPv4 adresa a port, např. 192.168.1.11:12060
                 Tik "External Callsign Lookup"a nastavit to stejným způsobem.

  TR4W           Nastavení UDP BROADCAST ADDRESS na stejnou adresu a přístav.

  WSJT-X         Settings > ReportingTik "Povolit přihlášený kontakt ADIF
                 vysílání "a zadejte svou IP adresu - 127.0.0.1 pokud WSJT-X vá
                 na stejném počítači - a 12060 ve Server port number
                 Pole.

                 Doporučujeme použít JTAlert místo toho, nebo poslat kontakty přímo
                 na DXLab žádosti; viz DXLab pokyny. Tohle
                 Trasa funguje, ale to jsou lepší cesty.

  SDR-Control    Bod jeho záznamu vysílání v přístavu 12060.

Pokud spustíte více než jeden z nich najednou, buďte opatrní, aby neměl stejný
QSO dostat brány dvakrát - například WSJT-X vysílání přímo do brány a krmení
N1MM, který pak vysílá to stejně. DXKeeper nedetekuje duplikáty a oba by
zaznamenal.


UMÍSTĚNÍ NA DXKeeper
--------------------

Není co konfigurovat. Brána čte DXKeeper vlastní Base Port nastavení a
použití. Pokud se změníte DXKeeper s Base Port (Config > Defaults tab >
Network Service), restartovat bránu později.

Ten samý panel vám řekne, jestli DXKeeper síťová služba poslouchá. Pokud
Gateway hlásí, že se nemůže připojit, podívejte se tam první.


CO UKAZUJE VÍTĚZ
----------------

  Settings          UDP port, volitelná multicast skupina, co DXKeeper by
                    dělat s každým QSO (Callbook lookup, eQSL, LoTW, Club Log),
                    možnosti logování a jazyk rozhraní.

  Connection Status DXKeeper, DXView a Pathfinder. Odpojení je normální
                    pro programy, které neprovozujete.

  Operation Log     Co Gateway udělal, nejnovější na dně. Problémy
                    jsou barevné. Toto je první místo, kde se podívat, a
                    Tlačítko kopírování ho umístí na schránku pro hlášení chyb.

Minimalizace umístí bránu do oznamovací oblasti (podle hodin) spíše než do
panelu úloh, kde vede běh počtu toho, co obdržela a přihlásila. Windows 11
skryje nové ikony oznámení ve výchozím nastavení - pokud ji chcete vidět,
vytáhněte ji z "skryté ikony" vyletět na taskbar. Zavření okna končí u brány.


ZMĚNA / DELETE QSOs A UPLOAD TOGGLES
------------------------------------

Přečtěte si to před zapnutím Upload to eQSL.cc, Upload to LoTW nebo Upload to
Club Log.

Ty přepínače říkají: DXKeeper nahrát každý QSO do on-line lodního deníku,
jakmile je přihlášen. Gateway samostatně podporuje editaci a mazání QSOs: když
váš záznamník pošle změnu, Brána odstraní QSO od DXKeeper a zaznamenává
opravené, protože DXKeeper nemá jedinou "náhradní" operaci.

Tyto dvě funkce nemají dobře kombinovat, a ani brány ani DXKeeper Můžu je
udělat. Nahrávání, které už vyšlo, nelze odvolat. LoTW zejména nemá žádný
způsob, jak odstranit QSO Nahrála jsi to. Takže QSO nahráno a pak editováno
opustí ORIGINAL stojící na LoTW na věky, s korekcí přidána vedle něj spíše než
nahradit. A QSO nahrát a pak smazat pobyty na LoTW Poté, co je pryč z vašeho
vlastního deníku.

Před Gateway podporované editace a mazání, to nemohlo vzniknout: každý QSO
Bylo to konečné.

Co s tím dělat

Přímočará odpověď, a ta, kterou autor používá, je nechat všechny tři upload
přepínače vypnuté během soutěže, a nahrávat od DXKeeper ručně, jakmile je
záznam konečný a všechny opravy byly provedeny. DXKeeper uploaduje celý log
tak snadno jako jeden QSO A už není co napravovat.

Zapněte je, pokud dáváte přednost - Gateway vás varuje jednou a pak dělá, co
se říká - ale uvědomte si, že pozdější oprava nedosáhne on-line lodní deník
čistě.

To se nevztahuje na Query Callbook nebo Lookup previous QSOs To se jen čte.


Napíše to.
----------

Oba se objeví ve složce brány. Pokud byl Gateway nainstalován někde Windows
nenechá psát - pod C:\Program Soubory, například - místo toho používá složku
per- user a zaznamenává, která je na vrcholu ErrorLog.txt.

  ErrorLog.txt          Diagnostika. Červená "see ErrorLog"odkaz se objeví v
                        okno, když je na něm něco napsáno. Tik
                        "Log debugging information"pro mnohem podrobnější, když
                        pronásledovat problém.

  FailedQSOs_<date>_<time>.adi
                        QSOs DXKeeper Nepotvrdil. DŮLEŽITÉ: Brána
                        nikdy tiše nevyhodí QSO, ale také nikdy
                        znovu jeden, protože DXKeeper nedetekuje
                        duplikáty a opakování by to mohlo zaznamenat dvakrát. Pokud
                        soubor existuje, importovat do DXKeeper ručně a pak
                        Vymaž to. "Failed QSOs"na dně okna
                        změní červené s počítat, když se to stane; klepněte na to
                        otevřít složku se zvoleným souborem. Hrabě jde
                        zpět na nulu, když je soubor pryč.

                        Jeden soubor za jeden běh. A run that loses nothing leaves no
                        soubor, takže existující soubor vždy něco znamená
                        potřebuje vaši pozornost.


IF A QSO NEDOSTAL
-----------------

  - Operation Log ukázat QSO Přijímáme? Pokud ne, logger je
    nedosáhne brány: zkontrolujte adresu a port, a zkontrolujte firewall
    neblokuje UDP.

  - Ukazuje to, že byl poslán, ale nepotvrzen? DXKeeper nepřiznal
    To. Kontrola DXKeeper je běží a že jeho Network Service říká Listening.
    • QSO bude v FailedQSOs.

  - DXKeeper může běžet několik sekund pozadu během rušné soutěže. Brána
    Odešle jeden QSO v čase a čeká na DXKeeper pro potvrzení každého, takže
    Backlog je normální a kanalizace sama.


JAZYK
-----

Gateway sleduje váš zobrazovací jazyk Windows, pokud má pro něj překlad, a
můžete si vybrat jeden explicitně pod Nastavení > Generále. Změna nabude
účinnosti, až příště začne.

Překlady jiné než angličtina jsou strojově zhotovené a opravené dobrovolníky.
Pokud vaše čtení špatně, opravy jsou velmi vítány - a jméno překladatele se
objeví v okně About.


LICENCE
-------

Volný software pod GNU General Public License verze 3 nebo novější, s ABSOLULY
NO WARRANTY. Celý text je v COPYING.txt; NOTICE.txt zaznamenává autorská
práva, komponenty třetích stran a jejich licence.

Můžete jej použít pro jakýkoli účel, studovat, jak funguje, sdílet a změnit.


Pomoc
-----

  Documentation   https://ny4i.com/n1mm-dxkeeper-gateway/
  Questions       DXLab Diskusní skupina, DXLab@groups.io

Když hlásíte problém, okno About je "Copy details"tlačítko umístí verzi a vaše
prostředí na schránky. Uveďte tuto a příslušnou část Operation Log nebo
ErrorLog.txt.
