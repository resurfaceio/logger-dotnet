using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

namespace Resurfaceio
{
    class HttpRules
    {
        public const string DEBUG_RULES = "allow_http_url\ncopy_session_field /.*/\n";
        public const string STANDARD_RULES = "/request_header:cookie|response_header:set-cookie/remove\n" +
            "/(request|response)_body|request_param/ replace /[a-zA-Z0-9.!#$%&’*+\\/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\\.[a-zA-Z0-9-]+)/, /x@y.com/\n" +
            "/request_body|request_param|response_body/ replace /[0-9\\.\\-\\/]{9,}/, /xyxy/\n";
        public const string STRICT_RULES = "/request_url/ replace /([^\\?;]+).*/, /$1/\n" +
            "/request_body|response_body|request_param:.*|request_header:(?!user-agent).*|response_header:(?!(content-length)|(content-type)).*/ remove\n";
        private static string defaultRules = STRICT_RULES;
        public static string DefaultRules
        {
            get => defaultRules;
            set => defaultRules = new Regex(@"^\s*include default\s*$", RegexOptions.Multiline | RegexOptions.ECMAScript)
            .Replace(value, "");
        }
        public static string debugRules { get => DEBUG_RULES; }
        public static string standardRules { get => STANDARD_RULES; }
        public static string strictRules { get => STRICT_RULES; }
        public static HttpRule ParseRule(string r)
        {
            if ((r is null) || REGEX_BLANK_OR_COMMENT.Match(r).Success) return null;
            var m = REGEX_ALLOW_HTTP_URL.Match(r);
            if (m.Success) return new HttpRule("allow_http_url", null, null, null);
            m = REGEX_COPY_SESSION_FIELD.Match(r);
            if (m.Success) return new HttpRule("copy_session_field", null, ParseRegex(r, m.Groups[1].Value), null);
            m = REGEX_REMOVE.Match(r);
            if (m.Success) return new HttpRule("remove", ParseRegex(r, m.Groups[1].Value), null, null);
            m = REGEX_REMOVE_IF.Match(r);
            if (m.Success) return new HttpRule("remove_if", ParseRegex(r, m.Groups[1].Value), ParseRegex(r, m.Groups[2].Value), null);
            m = REGEX_REMOVE_IF_FOUND.Match(r);
            if (m.Success) return new HttpRule("remove_if_found", ParseRegex(r, m.Groups[1].Value), ParseRegexFind(r, m.Groups[2].Value), null);
            m = REGEX_REMOVE_UNLESS.Match(r);
            if (m.Success) return new HttpRule("remove_unless", ParseRegex(r, m.Groups[1].Value), ParseRegex(r, m.Groups[2].Value), null);
            m = REGEX_REMOVE_UNLESS_FOUND.Match(r);
            if (m.Success)
                return new HttpRule("remove_unless_found", ParseRegex(r, m.Groups[1].Value), ParseRegexFind(r, m.Groups[2].Value), null);
            m = REGEX_REPLACE.Match(r);
            if (m.Success)
                return new HttpRule("replace", ParseRegex(r, m.Groups[1].Value), ParseRegexFind(r, m.Groups[2].Value), ParseString(r, m.Groups[3].Value));
            m = REGEX_SAMPLE.Match(r);
            if (m.Success) {
                int m1 = Int32.Parse(m.Groups[1].Value);
                if (m1 < 1 || m1 > 99) throw new ArgumentException($"Invalid sample percent: {m1}");
                return new HttpRule("sample", null, m1, null);
            }
            m = REGEX_SKIP_COMPRESSION.Match(r);
            if (m.Success) return new HttpRule("skip_compression", null, null, null);
            m = REGEX_SKIP_SUBMISSION.Match(r);
            if (m.Success) return new HttpRule("skip_submission", null, null, null);
            m = REGEX_STOP.Match(r);
            if (m.Success) return new HttpRule("stop", ParseRegex(r, m.Groups[1].Value), null, null);
            m = REGEX_STOP_IF.Match(r);
            if (m.Success) return new HttpRule("stop_if", ParseRegex(r, m.Groups[1].Value), ParseRegex(r, m.Groups[2].Value), null);
            m = REGEX_STOP_IF_FOUND.Match(r);
            if (m.Success) return new HttpRule("stop_if_found", ParseRegex(r, m.Groups[1].Value), ParseRegexFind(r, m.Groups[2].Value), null);
            m = REGEX_STOP_UNLESS.Match(r);
            if (m.Success) return new HttpRule("stop_unless", ParseRegex(r, m.Groups[1].Value), ParseRegex(r, m.Groups[2].Value), null);
            m = REGEX_STOP_UNLESS_FOUND.Match(r);
            if (m.Success) return new HttpRule("stop_unless_found", ParseRegex(r, m.Groups[1].Value), ParseRegexFind(r, m.Groups[2].Value), null);
            throw new ArgumentException($"Invalid rule: {r}");
        }
        private static Regex ParseRegex(string r, string regex)
        {
            string s = ParseString(r, regex);
            if ("*".Equals(s) || "+".Equals(s) || "?".Equals(s))
                throw new ArgumentException($"Invalid regex ({regex}) in rule: {r}");
            if (!s.StartsWith("^")) s = "^" + s;
            if (!s.EndsWith("$")) s = s + "$";
            try
            {
                return new Regex(s);
            }
            catch (Exception)
            {
                throw new ArgumentException($"Invalid regex ({regex}) in rule: {r}");
            }
        }
        private static Regex ParseRegexFind(string r, string regex)
        {
            try
            {
                return new Regex(ParseString(r, regex));
            }
            catch (Exception)
            {
                throw new ArgumentException($"Invalid regex ({regex}) in rule: {r}");
            }
        }
        private static string ParseString(string r, string expr)
        {
            foreach (string sep in new string[] {"~", "!", "%", "|", "/"}) {
                var m = Regex.Match(expr, $"^[{sep}](.*)[{sep}]$");
                if (m.Success) {
                    string m1 = m.Groups[1].Value;
                    if (Regex.Match(m1, $"^[{sep}].*|.*[^\\\\][{sep}].*").Success)
                        throw new ArgumentException($"Unescaped separator ({sep}) in rule: {r}");
                    return Regex.Replace(m1, "\\" + sep, sep);
                }
            }
            throw new ArgumentException($"Invalid expression ({expr}) in rule: {r}");
        }
        public HttpRules(string rules)
        {
            if (rules == null) rules = HttpRules.DefaultRules;

            // load rules from external files
            if (rules.StartsWith("file://")) {
                string rfile = rules.Substring(7).Trim();
                try
                {
                    rules = new string(File.ReadAllTextAsync(rfile).Result);
                }
                catch (Exception) {
                    throw new ArgumentException("Failed to load rules: " + rfile);
                }
            }

            // force default rules if necessary
            rules = Regex.Replace(rules, @"^\s*include default\s*$", HttpRules.DefaultRules, RegexOptions.Multiline | RegexOptions.ECMAScript);
            if (rules.Trim().Length == 0) rules = HttpRules.DefaultRules;

            // expand rule includes
            rules = Regex.Replace(rules, @"^\s*include debug\s*$", HttpRules.debugRules, RegexOptions.Multiline | RegexOptions.ECMAScript);
            rules = Regex.Replace(rules, @"^\s*include standard\s*$", HttpRules.standardRules, RegexOptions.Multiline | RegexOptions.ECMAScript);
            rules = Regex.Replace(rules, @"^\s*include strict\s*$", HttpRules.strictRules, RegexOptions.Multiline | RegexOptions.ECMAScript);
            this.text = rules;

            // parse all rules
            var prs = new List<HttpRule>();
            foreach (string rule in this.text.Split(new char[] {'\n', '\r'})) {
                HttpRule parsed = ParseRule(rule);
                if (parsed != null) prs.Add(parsed);
            }
            this.size = prs.Count;

            // break out rules by verb
            this.allow_http_url = prs.Find(r => r.verb.Equals("allow_http_url")) is not null;
            this.copy_session_field = prs.FindAll(r => r.verb.Equals("copy_session_field"));
            this.remove = prs.FindAll(r => r.verb.Equals("remove"));
            this.remove_if = prs.FindAll(r => r.verb.Equals("remove_if"));
            this.remove_if_found = prs.FindAll(r => r.verb.Equals("remove_if_found"));
            this.remove_unless = prs.FindAll(r => r.verb.Equals("remove_unless"));
            this.remove_unless_found = prs.FindAll(r => r.verb.Equals("remove_unless_found"));
            this.replace = prs.FindAll(r => r.verb.Equals("replace"));
            this.sample = prs.FindAll(r => r.verb.Equals("sample"));
            this.skip_compression = prs.Find(r => r.verb.Equals("skip_compression")) is not null;
            this.skip_submission = prs.Find(r => r.verb.Equals("skip_submission")) is not null;
            this.stop = prs.FindAll(r => r.verb.Equals("stop"));
            this.stop_if = prs.FindAll(r => r.verb.Equals("stop_if"));
            this.stop_if_found = prs.FindAll(r => r.verb.Equals("stop_if_found"));
            this.stop_unless = prs.FindAll(r => r.verb.Equals("stop_unless"));
            this.stop_unless_found = prs.FindAll(r => r.verb.Equals("stop_unless_found"));

            // finish validating rules
            if (this.sample.Count > 1) throw new ArgumentException("Multiple sample rules");
        }
        public readonly bool allow_http_url;
        public readonly List<HttpRule> copy_session_field;
        public readonly List<HttpRule> remove;
        public readonly List<HttpRule> remove_if;
        public readonly List<HttpRule> remove_if_found;
        public readonly List<HttpRule> remove_unless;
        public readonly List<HttpRule> remove_unless_found;
        public readonly List<HttpRule> replace;
        public readonly List<HttpRule> sample;
        public readonly bool skip_compression;
        public readonly bool skip_submission;
        public readonly int size;
        public readonly List<HttpRule> stop;
        public readonly List<HttpRule> stop_if;
        public readonly List<HttpRule> stop_if_found;
        public readonly List<HttpRule> stop_unless;
        public readonly List<HttpRule> stop_unless_found;
        public readonly string text;
        public List<string[]> Apply(List<string[]> details)
        {
            // stop rules come first
            foreach (HttpRule r in stop)
                foreach (string[] d in details)
                    if (r.scope.Match(d[0]).Success) return null;
            foreach (HttpRule r in stop_if_found)
                foreach (string[] d in details)
                    if (r.scope.Match(d[0]).Success && ((Regex) r.param1).Match(d[1]).Success) return null;
            foreach (HttpRule r in stop_if)
                foreach (string[] d in details)
                    if (r.scope.Match(d[0]).Success && ((Regex) r.param1).Match(d[1]).Success) return null;
            int passed = 0;
            foreach (HttpRule r in stop_unless_found)
                foreach (string[] d in details)
                    if (r.scope.Match(d[0]).Success && ((Regex) r.param1).Match(d[1]).Success) passed++;
            if (passed != stop_unless_found.Count) return null;
            passed = 0;
            foreach (HttpRule r in stop_unless)
                foreach (string[] d in details)
                    if (r.scope.Match(d[0]).Success && ((Regex) r.param1).Match(d[1]).Success) passed++;
            if (passed != stop_unless.Count) return null;

            // do sampling if configured
            if ((sample.Count == 1) && (RANDOM.Next(100) >= (int) sample[0].param1)) return null;

            // winnow sensitive details based on remove rules if configured
            foreach (HttpRule r in remove)
                details = details.FindAll(d => !r.scope.Match(d[0]).Success);
            foreach (HttpRule r in remove_unless_found)
                details = details.FindAll(d => !r.scope.Match(d[0]).Success || Regex.IsMatch((string) r.param1, d[1]));
            foreach (HttpRule r in remove_if_found)
                details = details.FindAll(d => !r.scope.Match(d[0]).Success || !Regex.IsMatch((string) r.param1, d[1]));
            foreach (HttpRule r in remove_unless)
                details = details.FindAll(d => !r.scope.Match(d[0]).Success || Regex.IsMatch((string) r.param1, d[1]));
            foreach (HttpRule r in remove_if)
                details = details.FindAll(d => !r.scope.Match(d[0]).Success || !Regex.IsMatch((string) r.param1, d[1]));
            if (details.Count == 0) return null;

            // mask sensitive details based on replace rules if configured
            foreach (HttpRule r in replace)
                foreach (string[] d in details)
                    if (r.scope.Match(d[0]).Success) d[1] = Regex.Replace(d[1], (string) r.param1, (string) r.param2);

            // remove any details with empty values
            details = details.FindAll(d => !("".Equals(d[1])));
            if (details.Count == 0) return null;

            return details;
        }
        private static readonly Random RANDOM = new Random();
        private static readonly Regex REGEX_ALLOW_HTTP_URL = new Regex(@"^\s*allow_http_url\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_BLANK_OR_COMMENT = new Regex(@"^\s*([#].*)*$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_COPY_SESSION_FIELD = new Regex(@"^\s*copy_session_field\s+([~!%|/].+[~!%|/])\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_REMOVE = new Regex(@"^\s*([~!%|/].+[~!%|/])\s*remove\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_REMOVE_IF = new Regex(@"^\s*([~!%|/].+[~!%|/])\s*remove_if\s+([~!%|/].+[~!%|/])\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_REMOVE_IF_FOUND = new Regex(@"^\s*([~!%|/].+[~!%|/])\s*remove_if_found\s+([~!%|/].+[~!%|/])\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_REMOVE_UNLESS = new Regex(@"^\s*([~!%|/].+[~!%|/])\s*remove_unless\s+([~!%|/].+[~!%|/])\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_REMOVE_UNLESS_FOUND = new Regex(@"^\s*([~!%|/].+[~!%|/])\s*remove_unless_found\s+([~!%|/].+[~!%|/])\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_REPLACE = new Regex(@"^\s*([~!%|/].+[~!%|/])\s*replace[\s]+([~!%|/].+[~!%|/]),[\s]+([~!%|/].*[~!%|/])\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_SAMPLE = new Regex(@"^\s*sample\s+(\d+)\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_SKIP_COMPRESSION = new Regex(@"^\s*skip_compression\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_SKIP_SUBMISSION = new Regex(@"^\s*skip_submission\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_STOP = new Regex(@"^\s*([~!%|/].+[~!%|/])\s*stop\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_STOP_IF = new Regex(@"^\s*([~!%|/].+[~!%|/])\s*stop_if\s+([~!%|/].+[~!%|/])\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_STOP_IF_FOUND = new Regex(@"^\s*([~!%|/].+[~!%|/])\s*stop_if_found\s+([~!%|/].+[~!%|/])\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_STOP_UNLESS = new Regex(@"^\s*([~!%|/].+[~!%|/])\s*stop_unless\s+([~!%|/].+[~!%|/])\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
        private static readonly Regex REGEX_STOP_UNLESS_FOUND = new Regex(@"^\s*([~!%|/].+[~!%|/])\s*stop_unless_found\s+([~!%|/].+[~!%|/])\s*(#.*)?$", RegexOptions.Multiline | RegexOptions.ECMAScript);
    }
}