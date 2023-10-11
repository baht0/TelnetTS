using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TelnetInfo.Core
{
    public class TelnetClient
    {
        private TcpClient Client;
        private StreamReader Reader;
        private StreamWriter Writer;

        public bool IsConneted => Client != null && Client.Connected;

        public async Task ConnectionAsync(string ip, string user, string pass)
        {
            Client = new TcpClient();
            await Client.ConnectAsync(ip, 23);
            NetworkStream stream = Client.GetStream();
            Reader = new StreamReader(stream);
            Writer = new StreamWriter(stream)
            {
                AutoFlush = true
            };
            await Reader.ReadLineAsync();

            await Writer.WriteLineAsync(user);
            await Writer.WriteLineAsync(pass);
            await Task.Delay(500);
            await Writer.WriteLineAsync("\r\n");
        }
        public void Disconnect()
        {
            Writer.Dispose();
            Reader.Dispose();
            Client.Dispose();
        }

        public async Task SendAsync(string command)
        {
            try
            {
                await Writer.WriteLineAsync(command);
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        public async Task SendAsync(string command, string pressButton)
        {
            try
            {
                await Writer.WriteLineAsync(command);
                await Task.Delay(500);
                await Writer.WriteLineAsync(pressButton);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        public async Task<string> ReadAsync()
        {
            try
            {
                int timeout = 1500;
                var task = Reader.ReadLineAsync();

                if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                {
                    var line = task.Result;
                    return line.Replace("\u001b7", string.Empty).Replace("��\u0001", string.Empty).Replace("??\u0001", string.Empty).Replace("---- More ( Press 'Q' to break ) ----\u001b[37D                                     \u001b[37D  ", string.Empty).Trim();
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }
    }
}
