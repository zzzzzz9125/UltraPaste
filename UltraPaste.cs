#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Drawing;
using System.Windows.Forms;

public sealed partial class UltraPasteWindow : DockableControl
{
    public Vegas MyVegas;

    public UltraPasteWindow()
        : base("UltraPaste")
    {
        InitializeComponent();
        PersistDockWindowState = true;
    }

    public override DockWindowStyle DefaultDockWindowStyle
    {
        get { return DockWindowStyle.Floating; }
    }

    public override Size DefaultFloatingSize
    {
        get { return new Size(500, 450); }
    }

    protected override void OnLoaded(EventArgs args)
    {
        base.OnLoaded(args);
    }

    protected override void InitLayout()
    {
        base.InitLayout();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        if (Visible)

        base.OnVisibleChanged(e);
    }

    private readonly System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
#if !Sony
        Color[] colors = new Color[] { ScriptPortal.MediaSoftware.Skins.Skins.Colors.ButtonFace, ScriptPortal.MediaSoftware.Skins.Skins.Colors.ButtonText };
#else
        Color[] colors = new Color[] { Sony.MediaSoftware.Skins.Skins.Colors.ButtonFace, Sony.MediaSoftware.Skins.Skins.Colors.ButtonText };
#endif

        SuspendLayout();
        AutoScaleMode = AutoScaleMode.Font;
        MinimumSize = new Size(433, 172);
        BackColor = colors[0];
        ForeColor = colors[1];
        DisplayName = string.Format("{0} {1}", L.UltraPaste, UltraPasteCommon.VERSION);
        Font = new Font(L.Font, 9);

        TableLayoutPanel l = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            ColumnCount = 4,
            Dock = DockStyle.Fill
        };
        l.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        l.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39));
        l.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
        l.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        Controls.Add(l);

        ResumeLayout(false);
        PerformLayout();
    }
}