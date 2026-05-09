using System;
using System.Diagnostics;
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

    void Start()
    {
        // Buscamos el componente GameManagerLaberinto en este mismo objeto
        gameManager = GetComponent<GameManagerLaberinto>();

        // Mostramos la IP en el Panel IP si el texto está asignado
        if (gameManager.textDisplayIP != null)
            gameManager.textDisplayIP.text = "IP: " + GetLocalIPAddress();

        listenThread = new Thread(ListenForClients);
        listenThread.IsBackground = true;
        listenThread.Start();
    }

    private void ListenForClients()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            UnityEngine.Debug.Log("Servidor iniciado en puerto " + port);

            while (true)
            {
                using (TcpClient client = server.AcceptTcpClient())
                {
                    // Al conectar, forzamos el inicio del juego en el hilo principal
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        gameManager.EmpezarJuego();
                    });

                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                            ProcessMessage(message);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log("Socket Error: " + e.Message);
        }
    }

    private void ProcessMessage(string msg)
    {
        // Formato esperado: Alpha,Beta,Gamma,Accion
        string[] data = msg.Split(',');

        if (data.Length >= 4)
        {
            try
            {
                // 1. Actualizamos sensores de inclinación
                gameManager.sensor_Alpha = float.Parse(data[0]);
                gameManager.sensor_Beta = float.Parse(data[1]);
                gameManager.sensor_Gamma = float.Parse(data[2]);

                // 2. Leemos la acción (botón pulsado)
                string accion = data[3].Trim().ToLower();

                // 3. Ejecutamos la acción en el hilo principal de Unity
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    if (accion == "cambiar")
                    { // BOTÓN A en el móvil
                        gameManager.AccionBotonA();
                    }
                    else if (accion == "validar")
                    { // BOTÓN B en el móvil
                        gameManager.AccionBotonB();
                    }
                });

            }
            catch (Exception e)
            {
                // Ignoramos errores de parseo de datos corruptos
            }
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

    void OnApplicationQuit()
    {
        if (server != null) server.Stop();
        if (listenThread != null) listenThread.Abort();
    }
}