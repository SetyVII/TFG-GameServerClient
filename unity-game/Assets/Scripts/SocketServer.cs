using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class SocketServer : MonoBehaviour
{
    public int port = 5000;
    private TcpListener server;
    private Thread listenThread;
    private GameManagerLaberinto gameManager;
    private UnityMainThreadDispatcher mainThreadDispatcher;
    private volatile bool keepListening = true;
    private volatile bool hasActiveClient = false;

    public bool IsClientConnected => hasActiveClient;

    void Start()
    {
        gameManager = GetComponent<GameManagerLaberinto>();
        mainThreadDispatcher = UnityMainThreadDispatcher.Instance();

        if (gameManager.textDisplayIP != null)
            gameManager.textDisplayIP.text = "IP: " + GetLocalIPAddress();

        keepListening = true;
        listenThread = new Thread(ListenForClients);
        listenThread.IsBackground = true;
        listenThread.Start();
        Debug.Log($"[Socket] Listener iniciado en puerto {port}. Esperando cliente...");
    }

    private void ListenForClients()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            while (keepListening)
            {
                TcpClient client;
                try
                {
                    client = server.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                hasActiveClient = true;
                string remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
                Debug.Log($"[Socket] Cliente conectado: {remote}");

                mainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log($"[Socket] Cliente conectado. EsperandoConexionMovil={gameManager != null && gameManager.EsperandoConexionMovil}");
                    if (gameManager != null && gameManager.isActiveAndEnabled && gameManager.EsperandoConexionMovil)
                    {
                        Debug.Log("[Socket] Llamando a EmpezarJuego()...");
                        gameManager.EmpezarJuego();
                    }
                    else
                    {
                        Debug.Log("[Socket] NO se llama a EmpezarJuego (esperandoConexionMovil=false)");
                    }
                });

                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }
        catch (SocketException) { }
        catch (Exception e)
        {
            Debug.LogError("[Socket] Error en listener: " + e.Message);
        }
    }

    private void HandleClient(object state)
    {
        using TcpClient client = (TcpClient)state;
        string remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";

        try
        {
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream, Encoding.ASCII);
            string line;
            while (keepListening && (line = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    ProcessMessage(line);
            }
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
        finally
        {
            hasActiveClient = false;
            Debug.Log($"[Socket] Cliente desconectado: {remote}");
        }
    }

    private void ProcessMessage(string msg)
    {
        if (gameManager == null) return;

        string[] data = msg.Split(',');

        if (data.Length >= 4)
        {
            bool parsedAlpha = float.TryParse(data[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float alpha);
            bool parsedBeta = float.TryParse(data[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float beta);
            bool parsedGamma = float.TryParse(data[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float gamma);

            if (!parsedAlpha || !parsedBeta || !parsedGamma)
                return;

            gameManager.sensor_Alpha = alpha;
            gameManager.sensor_Beta = beta;
            gameManager.sensor_Gamma = gamma;

            string accion = data[3].Trim().ToLowerInvariant();

            mainThreadDispatcher.Enqueue(() =>
            {
                if (gameManager == null || !gameManager.isActiveAndEnabled) return;

                if (accion == "cambiar")
                    gameManager.AccionBotonA();
                else if (accion == "validar")
                    gameManager.AccionBotonB();
            });
        }
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString();
        }
        return "127.0.0.1";
    }

    void OnDestroy()
    {
        Debug.Log("[Socket] OnDestroy - deteniendo servidor...");
        StopServer();
    }

    void OnApplicationQuit()
    {
        StopServer();
    }

    private void StopServer()
    {
        keepListening = false;
        hasActiveClient = false;
        try { server?.Stop(); } catch { }
        if (listenThread != null && listenThread.IsAlive)
            listenThread.Join(500);
        Debug.Log("[Socket] Servidor detenido.");
    }
}
