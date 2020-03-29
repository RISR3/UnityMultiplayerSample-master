using System.Text;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkData;

//NetworkClient Script
public class NetworkClient : MonoBehaviour
{
    public static NetworkClient instance { get; private set; }

    public string IP;
    public ushort Port;
    public GameObject cube;

    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;

    public string myID { get; private set; }
    private Dictionary<string, GameObject> players;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }

        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        
        var endpoint = NetworkEndPoint.Parse(IP, Port);
        m_Connection = m_Driver.Connect(endpoint);

        players = new Dictionary<string, GameObject>();
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }

    private void OnConnect()
    {
        Debug.Log("Connected");
    }
    private void OnData(DataStreamReader stream)
    {
        NativeArray<byte> message = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(message);
        string returnData = Encoding.ASCII.GetString(message.ToArray());

        NetworkHeader header = new NetworkHeader();
        try
        {
            header = JsonUtility.FromJson<NetworkHeader>(returnData);
        }
        catch (System.ArgumentException e)
        {
            Debug.LogError(e.ToString() + "\nFailed To Load. Disconnecting");
            Disconnect();
            return;
        }

        try
        {
            switch (header.cmd)
            {
                case Commands.NEW_CLIENT:
                {
                    Debug.Log("Client Available");
                    NewPlayer np = JsonUtility.FromJson<NewPlayer>(returnData);
                    Debug.Log(np.player.ToString());
                    SpawnPlayers(np.player);
                    break;
                }
                case Commands.UPDATE:
                {
                    UpdatedPlayer up = JsonUtility.FromJson<UpdatedPlayer>(returnData);
                    UpdatePlayers(up.update);
                    break;
                }
                case Commands.CLIENT_DROPPED:
                {
                    DisconnectedPlayer dp = JsonUtility.FromJson<DisconnectedPlayer>(returnData);
                    DestroyPlayers(dp.disconnect);
                    Debug.Log("Client Unavailable");
                    break;
                }
                case Commands.CLIENT_LIST:
                {
                    ConnectedPlayer cp = JsonUtility.FromJson<ConnectedPlayer>(returnData);
                    SpawnPlayers(cp.connect);
                    Debug.Log("Client_List");
                    break;
                }
                case Commands.OWN_ID:
                {
                    NewPlayer p = JsonUtility.FromJson<NewPlayer>(returnData);
                    myID = p.player.id;
                    SpawnPlayers(p.player);
                    Debug.Log("PlayerID");
                    break;
                }
                default:
                    Debug.Log("Failure");
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.ToString() + "\nMessage Contents Failed to Load. Disconnect");
            Disconnect();
            return;
        }
    }
    private void Disconnect()
    {
        Debug.Log("Disconnecting");
        m_Connection.Disconnect(m_Driver);
    }
    private void OnDisconnect()
    {
        Debug.Log("Client Failed to Connect");
        m_Connection = default(NetworkConnection);
    }

    private void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            Debug.Log("Connection Failure");
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        
        while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }
        }
    }

    private void SpawnPlayers(Player p)
    {
        if (players.ContainsKey(p.id))
        {
            Debug.LogError("Player In Instance");
            return;
        }
        Debug.Log(p.ToString());
        GameObject temp = Instantiate(cube, p.position, p.rotation);
        temp.GetComponent<NetworkCharacter>().SetNetworkID(p.id);
        temp.GetComponent<NetworkCharacter>().Setmoveable(p.id == myID);
        temp.GetComponent<Renderer>().material.color = new Color(p.color.R, p.color.G, p.color.B, 1.0f);
        players.Add(p.id, temp);
    }
    private void SpawnPlayers(Player[] p)
    {
        foreach (Player player in p)
        {
            SpawnPlayers(player);
        }
    }
    private void UpdatePlayers(Player[] p)
    {
        foreach (Player player in p)
        {
            if (players.ContainsKey(player.id))
            {
                players[player.id].transform.position = player.position;
                players[player.id].transform.rotation = player.rotation;
            }
        }
    }
    private void DestroyPlayers(Player[] p)
    {
        foreach (Player player in p)
        {
            if (players.ContainsKey(player.id))
            {
                Destroy(players[player.id]);
                players.Remove(player.id);
            }
        }
    }

    private void SendData(object data)
    {
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> sendBytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(JsonUtility.ToJson(data)), Allocator.Temp);
        writer.WriteBytes(sendBytes);
        m_Driver.EndSend(writer);
    }
    public void SendInput(Vector3 input)
    {
        PlayerInput playerInput = new PlayerInput();
        playerInput.input = input;
        SendData(playerInput);
    }
}