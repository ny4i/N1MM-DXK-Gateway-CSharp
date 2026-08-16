# N1MM -> DXKeeper Gateway

## Overview

The [N1MM](https://n1mmwp.hamdocs.com/setup/interfacing/) to [DXKeeper](https://www.dxlabsuite.com/dxkeeper/) Gateway enables each QSO logged in [N1MM+](https://n1mmwp.hamdocs.com/setup/interfacing/) or N1MM to be immediately logged to [DXKeeper](https://www.dxlabsuite.com/dxkeeper/), and to direct [DXView](https://www.dxlabsuite.com/dxview/) and [Pathfinder](https://www.dxlabsuite.com/pathfinder/) to “lookup” the logged callsign; you can configure the Gateway to direct DXView to display all previous QSOs with the logged callsign. 

One compelling use case is for those that are using N1MM /TR4W for casual DXing (such as picking up DX Marathon countries or DXCC Challenge slots). When one works the station with the contest software like N1MM, the message is sent in real-time to the Gateway program which in turn loads the QSO record into DXKeeper. This also updates the SpotCollector NEEDED countries so you can work DX by using SpotCollector for only the items you want to work for needed credits, yet you get the full benefit of dupe checking, message processing, etc. that a full-featured, dedicated contest logger provides. 

**The Gateway is free, and contains no advertising; commercial use is expressly forbidden.**

[Press "Enter" to skip to content](https://ny4i.com/n1mm-dxkeeper-gateway/#main)

[NY4I](https://ny4i.com/)

August 15, 2026

- [Home](https://ny4i.com/)
   - [N1MM -> DXKeeper Gateway](https://ny4i.com/n1mm-dxkeeper-gateway/)
   - [Presentations](https://ny4i.com/presentations/)
   - [About Me](https://ny4i.com/about-me/)

# N1MM -> DXKeeper Gateway

The [N1MM](https://n1mmwp.hamdocs.com/setup/interfacing/) to [DXKeeper](https://www.dxlabsuite.com/dxkeeper/) Gateway enables each QSO logged in [N1MM+](https://n1mmwp.hamdocs.com/setup/interfacing/) or N1MM to be immediately logged to [DXKeeper](https://www.dxlabsuite.com/dxkeeper/), and to direct [DXView](https://www.dxlabsuite.com/dxview/) and [Pathfinder](https://www.dxlabsuite.com/pathfinder/) to “lookup” the logged callsign; you can configure the Gateway to direct DXView to display all previous QSOs with the logged callsign. One compelling use case is for those that are using N1MM /TR4W for casual DXing (such as picking up DX Marathon countries or DXCC Challenge slots). When one works the station with the contest software like N1MM, the message is sent in real-time to the Gateway program which in turn loads the QSO record into DXKeeper. This also updates the SpotCollector NEEDED countries so you can work DX by using SpotCollector for only the items you want to work for needed credits, yet you get the full benefit of dupe checking, message processing, etc. that a full-featured, dedicated contest logger provides. 

The Gateway is free, and contains no advertising; commercial use is expressly forbidden.

## Installation

To install the Gateway, download its [zip archive](https://ny4i.com/n1mm_dxk_gateway.zip), and extract the application it contains into the folder of your choice. This application will only work correctly if you have [DXKeeper](https://www.dxlabsuite.com/dxkeeper/)  
installed.  You can create a desktop icon to start the Gateway, or you can configure the [DXLab Launcher](https://www.dxlabsuite.com/Launcher/)to [automatically start the Gateway](https://www.dxlabsuite.com/Launcher/Help/Configuration.htm#Specifying%20a%20non-DXLab%20application's%20pathname) with your [DXLab applications](https://www.dxlabsuite.com/).

If the Gateway’s Main window does not look like this then you are running an obsolete version.



## Configuring Logging Software to send UDP Log Records to the Gateway

While the initial case for this program was processing N1MM UDP messages, several logging package have adopted the “N1MM Standard” for a UDP message to send log records in real-time. Some of the programs that send a ContactInfo packet in the same N1MM format include:

- N1MM+
- TR4W
- Ham Radio Deluxe (HRD)

Note there is another UDP interface that allows N1MM to receive UDP messages to log data. The Gateway also supports this standard but instead of N1MM being the target for the contacts, DXKeeper can be. This is supported to specifically allow SDR-Control to send messages from its built-in logger to DXKeeper. But any program that can send log records into N1MM, can also use this facility to log records into DXKeeper. This data is typically sent to N1MM on port 2333 for WSJT-X but the Gateway will also process those messages **sent on port 12060**. You have to change the port number in SDR-Control and/or WSJT-X.

The following  programs support this interface

- SDR-Control (by DL8MRE for MAC and IOS for remote operation of various radios)
- WSJT-X
  - It is recommended that if you want to send log records from WSJT-X to 

### **Configuring N1MM+**

[  
Configure N1MM+ to use the correct callsign](https://n1mmwp.hamdocs.com/setup/software-setup/#multiple-call-signs).

Display the  [N1MM+](https://n1mmwp.hamdocs.com/setup/interfacing/) **Configurer** window by opening the  [N1MM+](https://n1mmwp.hamdocs.com/setup/interfacing/) Config menu and selecting the **Configure Ports …** command. On the **Configurer** window’s **Broadcast Data** tab,

1. check the **Contacts** box, and set the **IP Addr: PortIP Addr:Port** box to its right to  your computer’s IPv4 address, followed by a colon and the port number 12060;  
   if your computer’s IPv4 address is  192.168.1.11, then you should specify  192.168.1.11:12060

2. check the **External Callsign Lookup** box, and set the **IP Addr: PortIP Addr:Port** box to its right to  your computer’s IPv4 address, followed by a colon and the port number 12060;  
   if your computer’s IPv4 address is 192.168.1.11, then you should specify  192.168.1.11:12060

To determine your computer’s IPv4 address, direct windows top open a **Cmd** window, enter the ipconfig command, and strike the enter key; your computer’s IPv4 address will be included  
in the results displayed by this command.  

For more information about N1MM+’s UDP Broadcasts, go [here](https://n1mmwp.hamdocs.com/appendices/external-udp-broadcasts/).

Configuring the Gateway

1. check the **Lookup previous QSOs** box  
   to direct DXKeeper to display previous QSOs with the logged callsign

2. check the **Query Callbook** box to  
   direct DXKeeper to perform a callbook lookup and augment the information in  
   the logged QSO with the results of that lookup

3. check the **Upload to eQSL.cc** box to  
   direct DXKeeper to upload the logged QSO to eQSL.cc

4. check the **Upload to LoTW** box to  
   direct DXKeeper to upload the logged QSO to LoTW

5. check the **Upload to Club Log** box to  
   direct DXKeeper to upload the logged QSO to Club Log

6. check the **Log debugging information** box to create an errorlog.txt file containing  
   diagnostic information

If you modify the **Base Port** in the **Network Service** panel on the **Defaults** tab of DXKeeper’s **Configuration** window, terminate and then restart the Gateway.

### **Operation**

You can start the Gateway, [N1MM+](https://n1mmwp.hamdocs.com/setup/interfacing/), [DXKeeper](https://www.dxlabsuite.com/dxkeeper/), [DXView](https://www.dxlabsuite.com/dxview/) and [Pathfinder](https://www.dxlabsuite.com/pathfinder/) in any order. The  
Gateway displays a small window that indicates whether it has “connected” with DXKeeper, DXView and Pathfinder, which it does automatically when these applications start.

In N1MM or N1MM+, if typing a callsign and striking the **Enter** key logs the QSO to N1MM or N1MM+ respectively, the Gateway will also log the QSO to [DXKeeper](https://www.dxlabsuite.com/dxkeeper/).

In N1MM+ version 7336 or later, typing a callsign and striking the **Space** bar will direct  
[DXView](https://www.dxlabsuite.com/dxview/) and [Pathfinder](https://www.dxlabsuite.com/pathfinder/) to perform “lookup” operations, if they are running. If the Gateway’s **Lookup previous QSOs** box is checked, [DXKeeper](https://www.dxlabsuite.com/dxkeeper/) will also be directed to display all previous QSOs with the newly-logged callsign. Note: the display of some web pages will cause [Pathfinder](https://www.dxlabsuite.com/pathfinder/) to steal mouse cursor focus from N1MM.

Clicking this window’s **Help** button displays the web page you’re now reading.

An indication in red font will appear if new entries have been placed in an Errorlog.txt file in the folder in which the Gateway resides; if this happens, please [<u style="box-sizing: border-box;">report<br style="box-sizing: border-box;">it</u>](mailto:DXLab@groups.io) to the DXLab Discussion Group. During normal operation, the Gateway’s main window can be minimized; its primary purpose is to permit the Gateway to be terminated when desired.

With the Gateway running, logging a QSO in  [N1MM+](https://n1mmwp.hamdocs.com/setup/interfacing/) will log a QSO with the following [ADIF fields](https://www.dxlabsuite.com/ADIF.htm#ADIF-defined%20Fields) in [DXKeeper](https://www.dxlabsuite.com/dxkeeper/):

- BAND

- BAND_RX

- CALL

- COMMENT

- CONTEST_ID

- FREQ

- FREQ_RX

- GRIDSQUARE

- MODE

- QSO_DATE

- NAME

- OPERATOR

- PFX

- QTH

- RST_RCVD

- RST_SENT

- SRX

- SRX_STRING (populated  
  from N1MM’s Exchange1field)

- STATION_CALLSIGN

- STX

- TIME_ON

- RX_PWR (K or KW will be recorded as 1000; QRP will be recorded as 5)

If you have questions or suggestions, please [<u style="box-sizing: border-box;">send them via email</u>](mailto:DXLab@groups.io) to the DXLab Discussion Group.

## Theory of Operation

In Process…

## Pending Changes

Please note that the Gateway does not handle the Log Editing and Log deleting capabilities that the N1MM UDP messages support. That is implemented in this beta version: [N1MM_DXK_GW_1.3.3](https://ny4i.com/wp-content/uploads/2026/08/N1MM_DXK_GW_1.3.3.zip)

[NY4I](https://ny4i.com/)

Amateur Radio Station NY4I

[Mission News Theme](https://www.competethemes.com/mission-news/) by Compete Themes.
