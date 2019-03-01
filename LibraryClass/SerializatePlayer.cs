using Game;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class SerializatePlayer
    {
        public SerializatePlayer() { }

        public static Player Deserialization(string player)
        {
            var playerSplit = player.Split('|');
            Player newPlayerFromSplit = new Player(int.Parse(playerSplit[0]), int.Parse(playerSplit[1]));

            Position newPosition = JsonConvert.DeserializeObject<Position>(playerSplit[2]);
            newPlayerFromSplit.position = new Position();
            newPlayerFromSplit.position = newPosition;

            Ball newBall = JsonConvert.DeserializeObject<Ball>(playerSplit[3]);
            newPlayerFromSplit.ball = new Ball(newBall.color);
            //newPlayerFromSplit.ball = newBall;
            return newPlayerFromSplit;
        }

        public static string Serializate(Player player)
        {
            var playerPosition = JsonConvert.SerializeObject(player.position);
            Ball tempBall = new Ball(player.ball.color);
            var playerBall = JsonConvert.SerializeObject(tempBall);
            return player.id + "|" + player.kills + "|" + playerPosition + "|" + playerBall;
        }
    }     
}
