using System;
using UnityEngine;

//NetworkData Script

namespace NetworkData
{
    public enum Commands
    {
        NEW_CLIENT,
        UPDATE,
        CLIENT_DROPPED,
        CLIENT_LIST,
        OWN_ID,
        INPUT
    }

    [Serializable]
    public class NetworkHeader
    {
        public Commands cmd;
    }

    [Serializable]
	public class Player
	{
        public string id;
        [Serializable]
        public struct receivedColor
        {
            public float R;
            public float G;
            public float B;
        }
        public receivedColor color;
        public Vector3 position;
        public Quaternion rotation;

        public Player()
        {
            id = "-1";
        }
        public Player(Client c)
        {
            id = c.id;
            color = c.color;
            position = c.position;
            rotation = c.rotation;
        }
        public override string ToString()
        {
            string result = "Player : \n";
            result += "ID : " + id + "\n";
            result += "R : " + color.R.ToString() + ", ";
            result += "G : " + color.G.ToString() + ", ";
            result += "B : " + color.B.ToString() + "\n";
            result += "Position : " + position.ToString() + "\n";
            result += "Rotation : " + rotation.ToString() + "\n";

            return result;
        }
    }

    public class Client : Player
    {
        public float interval;
        public void SetPlayer(Player p)
        {
            id = p.id;
            color = p.color;

            position = p.position;
            rotation = p.rotation;
        }
        public override string ToString()
        {
            string result = base.ToString();
            result += "interval : " + interval + "\n";
            return result;
        }
    }

    [Serializable]
    public class NewPlayer : NetworkHeader
    {
        public Player player;

        public NewPlayer()
        {
            cmd = Commands.NEW_CLIENT;
        }
        public NewPlayer(Client c)
        {
            cmd = Commands.NEW_CLIENT;
            player = new Player(c);
        }
    }
    [Serializable]
    public class ConnectedPlayer : NetworkHeader
    {
        public Player[] connect;

        public ConnectedPlayer()
        {
            cmd = Commands.CLIENT_LIST;
        }
        public ConnectedPlayer(System.Collections.Generic.List<Client> clients)
        {
            cmd = Commands.CLIENT_LIST;
            connect = new Player[clients.Count];
            for (int i = 0; i < clients.Count; i++)
            {
                connect[i] = new Player(clients[i]);
            }
        }
    }
    [Serializable]
    public class DisconnectedPlayer : NetworkHeader
    {
        public Player[] disconnect;
        public DisconnectedPlayer()
        {
            cmd = Commands.CLIENT_DROPPED;
        }
        public DisconnectedPlayer(System.Collections.Generic.List<Client> clients)
        {
            cmd = Commands.CLIENT_DROPPED;
            disconnect = new Player[clients.Count];
            for (int i = 0; i < clients.Count; i++)
            {
                disconnect[i] = new Player(clients[i]);
            }
        }
    }
    [Serializable]
    public class UpdatedPlayer : NetworkHeader
    {
        public Player[] update;
        public UpdatedPlayer()
        {
            cmd = Commands.UPDATE;
        }
        public UpdatedPlayer(System.Collections.Generic.List<Client> clients)
        {
            cmd = Commands.UPDATE;
            update = new Player[clients.Count];
            for (int i = 0; i < clients.Count; i++)
            {
                update[i] = new Player(clients[i]);
            }
        }
    }

    [Serializable]
    public class PlayerInput : NetworkHeader
    {
        public Vector3 input;
        public PlayerInput()
        {
            cmd = Commands.INPUT;
        }
    }
}
