﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.PacketHttpClient
{
   public class PacketHttpClient : NoscoreHttpClient, IPacketHttpClient
   {
       public PacketHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient)
           : base(httpClientFactory, channel, channelHttpClient)
       {

       }

        public void BroadcastPacket(PostedPacket packet, int channelId)
        {
            throw new NotImplementedException();
            //    var channel = Get<List<ChannelInfo>>(WebApiRoute.Channel, channelId).FirstOrDefault();
            //    if (channel != null)
            //    {
            //        Post<PostedPacket>(WebApiRoute.Packet, packet, channel.WebApi);
            //    }
        }

        public void BroadcastPacket(PostedPacket packet)
        {
            throw new NotImplementedException();
            //foreach (var channel in Get<List<ChannelInfo>>(WebApiRoute.Channel)
            //    ?.Where(c => c.Type == ServerType.WorldServer) ?? new List<ChannelInfo>())
            //{
            //    Post<PostedPacket>(WebApiRoute.Packet, packet, channel.WebApi);
            //}
        }

        public void BroadcastPackets(List<PostedPacket> packets)
        {
            foreach (var packet in packets)
            {
                BroadcastPacket(packet);
            }
        }

        public void BroadcastPackets(List<PostedPacket> packets, int channelId)
        {
            foreach (var packet in packets)
            {
                BroadcastPacket(packet, channelId);
            }
        }
    }
}
