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
using ChickenAPI.Packets.ClientPackets.Groups;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;

namespace NosCore.PacketHandlers.Group
{
    public class PleavePacketHandler : PacketHandler<PleavePacket>, IWorldPacketHandler
    {
        public override void Execute(PleavePacket bIPacket, ClientSession clientSession)
        {
            var group = clientSession.Character.Group;

            if (group.Count == 1)
            {
                return;
            }

            if (group.Count > 2)
            {
                var isLeader = group.IsGroupLeader(clientSession.Character.CharacterId);
                clientSession.Character.LeaveGroup();

                if (isLeader)
                {
                    var targetsession = Broadcaster.Instance.GetCharacter(s =>
                        s.VisualId == group.Values.First().Item2.VisualId);

                    if (targetsession == null)
                    {
                        return;
                    }

                    targetsession.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.NEW_LEADER,
                            clientSession.Account.Language)
                    });
                }

                if (group.Type != GroupType.Group)
                {
                    return;
                }

                foreach (var member in group.Values.Where(s => s.Item2 is ICharacterEntity))
                {
                    var character = member.Item2 as ICharacterEntity;
                    character.SendPacket(character.Group.GeneratePinit());
                    character.SendPacket(new MsgPacket
                    {
                        Message = string.Format(
                            Language.Instance.GetMessageFromKey(LanguageKey.LEAVE_GROUP,
                                clientSession.Account.Language),
                            clientSession.Character.Name)
                    });
                }

                clientSession.SendPacket(clientSession.Character.Group.GeneratePinit());
                clientSession.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_LEFT,
                        clientSession.Account.Language)
                });
                clientSession.Character.MapInstance.SendPacket(
                    clientSession.Character.Group.GeneratePidx(clientSession.Character));
            }
            else
            {
                var memberList = new List<INamedEntity>();
                memberList.AddRange(group.Values.Select(s => s.Item2));

                foreach (var member in memberList.Where(s => s is ICharacterEntity))
                {
                    var targetsession =
                        Broadcaster.Instance.GetCharacter(s =>
                            s.VisualId == member.VisualId);

                    if (targetsession == null)
                    {
                        continue;
                    }

                    targetsession.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_CLOSED,
                            targetsession.AccountLanguage),
                        Type = MessageType.White
                    });

                    targetsession.LeaveGroup();
                    targetsession.SendPacket(targetsession.Group.GeneratePinit());
                    Broadcaster.Instance.SendPacket(targetsession.Group.GeneratePidx(targetsession));
                }

                GroupAccess.Instance.Groups.TryRemove(group.GroupId, out _);
            }
        }
    }
}