==============================================================================
 MACHINE TRANSLATION into sk. The English README.txt is
 authoritative; where this file disagrees with it, it is this file
 that is wrong. Corrections are very welcome.

 source-sha256: e67ddcc749a9e9c5
==============================================================================

N1MM-DXKeeper Gateway 2.0
=========================

Carries QSOs prihlásený TR4W alebo N1MM Logger+ priamo do DXKeeper, a pýta sa
DXView a Pathfinder aby ste si pozreli telefónne značky, ktoré pracujete.

Úplná dokumentácia: https://ny4i.com/n1mm-dxkeeper-gateway/


SKÔR AKO ZAČNETE
----------------

1. DXKeeper musia byť nainštalované. Brána nerobí nič sama o sebe, je to
   medzi.

2. Microsoft .NET 8 DESKTOP Runtime, 64-bit (x64).

   Ak už bežíte JTAlert 2.80 alebo neskôr, máte to - JTAlert potrebuje
   To isté a musí byť nainštalované len raz. Windows ho aktualizuje
   potom ako súčasť normálu Windows Update.

   Ak Brána nezačne, alebo Windows ponúka ísť hľadať
   Niečo, to je to, čo chýba:

       https://dotnet.microsoft.com/download/dotnet/8.0

   Vybrať "Desktop Runtime"x64. Nie SDK, a nie rovina ".NET
   Runtime" - Desktop Runtime je ten, ktorý zahŕňa, čo tento program
   Potreby. Čím skôr VB6 Gateway nepotreboval žiadnu takúto inštaláciu; tento je
   Prepisuje a prepisuje.

3. Windows 10 alebo Windows 11.


Running it
----------

Začnite od Start menu, alebo zo skratky plochy, ak ste požiadali inštalátora o
jeden.

Štartujte Bránu, DXKeeper, DXView, Pathfinder a váš denník v každom poradí.
Brána sa spája s každou ako sa zdá.

Vaše nastavenia žijú v registri Windows, pod rovnakým kľúčom použitý VB6
Gateway, takže nastavenia zo starej verzie prenášajú samy.

EÚ DXLab Launcher môže spustiť Bránu vedľa vašej druhej DXLab programy; pozri
"Specification a non-DXLab Tematický názov aplikácie" v Pomoci Launcher.


NABUDÚC NA VÁŠHO AGENTÚRA
-------------------------

Brána počúva UDP prístav 12060 automaticky. Môžete to zmeniť v sekcii siete
jej okna.

  N1MM Logger+   Config > Configure Ports ... > Broadcast Data karta.
                 Označte "Kontakty" a nastavte adresu vedľa neho
                 počítačové IPv4 adresa a prístav, napr. 192.168.1.11:12060
                 Tik "External Callsign Lookup"a nastaviť to rovnako.

  TR4W           Nastaviť UDP BROADCAST ADDRESS na rovnakú adresu a prístav.

  WSJT-X         Settings > Reporting. Tick "Umožniť prihlásený kontakt ADIF
                 vysielanie" a zadajte svoju IP adresu - 127.0.0.1 ak WSJT-X ci
                 na rovnakom počítači - a 12060 v Server port number
                 Pole.

                 Odporúčame vám použiť JTAlert namiesto toho, alebo poslať kontakty priamo
                 k DXLab žiadosti; pozri DXLab pokyny. Toto
                 Trasa funguje, ale to sú lepšie cestovanie.

  SDR-Control    Nasmerujte jeho záznam v prístave 12060.

Ak spustíte viac ako jeden z nich naraz, buďte opatrní, aby ste nemali rovnaké
QSO dosiahnuť Gateway dvakrát - napríklad WSJT-X vysielanie priamo do Brány A
kŕmenie N1MM, ktorý potom vysiela to rovnako. DXKeeper nezisťuje duplikáty a
by logovať oboje.


BODOVANIE NA DXKEEPER
---------------------

Nie je čo nastaviť. Brána číta DXKeeper's vlastnou Base Port nastavenie a
použitie. Ak sa zmení DXKeeper's Base Port (Config > Defaults tab > Network
Service), potom reštartujte Bránu.

Ten istý panel vám povie, či DXKeeper Sieťová služba počúva. Ak sa Brána
hlási, že sa nemôže pripojiť, pozrite sa najprv tam.


ČO UKAZUJE OKNÁ
---------------

  Settings          UDP prístav, voliteľná multicastová skupina, čo DXKeeper -
                    do každého QSO (vyhľadávanie Callbook, eQSL, LoTW, Club Log),
                    Možnosti prihlásenia a jazyk rozhrania.

  Connection Status DXKeeper, DXView a Pathfinder. Odpojený je normálny
                    pre programy, ktoré nebeží.

  Operation Log     Čo urobila Brána, najnovšie na dne. Problémy
                    sú farebné. Toto je prvé miesto na pohľad, a
                    Skopírovacie tlačidlo ho dá do schránky pre hlásenie o chybe.

Minimalizácia stavia Bránu do oblasti hlásenia (v čase) skôr ako do panela
úloh, kde udržuje bežiaci počet toho, čo dostala a zaznamenala. Windows 11
skrýva nové ikony oznámenia predvolene - ak ho chcete vidieť, presuňte ho z
"skryté ikony" na panel úloh. Zatváranie okna opúšťa Bránu.


ZMENA/DELETE QSOs A VYSOKÉ VAJCE
--------------------------------

Prečítajte si to pred zapnutím Upload to eQSL.cc, Upload to LoTW alebo Upload
to Club Log.

Tí, ktorí hovoria DXKeeper Každý QSO do online lodného denníka hneď, ako sa
prihlási. Samostatne, Brána podporuje editáciu a vymazanie QSOs: keď váš
logger pošle zmenu, Brána odstráni QSO od DXKeeper a zaznamená opravenú,
pretože DXKeeper nemá žiadnu "nahradenú" operáciu.

Tieto dve funkcie nie sú dobre kombinovať, a ani Brána, ani DXKeeper môže ich
vyrobiť. Upload, ktorý už vyšiel, sa nedá odvolať. LoTW Najmä nemá žiadny
spôsob, ako odstrániť QSO Nahral si to. Takže a QSO nahrané a potom upravené
listy ORIGINÁL stojí na LoTW Navždy, s korekciou pridané vedľa neho skôr než
nahradiť. A QSO nahral a potom vymazal zostane v LoTW po tom, čo to zmizlo z
tvojho vlastného denníka.

Pred tým, než Brána podporila editáciu a vymazanie, to nemohlo vzniknúť: každý
QSO Prihlásilo sa to ako posledné.

ČO ROBIŤ S TÝM

Jednoduchá odpoveď, a ten, ktorý autor používa, je nechať všetky tri upload
toggles vypnuté počas súťaže, a nahrať z DXKeeper ručne po ukončení záznamu a
po vykonaní opráv. DXKeeper uploaduje celý log rovnako ľahko ako jeden QSO, a
potom už nie je čo opraviť.

Zapnite ich, ak chcete - Brána vás raz varuje a potom urobí, čo sa hovorí -
ale buďte si vedomí toho, že neskoršia oprava nedosiahne online lodný denník
čisto.

Toto sa nevzťahuje na: Query Callbook alebo Lookup previous QSOs Tie len
čítajú.


PRIPÍSA
-------

Oba sa objavujú vo vlastnej zložke Brány. Ak bola Brána nainštalovaná niekde,
Windows ju nenechá zapísať - pod C:\Program Súbory, napríklad - namiesto toho
používa priečinok na užívateľa a zaznamenáva, ktorý v hornej časti
ErrorLog.txt.

  ErrorLog.txt          Diagnostika. Červená "see ErrorLog" odkaz sa objavuje v
                        Okno, keď mu bolo niečo napísané. Tik
                        "Log debugging information"pre viac detailov, keď
                        naháňať problém.

  FailedQSOs_<date>_<time>.adi
                        QSOs DXKeeper nepotvrdil. DÔLEŽITÉ: Brána
                        a QSO, ale tiež nikdy
                        napíše jeden, pretože DXKeeper nedetekuje
                        Duplikáty a opakovanie by to mohlo zaznamenať dvakrát. Ak
                        súbor existuje, import do DXKeeper ručne a potom
                        Vymaž to. "Failed QSOs"v dolnej časti okna
                        zmení červenú s počtom, keď sa to stane; kliknite na
                        Otvoriť priečinok s vybraným súborom. Počet ide
                        späť na nulu, keď je súbor preč.

                        Jeden súbor na jeden pokus. Beh, ktorý nič nestratí.
                        súbor, takže súbor existuje vždy niečo znamená
                        potrebuje tvoju pozornosť.


AK A QSO NEPODÁVAJÚ
-------------------

  - Robí Operation Log Zobraziť QSO byť prijatý? Ak nie, logger je
    nedosiahnutia Brány: skontrolujte adresu a prístav a skontrolujte firewall
    neblokuje UDP.

  - Ukazuje to, že je poslaný, ale nie potvrdený? DXKeeper neuznal
    To. Poraďte DXKeeper beží a že jeho Network Service hovorí Listening.
    EÚ QSO bude FailedQSOs.

  - DXKeeper môže bežať niekoľko sekúnd pozadu počas rušnej súťaže. Brána
    posiela jeden QSO v čase a čaká na DXKeeper na potvrdenie každého, takže
    backlog je normálny a odtok sám.


JAZYK
-----

Gateway sa riadi vaším jazykom Windows, ak má na to preklad, a môžete si ho
výslovne vybrať v Nastaveniach > Generál. Zmena nadobudne účinnosť, keď sa
nabudúce začne.

Iné preklady ako angličtina sú strojovo vyrábané a opravované dobrovoľníkmi.
Ak vaše číta zle, opravy sú veľmi vítané - a meno prekladateľa sa nachádza v
okne.


LICENCIA
--------

Bezplatný softvér podľa GNU General Public License verzie 3 alebo neskôr, s
ABSOLUTELY NO WARRANTY. Celý text COPYING.txt; NOTICE.txt zaznamenáva autorské
práva, komponenty tretích strán a ich licencie.

Môžete ho používať na akýkoľvek účel, študovať ako funguje, deliť sa oň a
meniť ho.


POMOC
-----

  Documentation   https://ny4i.com/n1mm-dxkeeper-gateway/
  Questions       DXLab Diskusná skupina DXLab@groups.io

Pri podávaní správ o probléme "Copy details" Tlačidlo dáva verziu a vaše
prostredie do schránky. Uveďte túto a príslušnú časť Operation Log alebo
ErrorLog.txt.
