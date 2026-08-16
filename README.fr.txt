==============================================================================
 MACHINE TRANSLATION into fr. The English README.txt is
 authoritative; where this file disagrees with it, it is this file
 that is wrong. Corrections are very welcome.

 source-sha256: e67ddcc749a9e9c5
==============================================================================

N1MM-DXKeeper Gateway 2.0
=========================

Charges QSOs connecté TR4W ou N1MM Logger+ directement dans DXKeeper, et
demande DXView et Pathfinder pour vérifier les panneaux d'appel que vous
travaillez.

Documentation complète: https://ny4i.com/n1mm-dxkeeper-gateway/


QUELLES SONT LES INFORMATIONS A CONNAITRE AVANT DE CONNAITRE
------------------------------------------------------------

1. DXKeeper doivent être installés. La passerelle ne fait rien de lui-même; c'est un
   Entre les deux.

2. Le Microsoft .NET 8 DESKTOP Runtime, 64 bits (x64).

   Si tu cours déjà JTAlert 2.80 ou plus tard, vous l'avez - JTAlert a besoin des
   Et il ne doit être installé qu'une seule fois. Windows le tient à jour
   ensuite dans le cadre de la normale Windows Update.

   Si la passerelle ne démarre pas, ou Windows offre d'aller chercher
   Quelque chose, c'est ce qui manque :

       https://dotnet.microsoft.com/download/dotnet/8.0

   Choisir "Desktop Runtime", x64. Pas le SDK, et pas le simple ".NET
   Durée" - le Desktop Runtime est celui qui inclut ce que ce programme
   besoins. La passerelle précédente VB6 n'avait pas besoin d'une telle installation; celle-ci est une
   Réécrire et le fait.

3. Windows 10 ou Windows 11.


C'est fini.
-----------

Commencez à partir du Start menu, ou à partir du raccourci de bureau si vous
avez demandé à l'installateur pour un.

Démarre la passerelle, DXKeeper, DXView, Pathfinder et votre bûcheron dans
n'importe quel ordre. La passerelle se connecte à chaque fois qu'elle
apparaît.

Vos paramètres vivent dans le registre Windows, sous la même clé que la
passerelle VB6 utilisée, de sorte que les paramètres de l'ancienne version se
transmettent par eux-mêmes.

Les DXLab Launcher peut démarrer la passerelle à côté de votre autre DXLab les
programmes; voir la section « Spécifier un DXLab pathname de l'application"
sujet dans l'aide de Launcher.


MENTIONNEZ VOTRE LOGEUR
-----------------------

La passerelle écoute UDP port 12060 par défaut. Vous pouvez changer cela dans
la section Réseau de sa fenêtre.

  N1MM Logger+   Config > Configure Ports ... > Broadcast Data onglet.
                 Cochez "Contacts" et définissez l'adresse à côté
                 ordinateur IPv4 adresse et port, par exemple 192.168.1.11:12060
                 Cochez "External Callsign Lookup" et le mettre de la même façon.

  TR4W           Jeu UDP BROADCAST ADDRESS à la même adresse et au même port.

  WSJT-X         Settings > Reporting. Cochez "Activer le contact enregistré ADIF
                 et entrez votre adresse IP - 127.0.0.1 si WSJT-X est
                 sur ce même ordinateur - et 12060 dans le Server port number
                 sur le terrain.

                 Nous vous suggérons d'utiliser JTAlert à la place, ou envoyer des contacts directement
                 aux DXLab les demandes; voir DXLab instructions. Cette
                 le chemin fonctionne, mais ce sont les meilleurs chemins parcourus.

  SDR-Control    Pointer sa diffusion d'exploitation au port 12060.

Si vous exécutez plus d'un de ces à la fois, faites attention de ne pas avoir
la même QSO atteindre la passerelle deux fois - par exemple WSJT-X diffusion
directe à la passerelle ET alimentation N1MM, qui la diffuse alors aussi.
DXKeeper ne détecte pas les duplicates et enregistrerait les deux.


L'APPELANT À DXKEEPER
---------------------

Rien à configurer. La passerelle lit DXKeeper propres Base Port et l'utilise.
Si vous changez DXKeeper's Base Port (Config > Defaults tab > Network
Service), redémarrez la passerelle après.

Ce même panneau vous indique si DXKeeper Le service réseau écoute. Si la
passerelle signale qu'elle ne peut pas se connecter, regardez d'abord là.


QU'EST-CE QUE LE WINDOW SHOWS
-----------------------------

  Settings          UDP port, groupe multicast optionnel, quoi DXKeeper devrait
                    faire avec chaque QSO (Regarde le livre d'appel, eQSL, LoTW, Club Log),
                    les options de journalisation et le langage d'interface.

  Connection Status DXKeeper, DXView et Pathfinder. Déconnecté est normal
                    pour les programmes que vous n'exécutez pas.

  Operation Log     Ce que la passerelle a fait, plus récent en bas. Problèmes
                    sont colorés. C'est le premier endroit à regarder, et le
                    Copier bouton le met sur le presse-papiers pour un rapport de bogue.

Minimiser place la passerelle dans la zone de notification (par l'horloge)
plutôt que la barre des tâches, où il garde un compte courant de ce qu'il a
reçu et enregistré. Windows 11 cache de nouvelles icônes de notification par
défaut - si vous voulez la voir, faites-la sortir des "icônes cachées" sur la
barre des tâches. La fermeture de la fenêtre quitte la passerelle.


CHANGEMENT/DÉLÈTE QSOs ET DES TOGLES À CHARGER
----------------------------------------------

Lire ceci avant d'allumer Upload to eQSL.cc, Upload to LoTW ou Upload to Club
Log.

Ces toggles disent DXKeeper pour télécharger chaque QSO au journal de bord en
ligne dès qu'il est enregistré. Séparément, la passerelle prend en charge
l'édition et la suppression QSOs: lorsque votre enregistreur envoie une
modification, la passerelle supprime QSO de DXKeeper et enregistre celui
corrigé, parce que DXKeeper n'a pas d'opération de « remplacement » unique.

Ces deux caractéristiques ne combinent pas bien, ni la passerelle ni DXKeeper
peut les faire. Un téléchargement qui est déjà sorti ne peut pas être rappelé.
LoTW en particulier n'a aucun moyen de supprimer QSO Vous avez téléchargé.
Alors QSO téléchargé puis édité laisse la position ORIGINAL à LoTW pour
toujours, avec la correction ajoutée à côté plutôt que de la remplacer. A QSO
téléchargé puis supprimé reste à LoTW après qu'il soit sorti de votre propre
journal.

Avant que la passerelle ne supporte l'édition et la suppression, cela ne
pouvait pas se produire : QSO C'était définitif.

Que faire à ce sujet?

La réponse simple, et celle que l'auteur utilise, est de laisser les trois
toggles de téléchargement commuté OFF pendant le concours, et de télécharger à
partir DXKeeper une fois que le journal est définitif et que des corrections
ont été apportées. DXKeeper charge un journal entier aussi facilement qu'un
QSO, et il n'y a plus rien à corriger.

Activez-les si vous préférez - la passerelle vous avertit une fois et ensuite
fait comme il est dit - mais soyez conscient qu'une correction ultérieure
n'atteindra pas le journal de bord en ligne proprement.

Cela ne s'applique pas Query Callbook ou Lookup previous QSOs. Ceux qui lisent
seulement.


FICHIERS
--------

Les deux apparaissent dans le dossier de la passerelle. Si la passerelle a été
installée quelque part Windows ne le laisse pas écrire - sous C:\Program
Fichiers, par exemple - il utilise un dossier par utilisateur à la place et
enregistre celui qui en haut de ErrorLog.txt.

  ErrorLog.txt          Des diagnostics. Un rouge "see ErrorLog" lien apparaît dans le
                        fenêtre quand quelque chose lui a été écrit. Cochez
                        "Log debugging information" pour beaucoup plus de détails quand
                        Je cherche un problème.

  FailedQSOs_<date>_<time>.adi
                        QSOs DXKeeper n'a pas confirmé. IMPORTANT : la passerelle
                        ne rejette jamais silencieusement QSO, mais elle aussi jamais
                        demande un, parce que DXKeeper ne détecte pas
                        des duplicatas et une réessayer pourrait l'enregistrer deux fois. Si cela
                        fichier existe, l'importer dans DXKeeper à la main et ensuite
                        Supprimer. "Failed QSOs" au bas de la fenêtre
                        devient rouge avec un nombre lorsque cela se produit; cliquez sur
                        ouvrir le dossier avec le fichier sélectionné. Le compte va
                        retour à zéro quand le fichier est parti.

                        Un fichier par course. Une course qui ne perd rien
                        fichier, donc le fichier existant signifie toujours quelque chose
                        Il a besoin de votre attention.


SI A QSO N'ARRIVE PAS
---------------------

  - Est-ce que Operation Log montrer QSO être reçu ? Si non, l'enregistreur est
    ne pas atteindre la passerelle: vérifiez l'adresse et le port, et vérifiez un pare-feu
    ne bloque pas UDP.

  - Ça montre qu'il est envoyé mais pas confirmé ? DXKeeper n'a pas reconnu
    Ça. Vérifier DXKeeper est en cours d'exécution et que Network Service dit Listening.
    Les QSO sera dans FailedQSOs.

  - Oui. DXKeeper peut courir plusieurs secondes derrière lors d'un concours chargé. La passerelle
    envoie un QSO à un moment et attend DXKeeper pour confirmer chacun, donc un
    L'arriéré est normal et s'épuise tout seul.


LANGUE
------

La passerelle suit votre langue d'affichage Windows si elle a une traduction
pour elle, et vous pouvez en choisir une explicitement sous Paramètres >
Général. Un changement prend effet la prochaine fois qu'il commence.

Les traductions autres que l'anglais sont faites par machine et corrigées par
des bénévoles. Si le vôtre lit mal, les corrections sont les bienvenues - et
le nom du traducteur apparaît dans la fenêtre A propos.


LICENCE
-------

Logiciels libres sous la GNU General Public License version 3 ou ultérieure,
avec ABSOLUEMENT PAS DE GARANTIE. Le texte intégral est COPYING.txt;
NOTICE.txt enregistre les droits d'auteur, les composants tiers et leurs
licences.

Vous pouvez l'utiliser à n'importe quelle fin, étudier comment il fonctionne,
le partager et le changer.


AIDE
----

  Documentation   https://ny4i.com/n1mm-dxkeeper-gateway/
  Questions       DXLab Groupe de discussion, DXLab@groups.io

Quand vous signalez un problème, la fenêtre About's "Copy details" bouton met
la version et votre environnement sur le presse-papiers. Veuillez indiquer ce
qui suit et la partie pertinente de la Operation Log ou ErrorLog.txt.
