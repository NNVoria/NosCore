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

using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.ItemHandlerTests
{
    [TestClass]
    public class SpRechargerEventHandlerTests : UseItemEventHandlerTestsBase
    {
        private ItemProvider _itemProvider;

        [TestInitialize]
        public void Setup()
        {
            _session = TestHelpers.Instance.GenerateSession();
            _handler = new SpRechargerEventHandler(new WorldConfiguration {MaxAdditionalSpPoints = 1});
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, ItemType = ItemType.Special, EffectValue = 1},
            };
            _itemProvider = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());
        }
        [TestMethod]
        public void Test_SpRecharger_When_Max()
        {
            _session.Character.SpAdditionPoint = 1;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (MsgPacket)_session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.AreEqual(Language.Instance.GetMessageFromKey(LanguageKey.SP_ADDPOINTS_FULL, _session.Character.Account.Language), lastpacket.Message);
            Assert.AreEqual(1, _session.Character.SpAdditionPoint);
            Assert.AreEqual(1, _session.Character.InventoryService.Count);
        }

        [TestMethod]
        public void Test_SpRecharger()
        {
            _session.Character.SpAdditionPoint = 0;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (MsgPacket)_session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.AreEqual(1, _session.Character.SpAdditionPoint);
            Assert.AreEqual(0, _session.Character.InventoryService.Count);
        }
    }
}