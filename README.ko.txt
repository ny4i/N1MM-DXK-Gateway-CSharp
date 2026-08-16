==============================================================================
 MACHINE TRANSLATION into ko. The English README.txt is
 authoritative; where this file disagrees with it, it is this file
 that is wrong. Corrections are very welcome.

 source-sha256: e67ddcc749a9e9c5
==============================================================================

N1MM-DXKeeper Gateway 2.0
=========================

카리스마 QSOs 로그인 TR4W 또는 N1MM Logger+ 연결하기 DXKeeper· DXView 이름 * Pathfinder 당신이
일할 호출을 찾습니다.

전체 문서: https://ny4i.com/n1mm-dxkeeper-gateway/


시작하기
----

1. 명세 DXKeeper 설치해야합니다. Gateway는 그 자체에 아무것도 없다; 그것은
   ღ♥ღ

2. Microsoft .NET 8 DESKTOP 실행 시간, 64 비트 (x64).

   이미 실행중인 경우 JTAlert 2.80 이상, 당신은 그것을 가지고 - JTAlert 요구 사항
   같은 것은 한 번만 설치해야합니다. Windows는 그것을 새롭게 지킵니다
   정상의 부분으로 후뒤 Windows Update·

   Gateway가 시작되지 않을 경우, 또는 Windows가 찾고있는 것을 제공합니다.
   뭔가, 이것은 누락 된 것 이다:

       https://dotnet.microsoft.com/download/dotnet/8.0

   선택 "Desktop Runtime", x64. SDK가 아닌 일반 ".NET
   실행 시간 - Desktop Runtime 이 프로그램을 포함하는 것
   지원하다 이전 VB6 게이트웨이는 그런 설치가 필요하지 않습니다. 이 것은
   재 작성 및 수행.

3. 명세 Windows 10 또는 Windows 11·


런닝 IT
-----

시작하기 Start menu, 또는 데스크탑 단축키에서 당신은 하나에 대한 설치자를 물었다.

Gateway 시작하기 DXKeeper· DXView· Pathfinder 그리고 어떤 순서든지에 너의 통나무. Gateway는 각각에
연결됩니다.

설정은 Windows 레지스트리에서 라이브, 같은 키 아래 VB6 게이트웨이 사용, 그래서 이전 버전에서 설정은 스스로 수행.

더 보기 DXLab Launcher 다른 곳에서 게이트웨이를 시작할 수 있습니다. DXLab 프로그램; "비를 지정DXLab
application's pathname" 런처의 도움에 대한 주제.


IT에 LOGGER를 설치
--------------

게이트웨이는 UDP 항구 항구 12060 기본적으로. 창의 네트워크 섹션에서 변경할 수 있습니다.

  N1MM Logger+   Config > Configure Ports ... · Broadcast Data 탭.
                 Tick "Contacts"와 주소를 당신의 옆에 설정
                 컴퓨터의 IPv4 주소 및 포트, 예를들면 192.168.1.11:12060
                 킥 "External Callsign Lookup"그리고 같은 방법을 설정합니다.

  TR4W           설치하기 UDP BROADCAST ADDRESS 동일한 주소와 항구에.

  WSJT-X         Settings > Reporting. Tick "알 수 없는 로그인 연락처 ADIF
                 방송" 및 IP 주소를 입력 - 127.0.0.1 이름 * WSJT-X 이름 *
                 이 같은 컴퓨터에서 - 그리고 12060 으로 Server port number
                 이름 *

                 우리는 당신을 건의합니다 JTAlert 대신, 또는 직접 접촉을 보냅니다
                 으로 DXLab 신청; 보기 DXLab 이름 * 으로
                 경로는 작동하지만 더 나은 여행 경로입니다.

  SDR-Control    포트에서 로깅 방송 12060·

당신이 한 번에 이것의 하나 이상을 실행하면, 같은 것을 조심하지 QSO Gateway에 두 번 도달 - 예를 들어 WSJT-X
Gateway 및 Feed에 직접 방송 N1MM, 그런 다음 그것을 방송. DXKeeper 중복을 감지하지 않고 모두 로그 할 수 없습니다.


DXKEEPER에 IT 설치
---------------

구성할 수 없습니다. Gateway는 읽기 DXKeeper이름 * Base Port 설정 및 사용. 당신은 DXKeeper이름 * Base
Port (주)Config > Defaults tab > Network Service), 후에 Gateway를 다시 시작합니다.

같은 패널의 heading은 당신이 알고 DXKeeper'네트워크 서비스' 게이트웨이가 연결될 수 없는 보고서가 있다면 먼저 살펴보십시오.


WINDOW 쇼
--------

  Settings          UDP 항구, 선택적인 multicast 그룹, 무엇 DXKeeper 뚱 베어
                    서로 QSO (콜북 조회, eQSL· LoTW· Club Log),
                    로그인 옵션 및 인터페이스 언어.

  Connection Status DXKeeper· DXView 이름 * Pathfinder. 분리는 정상입니다
                    당신이 달리지 않는 프로그램.

  Operation Log     Gateway가 수행 한 것은 바닥에서 가장 최신입니다. 문제 해결
                    색상입니다. 이것은 첫 번째 장소이며,
                    복사 버튼은 버그 보고서에 클립보드에 넣어.

Minimising은 Taskbar보다는 알림 영역 (시계값)의 게이트웨이를 넣었습니다. 즉, 수신하고 로그인한 작업의 실행 수를
유지하십시오. Windows 11 기본적으로 새로운 알림 아이콘을 숨기십시오. 그것을보고 싶다면 "숨겨진 아이콘"을 "숨겨진 아이콘"을
태우십시오. 창 닫기는 Gateway를 종료합니다.


프로젝트 QSOs 그리고 UPLOAD TOGGLES
----------------------------

전환하기 전에이 읽기 Upload to eQSL.cc· Upload to LoTW 또는 Upload to Club Log·

그들 toggles 말한다 DXKeeper 각 업로드 QSO 온라인 로그북으로 즉시 로그인됩니다. 별도의 Gateway는 편집 및 삭제를
지원합니다. QSOs: Logger가 변경을 보낼 때 Gateway deletes QSO 이름 * DXKeeper 그리고 정확한 것을
기록하기 때문에 DXKeeper 단일 "replace"작업이 없습니다.

두 가지 특징은 잘 결합하지 않으며 게이트웨이도 없습니다. DXKeeper 그들을 만들 수 있습니다. 이미 사라지는 업로드는 회신 할 수
없습니다. LoTW 특히 삭제할 방법이 없습니다. QSO 견적 요청 한국어 QSO 업로드 한 다음 편집은 ORIGINAL 서서 LoTW 그
외에는 수정이 추가되었습니다. · QSO 업로드 및 삭제 된 숙박 LoTW 로그인 후 로그인합니다.

게이트웨이 지원 편집 및 삭제하기 전에, 이것은 발생 할 수 없습니다 : 모든 QSO 마지막으로 기록되었습니다.

IT에 대해

스트레이트로워드 답변, 그리고 하나의 저자 사용, 모든 세 개의 업로드 toggles 전환을 해제하는 동안 경연, 및 업로드 DXKeeper
로그인이 완료되면 모든 수정이 이루어집니다. DXKeeper 전체 로그를 쉽게 업로드 QSO, 그리고 그 후에 아무 것도 정정하지 않습니다.

당신이 선호하는 경우에 그들을 전환 - 게이트웨이는 한 번 경고하고 그대로 말했다 -하지만 나중에 수정이 온라인 로그 북에 도달하지 않는
인식한다.

이것은 적용되지 않습니다. Query Callbook 또는 Lookup previous QSOs. 그만 읽기.


파일 IT WRITES
------------

모두 Gateway의 자체 폴더에 나타납니다. Gateway가 Windows를 설치 한 경우 쓰기 할 수 없습니다 - 아래
C:\Program 파일, 예를 들어 - 대신 per-user 폴더를 사용하고, 상단의 기록 ErrorLog.txt·

  ErrorLog.txt          진단. 빨간 "see ErrorLog" 링크가 나타납니다.
                        뭔가 그것을 쓸 때 창. 뚱 베어
                        ·Log debugging information"더 많은 세부 사항
                        문제.

  FailedQSOs_<date>_<time>.adi
                        QSOs DXKeeper 확인하지 않았다. 중요 : 게이트웨이
                        결코 조용히 discards QSO, 그러나 그것은 또한 결코
                        retries 하나, 때문에 DXKeeper 감지되지 않음
                        중복과 재try는 두 번 로그인 할 수 있습니다. 이름 *
                        파일이 존재합니다. DXKeeper 손으로 그 후에
                        삭제. "Failed QSOs"창의 하단에
                        이 일이 일어날 때 계산으로 빨간색을 회전; 그것을 클릭
                        선택된 파일로 폴더를 엽니다. 숫자가 간다
                        파일이 사라지면 0으로 다시.

                        실행 당 하나의 파일. 아무것도 잎을 잃지 않는 실행
                        파일, 그래서 기존의 파일은 항상 무언가를 의미
                        당신의 관심


지원하다 QSO 자주 묻는 질문
-----------------

  - 있음 Operation Log 더 보기 QSO 접수중? 그렇지 않다면, logger는
    Gateway에 도달하지 않음: 주소와 포트를 확인하고 방화벽을 확인
    차단하지 않음 UDP·

  - 전송되었지만 확인되지 않습니까? DXKeeper 인정하지 않음
    이름 * 기타 DXKeeper 실행 및 그 Network Service 이름 * Listening·
    더 보기 QSO 에 있다 FailedQSOs·

  - - - DXKeeper 바쁜 대회 중 몇 초를 실행할 수 있습니다. 게이트웨이
    전송 1 QSO 시간 및 대기 DXKeeper 각을 확인하기 위해, 그래서
    backlog는 정상이고 그것의 자신의 하수구입니다.


이름 *
----

Gateway는 Windows 디스플레이 언어를 따라 번역이 필요한 경우 설정에서 명시적으로 선택할 수 있습니다. > 일반. 변경이 시작된
다음 시간이 걸립니다.

영어 이외의 번역은 기계 제작 및 자원 봉사에 의해 수정됩니다. 간단히 읽을 경우, 보정은 매우 환영 - 번역자의 이름은 약 창에
나타납니다.


제품정보
----

GNU General Public License version 3 이상에서 무료 소프트웨어, ABSOLLY NO WARRANTY. 전체
텍스트는 COPYING.txt· NOTICE.txt 저작권, 제 3 자 구성 요소 및 그 라이온을 기록합니다.

당신은 어떤 목적으로 사용할 수 있습니다, 그것이 작동하는 방법을 연구, 그것을 공유하고 그것을 변경.


사이트맵
----

  Documentation   https://ny4i.com/n1mm-dxkeeper-gateway/
  Questions       DXLab 토론 그룹, DXLab@groups.io

문제를 보고할 때, 약 창의 "Copy details"버튼은 클립보드에 버전과 환경을 넣습니다. 해당 이용 후기에 달린 코멘트가 없습니다.
Operation Log 또는 ErrorLog.txt·
