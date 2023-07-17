using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Logging
{
    public class LoggingDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingDelegatingHandler> _logger;

        public LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Sending request to {Url}", request.RequestUri);
                var response = await base.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Received a success response from {url}", response.RequestMessage.RequestUri);
                }
                else
                {
                    _logger.LogInformation("Received a non-sucess status code {StatusCode} from {Url}", (int)response.StatusCode, response.RequestMessage.RequestUri);
                }
                return response;
            }
            catch (Exception ex)
            when (ex.InnerException is SocketException se && se.SocketErrorCode == SocketError.ConnectionRefused)
            {
                var hostwithPort =
                    request.RequestUri.IsDefaultPort ? request.RequestUri.DnsSafeHost :
                    $"{request.RequestUri.DnsSafeHost}:{request.RequestUri.Port}";
                _logger.LogCritical(ex, "undable to connect to {Host}.please check the " +
                    "configuration to ensure the correct url for the service " +
                    "has been configured.", hostwithPort);
            }
            return new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                RequestMessage = request
            };
        }
    }
}
