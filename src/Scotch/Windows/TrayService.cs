using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Forms=System.Windows.Forms;
using Scotch.Core;

namespace Scotch.Windows;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon notify;
    private readonly Forms.ToolStripMenuItem avoidance,date,startup;
    private Icon icon;
    private readonly Forms.ToolStripMenuItem[] sizeItems;
    public Forms.ContextMenuStrip Menu {get;}=new();
    public TrayService(Action toggle,Action toggleDate,Action reset,Action toggleStartup,Action exit,Action<ClockSizePreset>? selectSize=null)
    {
        avoidance=new("Enable avoidance  (Ctrl+Alt+C)",null,(_,_)=>toggle());
        date=new("Show date",null,(_,_)=>toggleDate());
        startup=new("Launch at sign-in",null,(_,_)=>toggleStartup());
        var sizes=new Forms.ToolStripMenuItem("Size");
        sizeItems=Enum.GetValues<ClockSizePreset>().Select(preset=>new Forms.ToolStripMenuItem(preset switch {ClockSizePreset.Small=>"Small (75%)",ClockSizePreset.Large=>"Large (150%)",_=>"Medium (100%)"},null,(_,_)=>selectSize?.Invoke(preset))).ToArray();
        sizes.DropDownItems.AddRange(sizeItems);
        Menu.Items.AddRange([avoidance,date,sizes,new Forms.ToolStripSeparator(),new Forms.ToolStripMenuItem("Reset position",null,(_,_)=>reset()),startup,new Forms.ToolStripSeparator(),new Forms.ToolStripMenuItem("Exit",null,(_,_)=>exit())]);
        icon=CreateIcon(Color.SteelBlue);
        notify=new Forms.NotifyIcon {Icon=icon,Text="Scotch · Ctrl+Alt+C pauses avoidance",ContextMenuStrip=Menu,Visible=true};
        notify.DoubleClick+=(_,_)=>toggle();
    }
    public void Refresh(bool enabled,bool showDate,bool launchAtSignIn,ClockSizePreset size=ClockSizePreset.Medium)
    {
        avoidance.Checked=enabled;date.Checked=showDate;startup.Checked=launchAtSignIn;
        for(var i=0;i<sizeItems.Length;i++)sizeItems[i].Checked=i==(int)size;
        notify.Text=enabled?"Scotch · Avoidance on · Ctrl+Alt+C":"Scotch · Avoidance paused · Ctrl+Alt+C";
    }
    public void ApplyPalette(ClockPalette palette)
    {
        static Color Convert(System.Windows.Media.Color c)=>Color.FromArgb(c.R,c.G,c.B);
        var background=Convert(palette.Background);var foreground=Convert(palette.Foreground);
        Menu.BackColor=background;Menu.ForeColor=foreground;
        Menu.Renderer=new Forms.ToolStripProfessionalRenderer(new MenuColors(background,foreground));
        void ApplyItems(Forms.ToolStripItemCollection items)
        {
            foreach(Forms.ToolStripItem item in items)
            {
                item.BackColor=background;item.ForeColor=foreground;
                if(item is Forms.ToolStripMenuItem parent && parent.HasDropDownItems)
                {
                    parent.DropDown.BackColor=background;parent.DropDown.ForeColor=foreground;
                    parent.DropDown.Renderer=Menu.Renderer;ApplyItems(parent.DropDownItems);
                }
            }
        }
        ApplyItems(Menu.Items);
        var next=CreateIcon(Convert(palette.Accent));notify.Icon=next;icon.Dispose();icon=next;
    }
    public void ShowAtPointer()=>Menu.Show(Forms.Cursor.Position);
    public bool RestoreIcon(){notify.Visible=false;notify.Visible=true;return notify.Visible;}
    public void Notify(string message)=>notify.ShowBalloonTip(6000,"Scotch",message,Forms.ToolTipIcon.Info);
    public void Dispose(){notify.Visible=false;notify.Dispose();Menu.Dispose();icon.Dispose();}

    private static Icon CreateIcon(Color accent)
    {
        using var bitmap=new Bitmap(32,32);
        using(var g=Graphics.FromImage(bitmap))
        {
            g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.Transparent);
            using var face=new SolidBrush(accent);g.FillEllipse(face,2,2,28,28);
            // Pick readable hands even for a very pale system accent.
            var hands=accent.GetBrightness()>0.65?Color.Black:Color.White;
            using var pen=new Pen(hands,2.5f){StartCap=LineCap.Round,EndCap=LineCap.Round};
            g.DrawLine(pen,16,16,16,8);g.DrawLine(pen,16,16,22,19);
        }
        var handle=bitmap.GetHicon();
        try {using var borrowed=Icon.FromHandle(handle);return (Icon)borrowed.Clone();}
        finally {DestroyIcon(handle);}
    }
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyIcon(nint icon);

    private sealed class MenuColors(Color background,Color foreground):Forms.ProfessionalColorTable
    {
        private Color Selection=>Color.FromArgb((background.R*3+foreground.R)/4,(background.G*3+foreground.G)/4,(background.B*3+foreground.B)/4);
        public override Color ToolStripDropDownBackground=>background;
        public override Color ImageMarginGradientBegin=>background;
        public override Color ImageMarginGradientMiddle=>background;
        public override Color ImageMarginGradientEnd=>background;
        public override Color MenuItemSelected=>Selection;
        public override Color MenuItemSelectedGradientBegin=>Selection;
        public override Color MenuItemSelectedGradientEnd=>Selection;
        public override Color MenuItemBorder=>foreground;
        public override Color MenuBorder=>foreground;
        public override Color CheckBackground=>Selection;
        public override Color CheckSelectedBackground=>Selection;
        public override Color SeparatorDark=>Selection;
        public override Color SeparatorLight=>background;
    }
}
