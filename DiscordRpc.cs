using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public struct DiscordActivity
    {
        public string Details;
        public string State;
        public long StartTimestamp;
        public string LargeImage;
        public string LargeText;
        public string SmallImage;
        public string SmallText;
        public long StartUnix;
    }

    public class DiscordRpc : IDisposable
    {
        public const string APP_ID = "1544392370047688716";
        public const string IMG_LARGE = "runpro_logo";
        public const string IMG_SMALL_PLAYING = "st_playing";
        public const string IMG_SMALL_MENU = "st_menu";

        private NamedPipeClientStream _pipe;
        private Thread _reader;
        private readonly object _sendLock = new object();
        private volatile bool _connected;
        private volatile bool _running;
        private volatile bool _ready;
        private volatile bool _readyEvt;
        private volatile bool _connecting;

        private string _lastBody;
        private string _pendingBody;
        private DateTime _lastSendUtc = DateTime.MinValue;
        private DateTime _handshakeUtc = DateTime.MinValue;
        private DateTime _lastConnectAttempt = DateTime.MinValue;
        private readonly int _pid = Process.GetCurrentProcess().Id;

        public bool IsReady => _connected && _ready && (_readyEvt || (DateTime.UtcNow - _handshakeUtc).TotalSeconds > 2.0);

        public void TryConnect()
        {
            if (!_connected && !_connecting)
            {
                if ((DateTime.UtcNow - _lastConnectAttempt).TotalSeconds >= 5.0)
                {
                    _lastConnectAttempt = DateTime.UtcNow;
                    _connecting = true;
                    Thread thread = new Thread(ConnectLoop) { IsBackground = true };
                    thread.Start();
                }
            }
        }

        private void ConnectLoop()
        {
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    NamedPipeClientStream pipe = null;
                    try
                    {
                        pipe = new NamedPipeClientStream(".", "discord-ipc-" + i, PipeDirection.InOut, PipeOptions.Asynchronous);
                        pipe.Connect(150);
                        if (pipe.IsConnected)
                        {
                            _pipe = pipe;
                            _connected = true;
                            _running = true;
                            _ready = false;
                            _readyEvt = false;

                            _reader = new Thread(ReaderLoop) { IsBackground = true };
                            _reader.Start();

                            _handshakeUtc = DateTime.UtcNow;
                            SendFrame(0, "{\"v\":1,\"client_id\":\"" + APP_ID + "\"}");
                            _ready = true;
                            _lastBody = null;
                            _pendingBody = null;
                            return;
                        }
                        pipe.Dispose();
                    }
                    catch
                    {
                        pipe?.Dispose();
                    }
                }
                _connected = false;
            }
            finally
            {
                _connecting = false;
            }
        }

        private void ReaderLoop()
        {
            byte[] buf = new byte[8];
            try
            {
                while (_running && _pipe != null && _pipe.IsConnected)
                {
                    if (!ReadExact(buf, 8)) break;
                    int op = BitConverter.ToInt32(buf, 0);
                    int len = BitConverter.ToInt32(buf, 4);
                    if (len < 0 || len > 65536) break;

                    byte[] body = new byte[len];
                    if (!ReadExact(body, len)) break;

                    if (op == 2)
                    {
                        _connected = false;
                        _ready = false;
                        break;
                    }
                    if (op == 1 && !_readyEvt)
                    {
                        string str = Encoding.UTF8.GetString(body);
                        if (str.Contains("\"READY\""))
                        {
                            _readyEvt = true;
                        }
                    }
                }
            }
            catch { }
            _connected = false;
            _ready = false;
            _readyEvt = false;
        }

        private bool ReadExact(byte[] buf, int n)
        {
            int readTotal = 0;
            while (readTotal < n)
            {
                int read = 0;
                try { read = _pipe.Read(buf, readTotal, n - readTotal); } catch { return false; }
                if (read <= 0) return false;
                readTotal += read;
            }
            return true;
        }

        private bool SendFrame(int opcode, string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            byte[] header = new byte[8];
            Buffer.BlockCopy(BitConverter.GetBytes(opcode), 0, header, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(bytes.Length), 0, header, 4, 4);

            lock (_sendLock)
            {
                try
                {
                    _pipe.Write(header, 0, 8);
                    _pipe.Write(bytes, 0, bytes.Length);
                    _pipe.Flush();
                    return true;
                }
                catch
                {
                    _connected = false;
                    _ready = false;
                    return false;
                }
            }
        }

        public void SetActivity(DiscordActivity a)
        {
            if (_connected && IsReady)
            {
                string text = BuildBody(a);
                if (text != _lastBody)
                {
                    _pendingBody = text;
                }
                Pump();
            }
        }

        public void Pump()
        {
            if (_pendingBody != null && _connected && IsReady)
            {
                if ((DateTime.UtcNow - _lastSendUtc).TotalMilliseconds >= 1000.0)
                {
                    string body = _pendingBody;
                    if (SendFrame(1, $"{{\"cmd\":\"SET_ACTIVITY\",\"args\":{{\"pid\":{_pid},\"activity\":{body}}},\"nonce\":\"{Guid.NewGuid():N}\"}}"))
                    {
                        _lastBody = body;
                        _pendingBody = null;
                        _lastSendUtc = DateTime.UtcNow;
                    }
                }
            }
        }

        private string BuildBody(DiscordActivity a)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"instance\":true");
            if (a.StartUnix > 0L)
            {
                sb.Append(",\"timestamps\":{\"start\":").Append(a.StartUnix).Append("}");
            }
            sb.Append(",\"assets\":{");
            bool first = true;
            first = AppendAsset(sb, first, "large_image", a.LargeImage);
            first = AppendAsset(sb, first, "large_text", a.LargeText);
            first = AppendAsset(sb, first, "small_image", a.SmallImage);
            first = AppendAsset(sb, first, "small_text", a.SmallText);
            sb.Append("}");

            if (!string.IsNullOrEmpty(a.Details))
                sb.Append(",\"details\":\"").Append(Escape(a.Details)).Append("\"");
            if (!string.IsNullOrEmpty(a.State))
                sb.Append(",\"state\":\"").Append(Escape(a.State)).Append("\"");

            sb.Append("}");
            return sb.ToString();
        }

        private static bool AppendAsset(StringBuilder sb, bool first, string key, string val)
        {
            if (string.IsNullOrEmpty(val)) return first;
            if (!first) sb.Append(",");
            sb.Append("\"").Append(key).Append("\":\"").Append(Escape(val)).Append("\"");
            return false;
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\t': sb.Append("\\t"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    default:
                        if (c < ' ') sb.Append(' ');
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        public void Dispose()
        {
            _running = false;
            _connected = false;
            _ready = false;
            try { _pipe?.Dispose(); } catch { }
        }
    }
}