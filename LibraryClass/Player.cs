using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class Player
    {
        public int id { get; set; }
        public int kills { get; set; }
        public NetworkStream networkStream { get; set; }
        public Position position { get; set; }
        public Ball ball { get; set; }

        public Player(int id, NetworkStream networkStream)
        {
            this.id = id;
            this.networkStream = networkStream;
        }

        public Player(int id, Position position, NetworkStream networkStream)
        {
            this.id = id;
            this.kills = 0;
            this.networkStream = networkStream;
            this.position = position;
            this.networkStream = networkStream;
        }

        public Player(int id, int kill)
        {
            this.id = id;
            this.kills = kill;
        }
    }
}
