==============================================================================
 MACHINE TRANSLATION into zh-Hans. The English README.txt is
 authoritative; where this file disagrees with it, it is this file
 that is wrong. Corrections are very welcome.

 source-sha256: e67ddcc749a9e9c5
==============================================================================

N1MM-DXKeeper Gateway 2.0
=========================

卡莉丝 QSOs 登录 TR4W 或 N1MM Logger+ 直接进入 DXKeeper,并询问 DXView 和 Pathfinder 查查你工作的呼号

完整文档: https://ny4i.com/n1mm-dxkeeper-gateway/


在你开始之前
------

1. 联合国 DXKeeper 必须安装。 门户本身无所事事;它是
   之间。

2. 微软.NET 8 DESKTOP Truntime, 64位(x64).

   如果你已经运行 JTAlert 2.80或以后,你有它 - JTAlert 请检查access-date=中的日期值 (帮助)
   同样的东西,它只需要安装一次。 Windows 不断更新
   之后作为正常的一部分 Windows Update。 。 。 。

   如果 Gateway 无法启动, 或 Windows 提议寻找
   某种东西,这就是所缺少的:

       https://dotnet.microsoft.com/download/dotnet/8.0

   选择 "Desktop Runtime",x64 (中文(简体) ). 不是SDK,也不是平原 ". NET
   运行时间" - Desktop Runtime 是包含这个程序的一个
   需要帮助。 较早的 VB6 网关不需要安装; 这个是一个
   重写和做。

3个 Windows 10 或 Windows 11。 。 。 。


运行它
---

从头开始 Start menu,或者从桌面快捷键上,如果您要安装一个。

启动通道, DXKeeper, (中文). DXView, (中文). Pathfinder 和任何顺序的伐木工。 网关按显示连接到每个网关。

您的设置生活在 Windows 注册簿中, 在使用的 VB6 Gateway 的同一密钥下, 所以旧版本的设置会自行结转 。

那个 DXLab Launcher 可以在其它通道同时启动 DXLab 程序; 参见“指定一个非DXLab 应用程序的路径名"主题在启动器的帮助中.


指着你的懒汉
------

网关监听 UDP 端口 12060 默认。 您可以在其窗口的网络部分更改此内容 。

  N1MM Logger+   Config > Configure Ports ... > 翻译: Broadcast Data 选项卡。
                 选中“ 联系人 ” , 并设定地址 。
                 电脑 IPv4 地址和端口,例如: 192.168.1.11:12060
                 计数 "External Callsign Lookup" 并定其义.

  TR4W           设定 UDP BROADCAST ADDRESS 到同一地址和端口。

  WSJT-X         Settings > Reporting选中“可登录联系人” ADIF
                 输入您的 IP 地址 - 127.0.0.1 若为 WSJT-X 这是
                 在同一台电脑上 12060 输入 Server port number
                 字段。

                 我们建议你用 JTAlert 或者直接发送联系人
                 页:1 DXLab 应用程序; 参见 DXLab 说明。 这个
                 路线是可行的,但那些是更好的路线。

  SDR-Control    指向端口的伐木广播 12060。 。 。 。

如果你一次跑一次以上,小心不要有同样的 QSO 两次访问网关 - 例如 WSJT-X 直接广播到网关和供餐 N1MM,然后广播它。 DXKeeper
无法检测重复,并将同时登录。


在DXKEEPER上标注
------------

没什么好配置的 网关读取 DXKeeper属于自己的 Base Port 设置和使用它。 如果你改变了 DXKeeper因为 Base Port
(单位:千美元)Config > Defaults tab > Network Service),然后重新启动“网关”。

同一个面板的方向告诉你 DXKeeper'网络服务正在监听. 如果Gateway报告它无法连接,请先看那里.


什么风景
----

  Settings          UDP 端口,可选多播组,什么 DXKeeper 应
                    分别执行 QSO (电话簿查询, eQSL, (中文). LoTW, (中文). Club Log), (中文(简体) ).
                    日志选项和界面语言.

  Connection Status DXKeeper, (中文). DXView 和 Pathfinder。断开连接是正常的
                    您没有运行程序 。

  Operation Log     网关所做的,最新鲜的底部。 问题
                    颜色。 这是第一个看的地方,和
                    复制按钮将其放置在剪贴板上以获取错误报告 。

最小化将“网关”置于通知区(按时钟),而不是任务栏,它会保留它收到和记录的内容的行数。 Windows 11 默认情况下隐藏新的通知图标 -
如果您想要看到, 请将其从“ 隐藏图标” 中拖出到任务栏 。 关闭窗口退出 Gateway 。


变化/选举 QSOs 和丑闻
--------------

打开前读取此内容 Upload to eQSL.cc, (中文). Upload to LoTW 或 Upload to Club Log。 。 。 。

那些切换告诉 DXKeeper 上传每个 QSO 登录到在线日志。 另外,Gateway支持编辑和删除 QSOs: 当您的日志发送更改时, 网关会删除
QSO 从 DXKeeper 并记录更正的,因为 DXKeeper 没有单一的“替换”操作。

这两种特征的结合并不好,无论是门户还是网络。 DXKeeper 可以让他们。 已经退出的上传无法被召回 。 LoTW 特别是无法删除 QSO 您已经上传。
所以说 QSO 上传,然后编辑,使“原产地”处于 LoTW 永远,在它旁边加上更正,而不是替换它。 页:1 QSO 上传,然后删除 LoTW
在它从你的日志中消失之后。

在网关支持编辑和删除之前,不可能出现这种情况: QSO 它的登录是最终的。

怎么办呢?

作者使用的直截了当的答案是,让所有三个上传切换在比赛中切换,然后从上传 DXKeeper 一旦记录为最后记录,并作了任何更正,即用手进行。 DXKeeper
上传整个日志像一个一样容易 QSO,到那时已经没有什么可以纠正的了.

如果您喜欢, 请打开它 - 网关会警告您一次, 然后按它所被告知的去做 - 但请注意, 以后的更正不会干净地到达在线日志 。

这不适用于: Query Callbook 或 Lookup previous QSOs。这些只读。


问题
---

两者都出现在Gateway自己的文件夹中. 如果Gateway安装在 Windows 的某处, 它不让它写 - 下 C:\Program 例如文件 -
它使用一个每个用户文件夹来代替,并记录哪个位于顶端 ErrorLog.txt。 。 。 。

  ErrorLog.txt          诊断 红色的see ErrorLog" 链接出现在
                        窗口中,当已写入某物时。 选中
                        " , "Log debugging information" 对于更多细节,当
                        追寻一个问题。

  FailedQSOs_<date>_<time>.adi
                        QSOs DXKeeper 未确认。 重要:门户
                        从不默默抛弃 QSO但它也永远不会
                        重复一次,因为 DXKeeper 不检测
                        复制和重试可以登录两次。 如果这样
                        文件已存在,导入到 DXKeeper 手边然后
                        删除。 "Failed QSOs" 在窗底
                        发生时随计数而变为红色;单击以
                        打开选中的文件文件夹 。 倒计时
                        当文件丢失时返回到零。

                        每个运行一个文件 。 跑去一无所有,没有
                        文件,所以文件的存在总是意味着什么
                        需要你的关注。


A级 QSO 不确认
----------

  - 难道 Operation Log 显示 QSO 收到吗? 如果不是的话,日志是
    未到达 Gateway : 检查地址和端口, 并检查防火墙
    没有屏蔽 UDP。 。 。 。

  - 它是否显示它被发送 但没有确认? DXKeeper 不承认
    这个 检查 DXKeeper 正在运行,并且 Network Service 说 Listening。 。 。 。
    那个 QSO 将会在 FailedQSOs。 。 。 。

  - 怎么样? DXKeeper 在繁忙的比赛中可以跑几秒钟。 网关
    发送一个 QSO 时间等待 DXKeeper 来确认每个,所以
    积压是正常的,而排水量是自己的。


语言
---

网关遵循您的 Windows 显示语言, 如果它有翻译, 您可以在设置 > 下明确选择一个 将军 改变在下次开始时生效。

英语以外的翻译是机器制作的,由志愿者纠正. 如果您读得不好, 校正非常受欢迎,


证据
---

在GNU通用公共许可版本3下或之后的自由软件,与ABSOLUTELY NO WARRANTY. 全文在 COPYING.txt· ; NOTICE.txt
记录版权、第三方部件及其许可证。

你可以把它用于任何目的,研究它是如何工作的,分享它并改变它.


帮助
---

  Documentation   https://ny4i.com/n1mm-dxkeeper-gateway/
  Questions       DXLab 讨论小组, DXLab@groups.io

当报告问题时,The About window's "Copy details" 按钮将版本和您的环境放在剪贴板上。 请包括该部分和本报告的有关部分。
Operation Log 或 ErrorLog.txt。 。 。 。
