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
      dxkPortLabel = new Label();
      dxkPortValueBox = new TextBox();
      checkboxLayout = new TableLayoutPanel();
      dxkLookupCheck = new CheckBox();
      callbookCheck = new CheckBox();
      eqslCheck = new CheckBox();
      lotwCheck = new CheckBox();
      clubLogCheck = new CheckBox();
      logDebugInfoCheck = new CheckBox();
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
      settingsLayout.RowCount = 3;
      settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      settingsLayout.Controls.Add(udpPortLabel, 0, 0);
      settingsLayout.Controls.Add(udpPortTextBox, 1, 0);
      settingsLayout.Controls.Add(dxkPortLabel, 0, 1);
      settingsLayout.Controls.Add(dxkPortValueBox, 1, 1);
      settingsLayout.SetColumnSpan(dxkPortValueBox, 2);
      settingsLayout.Controls.Add(checkboxLayout, 0, 2);
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
      // dxkPortLabel
      //
      dxkPortLabel.Text = "DXKeeper TCP base port:";
      dxkPortLabel.AutoSize = true;
      dxkPortLabel.Anchor = AnchorStyles.Left;
      dxkPortLabel.Margin = new Padding(3, 6, 6, 6);

      //
      // dxkPortValueBox
      //
      // Read-only display of the DXKeeper port info from registry. Selectable
      // (TextBox) so users can copy values for troubleshooting, but TabStop
      // is off and BorderStyle is None to match a label visually.
      dxkPortValueBox.ReadOnly = true;
      dxkPortValueBox.TabStop = false;
      dxkPortValueBox.BorderStyle = BorderStyle.None;
      dxkPortValueBox.BackColor = SystemColors.Control;
      dxkPortValueBox.Width = 280;
      dxkPortValueBox.Anchor = AnchorStyles.Left;
      dxkPortValueBox.Margin = new Padding(0, 6, 0, 6);
      dxkPortValueBox.Text = "(reading from registry…)";

      //
      // checkboxLayout
      //
      checkboxLayout.Dock = DockStyle.Fill;
      checkboxLayout.AutoSize = true;
      checkboxLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      checkboxLayout.ColumnCount = 2;
      checkboxLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
      checkboxLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
      checkboxLayout.RowCount = 3;
      checkboxLayout.Margin = new Padding(0, 6, 0, 0);
      checkboxLayout.Controls.Add(dxkLookupCheck, 0, 0);
      checkboxLayout.Controls.Add(callbookCheck, 1, 0);
      checkboxLayout.Controls.Add(eqslCheck, 0, 1);
      checkboxLayout.Controls.Add(lotwCheck, 1, 1);
      checkboxLayout.Controls.Add(clubLogCheck, 0, 2);
      checkboxLayout.Controls.Add(logDebugInfoCheck, 1, 2);

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
      bottomPanel.Controls.Add(errorLogLink, 0, 0);
      bottomPanel.Controls.Add(buttonPanel, 1, 0);

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
   private Label dxkPortLabel;
   private TextBox dxkPortValueBox;
   private TableLayoutPanel checkboxLayout;
   private CheckBox dxkLookupCheck;
   private CheckBox callbookCheck;
   private CheckBox eqslCheck;
   private CheckBox lotwCheck;
   private CheckBox clubLogCheck;
   private CheckBox logDebugInfoCheck;
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
   private GroupBox logGroup;
   private ListBox operationLogListBox;
   private TableLayoutPanel bottomPanel;
   private LinkLabel errorLogLink;
   private FlowLayoutPanel buttonPanel;
   private Button helpButton;
   private Button showErrorLogButton;
}
