using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Game;
using System.Windows.Media;

namespace Server
{
    class Program
    {
        private static IPAddress ServerIP;
        private static int PortIP;
        private static IPEndPoint ServerEndPoint;
        private static TcpClient Client;
        private static TcpListener Server;
        private static Boolean exit = false;

        private static Dictionary<int, Player> dictPlayers = new Dictionary<int, Player>();
        private static int currentNameOfPlayer = 0;
        private static readonly object locker = new object();

        static void Main(string[] args)
        {
            PortIP = 50000;
            ServerIP = IPAddress.Parse("127.0.0.1");
            ServerEndPoint = new IPEndPoint(ServerIP, PortIP);

            Server = new TcpListener(ServerEndPoint);
            Console.WriteLine("Servidor creat");

            Server.Start();
            Console.WriteLine("Servidor iniciat");

            //Connexio del Client
            while (true)
            {
                Client = Server.AcceptTcpClient();
                Console.WriteLine("Client connectat");


                Thread clientNou = new Thread(serverResponse);
                clientNou.SetApartmentState(ApartmentState.STA);
                clientNou.Start(Client);
            }

            Console.WriteLine("Server finalitzat");

            Server.Stop();
            Console.ReadLine();
        }

        static void serverResponse(object TcpClient)
        {
            NetworkStream ServerNS = null;
            TcpClient client = (TcpClient)TcpClient;

            try
            {
                ServerNS = Client.GetStream();

                if (ServerNS != null)
                {
                    //Generem dades del client
                    generateIDPlayer(ServerNS);

                    //sendPositionAllCLients(ServerNS);
                    Thread thSendPositionAllCLients = new Thread(sendPositionAllCLients);
                    thSendPositionAllCLients.Start(ServerNS);

                    Thread randomObjects = new Thread(generateRandomObjects);
                    randomObjects.Start();
                }
            }
            catch (System.IO.IOException e)
            {
                closeClient();
            }
            catch (System.ObjectDisposedException e)
            {
                closeClient();
            }
            catch (Exception e)
            {
                closeClient();
            }
        }

        private static void generateIDPlayer(NetworkStream ServerNS)
        {
            Player playerN = newPlayer(ServerNS);

            var playerString = SerializatePlayer.Serializate(playerN);
            byte[] playerBytes = Encoding.UTF8.GetBytes(playerString);

            Thread broadcastPlayers = new Thread(() => waitXsecondsForBroadcast(playerN, playerString.Length, playerBytes));

            broadcastPlayers.Start();
            //broadcastPlayers(playerN, playerString.Length, playerBytes);
            ServerNS.Write(playerBytes, 0, playerString.Length);
        }

        private static Player newPlayer(NetworkStream serverNS)
        {
            Player player;
            var tempCurrentPlayers = currentNameOfPlayer;
            player = new Player(currentNameOfPlayer, serverNS);

            lock (locker)
            {
                dictPlayers.Add(tempCurrentPlayers, player);
                currentNameOfPlayer++;
            }

            player.position = new Position();
            player.ball = new Ball();

            generateInitialPosition(player);

            player.position.Width = 50;
            player.position.Width = 50;
            player.position.Height = 50;

            dictPlayers[player.id].position = player.position;
            dictPlayers[player.id].networkStream = serverNS;
            dictPlayers[player.id].position = new Position(player.position.PosX, player.position.PosY, player.position.Width, player.position.Height);
            dictPlayers[player.id].ball = new Ball(generateRandomColor());

            return dictPlayers[player.id];
        }

        private static void generateInitialPosition(Player player)
        {
            var position = generatePosXY();
            var posX = position[0];
            var posY = position[1];

            foreach (Player playerSel in dictPlayers.Values)
            {
                if (playerSel.position != null && (playerSel.position.PosX != posX && playerSel.position.PosY != posY))
                {
                    player.position.PosX = posX + playerSel.position.Width;
                    player.position.PosX = posY + playerSel.position.Width;
                }
            }
        }

        private static void sendPositionAllCLients(object object1)
        {
            NetworkStream ServerNS = (NetworkStream)object1;
            while (true)
            {
                byte[] BufferLocal = new byte[1024];
                int BytesRebuts = 0;
                string bytesAsString = null;
                //Rebem les dades del client i les imprimim
                do
                {
                    BytesRebuts = ServerNS.Read(BufferLocal, 0, BufferLocal.Length);
                    bytesAsString = Encoding.UTF8.GetString(BufferLocal, 0, BytesRebuts);
                } while (ServerNS.DataAvailable);
                try
                {
                    Player playerSend = SerializatePlayer.Deserialization(bytesAsString);
                    if (dictPlayers[playerSend.id].position.PosX != playerSend.position.PosX || dictPlayers[playerSend.id].position.PosY != playerSend.position.PosY)
                    {
                        dictPlayers[playerSend.id].id = playerSend.id;
                        dictPlayers[playerSend.id].kills = playerSend.kills;
                        dictPlayers[playerSend.id].position = playerSend.position;
                        dictPlayers[playerSend.id].ball = playerSend.ball;

                        //Console.WriteLine(listPlayers[playerSend.id].position.PosX);
                        //Console.WriteLine(listPlayers[playerSend.id].position.PosY);

                        broadcastPlayers(playerSend, BytesRebuts, BufferLocal);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(bytesAsString);
                }
            }
        }

        private static void broadcastPlayers(Player playerSend, int BytesRebuts, byte[] BufferLocal)
        {
            foreach (Player playerSel in dictPlayers.Values)
            {
                if (playerSend == null)
                {
                    playerSel.networkStream.Write(BufferLocal, 0, BytesRebuts);
                }
                else
                {
                    if (!playerSel.id.Equals(dictPlayers[playerSend.id].id))
                    {
                        playerSel.networkStream.Write(BufferLocal, 0, BytesRebuts);
                    }
                }
            }
        }

        private static Color generateRandomColor()
        {
            Color color;

            do
            {
                Random rnd = new Random();
                color = Color.FromArgb(byte.Parse(rnd.Next(256).ToString()), byte.Parse(rnd.Next(256).ToString()), byte.Parse(rnd.Next(256).ToString()), byte.Parse(rnd.Next(256).ToString()));
                //Comprova que el color assignat no coincideixi amb cap altre jugador de la sala
                foreach (Player colorDicPlayer in dictPlayers.Values)
                {
                    if (colorDicPlayer.ball.color == color)
                    {
                        color = Colors.White;
                    }
                }
            } while (color.ToString()[1] == 'F');

            return color;
        }

        private static void waitXsecondsForBroadcast(Player playerSend, int BytesRebuts, byte[] BufferLocal)
        {
            //Thread.Sleep(6000);
            broadcastPlayers(playerSend, BytesRebuts, BufferLocal);
            queryPlayersPlaying(playerSend);
        }

        private static void queryPlayersPlaying(Player player)
        {
            foreach (Player playerSel in dictPlayers.Values)
            {
                if (!playerSel.id.Equals(dictPlayers[player.id].id))
                {
                    var stringPlayer = SerializatePlayer.Serializate(playerSel);
                    byte[] playerSerialized = Encoding.UTF8.GetBytes(stringPlayer);

                    dictPlayers[player.id].networkStream.Write(playerSerialized, 0, stringPlayer.Length);
                }
            }
        }

        private static void generateRandomObjects()
        {
            do
            {
                Thread.Sleep(10000);
                var newObjectSerialize = JsonConvert.SerializeObject(generatePosXY());
                byte[] newObjectBytes = Encoding.UTF8.GetBytes(newObjectSerialize);

                broadcastPlayers(null, newObjectSerialize.Length, newObjectBytes);
            } while (true);
        }

        private static int[] generatePosXY()
        {
            Random rnd = new Random();
            return new int[] { rnd.Next(1200), rnd.Next(700) };
        }

        private static void closeClient()
        {
            exit = true;
            Console.WriteLine("Usuari finalitzat");

            //ServerNS.Close();
            Client.Close();
        }
    }
}
