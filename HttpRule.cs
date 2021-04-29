using System.Text.RegularExpressions;

namespace Resurfaceio
{
    class HttpRule
    {
        public HttpRule(string verb, Regex scope = null, object param1 = null, object param2 = null)
        {
            this.verb = verb;
            this.scope = scope;
            this.param1 = param1;
            this.param2 = param2;
        }
        public readonly string verb;
        public readonly Regex scope;
        public readonly object param1;
        public readonly object param2;
    }
}