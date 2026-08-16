==============================================================================
 MACHINE TRANSLATION into pt. The English README.txt is
 authoritative; where this file disagrees with it, it is this file
 that is wrong. Corrections are very welcome.

 source-sha256: e67ddcc749a9e9c5
==============================================================================

N1MM-DXKeeper Gateway 2.0
=========================

Transportadoras QSOs logado TR4W ou N1MM Logger+ directamente para dentro
DXKeeper, e pergunta DXView e Pathfinder para procurar os sinais de chamadas
que você trabalha.

Documentação completa: https://ny4i.com/n1mm-dxkeeper-gateway/


ANTES DE INÍCIO
---------------

1. DXKeeper deve ser instalado. O Portal não faz nada por si só; é um
   Entre.

2. O Microsoft .NET 8 DESKTOP Runtime, 64-bit (x64).

   Se você já correr JTAlert 2,80 ou mais tarde, você tem - JTAlert necessita da
   A mesma coisa, e só tem de ser instalada uma vez. O Windows mantém-no actualizado
   depois como parte do normal Windows Update.

   Se o Gateway não iniciar, ou Windows oferece para ir à procura
   Algo, isto é o que falta:

       https://dotnet.microsoft.com/download/dotnet/8.0

   Escolha "Desktop Runtime", x64. Não o SDK, e não a planície ".NET
   Tempo de execução" - o Desktop Runtime é aquele que inclui o que este programa
   Necessidades. O anterior VB6 Gateway não precisava de tal instalação; este é um
   reescrever e fazer.

3. Windows 10 ou Windows 11.


A correr.
---------

Comece-o a partir do Start menu, ou a partir do atalho desktop se você pediu o
instalador para um.

Comecem o portal. DXKeeper, DXView, Pathfinder e o seu registrador em qualquer
ordem. O portal liga-se a cada como aparece.

Suas configurações estão ao vivo no registro do Windows, sob a mesma chave que
o VB6 Gateway usou, então as configurações da versão antiga passam sozinhas.

A DXLab Launcher pode iniciar o portal ao lado do seu outro DXLab programas;
veja a "Especificar um não-DXLab tópico do caminho da aplicação" na ajuda do
Launcher.


PONTO DO SEU LOGADOR
--------------------

O Portal ouve UDP porto 12060 por omissão. Você pode alterar isso na seção
Rede de sua janela.

  N1MM Logger+   Config > Configure Ports ... > Broadcast Data tab.
                 Assinale "Contactos" e defina o endereço ao seu lado
                 computador IPv4 Endereço e porto, por exemplo. 192.168.1.11:12060
                 Tique "External Callsign Lookup" e colocá-lo da mesma forma.

  TR4W           Definir UDP BROADCAST ADDRESS para o mesmo endereço e porto.

  WSJT-X         Settings > Reporting. Assinale "Ativar o contacto registado ADIF
                 broadcast" e digite seu endereço IP - 127.0.0.1 se WSJT-X é
                 neste mesmo computador - e 12060 na Server port number
                 Campo.

                 Sugerimos que use JTAlert em vez disso, ou enviar contatos diretamente
                 à DXLab aplicações; ver DXLab instruções. Isto
                 A rota funciona, mas esses são os caminhos mais bem percorridos.

  SDR-Control    Apontar a sua transmissão de registo no porto 12060.

Se executar mais do que um destes de uma vez, tenha cuidado para não ter o
mesmo QSO chegar ao portal duas vezes - por exemplo WSJT-X transmissão directa
para o portal E alimentação N1MM, que então transmite-lo também. DXKeeper não
detecta duplicatas e registaria ambas.


PONTO EM DXKEEPER
-----------------

Nada para configurar. O Portal lê DXKeeper O próprio Base Port definir e usá-
lo. Se mudares DXKeeper's Base Port (Config > Defaults tab > Network Service),
reiniciar o Gateway depois.

O mesmo cabeçalho do painel diz-lhe se DXKeeper O serviço de rede está a
ouvir. Se o Gateway reporta que não pode se conectar, olhe lá primeiro.


O QUE A VIDA MOSTRA
-------------------

  Settings          UDP porto, grupo multicast opcional, o que DXKeeper deve
                    fazer com cada QSO (callbook procura, eQSL, LoTW, Club Log),
                    opções de registro e a linguagem de interface.

  Connection Status DXKeeper, DXView e Pathfinder. Desligado é normal
                    para programas que você não está executando.

  Operation Log     O que o Gateway fez, o mais novo no fundo. Problemas
                    são de cor. Este é o primeiro lugar para olhar, e o
                    O botão Copiar coloca- o na área de transferência para um relatório de erro.

Minimizando coloca o Gateway na área de notificação (pelo relógio) em vez da
barra de tarefas, onde mantém uma contagem em execução do que recebeu e
registrou. Windows 11 esconde novos ícones de notificação por padrão - se você
quiser vê-lo, arraste-o para fora do voo para a barra de tarefas. Fechar a
janela deixa o portal.


MUDANÇA/DELETO QSOs E TOGLES ATRÁS
----------------------------------

Leia isto antes de ligar Upload to eQSL.cc, Upload to LoTW ou Upload to Club
Log.

Aqueles interruptores dizem DXKeeper para enviar cada QSO para o diário de
bordo online assim que estiver registado. Separadamente, o Gateway suporta
edição e exclusão QSOs: quando o seu logger envia uma alteração, o Gateway
apaga o QSO de DXKeeper e registra o corrigido, porque DXKeeper não tem
nenhuma operação "substituir".

Estas duas características não combinam bem, e nem o Gateway nem DXKeeper pode
fazê-los. Um upload que já saiu não pode ser recuperado. LoTW em particular,
não tem como excluir uma QSO Você enviou. Então... QSO carregado e depois
editado deixa o ORIGINAL em pé em LoTW para sempre, com a correção adicionada
ao seu lado em vez de substituí-lo. A QSO carregado e então excluído permanece
em LoTW depois de ter saído do seu próprio tronco.

Antes da edição e exclusão suportada pelo Gateway, isso não poderia surgir:
cada QSO O registo era final.

O QUE FAZER A este respeito

A resposta direta, e a que o autor usa, é deixar os três alternâncias de
upload desligados enquanto contesta, e enviar de DXKeeper à mão, uma vez que o
registo seja definitivo e tenham sido efectuadas correcções. DXKeeper envia um
log inteiro tão facilmente quanto um QSO, e então não há mais nada para
corrigir.

Ligue-os se preferir - o Gateway avisa-o uma vez e depois faz o que é dito -
mas esteja ciente de que uma correção posterior não chegará ao diário de bordo
online de forma limpa.

Não se aplica a Query Callbook ou Lookup previous QSOs Eles só lêem.


FILHA QUE ESCREVE
-----------------

Ambos aparecem na pasta do Gateway. Se o Gateway foi instalado em algum lugar
o Windows não o deixa escrever - em C:\Program Arquivos, por exemplo - ele usa
uma pasta por usuário em vez e registra qual um no topo do ErrorLog.txt.

  ErrorLog.txt          Diagnósticos. Um vermelho "see ErrorLog" o link aparece na
                        janela quando algo lhe foi escrito. Tique
                        "Log debugging information"para muito mais detalhes quando
                        A perseguir um problema.

  FailedQSOs_<date>_<time>.adi
                        QSOs DXKeeper não confirmou. IMPORTANTE: A Porta
                        nunca descarta silenciosamente um QSO, mas também nunca
                        tenta um, porque DXKeeper não detecta
                        duplicatas e uma repetição podem registrá-lo duas vezes. Se isto
                        o ficheiro existe, importa- o para DXKeeper à mão e depois
                        apagar."Failed QSOs"no fundo da janela
                        fica vermelho com uma contagem quando isto acontece; clique nele para
                        abrir a pasta com o ficheiro seleccionado. A contagem vai
                        voltar ao zero quando o ficheiro desaparecer.

                        Um arquivo por execução. Uma corrida que não perde nada deixa não
                        arquivo, então o arquivo existente sempre significa algo
                        Precisa da tua atenção.


SE A QSO NÃO ATACA
------------------

  - Faz o Operation Log mostrar a QSO ser recebido? Caso contrário, o registrador é
    não chegar ao Gateway: verifique o endereço e a porta, e verifique um firewall
    não está bloqueando UDP.

  - Mostra que foi enviado mas não confirmado? DXKeeper não reconheceu
    Ele. Verificar DXKeeper está correndo e que a sua Network Service diz Listening.
    A QSO estará dentro FailedQSOs.

  - Não. DXKeeper pode correr vários segundos atrás durante um concurso ocupado. A Porta
    envia um QSO de uma vez e espera por DXKeeper para confirmar cada um, assim um
    O atraso é normal e drena-se sozinho.


LÍNGUA
------

O Gateway segue seu idioma de exibição do Windows se ele tem uma tradução para
ele, e você pode escolher um explicitamente em Configurações > General. Uma
mudança produz efeitos na próxima vez que começar.

Traduções que não o inglês são feitas por máquina e corrigidas por
voluntários. Se o seu ler mal, as correções são muito bem-vindas - e o nome do
tradutor aparece na janela Sobre.


LICENÇA
-------

Software livre sob a GNU General Public License versão 3 ou posterior, com
ABSOLUTAMENTE NENHUMA GARANTIA. O texto completo está em COPYING.txt;
NOTICE.txt Regista os direitos de autor, os componentes de terceiros e as
respectivas licenças.

Você pode usá-lo para qualquer propósito, estudar como ele funciona,
compartilhá-lo e mudá-lo.


Ajuda
-----

  Documentation   https://ny4i.com/n1mm-dxkeeper-gateway/
  Questions       DXLab Grupo de Discussão DXLab@groups.io

Ao relatar um problema, a janela Sobre "Copy details" botão coloca a versão e
seu ambiente na área de transferência. Incluir esta e a parte relevante do
Operation Log ou ErrorLog.txt.
