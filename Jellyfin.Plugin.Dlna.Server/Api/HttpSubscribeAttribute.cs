using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Jellyfin.Plugin.Dlna.Server.Api
{
    /// <summary>
    /// Identifies an action that supports the HTTP GET method.
    /// </summary>
    public class HttpSubscribeAttribute : HttpMethodAttribute
    {
        private static readonly IEnumerable<string> _supportedMethods = new[] { "SUBSCRIBE" };

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpSubscribeAttribute"/> class.
        /// </summary>
        /// <param name="template">The route template. May not be null.</param>
        public HttpSubscribeAttribute(string template)
            : base(_supportedMethods, template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }
        }
    }
}
