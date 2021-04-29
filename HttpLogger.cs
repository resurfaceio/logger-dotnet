using System.Collections.Generic;
using System.Text.Json;

namespace Resurfaceio
{
    class HttpLogger : BaseLogger
    {
        const string AGENT = "HttpLogger.cs";
        public HttpLogger(
            bool enabled = true,
            string url = null,
            string[] queue = null,
            string rules = null,
            bool skip_compression = false,
            bool skip_submission = false)
        : base(AGENT, url, queue, enabled, skip_compression, skip_submission)
        {
            this.rules = new HttpRules(rules);
            this.skip_compression = this.rules.skip_compression;
            this.skip_submission = this.rules.skip_submission;
            if ((this.url is not null) && (this.url.StartsWith("http:") && !this.rules.allow_http_url))
            {
                this.enableable = false;
                this.enabled = false;
            }
        }
        public HttpLogger() : this(true) {}
        public HttpLogger(string url) : this(true, url) {}
        public HttpLogger(string url, string rules) : this(true, url, rules:rules) {}
        public HttpLogger(string url, bool enabled) : this(enabled, url) {}
        public HttpLogger(string url, bool enabled, string rules) : this(enabled, url, rules:rules) {}
        public HttpLogger(string[] queue) : this(true, queue:queue) {}
        public HttpLogger(string[] queue, string rules) : this(true, queue:queue, rules: rules) {}
        public HttpLogger(string[] queue, bool enabled) : this(enabled, null, queue) {}
        public HttpLogger(string[] queue, bool enabled, string rules) : this(enabled, queue:queue, rules:rules) {}

        public void SubmitIfPassing(List<string[]> details)
        {
            details = this.rules.Apply(details);
            if (details is null) return;
            details.Add(new string[] {"host", this.host});

            Submit(JsonSerializer.Serialize(details));
        }
        protected internal HttpRules rules { get; }
    }
}