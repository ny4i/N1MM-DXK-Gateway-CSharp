==============================================================================
 MACHINE TRANSLATION into fi. The English README.txt is
 authoritative; where this file disagrees with it, it is this file
 that is wrong. Corrections are very welcome.

 source-sha256: e67ddcc749a9e9c5
==============================================================================

N1MM-DXKeeper Gateway 2.0
=========================

Carries QSOs kirjautunut sisään TR4W tai N1MM Logger+ suoraan DXKeeper, ja
kysyy DXView sekä Pathfinder Etsimään puhelumerkkejäsi.

Täydelliset asiakirjat: https://ny4i.com/n1mm-dxkeeper-gateway/


ENNEN KUIN aloitat
------------------

1. DXKeeper on asennettava. Gateway ei tee mitään yksin; se on
   Mene välikäteen.

2. Microsoft.NET 8 DESKTOP Runtime, 64-bittinen (x64).

   Jos jo juokset JTAlert 2,80 tai myöhemmin. JTAlert on
   Sama asia, ja se on asennettava vain kerran. Windows pitää sen ajan tasalla
   jälkeen osana normaalia Windows Update.

   Jos Gateway ei käynnisty, tai Windows tarjoaa mennä etsimään
   Tästä puuttuu jotain:

       https://dotnet.microsoft.com/download/dotnet/8.0

   Valitse "Desktop Runtime"x64. Ei SDK, eikä tavallinen ".NET
   Runtime" - Desktop Runtime on se, joka sisältää mitä tämä ohjelma
   tarpeen. Aiemmin VB6 Gateway ei tarvinnut tällaista asennusta; tämä on
   Kirjoittaa ja tekee.

3. Windows 10 tai Windows 11.


- En tiedä.
-----------

Aloita Start menu, tai työpöydän pikanäppäintä, jos pyydät asentajalta yhden.

Käynnistä portti. DXKeeper, DXView, Pathfinder ja kirjailijasi missä tahansa
järjestyksessä. Portti on yhteydessä kaikkiin.

Asetuksesi elävät Windowsin rekisterissä, saman avaimen alla, jota VB6 Gateway
käytti, joten vanhan version asetukset siirtyvät itsestään.

• DXLab Launcher voi käynnistää Gateway rinnalla toisen DXLab Ohjelmat;
ks.DXLab sovelluksen polkunimi" aihe Launcherin apuna.


Longgerille.
------------

Gateway kuuntelee UDP satama 12060 Oletuksena. Voit muuttaa sen verkko-osassa
sen ikkunassa.

  N1MM Logger+   Config > Configure Ports ... > Broadcast Data Katso.
                 Valitse "Yhteys" ja aseta osoite sen viereen
                 tietokoneen IPv4 osoite ja satama, esim. 192.168.1.11:12060
                 Tik "External Callsign Lookup"ja aseta se samalla tavalla.

  TR4W           Aseta UDP BROADCAST ADDRESS samaan osoitteeseen ja satamaan.

  WSJT-X         Settings > Reporting. Tick "Ota kirjautunut yhteys ADIF
                 Lähetä" ja syötä IP-osoitteesi - 127.0.0.1 jos WSJT-X on
                 tällä samalla tietokoneella - ja 12060 ae Server port number
                 Kenttä.

                 Ehdotamme, että käytät JTAlert sen sijaan, tai lähettää yhteystietoja suoraan
                 ja DXLab sovellukset; ks. DXLab ohjeet. Tämä
                 Reitti toimii, mutta ne ovat paremmin kuljettuja polkuja.

  SDR-Control    Osoita sen hakkuulähetys satamaan 12060.

Jos käytät enemmän kuin yhden näistä kerralla, ole varovainen olla samaa QSO
Päästä Gateway kahdesti - esimerkiksi WSJT-X lähetys suoraan Gateway JA
ruokinta N1MM, joka sitten lähettää sen myös. DXKeeper ei havaitse
kaksoiskappaleita ja kirjaisi molemmat.


Osoitan sen DXKEEPERille.
-------------------------

Ei mitään konfiguroitavaa. Portti lukee DXKeeper Oma Base Port asettaa ja
käyttää sitä. Jos muutut DXKeeper's Base Port (Config > Defaults tab > Network
Service), käynnistä portti jälkeenpäin.

Sama paneeli kertoo, DXKeeper Verkkopalvelu kuuntelee. Jos Gateway ilmoittaa,
ettei se ole yhteydessä, katsokaa ensin.


Mitä Ikkuna näyttää?
--------------------

  Settings          UDP satama, valinnainen monilähetysryhmä, mitä DXKeeper tulisi
                    tehdä kunkin QSO (puhelukirjan haku) eQSL, LoTW, Club Log),
                    kirjautuminen vaihtoehtoja ja käyttöliittymän kieli.

  Connection Status DXKeeper, DXView sekä Pathfinder. Yhteys katkeaa normaalisti
                    ohjelmia, joita et suorita.

  Operation Log     Mitä Gateway on tehnyt, uusin pohjalla. Ongelmat
                    ovat värillisiä. Tämä on ensimmäinen paikka etsiä, ja
                    Kopionappi laittaa sen leikepöydälle vikaraporttia varten.

Minimointi laittaa Gateway ilmoitusalueella (kello) eikä tehtäväpalkissa,
jossa se pitää käynnissä laskea, mitä se on saanut ja kirjautunut. Windows 11
piilottaa uusia ilmoitus kuvakkeet oletuksena - jos haluat nähdä sen, vedä se
"piilotettu kuvakkeet" lentää tehtäväpalkkiin. Ikkunan sulkeminen lopettaa
portin.


MUUTOS/VASTUU QSOs JA KESKUSTELUT
---------------------------------

Lue tämä ennen kuin käynnistät Upload to eQSL.cc, Upload to LoTW tai Upload to
Club Log.

Nuo tikkarit kertovat DXKeeper lataa jokainen QSO verkkopäiväkirjaan heti kun
se on rekisteröity. Erikseen Gateway tukee muokkaamista ja poistamista QSOs:
Kun kirjautuja lähettää muutoksen, Gateway poistaa QSO alkaen DXKeeper ja
lokit korjattu yksi, koska DXKeeper jossa ei ole yhtä "korvaavaa" operaatiota.

Nämä kaksi piirrettä eivät yhdisty hyvin, eikä Gateway eikä DXKeeper Hän voi
tehdä niitä. Latausta, joka on jo lähtenyt, ei voida palauttaa. LoTW
erityisesti ei voi poistaa QSO Olet ladannut. A QSO ladattu ja sitten muokattu
jättää ALKUPERÄINEN seisoo LoTW iäksi, ja korjaus lisätään sen viereen eikä
korvata sitä. A QSO ladattu ja sitten poistettu pysyy osoitteessa LoTW Kun se
on lähtenyt omasta lokikirjastasi.

Ennen kuin Gateway tuki editointia ja poistamista, tämä ei voinut syntyä: QSO
Se oli lopullinen.

Mitä tehdä asialle?

Yksinkertainen vastaus, ja yksi kirjailija käyttää, on jättää kaikki kolme
ladata wiggles kytketty OFF kilpaillen, ja ladata DXKeeper käsin, kun loki on
lopullinen ja mahdolliset korjaukset on tehty. DXKeeper lataa koko lokin yhtä
helposti QSO Sitten ei ole mitään korjattavaa.

Laita ne päälle, jos haluat - Gateway varoittaa sinua kerran ja sitten tekee
kuten on kerrottu - mutta muista, että myöhemmin korjaus ei pääse online-
päiväkirjan puhtaaksi.

Tätä ei sovelleta Query Callbook tai Lookup previous QSOs Ne vain lukevat.


-Kyllä.
-------

Molemmat näkyvät Gatewayn omassa kansiossa. Jos Gateway asennettiin jonnekin
Windows ei anna sen kirjoittaa - alla C:\Program Tiedostot, esimerkiksi - se
käyttää käyttäjä-kansion sijaan ja tallentaa joka yksi alkuun ErrorLog.txt.

  ErrorLog.txt          Diagnoosi. Punainen.see ErrorLog"linkki näkyy
                        Ikkuna, kun siihen on kirjoitettu jotain. Tik
                        "Log debugging information" paljon tarkemmin kun
                        Jahtaan ongelmaa.

  FailedQSOs_<date>_<time>.adi
                        QSOs DXKeeper ei vahvistanut. TÄRKEÄÄ: portti
                        ei koskaan hiljaa poisheitettyä QSO, mutta se ei myöskään koskaan
                        retries yksi, koska DXKeeper ei havaitse
                        Kaksoiskappaleet ja uusi yritys voivat kirjautua kahdesti. Jos
                        tiedosto on olemassa, tuo se DXKeeper käsin ja sitten
                        Poista se."Failed QSOs" ikkunan alaosassa
                        muuttuu punaiseksi kun tämä tapahtuu; klikkaa sitä
                        Avaa kansio valitulla tiedostolla. Kreivi lähtee.
                        Takaisin nollaan, kun tiedosto on poissa.

                        Yksi tiedosto per keikka. Juoskaa, joka ei menetä mitään.
                        tiedosto, joten olemassa oleva tiedosto tarkoittaa aina jotain
                        Tarvitsen huomiotasi.


A QSO EI SAAVUTA
----------------

  - Onko Operation Log näytä QSO vastaanotetaanko? Jos ei, kirjautuja on
    ei pääse Gateway: tarkista osoite ja portti, ja tarkista palomuuri
    ei estä UDP.

  - Näyttääkö se, että se lähetetään, mutta ei vahvistettu? DXKeeper ei tunnustanut
    Se. Tarkista DXKeeper on käynnissä ja että sen Network Service sanoo Listening.
    • QSO tulee FailedQSOs.

  - DXKeeper voi olla useita sekunteja jäljessä kiireisen kilpailun aikana. Portti
    lähettää yhden QSO kerrallaan ja odottaa DXKeeper vahvistaa kunkin, niin a
    Ruuhka on normaali ja tyhjenee itsestään.


KIELI
-----

Gateway seuraa Windows-näyttökieltäsi, jos sillä on käännös sille, ja voit
valita yhden erikseen kohdasta Asetukset > Kenraali. Muutos tulee voimaan ensi
kerralla.

Muut käännökset kuin englanti ovat koneellisia ja vapaaehtoisia korjattavia.
Jos omasi lukee huonosti, korjaukset ovat erittäin tervetulleita - ja
kääntäjän nimi näkyy About-ikkunassa.


LUPA
----

Ilmainen ohjelmisto GNU General Public License version 3 tai uudempi, jossa ei
ole WARRANTY. Koko teksti on COPYING.txt; NOTICE.txt tallentaa
tekijänoikeudet, kolmannen osapuolen osat ja niiden lisenssit.

Voit käyttää sitä mihin tahansa tarkoitukseen, tutkia miten se toimii, jakaa
sen ja muuttaa sitä.


Apua
----

  Documentation   https://ny4i.com/n1mm-dxkeeper-gateway/
  Questions       DXLab keskusteluryhmä DXLab@groups.io

Kun raportoit ongelma, About ikkunan "Copy details" painike laittaa version ja
ympäristösi leikepöydälle. Mainitkaa tämä ja kyseinen osa Operation Log tai
ErrorLog.txt.
