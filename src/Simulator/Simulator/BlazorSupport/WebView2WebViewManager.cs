using Microsoft.AspNetCore.Components.WebView;
using System.IO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.FileProviders;
using Microsoft.Web.WebView2.Wpf;

namespace OpenSilver.Simulator.BlazorSupport
{
    internal class WebView2WebViewManager : WebViewManager
    {
        private readonly WebView2 _webView;

        public WebView2WebViewManager(WebView2 webView, IServiceProvider provider, Dispatcher dispatcher, Uri appBaseUri, IFileProvider fileProvider, JSComponentConfigurationStore jsComponents, string hostPageRelativePath)
            : base(provider, dispatcher, appBaseUri, fileProvider, jsComponents, hostPageRelativePath)
        {
            _webView = webView;

            webView.CoreWebView2.WebMessageReceived += (s, e) => {
                var msgAsString = e.TryGetWebMessageAsString();
                MessageReceived(new Uri(e.Source), msgAsString);
            };
        }

        protected override void NavigateCore(Uri absoluteUri)
        {
            _ = Dispatcher.InvokeAsync(() =>
            {
                _webView.Source = absoluteUri;
            });
        }

        protected override void SendMessage(string message)
        {
            _webView.CoreWebView2.PostWebMessageAsString(message);
        }

        public bool TryGetResponse(string uri, out int statusCode, out string statusMessage, out Stream content, out IDictionary<string, string> headers)
        {
            return this.TryGetResponseContent(uri, false, out statusCode, out statusMessage, out content, out headers);
        }
    }
}
