﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Matrix.Xml;
using Matrix.Xmpp.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Xmpp.Muc.User
{
    [TestClass]
    public class GTalkTest
    {
        string xml1 = @"<presence xmlns='jabber:client' from='private-chat-32fe72ae-497e-41c8-b9d6-4d6dc9b45e6f@groupchat.google.com/cd3bd3ffd02e7d7c' to='XXX/agsXMPP24F657E5'>
	<user:x xmlns:user='http://jabber.org/protocol/muc#user'>
		<user:item role='participant' affiliation='member' jid='XXX@gmail.com/gmail.59477926' />
	</user:x>
	<status>Working. DND!</status>
	<x xmlns='http://jabber.org/protocol/muc#user' />
	<nick:nick xmlns:nick='http://jabber.org/protocol/nick'>Alex</nick:nick>
    <show>dnd</show>
	<x xmlns='vcard-temp:x:update'>
		<photo>a8bb03071cb085b5705d49b43f32f7b7cb9c9a6c</photo>
	</x>
</presence>";


        [TestMethod]
        public void Test1()
        {
            var pres = XmppXElement.LoadXml(xml1) as Presence;

            Assert.AreEqual(pres.Show == Matrix.Xmpp.Show.DoNotDisturb, true);
            Assert.AreEqual(pres.Nick.Value, "Alex");
            
            var mucUser = pres.MucUser;
            var item = mucUser.Item;
            Assert.AreEqual(item.Role == Matrix.Xmpp.Muc.Role.Participant, true);
            Assert.AreEqual(item.Affiliation == Matrix.Xmpp.Muc.Affiliation.Member, true);
            Assert.AreEqual(item.Jid == "XXX@gmail.com/gmail.59477926", true);
            
        }
    }
}
