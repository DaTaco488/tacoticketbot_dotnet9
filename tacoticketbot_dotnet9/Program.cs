using System;
using System.Windows.Forms;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace tacoticketbot_dotnet9
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}