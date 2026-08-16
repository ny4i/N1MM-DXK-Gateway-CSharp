==============================================================================
 MACHINE TRANSLATION into es. The English README.txt is
 authoritative; where this file disagrees with it, it is this file
 that is wrong. Corrections are very welcome.

 source-sha256: e67ddcc749a9e9c5
==============================================================================

N1MM-DXKeeper Gateway 2.0
=========================

Carries QSOs conectado TR4W o N1MM Logger+ recto DXKeeper, y pregunta DXView y
Pathfinder para buscar los carteles que trabajas.

Documentación completa: https://ny4i.com/n1mm-dxkeeper-gateway/


Antes de empezar
----------------

1. DXKeeper debe ser instalado. El portal no hace nada por sí solo; es un
   Entrenador.

2. El Microsoft .NET 8 DESKTOP Runtime, 64-bit (x64).

   Si ya corres JTAlert 2.80 o más tarde, usted lo tiene - JTAlert necesidades
   lo mismo, y sólo tiene que ser instalado una vez. Windows mantiene actualizado
   después como parte de la normalidad Windows Update.

   Si el gateway no se iniciará, o Windows ofrece a ir buscando
   algo, esto es lo que falta:

       https://dotnet.microsoft.com/download/dotnet/8.0

   Elige "Desktop Runtime", x64. No el SDK, y no la llanura ".NET
   Hora de correr Desktop Runtime es el que incluye lo que este programa
   necesidades. El anterior VB6 Gateway no necesitaba tal instalación; éste es un
   Reescribir y hacerlo.

3. Windows 10 o Windows 11.


RUNNING IT
----------

Comience desde el Start menu, o desde el atajo de escritorio si le pidió al
instalador para uno.

Comienza la puerta de entrada, DXKeeper, DXView, Pathfinder y tu logger en
cualquier orden. El gateway se conecta a cada uno según parece.

Sus configuraciones viven en el registro de Windows, bajo la misma llave que
el VB6 Gateway utilizado, por lo que los ajustes de la versión antigua se
llevan a cabo por sí mismos.

El DXLab Launcher puede comenzar el Gateway junto a la otra DXLab programas;
ver el "Specifying a non-DXLab tema de la aplicación en ayuda del lanzador.


POINING SU LOGGER EN TI
-----------------------

El portal escucha UDP puerto 12060 por defecto. Puede cambiar eso en la
sección Red de su ventana.

  N1MM Logger+   Config > Configure Ports ... ■ Broadcast Data tab.
                 Tick "Contacts" y establecer la dirección a su lado
                 ordenadores IPv4 dirección y puerto, por ejemplo. 192.168.1.11:12060
                 Tick "External Callsign Lookup" y ponerlo de la misma manera.

  TR4W           Set UDP BROADCAST ADDRESS a la misma dirección y puerto.

  WSJT-X         Settings > Reporting. Tick "Habilitar el contacto registrado ADIF
                 transmitir" e introducir su dirección IP - 127.0.0.1 si WSJT-X es
                 en esta misma computadora - y 12060 en el Server port number
                 campo.

                 Sugerimos que use JTAlert o enviar contactos directamente
                 a la DXLab aplicaciones; ver DXLab instrucciones. Esto
                 la ruta funciona, pero esos son los caminos mejor recorridos.

  SDR-Control    Apunte su transmisión de registro en el puerto 12060.

Si corres más de uno de estos a la vez, ten cuidado de no tener el mismo QSO
llegar a la puerta dos veces - por ejemplo WSJT-X transmisión directamente a
la puerta de entrada y alimentación N1MM, que entonces lo transmite también.
DXKeeper no detecta duplicados y registraría ambos.


POLÍTICO EN DXKEEPER
--------------------

Nada que configurar. El portal lee DXKeeper Es propio Base Port y lo usa. Si
cambias DXKeeper's Base Port (G)Config > Defaults tab > Network Service),
reiniciar la puerta después.

Ese mismo panel te dice si DXKeeper El servicio de red está escuchando. Si el
portal informa que no puede conectarse, mire primero.


Lo que el sentido muestra
-------------------------

  Settings          UDP puerto, grupo multicast opcional, qué DXKeeper debería
                    hacer con cada QSO (búsqueda de libros, eQSL, LoTW, Club Log),
                    opciones de registro y el lenguaje de interfaz.

  Connection Status DXKeeper, DXView y Pathfinder. Desconectado es normal
                    para programas que no estás corriendo.

  Operation Log     Lo que ha hecho el Gateway, más nuevo en el fondo. Problemas
                    están coloreados. Este es el primer lugar en mirar, y el
                    El botón de copia lo pone en el portapapeles para un informe de fallos.

Minimising pone el gateway en el área de notificación (por el reloj) en lugar
de la barra de tareas, donde guarda un recuento de lo que ha recibido y
registrado. Windows 11 oculta nuevos iconos de notificación por defecto - si
quieres verlo, arrastrelo fuera de los "íconos escondidos" flyout en la barra
de tareas. Cerrar la ventana deja la puerta.


CHANGE/DELETE QSOs Y TOGGLES
----------------------------

Lea esto antes de encender Upload to eQSL.cc, Upload to LoTW o Upload to Club
Log.

Esos toggles dicen DXKeeper para subir cada uno QSO a la bitácora online tan
pronto como se haya registrado. Por separado, la puerta de entrada admite la
edición y eliminación QSOs: cuando su registrador envía un cambio, el gateway
elimina el QSO desde DXKeeper y registra el corregido, porque DXKeeper no
tiene ninguna operación "reemplazar".

Esas dos características no se combinan bien, ni la puerta de entrada ni
DXKeeper puede hacerlos. Una carga que ya ha salido no puede ser recordada.
LoTW en particular no tiene manera de eliminar un QSO has subido. Así que...
QSO subido y luego editado deja la posición original en LoTW para siempre, con
la corrección agregada a su lado en lugar de reemplazarla. A QSO subidas y
eliminadas estancias en LoTW después de que haya salido de tu propio registro.

Antes de que el Gateway apoyara la edición y eliminación, esto no podría
surgir: cada QSO Se registró fue final.

Qué hacer al respecto

La respuesta directa, y la que el autor utiliza, es dejar los tres toggles de
carga cambiados OFF mientras se disputa, y subir de DXKeeper a mano una vez
que el registro es definitivo y se han realizado correcciones. DXKeeper subir
un registro entero tan fácilmente como uno QSO, y para entonces no hay nada
que corregir.

Enciende si lo prefieres - el Gateway te advierte una vez y luego hace lo que
se dice - pero ten en cuenta que una corrección posterior no llegará a la
bitácora online de forma limpia.

Esto no se aplica a Query Callbook o Lookup previous QSOs Sólo leen.


FILES IT WRITES
---------------

Ambos aparecen en la propia carpeta de Gateway. Si el Gateway fue instalado en
algún lugar de Windows no lo permite escribir - bajo C:\Program Archivos, por
ejemplo - utiliza una carpeta por usuario en lugar y registra cuál en la parte
superior de ErrorLog.txt.

  ErrorLog.txt          Diagnósticos. Un rojo "see ErrorLog"El enlace aparece en el
                        ventana cuando algo ha sido escrito. Tick
                        "Log debugging information"para mucho más detalle cuando
                        persiguiendo un problema.

  FailedQSOs_<date>_<time>.adi
                        QSOs DXKeeper no lo confirmó. IMPORTANTE: la puerta
                        nunca silenciosamente descarta un QSO, pero nunca
                        uno, porque DXKeeper no detecta
                        duplicados y un reingreso podría registrarlo dos veces. Si
                        archivo existe, importarlo en DXKeeper a mano y luego
                        borrarlo."Failed QSOs"en la parte inferior de la ventana
                        se vuelve rojo con un conteo cuando esto sucede; haga clic en
                        abrir la carpeta con el archivo seleccionado. La cuenta va
                        volver a cero cuando el archivo se ha ido.

                        Un archivo por carrera. Una carrera que no pierde nada no
                        archivo, así que el archivo existente siempre significa algo
                        necesita tu atención.


IF A QSO NO ARRIVE
------------------

  - ¿El qué? Operation Log mostrar el QSO ser recibido? Si no, el logger es
    no llegar a la puerta: comprobar la dirección y el puerto, y comprobar un cortafuegos
    no está bloqueando UDP.

  - ¿Le muestra ser enviado pero no confirmado? DXKeeper no reconoció
    Es. Check DXKeeper está corriendo y que Network Service dice: Listening.
    El QSO estará dentro FailedQSOs.

  - DXKeeper puede correr varios segundos detrás durante un concurso ocupado. La puerta de entrada
    envía uno QSO a la vez y espera DXKeeper para confirmar cada uno, así que
    atraso es normal y drena por sí mismo.


IDGUAGE
-------

El gateway sigue su lenguaje de visualización de Windows si tiene una
traducción para él, y puede elegir uno explícitamente bajo Ajustes > General.
Un cambio tiene efecto la próxima vez que comience.

Las traducciones distintas del inglés son hechas a máquina y son corregidas
por voluntarios. Si el suyo lee mal, las correcciones son muy bienvenidas - y
el nombre del traductor aparece en la ventana Acerca.


LICENCIA
--------

Software libre bajo la versión 3 o posterior de GNU General Public License,
con ABSOLUTELY NO WARRANTY. El texto completo está en COPYING.txt; NOTICE.txt
registra los derechos de autor, los componentes de terceros y sus licencias.

Usted puede utilizarlo para cualquier propósito, estudiar cómo funciona,
compartirlo y cambiarlo.


Ayuda
-----

  Documentation   https://ny4i.com/n1mm-dxkeeper-gateway/
  Questions       DXLab Grupo de debate, DXLab@groups.io

Cuando reporta un problema, la ventana "Copy details" botón pone la versión y
su entorno en el portapapeles. Sírvase incluir eso y la parte pertinente de la
Operation Log o ErrorLog.txt.
