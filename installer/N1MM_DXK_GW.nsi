; N1MM-DXKeeper Gateway - NSIS installer
; SPDX-License-Identifier: GPL-3.0-or-later
;
; Build with:   "C:\Program Files (x86)\NSIS\makensis.exe" installer\N1MM_DXK_GW.nsi
; Payload from: dotnet publish N1MM_DXK_GW -c Release -o installer\stage

Unicode true

!define APPNAME     "N1MM-DXKeeper Gateway"
!define SHORTNAME   "N1MM-DXK-Gateway"
!define EXENAME     "N1MM_DXK_GW.exe"
!define VERSION     "2.0.0"
!define PUBLISHER   "NY4I"
!define WEBSITE     "https://ny4i.com/n1mm-dxkeeper-gateway/"
!define DOTNETURL   "https://dotnet.microsoft.com/download/dotnet/8.0"

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "FileFunc.nsh"

Name "${APPNAME} ${VERSION}"
OutFile "N1MM_DXK_GW_${VERSION}_Setup.exe"
RequestExecutionLevel admin
SetCompressor /SOLID lzma

; NOT under Program Files, deliberately. The gateway writes ErrorLog.txt and
; the failed-QSO file beside itself, and Program Files is read-only to a
; standard user - the program copes by falling back to a per-user folder, but
; then "look in the Gateway's folder" stops being a complete support answer.
; A plain writable folder keeps everything in one place, which is also where
; the DXLab programs live.
InstallDir "C:\Radio\${SHORTNAME}"
InstallDirRegKey HKLM "Software\${SHORTNAME}" "InstallDir"

VIProductVersion "${VERSION}.0"
VIAddVersionKey "ProductName"     "${APPNAME}"
VIAddVersionKey "CompanyName"     "${PUBLISHER}"
VIAddVersionKey "FileDescription" "${APPNAME} Setup"
VIAddVersionKey "FileVersion"     "${VERSION}"
VIAddVersionKey "LegalCopyright"  "Copyright (C) 2026 Tom Schaefer, NY4I. GPLv3."

Var ReadmeFile

!define MUI_ABORTWARNING
!define MUI_ICON   "..\N1MM_DXK_GW\N1MM_DXK_GW.ico"
!define MUI_UNICON "..\N1MM_DXK_GW\N1MM_DXK_GW.ico"

; The licence is shown for information, not as a contract. The GPL governs
; copying and modification; section 9 says outright that it need not be
; accepted in order to run the program, so the usual "you must accept to
; install" wording would assert a restriction the licence does not make.
!define MUI_LICENSEPAGE_BUTTON "$(^NextBtn)"
!define MUI_LICENSEPAGE_TEXT_BOTTOM "This program is free software and comes with ABSOLUTELY NO WARRANTY. You may use, study, share and change it. Press Next to continue."

!insertmacro MUI_PAGE_LICENSE "..\COPYING"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

; The finish page carries the read-me, in the operator's own language when one
; is installed. This is the moment somebody has just installed an unfamiliar
; program and has not been told anything about it yet.
!define MUI_FINISHPAGE_SHOWREADME "$INSTDIR\$ReadmeFile"
!define MUI_FINISHPAGE_SHOWREADME_TEXT "View the read-me"
!define MUI_FINISHPAGE_SHOWREADME_NOTCHECKED
!define MUI_FINISHPAGE_RUN "$INSTDIR\${EXENAME}"
!define MUI_FINISHPAGE_RUN_TEXT "Start the Gateway"
!define MUI_FINISHPAGE_LINK "Documentation and support"
!define MUI_FINISHPAGE_LINK_LOCATION "${WEBSITE}"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; English first so it is the fallback. NSIS picks by system language.
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "German"
!insertmacro MUI_LANGUAGE "Spanish"
!insertmacro MUI_LANGUAGE "French"
!insertmacro MUI_LANGUAGE "Italian"
!insertmacro MUI_LANGUAGE "Dutch"
!insertmacro MUI_LANGUAGE "Portuguese"
!insertmacro MUI_LANGUAGE "Russian"
!insertmacro MUI_LANGUAGE "Ukrainian"
!insertmacro MUI_LANGUAGE "Czech"
!insertmacro MUI_LANGUAGE "Slovak"
!insertmacro MUI_LANGUAGE "Finnish"
!insertmacro MUI_LANGUAGE "Swedish"
!insertmacro MUI_LANGUAGE "Norwegian"
!insertmacro MUI_LANGUAGE "Japanese"
!insertmacro MUI_LANGUAGE "Korean"
!insertmacro MUI_LANGUAGE "SimpChinese"

;--------------------------------------------------------------------------
; .NET 8 Desktop Runtime
;--------------------------------------------------------------------------
; Two checks, because neither alone is dependable. The registry key lives
; under the 32-bit view on the machine this was written on - an NSIS installer
; is 32-bit, so it reads that view by default - but the 64-bit view had no
; such key at all while the runtime was demonstrably installed. The folder
; probe is the backstop and was the one that told the truth there.

Function DotNetInstalled
   Push $0
   Push $1
   Push $2
   StrCpy $2 "0"

   StrCpy $1 0
   ${Do}
      EnumRegKey $0 HKLM \
         "SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App" $1
      ${If} $0 == ""
         ${ExitDo}
      ${EndIf}
      ${If} $0 >= "8.0"
      ${AndIf} $0 < "9.0"
         StrCpy $2 "1"
         ${ExitDo}
      ${EndIf}
      IntOp $1 $1 + 1
   ${Loop}

   ; EnumRegKey walks subkeys; the versions here are VALUES, so read them too.
   ${If} $2 != "1"
      StrCpy $1 0
      ${Do}
         EnumRegValue $0 HKLM \
            "SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App" $1
         ${If} $0 == ""
            ${ExitDo}
         ${EndIf}
         StrCpy $3 $0 2
         ${If} $3 == "8."
            StrCpy $2 "1"
            ${ExitDo}
         ${EndIf}
         IntOp $1 $1 + 1
      ${Loop}
   ${EndIf}

   ; Backstop: the runtime's own folder.
   ${If} $2 != "1"
      FindFirst $1 $0 "$PROGRAMFILES64\dotnet\shared\Microsoft.WindowsDesktop.App\8.*"
      ${If} $0 != ""
         StrCpy $2 "1"
      ${EndIf}
      FindClose $1
   ${EndIf}

   Pop $1
   Exch
   Pop $0
   Exch $2
FunctionEnd

;--------------------------------------------------------------------------
; Which read-me the finish page opens
;--------------------------------------------------------------------------
Function SelectReadme
   ; A Case and its body must be on separate lines - anything trailing the
   ; Case on the same line is read as extra macro parameters.
   StrCpy $ReadmeFile "README.txt"
   ${Switch} $LANGUAGE
      ${Case} ${LANG_GERMAN}
         StrCpy $ReadmeFile "README.de.txt"
         ${Break}
      ${Case} ${LANG_SPANISH}
         StrCpy $ReadmeFile "README.es.txt"
         ${Break}
      ${Case} ${LANG_FRENCH}
         StrCpy $ReadmeFile "README.fr.txt"
         ${Break}
      ${Case} ${LANG_ITALIAN}
         StrCpy $ReadmeFile "README.it.txt"
         ${Break}
      ${Case} ${LANG_DUTCH}
         StrCpy $ReadmeFile "README.nl.txt"
         ${Break}
      ${Case} ${LANG_PORTUGUESE}
         StrCpy $ReadmeFile "README.pt.txt"
         ${Break}
      ${Case} ${LANG_RUSSIAN}
         StrCpy $ReadmeFile "README.ru.txt"
         ${Break}
      ${Case} ${LANG_UKRAINIAN}
         StrCpy $ReadmeFile "README.uk.txt"
         ${Break}
      ${Case} ${LANG_CZECH}
         StrCpy $ReadmeFile "README.cs.txt"
         ${Break}
      ${Case} ${LANG_SLOVAK}
         StrCpy $ReadmeFile "README.sk.txt"
         ${Break}
      ${Case} ${LANG_FINNISH}
         StrCpy $ReadmeFile "README.fi.txt"
         ${Break}
      ${Case} ${LANG_SWEDISH}
         StrCpy $ReadmeFile "README.sv.txt"
         ${Break}
      ${Case} ${LANG_NORWEGIAN}
         StrCpy $ReadmeFile "README.nb.txt"
         ${Break}
      ${Case} ${LANG_JAPANESE}
         StrCpy $ReadmeFile "README.ja.txt"
         ${Break}
      ${Case} ${LANG_KOREAN}
         StrCpy $ReadmeFile "README.ko.txt"
         ${Break}
      ${Case} ${LANG_SIMPCHINESE}
         StrCpy $ReadmeFile "README.zh-Hans.txt"
         ${Break}
   ${EndSwitch}

   ${IfNot} ${FileExists} "$INSTDIR\$ReadmeFile"
      StrCpy $ReadmeFile "README.txt"
   ${EndIf}
FunctionEnd

Function .onInit
   !insertmacro MUI_LANGDLL_DISPLAY

   Call DotNetInstalled
   Pop $0
   ${If} $0 != "1"
      MessageBox MB_YESNO|MB_ICONEXCLAMATION \
"${APPNAME} needs the Microsoft .NET 8 Desktop Runtime (x64), which does not \
appear to be installed.$\r$\n$\r$\n\
If you run JTAlert 2.80 or later you already have it; it only has to be \
installed once.$\r$\n$\r$\n\
Open the download page now?$\r$\n\
Choose 'Desktop Runtime', x64 - not the SDK.$\r$\n$\r$\n\
You can continue installing without it, but the Gateway will not start until \
it is present." IDNO continue
      ExecShell "open" "${DOTNETURL}"
   continue:
   ${EndIf}
FunctionEnd

Function un.onInit
   !insertmacro MUI_UNGETLANGUAGE
FunctionEnd

;--------------------------------------------------------------------------
Section "Install"
   ; All Users, to match the rest of the install: this writes to HKLM and to a
   ; machine-wide folder, so shortcuts that appeared only for whoever happened
   ; to run the installer would be a surprise - typically on a shack computer
   ; where the operator installs as one account and operates as another.
   SetShellVarContext all

   ; A running copy holds its own exe open and the install would half-finish.
   DetailPrint "Closing any running copy of the Gateway..."
   nsExec::Exec 'taskkill /IM "${EXENAME}" /F'
   Pop $0
   Sleep 700

   SetOutPath "$INSTDIR"
   File /r "stage\*.*"

   ; Everything the operator might need to read, in one place.
   WriteRegStr HKLM "Software\${SHORTNAME}" "InstallDir" "$INSTDIR"
   WriteRegStr HKLM "Software\${SHORTNAME}" "Version"    "${VERSION}"

   CreateDirectory "$SMPROGRAMS\${APPNAME}"
   CreateShortcut  "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk" "$INSTDIR\${EXENAME}"
   CreateShortcut  "$SMPROGRAMS\${APPNAME}\Read me.lnk"    "$INSTDIR\README.txt"
   CreateShortcut  "$SMPROGRAMS\${APPNAME}\Uninstall.lnk"  "$INSTDIR\Uninstall.exe"
   CreateShortcut  "$DESKTOP\${APPNAME}.lnk"               "$INSTDIR\${EXENAME}"

   WriteUninstaller "$INSTDIR\Uninstall.exe"

   ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
   !define UNKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${SHORTNAME}"
   WriteRegStr   HKLM "${UNKEY}" "DisplayName"     "${APPNAME}"
   WriteRegStr   HKLM "${UNKEY}" "DisplayVersion"  "${VERSION}"
   WriteRegStr   HKLM "${UNKEY}" "Publisher"       "${PUBLISHER}"
   WriteRegStr   HKLM "${UNKEY}" "URLInfoAbout"    "${WEBSITE}"
   WriteRegStr   HKLM "${UNKEY}" "DisplayIcon"     "$INSTDIR\${EXENAME}"
   WriteRegStr   HKLM "${UNKEY}" "UninstallString" "$INSTDIR\Uninstall.exe"
   WriteRegDWORD HKLM "${UNKEY}" "NoModify" 1
   WriteRegDWORD HKLM "${UNKEY}" "NoRepair" 1
   WriteRegDWORD HKLM "${UNKEY}" "EstimatedSize" "$0"

   Call SelectReadme
SectionEnd

;--------------------------------------------------------------------------
Section "Uninstall"
   ; Must match the install, or the shortcuts are looked for in the wrong
   ; profile and left behind.
   SetShellVarContext all

   nsExec::Exec 'taskkill /IM "${EXENAME}" /F'
   Pop $0
   Sleep 700

   Delete "$DESKTOP\${APPNAME}.lnk"
   RMDir /r "$SMPROGRAMS\${APPNAME}"

   ; Everything the installer put here. ErrorLog.txt and any FailedQSOs file
   ; are deliberately NOT removed by the loop below - see after it.
   RMDir /r "$INSTDIR\third-party-licenses"
   Delete "$INSTDIR\*.dll"
   Delete "$INSTDIR\*.exe"
   Delete "$INSTDIR\*.json"
   Delete "$INSTDIR\*.pdb"
   Delete "$INSTDIR\README*.txt"
   Delete "$INSTDIR\COPYING.txt"
   Delete "$INSTDIR\NOTICE.txt"

   ; Satellite assemblies.
   RMDir /r "$INSTDIR\cs"
   RMDir /r "$INSTDIR\de"
   RMDir /r "$INSTDIR\es"
   RMDir /r "$INSTDIR\fi"
   RMDir /r "$INSTDIR\fr"
   RMDir /r "$INSTDIR\it"
   RMDir /r "$INSTDIR\ja"
   RMDir /r "$INSTDIR\ko"
   RMDir /r "$INSTDIR\nb"
   RMDir /r "$INSTDIR\nl"
   RMDir /r "$INSTDIR\pt"
   RMDir /r "$INSTDIR\ru"
   RMDir /r "$INSTDIR\sk"
   RMDir /r "$INSTDIR\sv"
   RMDir /r "$INSTDIR\uk"
   RMDir /r "$INSTDIR\zh-Hans"

   ; A FailedQSOs file holds QSOs DXKeeper never confirmed - the operator's own
   ; log data, and the whole point of writing it was that nothing throws a QSO
   ; away. Uninstalling is not consent to delete it. RMDir without /r removes
   ; the folder only when it is already empty, so anything left behind stays.
   RMDir "$INSTDIR"

   DeleteRegKey HKLM "Software\${SHORTNAME}"
   DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${SHORTNAME}"

   ; The gateway's own settings live under the VB6-compatible key and are
   ; deliberately left alone: reinstalling should find them, and they are
   ; shared with the VB6 gateway if that is still installed.
SectionEnd
