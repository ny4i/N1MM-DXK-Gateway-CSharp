==============================================================================
 MACHINE TRANSLATION into it. The English README.txt is
 authoritative; where this file disagrees with it, it is this file
 that is wrong. Corrections are very welcome.

 source-sha256: e67ddcc749a9e9c5
==============================================================================

N1MM-DXKeeper Gateway 2.0
=========================

Carri QSOs registrato TR4W o N1MM Logger+ dritto DXKeeper e chiede DXView e
Pathfinder per cercare i cartelli che lavori.

Documentazione completa: https://ny4i.com/n1mm-dxkeeper-gateway/


Prima di iniziare
-----------------

1. DXKeeper deve essere installato. Il gateway non fa nulla da solo; è un
   Via.

2. Microsoft .NET 8 DESKTOP Runtime, 64-bit (x64).

   Se già corri JTAlert 2.80 o più tardi, ce l'hai... JTAlert le esigenze
   stessa cosa, e deve essere installato solo una volta. Windows mantiene aggiornato
   dopo come parte del normale Windows Update.

   Se il gateway non si avvia, o Windows offre di andare alla ricerca
   qualcosa, questo è ciò che manca:

       https://dotnet.microsoft.com/download/dotnet/8.0

   Scegli "Desktop Runtime", x64. Non il SDK, e non il semplice ".NET
   Runtime" - il Desktop Runtime è quello che include quello che questo programma
   ha bisogno. Il primo VB6 Gateway non aveva bisogno di tale installazione; questo è un
   riscrivere e farlo.

3. Windows 10 o Windows 11.


Smettetela.
-----------

Iniziare da Start menu, o dalla scorciatoia del desktop se avete chiesto
l'installatore per uno.

Avviare il gateway, DXKeeper♪ DXView♪ Pathfinder e il tuo logger in qualsiasi
ordine. Il gateway si collega a ciascuno come appare.

Le tue impostazioni vivono nel registro di sistema di Windows, sotto la stessa
chiave utilizzata dal gateway VB6, quindi le impostazioni dalla vecchia
versione portano da sole.

The DXLab Launcher può avviare il gateway accanto all'altro DXLab programmi;
vedere il "specificare un non-DXLab Argomento del nome del percorso
dell'applicazione nell'aiuto di Launcher.


POINTARE IL TUO LOGGER
----------------------

Il gateway ascolta su UDP porto 12060 per impostazione predefinita. È
possibile modificarlo nella sezione Rete della finestra.

  N1MM Logger+   Config > Configure Ports ... > Broadcast Data scheda.
                 Tick "Contatti" e impostare l'indirizzo accanto a esso al
                 computer IPv4 indirizzo e porta, ad esempio. 192.168.1.11:12060
                 Tick "External Callsign Lookup" e impostalo allo stesso modo.

  TR4W           Set UDP BROADCAST ADDRESS allo stesso indirizzo e porta.

  WSJT-X         Settings > Reporting. Tick "Abilita il contatto registrato ADIF
                 trasmettere" e inserire il tuo indirizzo IP - 127.0.0.1 se WSJT-X è
                 su questo stesso computer - e 12060 nel Server port number
                 campo.

                 Ti suggeriamo di usare JTAlert invece, o inviare contatti direttamente
                 al DXLab applicazioni; vedere il DXLab istruzioni. Questo
                 funziona il percorso, ma questi sono i percorsi meglio percorsi.

  SDR-Control    Indicare la sua trasmissione di registrazione al porto 12060.

Se si esegue più di uno di questi contemporaneamente, fare attenzione a non
avere lo stesso QSO raggiungere il gateway due volte - per esempio WSJT-X
trasmettere direttamente al gateway E alimentazione N1MM, che poi lo trasmette
anche. DXKeeper non rileva duplicati e registri entrambi.


POINTE A DXKEEPER
-----------------

Niente da configurare. Il gateway legge DXKeeper È proprio Base Port
impostarlo e usarlo. Se cambi DXKeeper' Base Port (Config > Defaults tab >
Network Service), riavviare il gateway in seguito.

Quello stesso pannello ti dice se DXKeeper Il servizio di rete sta ascoltando.
Se il gateway segnala che non può connettersi, guarda lì prima.


CHE COSA IL MONDO
-----------------

  Settings          UDP porta, gruppo multicast opzionale, cosa DXKeeper dovrebbe
                    fare con ogni QSO (Callbook lookup, eQSL♪ LoTW♪ Club Log),
                    opzioni di registrazione e la lingua di interfaccia.

  Connection Status DXKeeper♪ DXView e Pathfinder. Scollegato è normale
                    per i programmi che non stai correndo.

  Operation Log     Quello che il gateway ha fatto, il più nuovo in basso. Problemi
                    sono colorati. Questo è il primo posto per guardare, e
                    Il pulsante di copia lo mette sul appunti per un bug report.

Minimising mette il gateway nell'area di notifica (da orologio) piuttosto che
nella barra delle applicazioni, dove mantiene un conteggio in esecuzione di
ciò che ha ricevuto e registrato. Windows 11 nasconde nuove icone di notifica
per impostazione predefinita - se vuoi vederlo, trascinalo fuori dalle "icona
nascosta" flyout sulla barra delle applicazioni. Chiudere la finestra termina
il gateway.


CAPITOLO/DELETATO QSOs E SUPERIORE
----------------------------------

Leggi questo prima di accendere Upload to eQSL.cc♪ Upload to LoTW o Upload to
Club Log.

Quegli occhiali dicono DXKeeper per caricare ogni QSO al logbook online non
appena è registrato. Separatamente, il gateway supporta la modifica e la
cancellazione QSOs: quando il tuo logger invia una modifica, il gateway
cancella il QSO da DXKeeper e registra quello corretto, perché DXKeeper non ha
un'unica operazione "sostituire".

Queste due caratteristiche non combinano bene, e né il gateway né DXKeeper può
renderli. Un upload che è già uscito non può essere richiamato. LoTW in
particolare non ha modo di eliminare un QSO Hai caricato. Quindi... QSO
caricato e poi modificato lascia l'ORIGINALE in piedi a LoTW per sempre, con
la correzione aggiunta accanto a esso piuttosto che sostituirlo. A QSO
caricati e poi cancellati soggiorni a LoTW dopo che è passato dal tuo tronco.

Prima che il Gateway supportasse la modifica e l'eliminazione, questo non
poteva sorgere: ogni QSO l'ho fatto.

Cosa fare a riguardo

La risposta semplice, e quella che l'autore utilizza, è quella di lasciare
tutti e tre le attivazioni di upload spento durante la contestazione, e di
caricare da DXKeeper a mano una volta che il registro è finale e tutte le
correzioni sono state fatte. DXKeeper carica un registro intero facilmente
come uno QSO, e poi non c'è più nulla da correggere.

Accendere se si preferisce - il gateway ti avvisa una volta e poi fa come
viene detto - ma essere consapevoli che una correzione successiva non
raggiungerà il logbook online in modo pulito.

Questo non si applica Query Callbook o Lookup previous QSOs Quelli leggono
solo.


IMPRESE
-------

Entrambi appaiono nella cartella del gateway. Se il gateway è stato installato
da qualche parte Windows non lo lascia scrivere - sotto C:\Program File, per
esempio - utilizza una cartella per utente invece e registra quale in cima
ErrorLog.txt.

  ErrorLog.txt          Diagnostica. Un rosso "see ErrorLog" link appare nel
                        finestra quando qualcosa è stato scritto ad esso. Tick
                        "Log debugging information" per molto più dettagli quando
                        inseguire un problema.

  FailedQSOs_<date>_<time>.adi
                        QSOs DXKeeper non ha confermato. IMPORTANTE: il gateway
                        mai silenziosamente scarta un QSO ma anche mai
                        ritiri uno, perché DXKeeper non rileva
                        duplicati e un retry potrebbero registrarlo due volte. Se è così
                        file esiste, importarlo in DXKeeper a mano e poi
                        cancellarlo. "Failed QSOs" in fondo alla finestra
                        diventa rosso con un conteggio quando questo accade; fare clic su
                        aprire la cartella con il file selezionato. Il conteggio va
                        quando il file è andato.

                        Un file per corsa. Una corsa che perde nulla non lascia nulla
                        file, quindi il file esistente significa sempre qualcosa
                        ha bisogno della vostra attenzione.


IF A QSO NON ARRIVE
-------------------

  - Fa il Operation Log mostra il QSO essere ricevuto? Se no, il logger è
    non raggiungere il gateway: controllare l'indirizzo e la porta, e controllare un firewall
    non blocca UDP.

  - Lo dimostra essere mandato ma non confermato? DXKeeper non ha riconosciuto
    Ecco. Check DXKeeper è in esecuzione e che la sua Network Service dice Listening.
    The QSO sarà in FailedQSOs.

  - No. DXKeeper può eseguire diversi secondi indietro durante una gara impegnativa. Il gateway
    manda uno QSO in un momento e aspetta DXKeeper per confermare ciascuno, quindi un
    backlog è normale e drena da solo.


LINGUA
------

Il gateway segue la lingua di visualizzazione di Windows se ha una traduzione
per esso, e si può scegliere esplicitamente in Impostazioni > Generale. Un
cambiamento ha effetto la prossima volta che inizia.

Traduzioni diverse dall'inglese sono fatte in macchina e vengono corrette da
volontari. Se il vostro legge male, le correzioni sono molto benvenuti - e il
nome del traduttore appare nella finestra Informazioni.


LICENZA
-------

Software gratuito sotto la GNU General Public License versione 3 o versioni
successive, con ABSOLUTELY NO WARRANTY. Il testo integrale è in COPYING.txt;
NOTICE.txt registra il copyright, i componenti di terze parti e le loro
licenze.

È possibile utilizzarlo per qualsiasi scopo, studiare come funziona,
condividerlo e cambiarlo.


Aiuto
-----

  Documentation   https://ny4i.com/n1mm-dxkeeper-gateway/
  Questions       DXLab Gruppo di discussione, DXLab@groups.io

Quando si segnala un problema, la finestra "Copy details" Pulsante mette la
versione e l'ambiente nella clipboard. Si prega di includere questo e la parte
rilevante del Operation Log o ErrorLog.txt.
