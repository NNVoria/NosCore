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
using ChickenAPI.Packets.ClientPackets.Bazaar;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CSkillPacketHandlerTest
    {
        private CSkillPacketHandler _cskillPacketHandler;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            TestHelpers.Reset();
            Broadcaster.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _session.Character.StaticBonusList = new List<StaticBonusDto>();
            _cskillPacketHandler = new CSkillPacketHandler();
        }

        [TestMethod]
        public void OpenWhenInShop()
        {
            _session.Character.InExchangeOrTrade = true;
            _cskillPacketHandler.Execute(new CSkillPacket(), _session);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }


        [TestMethod]
        public void OpenWhenNoMedal()
        {
            _cskillPacketHandler.Execute(new CSkillPacket(), _session);
            var lastpacket = (InfoPacket) _session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.IsTrue(lastpacket.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.NO_BAZAAR_MEDAL, _session.Account.Language));
        }

        [TestMethod]
        public void Open()
        {
            _session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                StaticBonusType = StaticBonusType.BazaarMedalGold
            });
            _cskillPacketHandler.Execute(new CSkillPacket(), _session);
            var lastpacket = (MsgPacket) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(lastpacket.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.INFO_BAZAAR, _session.Account.Language));
        }
    }
}