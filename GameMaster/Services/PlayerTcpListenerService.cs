using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using GameMaster.Managers;
using GameMaster.Models;
using Newtonsoft.Json;
using Serilog;
using Shared.Messages;

namespace GameMaster.Services
{
    public class PlayerTcpListenerService : TcpListenerService
    {
        private readonly BufferBlock<PlayerMessage> queue;

        public PlayerTcpListenerService(GM gameMaster, GameConfiguration conf,
            ISocketManager<TcpClient, GMMessage> manager, BufferBlock<PlayerMessage> queue)
            : base(Log.ForContext<PlayerTcpListenerService>(), gameMaster, conf, manager)
        {
            this.queue = queue;
        }

        protected override async Task OnMessageAsync(TcpClient socket, byte[] buffer, int count)
        {
            string json = Encoding.UTF8.GetString(buffer, 0, count);
            PlayerMessage message = JsonConvert.DeserializeObject<PlayerMessage>(json);

            // TODO: To be changed later.
            message.PlayerID = manager.GetId(socket);
            await queue.SendAsync(message);
        }
    }
}
