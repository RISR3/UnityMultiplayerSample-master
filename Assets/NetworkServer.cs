using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkData;

//NetworkServer Script

public class NetworkServer : MonoBehaviour
{
    public static NetworkServer instance { get; private set; }
    public NetworkDriver m_Driver;
    private NativeList<NetworkConnection> m_Connections;
    private const float INTERVAl = 1.0f / 60.0f;
    private List<Client> clients;
    private List<GameObject> players;

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
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 12345;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to Bind to Port9000"); //Its over 9000! 
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        clients = new List<Client>();
        players = new List<GameObject>();
    }

    private void Start()
    {
        InvokeRepeating("UpdateClients", 1, INTERVAl);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    private void UpdateClients()
    {
        foreach (NetworkConnection connection in m_Connections)
        {
            SendData(new UpdatedPlayer(clients), connection);
        }
    }

    private void OnConnect(NetworkConnection connection)
    {
        Debug.Log("New Connection Accepted");
        
        Client new_client = new Client();
        new_client.id = connection.InternalId.ToString();
        new_client.color.R = Random.Range(0.0f, 1.0f);
        new_client.color.G = Random.Range(0.0f, 1.0f);
        new_client.color.B = Random.Range(0.0f, 1.0f);

        NewPlayer new_player = new NewPlayer(new_client);
        foreach (Client client in clients)
        {
            Debug.Log("Sending Client to Connected Clients");
            SendData(new_player, client);
        }

        new_player.cmd = Commands.OWN_ID;
        Debug.Log("Sending My Own Id");
        SendData(new_player, connection);
        Debug.Log("Sending ClientList to the New Client");
        SendData(new ConnectedPlayer(clients), connection);

        clients.Add(new_client);
        players.Add(new GameObject());
        m_Connections.Add(connection);
    }

    private void CleanupClients()
    {
        List<Client> dropped = new List<Client>();
        for (int i = 0; i < clients.Count; i++)
        {
            clients[i].interval += Time.deltaTime;
            if (clients[i].interval >= 5.0f)
            {
                int connectionIndex = FindMatchingConnection(clients[i].id);
                if (connectionIndex >= 0)
                {
                    m_Connections[connectionIndex] = default(NetworkConnection);
                }
                dropped.Add(clients[i]);
                Destroy(players[i]);
                clients.RemoveAt(i);
                players.RemoveAt(i);
                i--;
            }
        }

        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        if (dropped.Count > 0)
        {
            DisconnectedPlayer drop = new DisconnectedPlayer(dropped);
            foreach (Client client in clients)
            {
                SendData(drop, client);
            }
        }
    }

    private void OnData(DataStreamReader stream, int connectionIndex)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);
        string returnData = Encoding.ASCII.GetString(bytes.ToArray());

        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(returnData);

        int clientIndex = FindMatchingClient(m_Connections[connectionIndex].InternalId);
        clients[clientIndex].interval = Time.deltaTime;

        switch (header.cmd)
        {
            case Commands.INPUT:
            {
                PlayerInput input = JsonUtility.FromJson<PlayerInput>(returnData);
                players[clientIndex].transform.Translate(Vector3.forward * INTERVAl * input.input.z);
                players[clientIndex].transform.Rotate(Vector3.up * input.input.y * INTERVAl * 90);
                clients[clientIndex].position = players[clientIndex].transform.position;
                clients[clientIndex].rotation = players[clientIndex].transform.rotation;
            }
                break;
            default:
                break;
        }
    }

    private void Update ()
    {
        m_Driver.ScheduleUpdate().Complete();

        // CleanUp the Connections
        CleanupClients();

        // Accept hte New Connections
        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default(NetworkConnection))
        {
            OnConnect(c);
        }

        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
                Assert.IsTrue(true);

            NetworkEvent.Type cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);

            while (cmd != NetworkEvent.Type.Empty)
            {
            //Receiving Data from Client
                if (cmd == NetworkEvent.Type.Data)
                {
                    OnData(stream, i);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    m_Connections[i] = default(NetworkConnection);
                    continue;
                }

                cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            }
        }
    }

    private void SendData(object data, NetworkConnection c)
    {
        if (c == default(NetworkConnection))
        {

            Debug.LogError("Invalid Network Connection. Exiting Function.....");
            return;
            
        }
        
        var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
        
        string jsonString = JsonUtility.ToJson(data);
        NativeArray<byte> sendBytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(jsonString), Allocator.Temp);
        writer.WriteBytes(sendBytes);
        m_Driver.EndSend(writer);
    }
    private void SendData(object data, int connectionIndex)
    {
        
        if (connectionIndex < 0)
        {
            
            Debug.LogError("NegativeIndex. Exiting Function....");
            return;
            
        }
        
        SendData(data, m_Connections[connectionIndex]);
    }
    private void SendData(object data, Client client)
    {
        
        SendData(data, FindMatchingConnection(client.id));
    }

    private int FindMatchingConnection(int id)
    {
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if ((m_Connections[i].InternalId == id) && m_Connections[i].IsCreated)
                return i;
        }
        return -1;
    }
    private int FindMatchingConnection(string id)
    {
        int clientId = -1;
        int.TryParse(id, out clientId);
        return FindMatchingConnection(clientId);
    }

    private int FindMatchingClient(int id)
    {
        for (int i = 0; i < clients.Count; i++)
        {
            int clientId = -1;
            int.TryParse(clients[i].id, out clientId);
            if (clientId == id)
                return i;
        }
        return -1;
    }
    private int FindMatchingClient(string id)
    {
        int connectionId = -2;
        int.TryParse(id, out connectionId);
        return FindMatchingClient(connectionId);
    }
}