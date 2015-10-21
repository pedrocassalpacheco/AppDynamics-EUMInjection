using System;
using System.Text;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Configuration;
using System.Reflection;
using System.Net;

/// <summary>
/// This is a custom module that inserts 
/// </summary>

public class EUMInjectionModule : IHttpModule
{
    string sSource = "AppDynamics HTTP Module";
    string sLog = "Application";
    Boolean reference;                  // Inline or reference script
    String uri;                         // URI to ADRUM.js

    public EUMInjectionModule()
    {
        if (!EventLog.SourceExists(sSource))
            EventLog.CreateEventSource(sSource, sLog);
    }

    public String ModuleName
    {
        get { return "EUMInjeectionModule"; }
    }

    public void Init(HttpApplication application)
    {
        EventLog.WriteEntry(sSource, "HTTP Module registration", EventLogEntryType.Information);

        EventLog.WriteEntry(sSource, "Loading configuration from file:" + AssemblyDirectory + "\\EUMInjection.dll");
        EventLog.WriteEntry(sSource, "HTTP Post Request", EventLogEntryType.Information);
        Configuration appConfig = ConfigurationManager.OpenExeConfiguration(AssemblyDirectory+ "\\EUMInjection.dll");

        reference = Convert.ToBoolean(appConfig.AppSettings.Settings["Reference"].Value);
        uri = Convert.ToString(appConfig.AppSettings.Settings["AdrumURI"].Value);

        EventLog.WriteEntry(sSource, "Reference:" + reference.ToString());
        EventLog.WriteEntry(sSource, "URI:" + uri);

        application.PostRequestHandlerExecute += (new EventHandler(Application_PostRequest));
    }
    
    private void Application_PostRequest(Object source, EventArgs e)
    {
        EventLog.WriteEntry(sSource, "HTTP Post Request", EventLogEntryType.Information);

        HttpApplication currentApplication = (HttpApplication)source;

        if (currentApplication.Response.ContentType.Contains("text/html"))
        {

            string insertionPointString = "</head>";

            // Substitute the Response.Filter with custom stream implementation
            currentApplication.Response.Filter = new PostRenderModifier(currentApplication.Response.Filter, insertionPointString);
            ((PostRenderModifier)currentApplication.Response.Filter).StringToInsert = (reference == true) ? Reference() : InLine();
        }
    }

    private string Reference()
    {
        return String.Format("\n<script>window['adrum-start-time'] = new Date().getTime();</script><script src=\"{0}\"></script>\n", uri);
    }

    private string InLine()
    {
        string javascript;
        using (WebClient client = new WebClient())
        {
            javascript = client.DownloadString(uri);
        }
        return String.Format("\n<script>window['adrum-start-time'] = new Date().getTime();</script><script type=\"text/javascript\">{0}</script>\n", javascript.ToString());
    }

    public static string AssemblyDirectory
    {
        get
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }

    public void Dispose() { }
}

