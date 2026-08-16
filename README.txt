N1MM-DXKeeper Gateway 2.0
=========================

Carries QSOs logged in TR4W or N1MM Logger+ straight into DXKeeper, and asks
DXView and Pathfinder to look up the callsigns you work.

Full documentation:  https://ny4i.com/n1mm-dxkeeper-gateway/


BEFORE YOU START
----------------

1. DXKeeper must be installed. The Gateway does nothing on its own; it is a
   go-between.

2. The Microsoft .NET 8 DESKTOP Runtime, 64-bit (x64).

   If you already run JTAlert 2.80 or later, you have it - JTAlert needs the
   same thing, and it only has to be installed once. Windows keeps it updated
   afterwards as part of normal Windows Update.

   If the Gateway will not start, or Windows offers to go looking for
   something, this is what is missing:

       https://dotnet.microsoft.com/download/dotnet/8.0

   Choose "Desktop Runtime", x64. Not the SDK, and not the plain ".NET
   Runtime" - the Desktop Runtime is the one that includes what this program
   needs. The earlier VB6 Gateway needed no such install; this one is a
   rewrite and does.

3. Windows 10 or Windows 11.


RUNNING IT
----------

Start it from the Start menu, or from the desktop shortcut if you asked the
installer for one.

Start the Gateway, DXKeeper, DXView, Pathfinder and your logger in any order.
The Gateway connects to each as it appears.

Your settings live in the Windows registry, under the same key the VB6 Gateway
used, so settings from the old version carry over by themselves.

The DXLab Launcher can start the Gateway alongside your other DXLab programs;
see the "Specifying a non-DXLab application's pathname" topic in Launcher's
help.


POINTING YOUR LOGGER AT IT
--------------------------

The Gateway listens on UDP port 12060 by default. You can change that in the
Network section of its window.

  N1MM Logger+   Config > Configure Ports ... > Broadcast Data tab.
                 Tick "Contacts" and set the address beside it to your
                 computer's IPv4 address and port, e.g. 192.168.1.11:12060
                 Tick "External Callsign Lookup" and set it the same way.

  TR4W           Set UDP BROADCAST ADDRESS to the same address and port.

  WSJT-X         Settings > Reporting. Either the N1MM Logger+ broadcast or
                 "Enable logged contact ADIF broadcast" works; point it at
                 port 12060. Both formats are accepted.

  SDR-Control    Point its logging broadcast at port 12060.

If you run more than one of these at once, be careful not to have the same QSO
reach the Gateway twice - for example WSJT-X broadcasting directly to the
Gateway AND feeding N1MM, which then broadcasts it as well. DXKeeper does not
detect duplicates and would log both.


POINTING IT AT DXKEEPER
-----------------------

Nothing to configure. The Gateway reads DXKeeper's own Base Port setting and
uses it. If you change DXKeeper's Base Port (Config > Defaults tab > Network
Service), restart the Gateway afterwards.

That same panel's heading tells you whether DXKeeper's network service is
listening. If the Gateway reports that it cannot connect, look there first.


WHAT THE WINDOW SHOWS
---------------------

  Settings          UDP port, optional multicast group, what DXKeeper should
                    do with each QSO (callbook lookup, eQSL, LoTW, Club Log),
                    logging options and the interface language.

  Connection Status DXKeeper, DXView and Pathfinder. Disconnected is normal
                    for programs you are not running.

  Operation Log     What the Gateway has done, newest at the bottom. Problems
                    are coloured. This is the first place to look, and the
                    Copy button puts it on the clipboard for a bug report.

Minimising puts the Gateway in the notification area (by the clock) rather
than the taskbar, where it keeps a running count of what it has received and
logged. Windows 11 hides new notification icons by default - if you want to
see it, drag it out of the "hidden icons" flyout onto the taskbar. Closing the
window quits the Gateway.


CHANGE/DELETE QSOs AND UPLOAD TOGGLES
-------------------------------------

Read this before switching on Upload to eQSL.cc, Upload to LoTW or Upload to
Club Log.

Those toggles tell DXKeeper to upload each QSO to the online logbook as soon as
it is logged. Separately, the Gateway supports editing and deleting QSOs: when
your logger sends a change, the Gateway deletes the QSO from DXKeeper and logs
the corrected one, because DXKeeper has no single "replace" operation.

Those two features do not combine well, and neither the Gateway nor DXKeeper
can make them. An upload that has already gone out cannot be recalled. LoTW in
particular has no way to delete a QSO you have uploaded. So a QSO uploaded and
then edited leaves the ORIGINAL standing at LoTW for ever, with the correction
added beside it rather than replacing it. A QSO uploaded and then deleted stays
at LoTW after it has gone from your own log.

Before the Gateway supported editing and deleting, this could not arise: every
QSO it logged was final.

WHAT TO DO ABOUT IT

The straightforward answer, and the one the author uses, is to leave all three
upload toggles switched OFF while contesting, and to upload from DXKeeper by
hand once the log is final and any corrections have been made. DXKeeper uploads
a whole log as easily as one QSO, and by then there is nothing left to correct.

Switch them on if you prefer - the Gateway warns you once and then does as it
is told - but be aware that a later correction will not reach the online
logbook cleanly.

This does not apply to Query Callbook or Lookup previous QSOs. Those only read.


FILES IT WRITES
---------------

Both appear in the Gateway's own folder. If the Gateway was installed somewhere
Windows does not let it write - under C:\Program Files, for instance - it uses
a per-user folder instead and records which one at the top of ErrorLog.txt.

  ErrorLog.txt          Diagnostics. A red "see ErrorLog" link appears in the
                        window when something has been written to it. Tick
                        "Log debugging information" for much more detail when
                        chasing a problem.

  FailedQSOs_<date>_<time>.adi
                        QSOs DXKeeper did not confirm. IMPORTANT: the Gateway
                        never silently discards a QSO, but it also never
                        retries one, because DXKeeper does not detect
                        duplicates and a retry could log it twice. If this
                        file exists, import it into DXKeeper by hand and then
                        delete it. A warning bar in the window tells you it is
                        there and how many QSOs are in it, and the warning
                        clears when the file is gone.

                        One file per run. A run that loses nothing leaves no
                        file, so the file existing always means something
                        needs your attention.


IF A QSO DOES NOT ARRIVE
------------------------

  - Does the Operation Log show the QSO being received? If not, the logger is
    not reaching the Gateway: check the address and port, and check a firewall
    is not blocking UDP.

  - Does it show it being sent but not confirmed? DXKeeper did not acknowledge
    it. Check DXKeeper is running and that its Network Service says Listening.
    The QSO will be in FailedQSOs.

  - DXKeeper can run several seconds behind during a busy contest. The Gateway
    sends one QSO at a time and waits for DXKeeper to confirm each, so a
    backlog is normal and drains on its own.


LANGUAGE
--------

The Gateway follows your Windows display language if it has a translation for
it, and you can choose one explicitly under Settings > General. A change takes
effect the next time it starts.

Translations other than English are machine-made and being corrected by
volunteers. If yours reads badly, corrections are very welcome - and the
translator's name appears in the About window.


LICENCE
-------

Free software under the GNU General Public License version 3 or later, with
ABSOLUTELY NO WARRANTY. The full text is in COPYING.txt; NOTICE.txt records
the copyright, the third-party components and their licences.

You may use it for any purpose, study how it works, share it and change it.


HELP
----

  Documentation   https://ny4i.com/n1mm-dxkeeper-gateway/
  Questions       DXLab Discussion Group, DXLab@groups.io

When reporting a problem, the About window's "Copy details" button puts the
version and your environment on the clipboard. Please include that and the
relevant part of the Operation Log or ErrorLog.txt.
