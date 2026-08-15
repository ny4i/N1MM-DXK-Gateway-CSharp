namespace N1MM_DXK_GW;

partial class MainForm
{
   private System.ComponentModel.IContainer components = null;

   protected override void Dispose(bool disposing)
   {
      if (disposing && (components != null))
      {
         components.Dispose();
      }
      base.Dispose(disposing);
   }

   #region Windows Form Designer generated code

   private void InitializeComponent()
   {
      components = new System.ComponentModel.Container();

      mainLayout = new TableLayoutPanel();
      settingsGroup = new GroupBox();
      settingsLayout = new TableLayoutPanel();
      udpPortLabel = new Label();
      udpPortTextBox = new TextBox();
      multicastLabel = new Label();
      multicastTextBox = new TextBox();
      dxkPortLabel = new Label();
      dxkPortValue = new Label();
      checkboxLayout = new TableLayoutPanel();
      dxkLookupCheck = new CheckBox();
      callbookCheck = new CheckBox();
      eqslCheck = new CheckBox();
      lotwCheck = new CheckBox();
      clubLogCheck = new CheckBox();
      logDebugInfoCheck = new CheckBox();
      verboseLoggingCheck = new CheckBox();
      statusGroup = new GroupBox();
      statusLayout = new TableLayoutPanel();
      dxkDot = new Label();
      dxkNameLabel = new Label();
      dxkStatusLabel = new Label();
      dxvDot = new Label();
      dxvNameLabel = new Label();
      dxvStatusLabel = new Label();
      pfDot = new Label();
      pfNameLabel = new Label();
      pfStatusLabel = new Label();
      leftStatusPanel = new FlowLayoutPanel();
      failedQsoLabel = new Label();
      failedQsoFileLink = new LinkLabel();
      failedQsoFolderLink = new LinkLabel();
      logGroup = new GroupBox();
      operationLogListBox = new ListBox();
      bottomPanel = new TableLayoutPanel();
      errorLogLink = new LinkLabel();
      buttonPanel = new FlowLayoutPanel();
      helpButton = new Button();
      showErrorLogButton = new Button();

      mainLayout.SuspendLayout();
      settingsGroup.SuspendLayout();
      settingsLayout.SuspendLayout();
      checkboxLayout.SuspendLayout();
      statusGroup.SuspendLayout();
      statusLayout.SuspendLayout();
      logGroup.SuspendLayout();
      bottomPanel.SuspendLayout();
      buttonPanel.SuspendLayout();
      SuspendLayout();

      //
      // mainLayout
      //
      mainLayout.Dock = DockStyle.Fill;
      mainLayout.ColumnCount = 1;
      mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
      mainLayout.RowCount = 4;
      mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
      mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      mainLayout.Padding = new Padding(10);
      mainLayout.Controls.Add(settingsGroup, 0, 0);
      mainLayout.Controls.Add(statusGroup, 0, 1);
      mainLayout.Controls.Add(logGroup, 0, 2);
      mainLayout.Controls.Add(bottomPanel, 0, 3);

      //
      // settingsGroup
      //
      settingsGroup.Text = "Settings";
      settingsGroup.Dock = DockStyle.Fill;
      settingsGroup.AutoSize = true;
      settingsGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      settingsGroup.Padding = new Padding(8);
      settingsGroup.Controls.Add(settingsLayout);

      //
      // settingsLayout
      //
      settingsLayout.Dock = DockStyle.Fill;
      settingsLayout.AutoSize = true;
      settingsLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      settingsLayout.ColumnCount = 3;
      settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
      settingsLayout.RowCount = 4;
      settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      settingsLayout.Controls.Add(udpPortLabel, 0, 0);
      settingsLayout.Controls.Add(udpPortTextBox, 1, 0);
      settingsLayout.Controls.Add(multicastLabel, 0, 1);
      settingsLayout.Controls.Add(multicastTextBox, 1, 1);
      settingsLayout.SetColumnSpan(multicastTextBox, 2);
      settingsLayout.Controls.Add(dxkPortLabel, 0, 2);
      settingsLayout.Controls.Add(dxkPortValue, 1, 2);
      settingsLayout.SetColumnSpan(dxkPortValue, 2);
      settingsLayout.Controls.Add(checkboxLayout, 0, 3);
      settingsLayout.SetColumnSpan(checkboxLayout, 3);

      //
      // udpPortLabel
      //
      udpPortLabel.Text = "UDP Listening Port:";
      udpPortLabel.AutoSize = true;
      udpPortLabel.Anchor = AnchorStyles.Left;
      udpPortLabel.Margin = new Padding(3, 6, 6, 6);

      //
      // udpPortTextBox
      //
      udpPortTextBox.Width = 80;
      udpPortTextBox.Anchor = AnchorStyles.Left;
      udpPortTextBox.Margin = new Padding(0, 3, 0, 6);

      //
      // multicastLabel
      //
      multicastLabel.Text = "Multicast group (optional):";
      multicastLabel.AutoSize = true;
      multicastLabel.Anchor = AnchorStyles.Left;
      multicastLabel.Margin = new Padding(3, 6, 6, 6);

      //
      // multicastTextBox
      //
      // Wider than the port box: it holds a dotted-quad group address.
      multicastTextBox.Width = 160;
      multicastTextBox.Anchor = AnchorStyles.Left;
      multicastTextBox.Margin = new Padding(0, 3, 0, 6);
      multicastTextBox.PlaceholderText = "blank = no multicast";

      //
      // dxkPortLabel
      //
      dxkPortLabel.Text = "DXKeeper TCP base port:";
      dxkPortLabel.AutoSize = true;
      dxkPortLabel.Anchor = AnchorStyles.Left;
      dxkPortLabel.Margin = new Padding(3, 6, 6, 6);

      //
      // dxkPortValue
      //
      // Plain Label for read-only display. Using Label (not a borderless
      // TextBox) keeps the text baseline aligned with the dxkPortLabel in
      // the adjacent column — a borderless TextBox has different internal
      // padding and renders a few pixels lower.
      dxkPortValue.AutoSize = true;
      dxkPortValue.Anchor = AnchorStyles.Left;
      dxkPortValue.Margin = new Padding(0, 6, 6, 6);
      dxkPortValue.Text = "(reading from registry…)";

      //
      // checkboxLayout
      //
      checkboxLayout.Dock = DockStyle.Fill;
      checkboxLayout.AutoSize = true;
      checkboxLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      checkboxLayout.ColumnCount = 2;
      checkboxLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
      checkboxLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
      checkboxLayout.RowCount = 4;
      checkboxLayout.Margin = new Padding(0, 6, 0, 0);
      checkboxLayout.Controls.Add(dxkLookupCheck, 0, 0);
      checkboxLayout.Controls.Add(callbookCheck, 1, 0);
      checkboxLayout.Controls.Add(eqslCheck, 0, 1);
      checkboxLayout.Controls.Add(lotwCheck, 1, 1);
      checkboxLayout.Controls.Add(clubLogCheck, 0, 2);
      checkboxLayout.Controls.Add(logDebugInfoCheck, 1, 2);
      checkboxLayout.Controls.Add(verboseLoggingCheck, 1, 3);

      ConfigureCheckbox(dxkLookupCheck, "Lookup previous QSOs",
         "Direct DXKeeper to display previous QSOs with the logged callsign");
      ConfigureCheckbox(callbookCheck, "Query Callbook",
         "Direct DXKeeper to query its selected callbook");
      ConfigureCheckbox(eqslCheck, "Upload to eQSL.cc",
         "Direct DXKeeper to upload logged QSO to eQSL.cc");
      ConfigureCheckbox(lotwCheck, "Upload to LoTW",
         "Direct DXKeeper to upload logged QSO to LoTW");
      ConfigureCheckbox(clubLogCheck, "Upload to Club Log",
         "Direct DXKeeper to upload logged QSO to Club Log");
      ConfigureCheckbox(logDebugInfoCheck, "Log debugging information",
         "Log debugging information to ErrorLog.txt");
      ConfigureCheckbox(verboseLoggingCheck, "Verbose logging",
         "Show low-priority status events in the operation log (e.g. DDE connect/disconnect)");
      // Indented sub-option of "Log debugging information"; the extra left
      // margin replaces the default 3 set by ConfigureCheckbox.
      verboseLoggingCheck.Margin = new Padding(24, 3, 12, 3);

      //
      // statusGroup
      //
      statusGroup.Text = "Connection Status";
      statusGroup.Dock = DockStyle.Fill;
      statusGroup.AutoSize = true;
      statusGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      statusGroup.Padding = new Padding(8);
      statusGroup.Controls.Add(statusLayout);

      //
      // statusLayout
      //
      statusLayout.Dock = DockStyle.Fill;
      statusLayout.AutoSize = true;
      statusLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      statusLayout.ColumnCount = 3;
      statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      statusLayout.RowCount = 3;
      statusLayout.Controls.Add(dxkDot, 0, 0);
      statusLayout.Controls.Add(dxkNameLabel, 1, 0);
      statusLayout.Controls.Add(dxkStatusLabel, 2, 0);
      statusLayout.Controls.Add(dxvDot, 0, 1);
      statusLayout.Controls.Add(dxvNameLabel, 1, 1);
      statusLayout.Controls.Add(dxvStatusLabel, 2, 1);
      statusLayout.Controls.Add(pfDot, 0, 2);
      statusLayout.Controls.Add(pfNameLabel, 1, 2);
      statusLayout.Controls.Add(pfStatusLabel, 2, 2);

      ConfigureStatusRow(dxkDot, dxkNameLabel, dxkStatusLabel, "DXKeeper:");
      ConfigureStatusRow(dxvDot, dxvNameLabel, dxvStatusLabel, "DXView:");
      ConfigureStatusRow(pfDot, pfNameLabel, pfStatusLabel, "Pathfinder:");

      //
      // logGroup
      //
      logGroup.Text = "Operation Log";
      logGroup.Dock = DockStyle.Fill;
      logGroup.Padding = new Padding(8);
      logGroup.Controls.Add(operationLogListBox);

      //
      // operationLogListBox
      //
      operationLogListBox.Dock = DockStyle.Fill;
      operationLogListBox.IntegralHeight = false;
      operationLogListBox.Font = new Font("Consolas", 9F);
      operationLogListBox.HorizontalScrollbar = true;

      //
      // bottomPanel
      //
      bottomPanel.Dock = DockStyle.Fill;
      bottomPanel.AutoSize = true;
      bottomPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      bottomPanel.ColumnCount = 2;
      bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
      bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      bottomPanel.RowCount = 1;
      bottomPanel.Margin = new Padding(0, 6, 0, 0);
      bottomPanel.Controls.Add(leftStatusPanel, 0, 0);
      bottomPanel.Controls.Add(buttonPanel, 1, 0);

      //
      // leftStatusPanel
      //
      // Standing status, bottom-left: the ErrorLog link and the count of QSOs
      // this session could not deliver. A counter rather than a banner — it
      // needs to be noticed, not acknowledged, and it clears itself when the
      // file is imported and deleted.
      leftStatusPanel.AutoSize = true;
      leftStatusPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      leftStatusPanel.FlowDirection = FlowDirection.LeftToRight;
      leftStatusPanel.WrapContents = false;
      leftStatusPanel.Anchor = AnchorStyles.Left;
      leftStatusPanel.Margin = new Padding(0);
      leftStatusPanel.Controls.Add(errorLogLink);
      leftStatusPanel.Controls.Add(failedQsoLabel);
      leftStatusPanel.Controls.Add(failedQsoFileLink);
      leftStatusPanel.Controls.Add(failedQsoFolderLink);

      //
      // failedQsoLabel / failedQsoFileLink / failedQsoFolderLink
      //
      // All three hidden together when nothing has been stranded.
      failedQsoLabel.AutoSize = true;
      failedQsoLabel.Anchor = AnchorStyles.Left;
      failedQsoLabel.ForeColor = Color.FromArgb(150, 60, 0);
      failedQsoLabel.Margin = new Padding(12, 8, 3, 3);
      failedQsoLabel.Visible = false;
      failedQsoLabel.Text = "0 QSOs not delivered — open";

      failedQsoFileLink.Text = "file";
      failedQsoFileLink.AutoSize = true;
      failedQsoFileLink.Anchor = AnchorStyles.Left;
      failedQsoFileLink.LinkBehavior = LinkBehavior.HoverUnderline;
      failedQsoFileLink.Margin = new Padding(0, 8, 3, 3);
      failedQsoFileLink.Visible = false;

      failedQsoFolderLink.Text = "folder";
      failedQsoFolderLink.AutoSize = true;
      failedQsoFolderLink.Anchor = AnchorStyles.Left;
      failedQsoFolderLink.LinkBehavior = LinkBehavior.HoverUnderline;
      failedQsoFolderLink.Margin = new Padding(0, 8, 3, 3);
      failedQsoFolderLink.Visible = false;

      //
      // errorLogLink
      //
      errorLogLink.Text = "see ErrorLog";
      errorLogLink.AutoSize = true;
      errorLogLink.Anchor = AnchorStyles.Left;
      errorLogLink.Visible = false;
      errorLogLink.LinkBehavior = LinkBehavior.HoverUnderline;
      errorLogLink.LinkColor = Color.Red;
      errorLogLink.Margin = new Padding(3, 8, 3, 3);

      //
      // buttonPanel
      //
      buttonPanel.AutoSize = true;
      buttonPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      buttonPanel.FlowDirection = FlowDirection.LeftToRight;
      buttonPanel.WrapContents = false;
      buttonPanel.Margin = new Padding(0);
      buttonPanel.Controls.Add(showErrorLogButton);
      buttonPanel.Controls.Add(helpButton);

      //
      // showErrorLogButton
      //
      showErrorLogButton.Text = "Display Error Log";
      showErrorLogButton.AutoSize = true;
      showErrorLogButton.Padding = new Padding(8, 2, 8, 2);
      showErrorLogButton.Margin = new Padding(0, 0, 6, 0);

      //
      // helpButton
      //
      helpButton.Text = "Help";
      helpButton.AutoSize = true;
      helpButton.Padding = new Padding(12, 2, 12, 2);
      helpButton.Margin = new Padding(0);

      //
      // MainForm
      //
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(720, 540);
      MinimumSize = new Size(560, 460);
      Controls.Add(mainLayout);
      Text = "N1MM-DXKeeper Gateway";

      buttonPanel.ResumeLayout(false);
      buttonPanel.PerformLayout();
      bottomPanel.ResumeLayout(false);
      bottomPanel.PerformLayout();
      logGroup.ResumeLayout(false);
      statusLayout.ResumeLayout(false);
      statusLayout.PerformLayout();
      statusGroup.ResumeLayout(false);
      statusGroup.PerformLayout();
      checkboxLayout.ResumeLayout(false);
      checkboxLayout.PerformLayout();
      settingsLayout.ResumeLayout(false);
      settingsLayout.PerformLayout();
      settingsGroup.ResumeLayout(false);
      settingsGroup.PerformLayout();
      mainLayout.ResumeLayout(false);
      mainLayout.PerformLayout();
      ResumeLayout(false);
   }

   private static void ConfigureCheckbox(CheckBox cb, string text, string toolTip)
   {
      cb.Text = text;
      cb.AutoSize = true;
      cb.Anchor = AnchorStyles.Left;
      cb.Margin = new Padding(3, 3, 12, 3);
      // ToolTip attached at runtime in MainForm
      cb.Tag = toolTip;
   }

   private static void ConfigureStatusRow(Label dot, Label name, Label status, string nameText)
   {
      dot.Text = "●"; // BLACK CIRCLE
      dot.AutoSize = true;
      dot.Anchor = AnchorStyles.Left;
      dot.Font = new Font("Segoe UI Symbol", 11F);
      dot.ForeColor = Color.Gray;
      dot.Margin = new Padding(0, 3, 4, 3);

      name.Text = nameText;
      name.AutoSize = true;
      name.Anchor = AnchorStyles.Left;
      name.Margin = new Padding(0, 6, 8, 3);
      name.MinimumSize = new Size(90, 0);

      status.Text = "not connected";
      status.AutoSize = true;
      status.Anchor = AnchorStyles.Left;
      status.ForeColor = SystemColors.GrayText;
      status.Margin = new Padding(0, 6, 0, 3);
   }

   #endregion

   private TableLayoutPanel mainLayout;
   private GroupBox settingsGroup;
   private TableLayoutPanel settingsLayout;
   private Label udpPortLabel;
   private TextBox udpPortTextBox;
   private Label multicastLabel;
   private TextBox multicastTextBox;
   private Label dxkPortLabel;
   private Label dxkPortValue;
   private TableLayoutPanel checkboxLayout;
   private CheckBox dxkLookupCheck;
   private CheckBox callbookCheck;
   private CheckBox eqslCheck;
   private CheckBox lotwCheck;
   private CheckBox clubLogCheck;
   private CheckBox logDebugInfoCheck;
   private CheckBox verboseLoggingCheck;
   private GroupBox statusGroup;
   private TableLayoutPanel statusLayout;
   private Label dxkDot;
   private Label dxkNameLabel;
   private Label dxkStatusLabel;
   private Label dxvDot;
   private Label dxvNameLabel;
   private Label dxvStatusLabel;
   private Label pfDot;
   private Label pfNameLabel;
   private Label pfStatusLabel;
   private FlowLayoutPanel leftStatusPanel;
   private Label failedQsoLabel;
   private LinkLabel failedQsoFileLink;
   private LinkLabel failedQsoFolderLink;
   private GroupBox logGroup;
   private ListBox operationLogListBox;
   private TableLayoutPanel bottomPanel;
   private LinkLabel errorLogLink;
   private FlowLayoutPanel buttonPanel;
   private Button helpButton;
   private Button showErrorLogButton;
}
