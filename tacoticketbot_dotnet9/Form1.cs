using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.VisualBasic.ApplicationServices;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Channels;

namespace tacoticketbot_dotnet9
{
    public partial class Form1 : Form
    {
        private TacoTicketBot bot;
        private readonly SynchronizationContext _uiContext;

        private Dictionary<ulong, SocketSlashCommand> openMessages = new Dictionary<ulong, SocketSlashCommand>();

        public Form1()
        {
            InitializeComponent();
            this.Icon = new System.Drawing.Icon(@"C:\Users\DaTaco\Downloads\New Piskel-1.png.ico");
            _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            bot = new TacoTicketBot(new TextAreaLogger(_uiContext, richTextBox1));
        }

        public class TextAreaLogger : ILogger
        {
            private readonly SynchronizationContext _uiContext;
            private readonly RichTextBox _richTextBox;
            public TextAreaLogger(SynchronizationContext uiContext, RichTextBox richTextBox)
            {
                _uiContext = uiContext;
                _richTextBox = richTextBox;
            }
            public void Log(string message)
            {
                if (_uiContext != null)
                {
                    _uiContext.Post(_ => _richTextBox.AppendText(message + Environment.NewLine), null);
                }
                else
                {
                    _richTextBox.AppendText(message + Environment.NewLine);
                }
            }
            public void clear()
            {
                if (_uiContext != null)
                {
                    _uiContext.Post(_ => _richTextBox.Clear(), null);
                }
                else
                {
                    _richTextBox.Clear();
                }
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            //start button pressed
            button1.Enabled = false;

            richTextBox1.Clear();
            bot.start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //pause button pressed
            if (bot.isBotPaused())
            {
                bot.unpause();
                button2.Text = "PAUSE";
            }
            else
            {
                bot.pause();
                button2.Text = "RESUME";
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            //stop button pressed
            bot.stop();
            button1.Enabled = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            bot.stop();
        }
    }
}
