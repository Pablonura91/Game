using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace Game
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        //Connexion ------------------------------------------------------
        private IPAddress ServerIP = IPAddress.Parse("127.0.0.1");
        private int PortIP = 50000;
        private IPEndPoint ServerEndPoint;
        private TcpClient Client;
        private NetworkStream ClientNS;
        //Connexion ------------------------------------------------------
        static readonly object locker = new object();
        static int indexPlayer;
        private DispatcherTimer dTimer = new DispatcherTimer();
        Dictionary<int, Player> dicPlayers = new Dictionary<int, Player>();
        Dictionary<int, BallGraphics> dicBallgraphics = new Dictionary<int, BallGraphics>();
        Dictionary<int, BallGraphics> dicObjectsgraphics = new Dictionary<int, BallGraphics>();

        public MainWindow()
        {
            InitializeComponent();

            Loop();
            createPlayer();
        }

        private void createPlayer()
        {
            string player;
            ServerEndPoint = new IPEndPoint(ServerIP, PortIP);

            Client = new TcpClient();

            Client.Connect(ServerEndPoint);

            if (Client.Connected)
            {
                ClientNS = Client.GetStream();

                byte[] BufferLocal = new byte[1024];
                int BytesRebuts = ClientNS.Read(BufferLocal, 0, BufferLocal.Length);
                //Passem de bytes a string
                player = Encoding.UTF8.GetString(BufferLocal, 0, BytesRebuts);

                Player newPlayerFromSplit = SerializatePlayer.Deserialization(player);

                indexPlayer = newPlayerFromSplit.id;
                dicPlayers.Add(indexPlayer, newPlayerFromSplit);
                CreateBall(newPlayerFromSplit);

                Thread clientConnexion = new Thread(serverLisener);
                clientConnexion.SetApartmentState(ApartmentState.STA);
                clientConnexion.Start(ClientNS);

                Thread sendMovementPlayer = new Thread(ResponseToServer);
                sendMovementPlayer.Start();
            }

        }

        private void serverLisener(object ClientNS)
        {
            NetworkStream currentClientNS = (NetworkStream)ClientNS;

            Thread thReceivedNewPlayer = new Thread(receivedNewPlayer);
            thReceivedNewPlayer.Start(currentClientNS);
            //receivedNewPlayer(currentClientNS);

            clientListener(currentClientNS);
        }

        private void receivedNewPlayer(object currentClientNSObject)
        {
            string player = "";
            int bytesFromBuffer;
            bool close = false;
            NetworkStream currentClientNS = (NetworkStream)currentClientNSObject;
            do
            {
                if (currentClientNS.CanRead)
                {
                    do
                    {
                        try
                        {
                            byte[] receivedBuffer = new byte[1024];
                            bytesFromBuffer = currentClientNS.Read(receivedBuffer, 0, receivedBuffer.Length);

                            player = Encoding.UTF8.GetString(receivedBuffer, 0, bytesFromBuffer);
                        }
                        catch (System.IO.IOException ex)
                        {
                            // if the ReceiveTimeout is reached an IOException will be raised...
                            // with an InnerException of type SocketException and ErrorCode 10060
                            var socketExept = ex.InnerException as SocketException;
                            if (socketExept == null || socketExept.ErrorCode != 10060)

                                // if it is the receive timeout, then reading ended
                                bytesFromBuffer = 0;
                        }
                    } while (currentClientNS.DataAvailable);
                }
                if (player != "")
                {
                    try
                    {
                        Player newPlayerFromSplit = SerializatePlayer.Deserialization(player);

                        dicPlayers.Add(newPlayerFromSplit.id, newPlayerFromSplit);

                        Dispatcher.Invoke(() =>
                        {
                            CreateBall(newPlayerFromSplit);
                        });

                    }
                    catch (Exception e)
                    {

                    }
                }
            } while (!close);
        }

        private void clientListener(NetworkStream currentClientNS)
        {
            bool closed = false;

            // Fem un bucle que actua com "listener" on li afegim un try catch per controlar IOException quan l'usuari tanqui l'aplicació
            while (!closed)
            {
                try
                {
                    byte[] localBuffer = new byte[1024];
                    int bytesFromBuffer = currentClientNS.Read(localBuffer, 0, localBuffer.Length);

                    var player = Encoding.UTF8.GetString(localBuffer, 0, bytesFromBuffer);
                    try
                    {
                        Player newPlayerFromSplit = SerializatePlayer.Deserialization(player);
                        if (dicPlayers.ContainsKey(newPlayerFromSplit.id))
                        {
                            dicPlayers[newPlayerFromSplit.id].position.PosX = newPlayerFromSplit.position.PosX;
                            dicPlayers[newPlayerFromSplit.id].position.PosY = newPlayerFromSplit.position.PosY;

                            this.Dispatcher.Invoke(() =>
                            {
                                DrawBall(dicPlayers[newPlayerFromSplit.id], dicBallgraphics[newPlayerFromSplit.id]);
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        DrawObject(player);
                    }
                }
                catch (Exception e)
                {
                    closed = true;
                }
            }
        }


        // Envia un objecte Position serialitzat cap al servidor
        void ResponseToServer()
        {
            while (true)
            {
                Position position = new Position(dicPlayers[indexPlayer].position.PosX, dicPlayers[indexPlayer].position.PosY, dicPlayers[indexPlayer].position.Width, dicPlayers[indexPlayer].position.Height);
                if (position.PosX != dicPlayers[indexPlayer].position.PosX || position.PosY != dicPlayers[indexPlayer].position.PosY)
                {
                    var stringPlayer = SerializatePlayer.Serializate(dicPlayers[indexPlayer]);
                    byte[] playerSerialized = Encoding.UTF8.GetBytes(stringPlayer);

                    ClientNS.Write(playerSerialized, 0, stringPlayer.Length);
                }
            }
        }

        public void CreateBall(Player player)
        {
            if (!dicBallgraphics.ContainsKey(player.id))
            {
                dicBallgraphics.Add(player.id, new BallGraphics(player.position.Width, player.position.Height, player.ball.color));
            } else
            {
                CanvasBalls.Children.Remove(dicBallgraphics[player.id].ShapeBall);
                dicBallgraphics[player.id] = new BallGraphics(player.position.Width, player.position.Height, player.ball.color);
            }
            CanvasBalls.Children.Add(dicBallgraphics[player.id].ShapeBall);
            DrawBall(player, dicBallgraphics[player.id]);
        }

        public void Loop()
        {
            dTimer.Interval = TimeSpan.FromMilliseconds(30);
            dTimer.Tick += Timer_Tick;
            dTimer.Start();
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            DrawBall(dicPlayers[indexPlayer], dicBallgraphics[indexPlayer]);
        }

        public void DrawBall(Player player, BallGraphics ballGraphics)
        {
            Canvas.SetLeft(ballGraphics.ShapeBall, player.position.PosX);
            Canvas.SetTop(ballGraphics.ShapeBall, player.position.PosY);
        }

        void CanvasKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.D:
                    if (checkPosition(dicPlayers[indexPlayer].position, 'X', 4))
                    {
                        dicPlayers[indexPlayer].position.PosX = dicPlayers[indexPlayer].position.PosX + 4;
                    }
                    else
                    {
                        reloadPlayer();
                        delDrawObject(dicObjectsgraphics[dicPlayers[indexPlayer].position.PosX + dicPlayers[indexPlayer].position.PosY].ShapeBall);
                    }
                    break;
                case Key.A:
                    if (checkPosition(dicPlayers[indexPlayer].position, 'X', -4))
                    {
                        dicPlayers[indexPlayer].position.PosX = dicPlayers[indexPlayer].position.PosX - 4;
                    }
                    else
                    {
                        reloadPlayer();
                        delDrawObject(dicObjectsgraphics[dicPlayers[indexPlayer].position.PosX + dicPlayers[indexPlayer].position.PosY].ShapeBall);
                    }
                    break;
                case Key.S:
                    if (checkPosition(dicPlayers[indexPlayer].position, 'Y', 4))
                    {
                        dicPlayers[indexPlayer].position.PosY = dicPlayers[indexPlayer].position.PosY + 4;
                    }
                    else
                    {
                        reloadPlayer();
                        delDrawObject(dicObjectsgraphics[dicPlayers[indexPlayer].position.PosX + dicPlayers[indexPlayer].position.PosY].ShapeBall);
                    }
                    break;
                case Key.W:
                    if (checkPosition(dicPlayers[indexPlayer].position, 'X', -4))
                    {
                        dicPlayers[indexPlayer].position.PosY = dicPlayers[indexPlayer].position.PosY - 4;
                    }
                    else
                    {
                        reloadPlayer();
                        delDrawObject(dicObjectsgraphics[dicPlayers[indexPlayer].position.PosX + dicPlayers[indexPlayer].position.PosY].ShapeBall);
                    }
                    break;
                default:
                    break;
            }
        }

        private void reloadPlayer()
        {
            dicPlayers[indexPlayer].kills += 1;
            dicPlayers[indexPlayer].position.Height += 10;
            dicPlayers[indexPlayer].position.Width += 10;
        }

        private void delDrawObject(UIElement objectDraw)
        {            
            CanvasBalls.Children.Remove(objectDraw);
            dicObjectsgraphics.Remove(dicPlayers[indexPlayer].position.PosX + dicPlayers[indexPlayer].position.PosY);
            CreateBall(dicPlayers[indexPlayer]);
        }

        public bool checkPosition(Position position, char positionChange, int positionSum)
        {
            var tempPosX = position.PosX;
            var tempPosY = position.PosY;
            int iterations = positionSum < 0 ? 0 : 4;

            for (int i = positionSum; i <= iterations; i++)
            {
                switch (positionChange)
                {
                    case 'X':
                        tempPosX = position.PosX;
                        tempPosX += i;
                        break;
                    case 'Y':
                        tempPosY = position.PosY;
                        tempPosY += i;
                        break;
                }
                var test = tempPosX + tempPosY;
                if (dicObjectsgraphics.ContainsKey(tempPosX + tempPosY))
                {
                    position.PosX = tempPosX;
                    position.PosY = tempPosY;
                    return false;
                }
            }

            return true;
        }

        public void DrawObject(object object1)
        {
            string objectMap = (string)object1;

            int[] rnd = JsonConvert.DeserializeObject<int[]>(objectMap);
            
            this.Dispatcher.Invoke(() =>
            {
                BallGraphics ballGraphics = new BallGraphics(10, 10, Colors.Black);
                CanvasBalls.Children.Add(ballGraphics.ShapeBall);
                Canvas.SetLeft(ballGraphics.ShapeBall, rnd[0]);
                Canvas.SetTop(ballGraphics.ShapeBall, rnd[1]);
                dicObjectsgraphics.Add(rnd[0] + rnd[1], ballGraphics);
            });
        }
    }
}
