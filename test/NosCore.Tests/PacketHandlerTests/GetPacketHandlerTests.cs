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

using System.Linq;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Chats;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.PacketHandlers.Inventory;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class GetPacketHandlerTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private GetPacketHandler _getPacketHandler;
        private IItemProvider _item;
        private ClientSession _session;

        [TestCleanup]
        public void Cleanup()
        {
            SystemTime.Freeze(SystemTime.Now());
        }

        [TestInitialize]
        public void Setup()
        {
            SystemTime.Freeze();
            TestHelpers.Reset();
            _item = TestHelpers.Instance.GenerateItemProvider();
            _session = TestHelpers.Instance.GenerateSession();
            _getPacketHandler = new GetPacketHandler(_logger);
        }


        [TestMethod]
        public void Test_Get()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _session.Character.MapInstance.MapItems.TryAdd(100001,
                TestHelpers.Instance.MapItemProvider.Create(_session.Character.MapInstance, _item.Create(1012, 1), 1,
                    1));

            _getPacketHandler.Execute(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session);
            Assert.IsTrue(_session.Character.InventoryService.Count > 0);
        }

        [TestMethod]
        public void Test_GetInStack()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;

            _session.Character.MapInstance.MapItems.TryAdd(100001,
                TestHelpers.Instance.MapItemProvider.Create(_session.Character.MapInstance, _item.Create(1012, 1), 1,
                    1));
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1012, 1), 0));
            _getPacketHandler.Execute(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session);
            Assert.IsTrue(_session.Character.InventoryService.First().Value.ItemInstance.Amount == 2);
        }

        [TestMethod]
        public void Test_GetFullInventory()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _session.Character.MapInstance.MapItems.TryAdd(100001,
                TestHelpers.Instance.MapItemProvider.Create(_session.Character.MapInstance, _item.Create(1, 1), 1, 1));
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1), 0));
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1), 0));
            _getPacketHandler.Execute(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session);
            var packet = (MsgPacket) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue((packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
                _session.Account.Language)) && (packet.Type == 0));
            Assert.IsTrue(_session.Character.InventoryService.Count == 2);
        }

        [TestMethod]
        public void Test_Get_KeepRarity()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _session.Character.MapInstance.MapItems.TryAdd(100001,
                TestHelpers.Instance.MapItemProvider.Create(_session.Character.MapInstance, _item.Create(1, 1, 6), 1,
                    1));

            _getPacketHandler.Execute(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session);
            Assert.IsTrue(_session.Character.InventoryService.First().Value.ItemInstance.Rare == 6);
        }

        [TestMethod]
        public void Test_Get_NotYourObject()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            var mapItem =
                TestHelpers.Instance.MapItemProvider.Create(_session.Character.MapInstance, _item.Create(1012, 1), 1,
                    1);
            mapItem.VisualId = 1012;
            mapItem.OwnerId = 2;
            mapItem.DroppedAt = SystemTime.Now();
            _session.Character.MapInstance.MapItems.TryAdd(100001, mapItem);

            _getPacketHandler.Execute(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session);
            var packet = (SayPacket) _session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsTrue((packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.NOT_YOUR_ITEM,
                _session.Account.Language)) && (packet.Type == SayColorType.Yellow));
            Assert.IsTrue(_session.Character.InventoryService.Count == 0);
        }

        [TestMethod]
        public void Test_Get_NotYourObjectAfterDelay()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;

            var mapItem =
                TestHelpers.Instance.MapItemProvider.Create(_session.Character.MapInstance, _item.Create(1012, 1), 1,
                    1);
            mapItem.VisualId = 1012;
            mapItem.OwnerId = 2;
            mapItem.DroppedAt = SystemTime.Now().AddSeconds(-30);
            _session.Character.MapInstance.MapItems.TryAdd(100001, mapItem);

            _getPacketHandler.Execute(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session);
            Assert.IsTrue(_session.Character.InventoryService.Count > 0);
        }

        [TestMethod]
        public void Test_GetAway()
        {
            _session.Character.PositionX = 7;
            _session.Character.PositionY = 7;

            _session.Character.MapInstance.MapItems.TryAdd(100001,
                TestHelpers.Instance.MapItemProvider.Create(_session.Character.MapInstance, _item.Create(1012, 1), 1,
                    1));
            _getPacketHandler.Execute(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session);
            Assert.IsTrue(_session.Character.InventoryService.Count == 0);
        }
    }
}