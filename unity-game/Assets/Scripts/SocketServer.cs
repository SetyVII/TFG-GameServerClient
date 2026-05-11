using System;
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
    private UnityMainThreadDispatcher dispatcher;
    private volatile bool unityValidated = false;
    private volatile bool juegoIniciado = false;
    private TcpClient currentClient;
    private volatile bool shouldStop = false;

    void Start()
    {
        gameManager = GetComponent<GameManagerLaberinto>();
        dispatcher = UnityMainThreadDispatcher.Instance();

        if (gameManager.textDisplayIP != null)
            gameManager.textDisplayIP.text = "IP: " + GetLocalIPAddress();
        if (gameManager.textoEstadoConexion != null)
            gameManager.textoEstadoConexion.text = "Esperando controlador...";

        StartServer();
    }

    private void StartServer()
    {
        // Si ya hay un servidor corriendo, lo detenemos primero
        if (listenThread != null && listenThread.IsAlive)
        {
            shouldStop = true;
            if (server != null)
            {
                try { server.Stop(); } catch { }
            }
            listenThread.Join(1000);
        }

        shouldStop = false;
        listenThread = new Thread(ListenForClients);
        listenThread.IsBackground = true;
        listenThread.Start();
        UnityEngine.Debug.Log("[SocketServer] Servidor iniciado");
    }

    private void ListenForClients()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            UnityEngine.Debug.Log("[SocketServer] Servidor iniciado en puerto " + port);

            while (!shouldStop)
            {
                // Usar BeginAcceptTcpClient para poder cancelar
                if (!server.Pending())
                {
                    Thread.Sleep(100);
                    continue;
                }

                TcpClient client = server.AcceptTcpClient();
                UnityEngine.Debug.Log("[SocketServer] Cliente conectado desde: " + client.Client.RemoteEndPoint);

                // Cerramos conexi�n anterior si existe
                CloseCurrentClient();
                currentClient = client;

                // Procesamos mensajes en el mismo hilo (solo 1 conexi�n a la vez)
                ProcessClient(client);
            }
        }
        catch (Exception e)
        {
            if (!shouldStop)
            {
                UnityEngine.Debug.LogError("[SocketServer] Error en ListenForClients: " + e.Message);
            }
        }
    }

    private void ProcessClient(TcpClient client)
    {
        NetworkStream stream = null;
        try
        {
            stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;
            StringBuilder messageBuffer = new StringBuilder();

            while (!shouldStop)
            {
                // Verificar si hay datos disponibles
                if (!stream.DataAvailable)
                {
                    Thread.Sleep(50);
                    continue;
                }

                bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    UnityEngine.Debug.Log("[SocketServer] Cliente cerr� conexi�n normalmente");
                    break;
                }

                string chunk = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                messageBuffer.Append(chunk);
                UnityEngine.Debug.Log("[SocketServer] Bytes recibidos: " + bytesRead + " | Chunk: [" + chunk.Replace("\n", "\\n") + "]");

                // Procesar mensajes completos (separados por \n)
                ProcessBuffer(stream, messageBuffer);
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("[SocketServer] Error en ProcessClient: " + e.Message);
        }
        finally
        {
            unityValidated = false;
            juegoIniciado = false;
            currentClient = null;
            if (stream != null)
            {
                try { stream.Close(); } catch { }
            }
            dispatcher.Enqueue(() => {
                if (gameManager.textoEstadoConexion != null)
                    gameManager.textoEstadoConexion.text = "Esperando controlador...";
            });
        }
    }

    private void ProcessBuffer(NetworkStream stream, StringBuilder messageBuffer)
    {
        string accumulated = messageBuffer.ToString();
        int newlineIndex;

        while ((newlineIndex = accumulated.IndexOf('\n')) >= 0)
        {
            string line = accumulated.Substring(0, newlineIndex).Trim();
            accumulated = accumulated.Substring(newlineIndex + 1);

            if (!string.IsNullOrEmpty(line))
            {
                UnityEngine.Debug.Log("[SocketServer] Procesando l�nea: [" + line + "]");
                ProcessLine(stream, line);
            }
        }

        messageBuffer.Clear();
        messageBuffer.Append(accumulated);
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
                unityValidated = true;
                UnityEngine.Debug.Log("[SocketServer] Handshake exitoso - enviado UNITY_OK");

                dispatcher.Enqueue(() => {
                    if (gameManager.textoEstadoConexion != null)
                        gameManager.textoEstadoConexion.text = "Controlador validado";
                });
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[SocketServer] Error enviando handshake: " + ex.Message);
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
                UnityEngine.Debug.Log("[SocketServer] Respondido UNITY_PONG");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[SocketServer] Error enviando pong: " + ex.Message);
            }
            return;
        }

        if (line.StartsWith("CONFIG,"))
        {
            try
            {
                string[] configData = line.Split(',');
                if (configData.Length >= 3)
                {
                    string sensitivity = configData[1];
                    int force = int.Parse(configData[2]);
                    
                    UnityEngine.Debug.Log("[SocketServer] Config recibida - Sensitivity: " + sensitivity + " Force: " + force);
                    
                    dispatcher.Enqueue(() => {
                        // Configurar fuerza y velocidad máxima según sensibilidad
                        // Valores extremos para notar la diferencia
                        switch (sensitivity.ToLower())
                        {
                            case "low":
                                gameManager.fuerzaMando = 0.5f;  // Muy lento
                                gameManager.velocidadMaximaMovil = 3f;
                                break;
                            case "medium":
                                gameManager.fuerzaMando = 5.0f;  // Normal
                                gameManager.velocidadMaximaMovil = 10f;
                                break;
                            case "high":
                                gameManager.fuerzaMando = 20.0f;  // Muy rápido
                                gameManager.velocidadMaximaMovil = 25f;
                                break;
                            case "custom":
                                float fuerzaCustom = Mathf.Clamp(force / 10f, 0.5f, 25f);
                                gameManager.fuerzaMando = fuerzaCustom;
                                gameManager.velocidadMaximaMovil = fuerzaCustom * 1.5f;
                                break;
                            default:
                                gameManager.fuerzaMando = 5.0f;
                                gameManager.velocidadMaximaMovil = 10f;
                                break;
                        }
                        string msg = "[SocketServer] CONFIG APLICADA: " + sensitivity + " | Fuerza: " + gameManager.fuerzaMando + " | Vel.Max: " + gameManager.velocidadMaximaMovil;
                        UnityEngine.Debug.Log(msg);
                        
                        // Mostrar en UI si existe texto de estado
                        if (gameManager.textoEstadoConexion != null)
                        {
                            gameManager.textoEstadoConexion.text = "Sensibilidad: " + sensitivity.ToUpper() + " (F" + gameManager.fuerzaMando + ")";
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[SocketServer] Error procesando config: " + ex.Message);
            }
            return;
        }

        // Procesamos mensajes de datos (CSV)
        ProcessMessage(line);
    }

    private void ProcessMessage(string msg)
    {
        string[] data = msg.Split(',');
        UnityEngine.Debug.Log("[SocketServer] Parseando CSV, elementos: " + data.Length);

        if (data.Length >= 4)
        {
            try
            {
                float alpha = float.Parse(data[0]);
                float beta = float.Parse(data[1]);
                float gamma = float.Parse(data[2]);
                string accion = data[3].Trim().ToLower();
                
                // Solo actualizar sensores si no es un mensaje de register (para evitar resetear a 0)
                if (accion != "register")
                {
                    gameManager.sensor_Alpha = alpha;
                    gameManager.sensor_Beta = beta;
                    gameManager.sensor_Gamma = gamma;
                }
                
                UnityEngine.Debug.Log("[SocketServer] Sensores actualizados - Alpha:" + alpha + " Beta:" + beta + " Gamma:" + gamma);
                UnityEngine.Debug.Log("[SocketServer] Acci�n recibida: " + accion);

                dispatcher.Enqueue(() => {
                    if (accion == "register" && !juegoIniciado)
                    {
                        juegoIniciado = true;
                        gameManager.EmpezarJuego();
                    }
                    else if (accion == "cambiar")
                    {
                        gameManager.AccionBotonA();
                    }
                    else if (accion == "validar")
                    {
                        gameManager.AccionBotonB();
                    }
                });

            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[SocketServer] Error parseando mensaje: " + e.Message);
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("[SocketServer] Mensaje CSV inv�lido (menos de 4 elementos): [" + msg + "]");
        }
    }

    private void CloseCurrentClient()
    {
        if (currentClient != null)
        {
            try
            {
                currentClient.Close();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[SocketServer] Error cerrando cliente anterior: " + ex.Message);
            }
            currentClient = null;
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

    void OnDisable()
    {
        // Se llama cuando el script se deshabilita o Unity recompila scripts
        Cleanup();
    }

    void OnApplicationQuit()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        shouldStop = true;
        juegoIniciado = false;
        if (server != null)
        {
            try { server.Stop(); } catch { }
        }
        CloseCurrentClient();
        // No usar Thread.Abort() - est� deprecado y causa problemas
        if (listenThread != null && listenThread.IsAlive)
        {
            listenThread.Join(1000); // Esperar 1 segundo a que termine
        }
    }
}
