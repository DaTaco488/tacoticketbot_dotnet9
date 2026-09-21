using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tacoticketbot_dotnet9
{
    public class BotSettings
    {
        public string Token { get; set; } = "";
        public ulong guildId { get; set; } = 0;
        public ulong catId { get; set; } = 0;
        public ulong ticketNum { get; set; } = 1;
        public List<CreatedTicket> createdTickets { get; set; } = new List<CreatedTicket>();
    }

    public class CreatedTicket
    {
        public ulong userId { get; set; } = 0;
        public string userName { get; set; } = "";
        public ulong channelId { get; set; } = 0;
        public string channelName { get; set; } = "";
        public string category { get; set; } = "";
        public ulong ticketId { get; set; } = 1;
    }
}
