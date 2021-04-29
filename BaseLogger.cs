using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using System.Net.Http;
using System.Net.Sockets;
using System.Net;
using System;

namespace Resurfaceio
{
    class BaseLogger
    {
        public BaseLogger(
            string agent,
            string url = null,
            string[] queue = null,
            bool enabled = true,
            bool skip_compression = false,
            bool skip_submission = false)
        {
            this.agent = agent;
            this.host = HostLookup();
            this.version = VersionLookup();
            this.conn.DefaultRequestHeaders
            .Add("User-Agent", $"Resurface/{this.version} ({this.agent})");
            this.skip_compression = skip_compression;
            this.skip_submission = skip_submission;

            if (url is null) this.url = UsageLoggers.UrlByDefault();

            this.enabled = enabled;
            this.queue = queue is string[]? new List<string>(queue) : null;
            if (queue is not null)
            {
                this.url = null;
            }
            else if (url is not null && url is string)
            {
                try
                {
                    this.url_parsed = new Uri(url);
                    if (!this.url_parsed.Scheme.Contains("http"))
                        throw new UriFormatException("incorrect URL scheme");
                    this.url = url;
                }
                catch (UriFormatException)
                {
                    this.url = null;
                    this.url_parsed = null;
                    this.enabled = false;
                }
            }
            else
            {
                this.enabled = false;
                this.url = null;
            }

            this.enableable = this.queue is not null || this.url is not null;
            this.submit_failures = 0;
            this.submit_successes = 0;
        }
        public BaseLogger(string agent, bool enabled)
            : this(agent, url:null, enabled:enabled) {}
        public BaseLogger(string agent, string[] queue)
            : this(agent, null, queue) {}
        public BaseLogger(string agent, string url, bool enabled)
            : this(agent, url, null, enabled) {}
        public BaseLogger(string agent, string[] queue, bool enabled)
            : this(agent, null, queue, enabled) {}
        public void Submit(string msg)
        {
            if (msg is null || this.skip_submission || !Enabled)
            {}
            else if (this.queue is not null)
            {
                this.queue.Add(msg);
                Interlocked.Increment(ref submit_successes);
            }
            else
            {
                try
                {
                    HttpContent content = new StringContent(msg, Encoding.UTF8, "application/json");
                    content.Headers.Add("Content-Encoding", this.skip_compression ? "identity" : "deflated");
                    // TODO - zlib compression
                    var response = this.conn.PostAsync(this.url, content).Result;
                    if ((int) response.StatusCode == 204)
                    {
                        Interlocked.Increment(ref submit_successes);
                    }
                    else
                    {
                        Interlocked.Increment(ref submit_failures);
                    }
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref submit_failures);
                }
            }
            return;
        }
        public BaseLogger Disable()
        {
            enabled = false;
            return this;
        }
        public BaseLogger Enable()
        {
            if (enableable) enabled = true;
            return this;
        }
        protected internal readonly string agent;
        protected internal bool enableable;
        protected internal bool enabled;
        protected internal bool Enabled
        {
            get { return enabled && UsageLoggers.IsEnabled(); }
            set { enabled = value; }
        }
        protected internal readonly string host;
        protected internal readonly List<string> queue;
        protected internal bool skip_compression = false;
        protected internal bool skip_submission = false;
        protected internal int submit_failures;
        protected internal int submit_successes;
        protected internal string url;
        protected internal Uri url_parsed;
        protected internal readonly string version;
        private readonly HttpClient conn = new HttpClient();
        public static string HostLookup()
        {
            var dyno = Environment.GetEnvironmentVariable("DYNO");
            if (dyno != null)
            {
                return dyno;
            }
            else
            {
                try
                {
                    return Dns.GetHostName();
                }
                catch (SocketException)
                {
                    return "unknown";
                }
            }
        }

        public static string VersionLookup()
        {
            return Assembly.GetEntryAssembly().GetName().Version.ToString();
        }
    }

}