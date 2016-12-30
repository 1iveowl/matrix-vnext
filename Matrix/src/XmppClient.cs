﻿using System;
using System.Net.Security;
using System.Threading.Tasks;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;

using Matrix.Sasl;
using Matrix.Xmpp;
using Matrix.Xmpp.Client;
using Matrix.Xmpp.Sasl;
using Matrix.Xmpp.Stream;
using Matrix.Xmpp.Tls;

namespace Matrix
{
    public class XmppClient : XmppConnection
    {
        private int priority;
        #region << Properties >>

        public string Username { get; set; }

        public string Password { get; set; }

        public string Resource { get; set; } = "MatriX";

        public bool UseStartTls { get; set; } = true;

        public Show Show { get; set; } = Show.None;

        public string Status { get; set; } = "online";
        public int Priority
        {
            get { return priority; }
            set
            {
                if (value < -127 || value > 127)
                    throw new ArgumentOutOfRangeException("The value must be an integer between -128 and +127.");

                priority = value;
            }
        }

        public IAuthenticate SalsHandler { get; set; } = new DefaultSaslHandler();
        #endregion
        
        public async Task<IChannel> ConnectAsync()
        {
            var iChannel = await Bootstrap.ConnectAsync(XmppDomain, Port);
            //await SendStreamHeader();
            var feat = await SendStreamHeaderAsync();
            await HandleStreamFeaturesAsync(feat);
            return iChannel;
        }

        //        public async Task<bool> CloseAsync()
        //        {
        //            return await CloseAsync(2000);
        //        }


        private async Task HandleStreamFeaturesAsync(StreamFeatures features)
        {
            if (SessionState < SessionState.Securing && features.SupportsStartTls && UseStartTls)
            {
                await HandleStreamFeaturesAsync(await DoStartTlsAsync());
            }
            else if (SessionState < SessionState.Authenticating)
            {
                var authRet = await DoAuthenicateAsync(features.Mechanisms);
                await HandleStreamFeaturesAsync(authRet);
            }
            else if (SessionState < SessionState.Binding)
                await DoBindAsync();
        }

        private async Task<StreamFeatures> DoStartTlsAsync()
        {
            SessionState = SessionState.Securing;
            var tlsHandler =
                new TlsHandler(stream
                => new SslStream(stream,
                true,
                (sender, certificate, chain, errors) => CertificateValidator.RemoteCertificateValidationCallback(sender, certificate, chain, errors)),
                new ClientTlsSettings(XmppDomain));

            var res = await WaitForStanzaHandler.SendAsync<Proceed>(new StartTls());
            Pipeline.AddFirst(tlsHandler);
            var streamFeatures = await ResetStreamAsync();
            SessionState = SessionState.Secure;

            return streamFeatures;
        }

        private async Task<StreamFeatures> DoAuthenicateAsync(Mechanisms mechanisms)
        {
            SessionState = SessionState.Authenticating;
            var res = await SalsHandler.AuthenticateAsync(mechanisms, this);

            if (res is Success)
            {
                SessionState = SessionState.Authenticated;
                return await ResetStreamAsync();
            }

            return null;
        }

        private async Task<Iq> DoBindAsync()
        {
            SessionState = SessionState.Binding;
            var bIq = new BindIq { Type = IqType.Set, Bind = { Resource = Resource } };
            var resIq = await IqHandler.SendIqAsync(bIq);
            SessionState = SessionState.Binded;
            return resIq as Iq;
        }

        public async Task<Iq> RequestRosterAsync(string version = null)
        {
            var riq = new RosterIq()
            {
                Type = IqType.Get
            };

            if (version != null)
                riq.Roster.Version = version;
                        
            var resIq = await IqHandler.SendIqAsync(riq);            
            return resIq as Iq;
        }

        #region << SendPresence >>
        public async Task SendPresenceAsync(Presence pres)
        {
            await SendAsync(pres);
        }

        public async Task SendPresenceAsync(Show show)
        {
            Show = show;
            await SendPresenceAsync();
        }

        public async Task SendPresenceAsync(Show show, string status)
        {
            Show = show;
            Status = status;

            await SendPresenceAsync();
        }

        public async Task SendPresenceAsync(Show show, string status, int priority)
        {
            Show = show;
            Status = status;
            Priority = priority;

            await SendPresenceAsync();
        }

        public async Task SendPresenceAsync()
        {
            await SendAsync(new Presence
            {
                Show = Show,
                Status = Status,
                Priority = Priority
            });
        }
        #endregion
    }
}