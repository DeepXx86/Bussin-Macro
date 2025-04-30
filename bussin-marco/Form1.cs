using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;


namespace bussin_marco

{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        const int MOUSEEVENTF_LEFTDOWN = 0x02;
        const int MOUSEEVENTF_LEFTUP = 0x04;
        const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        const int MOUSEEVENTF_RIGHTUP = 0x10;
        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        static extern short VkKeyScan(char ch);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);

        const int INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYUP = 0x0002;
        const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        const uint VK_KEYDOWN = 0x0000;
        const uint MAPVK_VK_TO_VSC = 0x00;

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        void PressKey(Keys key, int holdDuration)
        {
            try
            {
                ushort virtualKeyCode = (ushort)key;
                ushort scanCode = (ushort)MapVirtualKey((uint)key, MAPVK_VK_TO_VSC);

                INPUT[] inputs = new INPUT[2];

                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].u.ki.wVk = virtualKeyCode;
                inputs[0].u.ki.wScan = scanCode;
                inputs[0].u.ki.dwFlags = 0; 
                inputs[0].u.ki.time = 0;
                inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

                SendInput(1, new INPUT[] { inputs[0] }, Marshal.SizeOf(typeof(INPUT)));

                Thread.Sleep(holdDuration);

                inputs[1].type = INPUT_KEYBOARD;
                inputs[1].u.ki.wVk = virtualKeyCode;
                inputs[1].u.ki.wScan = scanCode;
                inputs[1].u.ki.dwFlags = KEYEVENTF_KEYUP;
                inputs[1].u.ki.time = 0;
                inputs[1].u.ki.dwExtraInfo = IntPtr.Zero;

                SendInput(1, new INPUT[] { inputs[1] }, Marshal.SizeOf(typeof(INPUT)));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error pressing key: {ex.Message}", "Key Press Error");
            }
        }

        void PressSpecialKey(Keys key, int holdDuration)
        {
            try
            {
                ushort virtualKeyCode = (ushort)key;
                ushort scanCode = (ushort)MapVirtualKey((uint)key, MAPVK_VK_TO_VSC);

                bool isExtendedKey = (key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right ||
                                     key == Keys.Home || key == Keys.End || key == Keys.PageUp || key == Keys.PageDown ||
                                     key == Keys.Insert || key == Keys.Delete || key == Keys.NumLock);

                INPUT[] inputs = new INPUT[2];

                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].u.ki.wVk = virtualKeyCode;
                inputs[0].u.ki.wScan = scanCode;
                inputs[0].u.ki.dwFlags = isExtendedKey ? KEYEVENTF_EXTENDEDKEY : 0;
                inputs[0].u.ki.time = 0;
                inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

                SendInput(1, new INPUT[] { inputs[0] }, Marshal.SizeOf(typeof(INPUT)));

                Thread.Sleep(holdDuration);

                inputs[1].type = INPUT_KEYBOARD;
                inputs[1].u.ki.wVk = virtualKeyCode;
                inputs[1].u.ki.wScan = scanCode;
                inputs[1].u.ki.dwFlags = KEYEVENTF_KEYUP | (isExtendedKey ? KEYEVENTF_EXTENDEDKEY : 0);
                inputs[1].u.ki.time = 0;
                inputs[1].u.ki.dwExtraInfo = IntPtr.Zero;

                SendInput(1, new INPUT[] { inputs[1] }, Marshal.SizeOf(typeof(INPUT)));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error pressing special key: {ex.Message}", "Key Press Error");
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                if (!macroReady)
                {
                    MessageBox.Show("Click Start first before using F8.", "Macro Not Ready");
                    return;
                }

                if (!macroRunning)
                {
                    macroRunning = true;
                    RunMacro();
                }
                else
                {
                    macroRunning = false;
                    macroToken?.Cancel();
                }
            }

            base.WndProc(ref m);
        }

        private async void RunMacro()
        {
            macroToken = new CancellationTokenSource();
            var token = macroToken.Token;

            try
            {
                do
                {
                    foreach (Control block in app.Controls)
                    {
                        if (!macroRunning || token.IsCancellationRequested) return;

                        if (block.Controls.Count == 0) continue;
                        Label lbl = block.Controls[0] as Label;
                        if (lbl == null) continue;

                        int hold = 0;
                        Keys key = Keys.None;

                        if (lbl.Text.StartsWith("🖱️") || lbl.Text.StartsWith("⏱️"))
                        {
                            hold = (int)block.Tag;
                        }
                        else if (lbl.Text.StartsWith("⌨️"))
                        {
                            var tag = (Tuple<Keys, int>)block.Tag;
                            key = tag.Item1;
                            hold = tag.Item2;
                        }

                        if (lbl.Text.StartsWith("🖱️"))
                        {
                            Point pos = Cursor.Position;
                            // If right-click, use negative hold value
                            if (hold >= 0)
                            {
                                mouse_event(MOUSEEVENTF_LEFTDOWN, pos.X, pos.Y, 0, 0);
                                await Task.Delay(hold, token);
                                mouse_event(MOUSEEVENTF_LEFTUP, pos.X, pos.Y, 0, 0);
                            }
                            else
                            {
                                int delay = Math.Abs(hold);
                                mouse_event(MOUSEEVENTF_RIGHTDOWN, pos.X, pos.Y, 0, 0);
                                await Task.Delay(delay, token);
                                mouse_event(MOUSEEVENTF_RIGHTUP, pos.X, pos.Y, 0, 0);
                            }
                        }

                        else if (lbl.Text.StartsWith("⌨️"))
                        {
                            
                            if (key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right ||
                                key == Keys.Home || key == Keys.End || key == Keys.PageUp || key == Keys.PageDown ||
                                key == Keys.Insert || key == Keys.Delete || key == Keys.NumLock)
                            {
                                PressSpecialKey(key, hold);
                            }
                            else
                            {
                                PressKey(key, hold);
                            }
                        }
                        else if (lbl.Text.StartsWith("⏱️"))
                        {
                            await Task.Delay(hold, token);
                        }
                    }
                } while (isLoop && macroRunning && !token.IsCancellationRequested);
            }
            catch (TaskCanceledException)
            {
                
            }
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 1;
        private const int MOD_NONE = 0x0000;

        public Form1()
        {
            InitializeComponent();
        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
        public static void Curve(Control ctrl, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            Rectangle bounds = ctrl.ClientRectangle;
            path.AddArc(bounds.X, bounds.Y, radius, radius, 180, 90);
            path.AddArc(bounds.Right - radius, bounds.Y, radius, radius, 270, 90);
            path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            ctrl.Region = new Region(path);
        }
        int blockY = 0; 
        int blockHeight = 50;
        int space = 10;
        private void button1_Click(object sender, EventArgs e)
        {
            Panel block = new Panel();
            block.Width = app.Width - 10;
            block.Height = blockHeight;
            block.Location = new Point(0, blockY);
            block.BackColor = Color.FromArgb(223, 208, 184);
            Curve(block, 15);

            Label lbl = new Label();
            lbl.Text = $"🖱️ Click (Hold: 0ms)";
            lbl.Location = new Point(10, 15);
            lbl.AutoSize = true;
            lbl.Font = new Font("Segoe UI Emoji", 10);

            block.Tag = 0;

            block.Click += (s, evt) =>
            {
                int oldHold = (int)block.Tag;
                string input = Microsoft.VisualBasic.Interaction.InputBox("Set Hold (ms)", "Hold Time", oldHold.ToString());
                if (int.TryParse(input, out int newHold))
                {
                    block.Tag = newHold;
                    lbl.Text = $"🖱️ Click (Hold: {newHold}ms)";
                }
            };

            block.Controls.Add(lbl);
            app.Controls.Add(block);
            blockY += blockHeight + space;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Panel block = new Panel();
            block.Width = app.Width - 10;
            block.Height = blockHeight;
            block.Location = new Point(0, blockY);
            block.BackColor = Color.FromArgb(246, 222, 216);
            Curve(block, 15);

            Label lbl = new Label();
            lbl.Text = $"⌨️ Press (Key: A, Hold: 0ms)";
            lbl.Location = new Point(10, 15);
            lbl.AutoSize = true;
            lbl.Font = new Font("Segoe UI Emoji", 10);

            block.Tag = Tuple.Create(Keys.A, 0);

            block.Click += (s, evt) =>
            {
                var old = (Tuple<Keys, int>)block.Tag;

                string keyInput = Interaction.InputBox("Enter key (like A, B, Enter):", "Key To Press", old.Item1.ToString());
                string holdInput = Interaction.InputBox("Set Hold (ms)", "Hold Time", old.Item2.ToString());

                if (Enum.TryParse(keyInput, true, out Keys chosenKey) && int.TryParse(holdInput, out int newHold))
                {
                    block.Tag = Tuple.Create(chosenKey, newHold);
                    lbl.Text = $"⌨️ Press (Key: {chosenKey}, Hold: {newHold}ms)";
                }
                else
                {
                    MessageBox.Show("Invalid key or hold value.");
                }
            };

            block.Controls.Add(lbl);
            app.Controls.Add(block);
            blockY += blockHeight + space;
        }


        private void button3_Click(object sender, EventArgs e)
        {
            Panel block = new Panel();
            block.Width = app.Width - 10;
            block.Height = blockHeight;
            block.Location = new Point(0, blockY);
            block.BackColor = Color.FromArgb(202, 224, 188);
            Curve(block, 15);

            Label lbl = new Label();
            lbl.Text = $"⏱️ Wait: 0ms";
            lbl.Location = new Point(10, 15);
            lbl.AutoSize = true;
            lbl.Font = new Font("Segoe UI Emoji", 10);

            block.Tag = 0;

            block.Click += (s, evt) =>
            {
                int oldHold = (int)block.Tag;
                string input = Microsoft.VisualBasic.Interaction.InputBox("Set Hold (ms)", "Hold Time", oldHold.ToString());
                if (int.TryParse(input, out int newHold))
                {
                    block.Tag = newHold;
                    lbl.Text = $"⏱️ Wait: {newHold}ms";
                }
            };

            block.Controls.Add(lbl);
            app.Controls.Add(block);
            blockY += blockHeight + space;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you wanna clear all blocks, fr?", "Confirm Clear", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                app.Controls.Clear();
                blockY = 0;
            }
        }
        bool run = false;
        bool macroReady = false;
        bool macroRunning = false;
        CancellationTokenSource macroToken;
        private void button5_Click(object sender, EventArgs e)
        {
            if (!macroReady)
            {

                button5.Text = "x";
                button5.BackColor = Color.FromArgb(255, 130, 130);
                button5.ForeColor = Color.White;
                macroReady = true;
                RegisterHotKey(this.Handle, HOTKEY_ID, MOD_NONE, (int)Keys.F8);
            }
            else
            {
                button5.Text = ">";
                button5.BackColor = Color.FromArgb(159, 179, 223);
                button5.ForeColor = Color.White;
                macroReady = false;
                macroRunning = false;
                macroToken?.Cancel();
                UnregisterHotKey(this.Handle, HOTKEY_ID);
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID);
        }

        bool isLoop = true;
        private void button6_Click(object sender, EventArgs e)
        {
            isLoop = !isLoop; 

            if (isLoop)
            {
                button6.Text = "loop";
                button6.BackColor = Color.White;
            }
            else
            {
                button6.Text = "one time";
                button6.BackColor = Color.White;
            }

        }
    }
}
