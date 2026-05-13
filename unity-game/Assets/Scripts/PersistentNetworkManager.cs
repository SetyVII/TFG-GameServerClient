using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class PersistentNetworkManager : MonoBehaviour
{
    public static PersistentNetworkManager Instance { get; private set; }
    
    public int port = 5000;
    private TcpListener server;
    private Thread listenThread;
    private TcpClient currentClient;
    private NetworkStream currentStream;
    private volatile bool shouldStop = false;
    private volatile bool javaConnected = false;
    private UnityMainThreadDispatcher dispatcher;
    
    // Callbacks
    public System.Action onJavaConnected;
    public System.Action<string> onDataReceived;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        dispatcher = UnityMainThreadDispatcher.Instance();
        
        UnityEngine.Debug.Log("[PersistentNetworkManager] Inicializado (DontDestroyOnLoad)");
    }
    
    public void IniciarServidor()
    {
        if (listenThread != null && listenThread.IsAlive)
        {
            UnityEngine.Debug.Log("[PersistentNetworkManager] Servidor ya estaba iniciado");
            return;
        }
        
        shouldStop = false;
        javaConnected = false;
        listenThread = new Thread(ListenForClients);
        listenThread.IsBackground = true;
        listenThread.Start();
        UnityEngine.Debug.Log("[PersistentNetworkManager] Servidor iniciado en puerto " + port);
    }
    
    public void DetenerServidor()
    {
        shouldStop = true;
        javaConnected = false;
        
        if (currentStream != null)
        {
            try { currentStream.Close(); } catch { }
            currentStream = null;
        }
        
        if (currentClient != null)
        {
            try { currentClient.Close(); } catch { }
            currentClient = null;
        }
        
        if (server != null)
        {
            try { server.Stop(); } catch { }
            server = null;
        }
        
        if (listenThread != null && listenThread.IsAlive)
        {
            listenThread.Join(1000);
        }
        
        UnityEngine.Debug.Log("[PersistentNetworkManager] Servidor detenido");
    }
    
    private void ListenForClients()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            UnityEngine.Debug.Log("[PersistentNetworkManager] Escuchando en puerto " + port);
            
            while (!shouldStop)
            {
                if (!server.Pending())
                {
                    Thread.Sleep(100);
                    continue;
                }
                
                TcpClient client = server.AcceptTcpClient();
                UnityEngine.Debug.Log("[PersistentNetworkManager] Cliente conectado: " + client.Client.RemoteEndPoint);
                
                // Cerrar cliente anterior si existe
                if (currentClient != null)
                {
                    try { currentClient.Close(); } catch { }
                }
                
                currentClient = client;
                currentStream = client.GetStream();
                javaConnected = true;
                
                // Notificar conexion
                dispatcher.Enqueue(() => {
                    onJavaConnected?.Invoke();
                });
                
                // Procesar mensajes
                ProcessClient(client);
            }
        }
        catch (Exception e)
        {
            if (!shouldStop)
            {
                UnityEngine.Debug.LogError("[PersistentNetworkManager] Error: " + e.Message);
            }
        }
    }
    
    private void ProcessClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;
        StringBuilder messageBuffer = new StringBuilder();
        
        try
        {
            while (!shouldStop && client.Connected)
            {
                if (!stream.DataAvailable)
                {
                    Thread.Sleep(50);
                    continue;
                }
                
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;
                
                string chunk = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                messageBuffer.Append(chunk);
                
                // Procesar mensajes completos
                string accumulated = messageBuffer.ToString();
                int newlineIndex;
                
                while ((newlineIndex = accumulated.IndexOf('\n')) >= 0)
                {
                    string line = accumulated.Substring(0, newlineIndex).Trim();
                    accumulated = accumulated.Substring(newlineIndex + 1);
                    
                    if (!string.IsNullOrEmpty(line))
                    {
                        ProcessLine(stream, line);
                    }
                }
                
                messageBuffer.Clear();
                messageBuffer.Append(accumulated);
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("[PersistentNetworkManager] Cliente desconectado: " + e.Message);
        }
        finally
        {
            javaConnected = false;
            if (stream != null)
            {
                try { stream.Close(); } catch { }
            }
        }
    }
    
    private void ProcessLine(NetworkStream stream, string line)
    {
        if (line == "JAVA_HANDSHAKE")
        {
            try
            {
                byte[] response = Encoding.ASCII.GetBytes("UNITY_OK\n");
                stream.Write(response, 0, response.Length);
                stream.Flush();
                UnityEngine.Debug.Log("[PersistentNetworkManager] Handshake exitoso");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[PersistentNetworkManager] Error handshake: " + ex.Message);
            }
            return;
        }
        
        if (line == "JAVA_PING")
        {
            try
            {
                byte[] response = Encoding.ASCII.GetBytes("UNITY_PONG\n");
                stream.Write(response, 0, response.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[PersistentNetworkManager] Error pong: " + ex.Message);
            }
            return;
        }
        
        // Reenviar datos al GameManager actual
        dispatcher.Enqueue(() => {
            onDataReceived?.Invoke(line);
        });
    }
    
    public void EnviarMensaje(string mensaje)
    {
        if (currentStream != null && currentStream.CanWrite)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(mensaje + "\n");
                currentStream.Write(data, 0, data.Length);
                currentStream.Flush();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[PersistentNetworkManager] Error enviando: " + ex.Message);
            }
        }
    }
    
    public string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString();
        }
        return "127.0.0.1";
    }
    
    public bool IsJavaConnected()
    {
        return javaConnected && currentClient != null && currentClient.Connected;
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        DetenerServidor();
    }
    
    void OnApplicationQuit()
    {
        DetenerServidor();
    }
}
