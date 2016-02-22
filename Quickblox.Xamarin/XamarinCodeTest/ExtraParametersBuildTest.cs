﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.ObjectModel;
using Quickblox.Sdk.Modules.ChatXmppModule.ExtraParameters;

namespace XamarinCodeTest
{
    [TestClass]
    public class ExtraParametersBuildTest
    {
        [TestMethod]
        public void CallExtraParameterBuildTest()
        {
            var sessionId = "123123";
            var sdp = "234234234sfdgsfg345345fgsdgsdgsdf g34534534fsfgsdg345345";
            var platform = "android";
            var caller = 12324;
            var receiver = 234234;
             var callParameter = new CallExtraParameter(sessionId, sdp, platform, caller, receiver);
            var result = callParameter.Build();

            var message = new Sharp.Xmpp.Im.Message("234234-42", "sdfsdf", result);
            var messageText = message.ToString();
        }

        [TestMethod]
        public void IceCandidatesExtraParameterBuildTest()
        {
            var sessionId = "123123";
            var sdp = "234234234sfdgsfg345345fgsdgsdgsdf g34534534fsfgsdg345345";
            var platform = "android";
            var caller = "12324";
            var receiver = "234234";
            var iceCandidates = new IceCandidatesExtraParameter(sessionId,
                new Collection<IceCandidate>() {
                new IceCandidate() { Candidate = "candidate1", SdpMid = "sdfgosdfgsdfg1", SdpMLineIndex="123" },
                new IceCandidate() { Candidate = "candidate2", SdpMid = "sdfgosdfgsdfg2", SdpMLineIndex="123" },
                new IceCandidate() { Candidate = "candidate3", SdpMid = "sdfgosdfgsdfg3", SdpMLineIndex="123" }
                });
            var result = iceCandidates.Build();

            var message = new Sharp.Xmpp.Im.Message("234234-42", "sdfsdf", result);
            var messageText = message.ToString();
        }

        [TestMethod]
        public void GeneralMessageExtraParameterTest()
        {
            ChatMessageExtraParameter a = new ChatMessageExtraParameter("234234234sfdgsfg345345f", true, new Quickblox.Sdk.Modules.ChatXmppModule.Models.AttachmentExtraParamValue()
            {
                Id = "00000000000111111111"
            });
         
            var resultString = a.Build();
        }
    }
}