﻿//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChickenAPI.Packets.Interfaces;
using DotNetty.Transport.Channels.Groups;

namespace NosCore.GameObject.Networking.Group
{
    public static class IBroadcastableExtension
    {
        private const short maxPacketsBuffer = 250;

        public static void SendPacket(this IBroadcastable channelGroup, IPacket packet)
        {
            channelGroup.SendPackets(new[] { packet });
        }

        public static void SendPacket(this IBroadcastable channelGroup, IPacket packet, IChannelMatcher matcher)
        {
            channelGroup.SendPackets(new[] { packet }, matcher);
        }


        public static void SendPackets(this IBroadcastable channelGroup, IEnumerable<IPacket> packets,
            IChannelMatcher matcher)
        {
            var packetDefinitions = (packets as IPacket[] ?? packets.ToArray()).Where(c => c != null);
            if (packetDefinitions.Any())
            {
                Parallel.ForEach(packets, packet => channelGroup.LastPackets.Enqueue(packet));
                Parallel.For(0, channelGroup.LastPackets.Count - maxPacketsBuffer, (_, __) => channelGroup.LastPackets.TryDequeue(out var ___));
                channelGroup.Sessions?.WriteAndFlushAsync(packetDefinitions);
                if (matcher == null)
                {
                    channelGroup.Sessions?.WriteAndFlushAsync(packetDefinitions);
                }
                else
                {
                    channelGroup.Sessions?.WriteAndFlushAsync(packetDefinitions, matcher);
                }
            }
        }


        public static void SendPackets(this IBroadcastable channelGroup, IEnumerable<IPacket> packets)
        {
            channelGroup.SendPackets(packets, null);
        }
    }
}